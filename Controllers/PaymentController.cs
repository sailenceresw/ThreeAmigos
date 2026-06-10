using ecommerce.Models;
using ecommerce.Services.Payments;
using ecommerce.Settings;
using ecommerce.ViewModels.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace ecommerce.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _payments;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PaymentOptions _options;
        private readonly Context _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService payments,
            UserManager<ApplicationUser> userManager,
            IOptions<PaymentOptions> options,
            Context context,
            ILogger<PaymentController> logger)
        {
            _payments = payments;
            _userManager = userManager;
            _options = options.Value;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Select(int orderId, CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Order
                .Include(o => o.OrderItems)!.ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null || order.ApplicationUserId != user.Id) return NotFound();

            decimal amount = (order.OrderItems ?? new()).Sum(i => (i.Product?.Price ?? 0m) * i.Quantity);

            var vm = new SelectPaymentMethodViewModel
            {
                OrderId = order.Id,
                AmountUsd = amount,
                InAppBalanceEnabled = true,
                UserBalance = user.Balance,
                StripeEnabled = _options.Stripe.Enabled && !string.IsNullOrWhiteSpace(_options.Stripe.PublishableKey),
                CryptoEnabled = _options.Crypto.Enabled && _options.Crypto.Wallets.Count > 0,
                CryptoWallets = _options.Crypto.Wallets,
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Begin(int orderId, PaymentMethod method, string? cryptoCurrency, CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IDictionary<string, string?>? args = method == PaymentMethod.CryptoManual
                ? new Dictionary<string, string?> { ["currency"] = cryptoCurrency }
                : null;

            PaymentInitResult result;
            try
            {
                result = await _payments.BeginAsync(orderId, method, user, args, ct);
            }
            catch (InvalidOperationException ex)
            {
                TempData["PaymentError"] = ex.Message;
                return RedirectToAction(nameof(Select), new { orderId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return method switch
            {
                PaymentMethod.InAppBalance => RedirectToAction(nameof(Success), new { paymentId = result.Payment.Id }),
                PaymentMethod.StripeCard => View("Stripe", new StripeCheckoutViewModel
                {
                    PaymentId = result.Payment.Id,
                    OrderId = orderId,
                    AmountUsd = result.Payment.AmountUsd,
                    ClientSecret = result.StripeClientSecret ?? "",
                    PublishableKey = result.StripePublishableKey ?? "",
                }),
                PaymentMethod.CryptoManual => View("Crypto", new CryptoCheckoutViewModel
                {
                    PaymentId = result.Payment.Id,
                    OrderId = orderId,
                    AmountUsd = result.Payment.AmountUsd,
                    Currency = result.CryptoCurrency ?? "",
                    Network = result.CryptoNetwork ?? "",
                    WalletAddress = result.CryptoWalletAddress ?? "",
                }),
                _ => BadRequest(),
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitCryptoTx(CryptoTxSubmissionViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var payment = await _payments.GetByIdAsync(vm.PaymentId, ct);
            if (payment == null) return NotFound();

            var order = await _context.Order.FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct);
            if (order == null || order.ApplicationUserId != user.Id) return Forbid();

            await _payments.AttachCryptoTxAsync(vm.PaymentId, vm.TxHash, ct);
            return RedirectToAction(nameof(CryptoPending), new { paymentId = vm.PaymentId });
        }

        [HttpGet]
        public async Task<IActionResult> CryptoPending(int paymentId, CancellationToken ct)
        {
            var payment = await _payments.GetByIdAsync(paymentId, ct);
            if (payment == null) return NotFound();
            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> Success(int paymentId, CancellationToken ct)
        {
            var payment = await _payments.GetByIdAsync(paymentId, ct);
            if (payment == null) return NotFound();
            return View(payment);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook(CancellationToken ct)
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
            var signature = Request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _options.Stripe.WebhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature validation failed.");
                return BadRequest();
            }

            int? paymentId = null;
            if (stripeEvent.Data.Object is PaymentIntent intent
                && intent.Metadata != null
                && intent.Metadata.TryGetValue("paymentId", out var pidStr)
                && int.TryParse(pidStr, out var pid))
            {
                paymentId = pid;
            }

            var firstTime = await _payments.RecordExternalEventAsync(
                source: "stripe",
                externalEventId: stripeEvent.Id,
                eventType: stripeEvent.Type,
                rawPayload: json,
                paymentId: paymentId,
                ct: ct);
            if (!firstTime) return Ok();

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    if (paymentId is int succeededId)
                    {
                        await _payments.MarkSucceededAsync(succeededId, confirmedByUserId: null, ct);
                    }
                    break;
                case "payment_intent.payment_failed":
                case "payment_intent.canceled":
                    if (paymentId is int failedId)
                    {
                        await _payments.MarkFailedAsync(failedId, stripeEvent.Type, ct);
                    }
                    break;
            }

            return Ok();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCryptoReview(CancellationToken ct)
        {
            var pending = await _payments.ListAwaitingConfirmationAsync(PaymentMethod.CryptoManual, ct);
            return View(new AdminCryptoReviewViewModel { Pending = pending });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminConfirmCrypto(int paymentId, CancellationToken ct)
        {
            var admin = await _userManager.GetUserAsync(User);
            await _payments.MarkSucceededAsync(paymentId, admin?.Id, ct);
            return RedirectToAction(nameof(AdminCryptoReview));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminRejectCrypto(int paymentId, string reason, CancellationToken ct)
        {
            await _payments.MarkFailedAsync(paymentId, reason ?? "Rejected by admin", ct);
            return RedirectToAction(nameof(AdminCryptoReview));
        }
    }
}

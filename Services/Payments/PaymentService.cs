using System.Text;
using ecommerce.Models;
using ecommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Services.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly Context _context;
        private readonly IEnumerable<IPaymentProvider> _providers;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IProductService _productService;

        public PaymentService(
            Context context,
            IEnumerable<IPaymentProvider> providers,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IProductService productService)
        {
            _context = context;
            _providers = providers;
            _userManager = userManager;
            _configuration = configuration;
            _productService = productService;
        }

        public async Task<PaymentInitResult> BeginAsync(int orderId, PaymentMethod method, ApplicationUser user, IDictionary<string, string?>? providerArgs, CancellationToken ct)
        {
            var order = await _context.Order
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                ?? throw new InvalidOperationException("Order not found.");

            if (order.ApplicationUserId != user.Id)
            {
                throw new UnauthorizedAccessException("Order does not belong to the current user.");
            }

            if (order.Status != "AWAITING_PAYMENT")
            {
                throw new InvalidOperationException($"Order is not awaiting payment (status={order.Status}).");
            }

            decimal amount = 0m;
            foreach (var item in order.OrderItems ?? new List<OrderItem>())
            {
                var product = _productService.Get(item.ProductId);
                amount += product.Price * item.Quantity;
            }

            var provider = _providers.FirstOrDefault(p => p.Method == method)
                ?? throw new InvalidOperationException($"Payment method {method} is not enabled.");

            var result = await provider.InitiateAsync(order, amount, user, providerArgs, ct);

            order.PaymentId = result.Payment.Id;
            order.PaymentMethod = method;
            _context.Order.Update(order);
            await _context.SaveChangesAsync(ct);

            if (result.FinalizedImmediately)
            {
                await MarkSucceededAsync(result.Payment.Id, user.Id, ct);
            }

            return result;
        }

        public Task<Payment?> GetByIdAsync(int paymentId, CancellationToken ct)
            => _context.Payment.FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        public Task<Payment?> GetByOrderAsync(int orderId, CancellationToken ct)
            => _context.Payment.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        public Task<Payment?> GetByProviderReferenceAsync(string providerRef, CancellationToken ct)
            => _context.Payment.FirstOrDefaultAsync(p => p.ProviderReference == providerRef, ct);

        public Task<List<Payment>> ListAwaitingConfirmationAsync(PaymentMethod method, CancellationToken ct)
            => _context.Payment
                .Where(p => p.Method == method && p.Status == PaymentStatus.AwaitingConfirmation)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

        public Task<List<Payment>> ListRecentSucceededAsync(int take, CancellationToken ct)
            => _context.Payment
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .OrderByDescending(p => p.ConfirmedAt ?? p.CreatedAt)
                .Take(take)
                .ToListAsync(ct);

        public async Task AttachCryptoTxAsync(int paymentId, string txHash, CancellationToken ct)
        {
            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new InvalidOperationException("Payment not found.");

            if (payment.Method != PaymentMethod.CryptoManual)
            {
                throw new InvalidOperationException("Only manual crypto payments accept a tx hash.");
            }
            if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.AwaitingConfirmation)
            {
                throw new InvalidOperationException($"Payment is not in a state to attach a tx (status={payment.Status}).");
            }

            payment.CryptoTxHash = txHash;
            payment.Status = PaymentStatus.AwaitingConfirmation;
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payment.Update(payment);
            await _context.SaveChangesAsync(ct);
        }

        public async Task MarkSucceededAsync(int paymentId, string? confirmedByUserId, CancellationToken ct)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new InvalidOperationException("Payment not found.");

            if (payment.Status == PaymentStatus.Succeeded)
            {
                await tx.CommitAsync(ct);
                return;
            }
            if (payment.Status == PaymentStatus.Failed || payment.Status == PaymentStatus.Canceled || payment.Status == PaymentStatus.Refunded)
            {
                throw new InvalidOperationException($"Cannot mark a {payment.Status} payment as succeeded.");
            }

            var order = await _context.Order
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct)
                ?? throw new InvalidOperationException("Order not found.");

            await FinalizeOrderAsync(order, payment, ct);

            payment.Status = PaymentStatus.Succeeded;
            payment.ConfirmedAt = DateTime.UtcNow;
            payment.ConfirmedByUserId = confirmedByUserId;
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payment.Update(payment);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await SendOrderConfirmationEmailAsync(order);
        }

        public async Task MarkFailedAsync(int paymentId, string reason, CancellationToken ct)
        {
            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new InvalidOperationException("Payment not found.");

            if (payment.Status == PaymentStatus.Succeeded)
            {
                throw new InvalidOperationException("Cannot fail a succeeded payment; refund instead.");
            }

            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = reason;
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payment.Update(payment);

            var order = await _context.Order.FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct);
            if (order != null)
            {
                order.Status = "PAYMENT_FAILED";
                _context.Order.Update(order);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task<RefundResult> RefundAsync(int paymentId, string reason, string? adminUserId, CancellationToken ct)
        {
            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new InvalidOperationException("Payment not found.");

            if (payment.Status != PaymentStatus.Succeeded)
            {
                throw new InvalidOperationException($"Only succeeded payments can be refunded (status={payment.Status}).");
            }

            var provider = _providers.FirstOrDefault(p => p.Method == payment.Method)
                ?? throw new InvalidOperationException($"No provider registered for {payment.Method}.");

            var providerResult = await provider.RefundAsync(payment, reason, ct);

            await ApplyRefundDomainEffectsAsync(payment, providerResult.ProviderRefundReference, reason, adminUserId, ct);

            await SendRefundEmailAsync(payment, providerResult.RequiresManualSettlement);

            return providerResult;
        }

        public async Task MarkRefundedFromExternalAsync(int paymentId, string? providerRefundReference, string reason, CancellationToken ct)
        {
            var payment = await _context.Payment.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new InvalidOperationException("Payment not found.");

            if (payment.Status == PaymentStatus.Refunded)
            {
                return;
            }
            if (payment.Status != PaymentStatus.Succeeded)
            {
                throw new InvalidOperationException($"Cannot mark refunded; payment status is {payment.Status}.");
            }

            await ApplyRefundDomainEffectsAsync(payment, providerRefundReference, reason, adminUserId: null, ct);
            await SendRefundEmailAsync(payment, requiresManualSettlement: false);
        }

        private async Task ApplyRefundDomainEffectsAsync(Payment payment, string? providerRefundReference, string reason, string? adminUserId, CancellationToken ct)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            var order = await _context.Order
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct)
                ?? throw new InvalidOperationException("Order not found.");

            foreach (var item in order.OrderItems ?? new List<OrderItem>())
            {
                var product = _productService.Get(item.ProductId);
                _context.Stock.Add(new Stock { ProductId = product.Id, Quantity = item.Quantity });
                product.Quantity += item.Quantity;
                _productService.Update(product);
            }

            if (payment.Method == PaymentMethod.InAppBalance)
            {
                var user = await _userManager.FindByIdAsync(order.ApplicationUserId);
                if (user != null)
                {
                    user.Balance += payment.AmountUsd;
                    await _userManager.UpdateAsync(user);

                    _context.Statement.Add(new Statement
                    {
                        UserId = order.ApplicationUserId,
                        Amount = payment.AmountUsd,
                    });
                }
            }

            order.Status = "REFUNDED";
            _context.Order.Update(order);

            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundedByUserId = adminUserId;
            payment.RefundReason = reason;
            payment.RefundProviderReference = providerRefundReference;
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payment.Update(payment);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        private async Task SendRefundEmailAsync(Payment payment, bool requiresManualSettlement)
        {
            try
            {
                var order = await _context.Order.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                if (order == null) return;
                var user = await _userManager.FindByIdAsync(order.ApplicationUserId);
                if (user?.Email == null) return;

                var sb = new StringBuilder();
                sb.AppendLine("<h1>Refund processed</h1>");
                sb.AppendLine($"<p>Order #{order.Id} has been refunded for {payment.AmountUsd:C}.</p>");
                if (requiresManualSettlement)
                {
                    sb.AppendLine("<p>This crypto refund will be settled manually; expect the funds to arrive on-chain shortly.</p>");
                }
                if (!string.IsNullOrWhiteSpace(payment.RefundReason))
                {
                    sb.AppendLine($"<p>Reason: {payment.RefundReason}</p>");
                }

                var email = new EmailService(_configuration);
                await email.SendEmailAsync(user.Email, "Refund processed", sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send refund email for payment {payment.Id}: {ex.Message}");
            }
        }

        public async Task<bool> RecordExternalEventAsync(string source, string externalEventId, string? eventType, string? rawPayload, int? paymentId, CancellationToken ct)
        {
            var exists = await _context.PaymentEvent
                .AnyAsync(e => e.Source == source && e.ExternalEventId == externalEventId, ct);
            if (exists)
            {
                return false;
            }

            _context.PaymentEvent.Add(new PaymentEvent
            {
                Source = source,
                ExternalEventId = externalEventId,
                EventType = eventType,
                RawPayload = rawPayload,
                PaymentId = paymentId,
            });
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private async Task FinalizeOrderAsync(Order order, Payment payment, CancellationToken ct)
        {
            foreach (var item in order.OrderItems ?? new List<OrderItem>())
            {
                var product = _productService.Get(item.ProductId);
                if (product.Quantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}.");
                }
            }

            foreach (var item in order.OrderItems ?? new List<OrderItem>())
            {
                var product = _productService.Get(item.ProductId);

                _context.Stock.Add(new Stock { ProductId = product.Id, Quantity = -item.Quantity });

                product.Quantity -= item.Quantity;
                _productService.Update(product);
            }

            order.Status = "PENDING";
            _context.Order.Update(order);

            if (payment.Method == PaymentMethod.InAppBalance)
            {
                var user = await _userManager.FindByIdAsync(order.ApplicationUserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found while debiting in-app balance.");
                }
                if (user.Balance < order.TotalValue)
                {
                    throw new InvalidOperationException("In-app balance changed since checkout; insufficient funds.");
                }
                user.Balance -= order.TotalValue;
                await _userManager.UpdateAsync(user);

                _context.Statement.Add(new Statement
                {
                    UserId = order.ApplicationUserId,
                    Amount = -order.TotalValue,
                });
            }
        }

        private async Task SendOrderConfirmationEmailAsync(Order order)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(order.ApplicationUserId);
                if (user?.Email == null) return;

                var sb = new StringBuilder();
                sb.AppendLine("<h1>Order Confirmation</h1>");
                sb.AppendLine($"<p>Order #{order.Id} - {order.OrderDate:yyyy-MM-dd}</p>");
                sb.AppendLine($"<p>Total: {order.TotalValue:C}</p>");
                sb.AppendLine($"<p>Payment method: {order.PaymentMethod}</p>");

                var email = new EmailService(_configuration);
                await email.SendEmailAsync(user.Email, "Order Confirmation", sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send confirmation email for order {order.Id}: {ex.Message}");
            }
        }
    }
}

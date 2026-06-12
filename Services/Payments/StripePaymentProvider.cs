using ecommerce.Models;
using ecommerce.Settings;
using Microsoft.Extensions.Options;
using Stripe;

namespace ecommerce.Services.Payments
{
    public class StripePaymentProvider : IPaymentProvider
    {
        private readonly Context _context;
        private readonly StripeOptions _options;

        public StripePaymentProvider(Context context, IOptions<PaymentOptions> options)
        {
            _context = context;
            _options = options.Value.Stripe;
        }

        public PaymentMethod Method => PaymentMethod.StripeCard;

        public async Task<PaymentInitResult> InitiateAsync(Order order, decimal amountUsd, ApplicationUser user, IDictionary<string, string?>? providerArgs, CancellationToken ct)
        {
            if (!_options.Enabled)
            {
                throw new InvalidOperationException("Stripe card payments are not enabled.");
            }
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException("Stripe SecretKey is not configured. Set it via user-secrets or env var.");
            }

            StripeConfiguration.ApiKey = _options.SecretKey;

            var amountMinor = (long)Math.Round(amountUsd * 100m, MidpointRounding.AwayFromZero);

            var payment = new Payment
            {
                OrderId = order.Id,
                Method = PaymentMethod.StripeCard,
                Status = PaymentStatus.Pending,
                AmountUsd = amountUsd,
            };
            _context.Payment.Add(payment);
            await _context.SaveChangesAsync(ct);

            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = amountMinor,
                Currency = _options.Currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string>
                {
                    ["paymentId"] = payment.Id.ToString(),
                    ["orderId"] = order.Id.ToString(),
                    ["userId"] = user.Id,
                },
            };
            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(createOptions, cancellationToken: ct);

            payment.ProviderReference = intent.Id;
            _context.Payment.Update(payment);
            await _context.SaveChangesAsync(ct);

            return new PaymentInitResult
            {
                Payment = payment,
                StripeClientSecret = intent.ClientSecret,
                StripePublishableKey = _options.PublishableKey,
                FinalizedImmediately = false,
            };
        }

        public async Task<RefundResult> RefundAsync(Payment payment, string reason, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(payment.ProviderReference))
            {
                throw new InvalidOperationException("Payment has no Stripe PaymentIntent reference; cannot refund.");
            }
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException("Stripe SecretKey is not configured.");
            }

            StripeConfiguration.ApiKey = _options.SecretKey;

            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = payment.ProviderReference,
                Metadata = new Dictionary<string, string>
                {
                    ["paymentId"] = payment.Id.ToString(),
                    ["reason"] = reason ?? "",
                },
            };
            var service = new RefundService();
            var refund = await service.CreateAsync(refundOptions, cancellationToken: ct);

            return new RefundResult
            {
                ProviderRefundReference = refund.Id,
            };
        }
    }
}

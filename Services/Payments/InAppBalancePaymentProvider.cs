using ecommerce.Models;

namespace ecommerce.Services.Payments
{
    public class InAppBalancePaymentProvider : IPaymentProvider
    {
        private readonly Context _context;

        public InAppBalancePaymentProvider(Context context)
        {
            _context = context;
        }

        public PaymentMethod Method => PaymentMethod.InAppBalance;

        public async Task<PaymentInitResult> InitiateAsync(Order order, decimal amountUsd, ApplicationUser user, IDictionary<string, string?>? providerArgs, CancellationToken ct)
        {
            if (user.Balance < amountUsd)
            {
                throw new InvalidOperationException("Insufficient in-app balance.");
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                Method = PaymentMethod.InAppBalance,
                Status = PaymentStatus.Pending,
                AmountUsd = amountUsd,
                ProviderReference = $"balance:{user.Id}:{order.Id}:{DateTime.UtcNow:yyyyMMddHHmmss}",
            };
            _context.Payment.Add(payment);
            await _context.SaveChangesAsync(ct);

            return new PaymentInitResult
            {
                Payment = payment,
                FinalizedImmediately = true,
            };
        }
    }
}

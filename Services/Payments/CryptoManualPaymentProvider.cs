using ecommerce.Models;
using ecommerce.Settings;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Payments
{
    public class CryptoManualPaymentProvider : IPaymentProvider
    {
        private readonly Context _context;
        private readonly CryptoOptions _options;

        public CryptoManualPaymentProvider(Context context, IOptions<PaymentOptions> options)
        {
            _context = context;
            _options = options.Value.Crypto;
        }

        public PaymentMethod Method => PaymentMethod.CryptoManual;

        public async Task<PaymentInitResult> InitiateAsync(Order order, decimal amountUsd, ApplicationUser user, IDictionary<string, string?>? providerArgs, CancellationToken ct)
        {
            if (!_options.Enabled || _options.Wallets.Count == 0)
            {
                throw new InvalidOperationException("Crypto payments are not enabled or no wallets configured.");
            }

            providerArgs ??= new Dictionary<string, string?>();
            providerArgs.TryGetValue("currency", out var requested);

            var wallet = _options.Wallets.FirstOrDefault(w =>
                string.Equals(w.Currency, requested, StringComparison.OrdinalIgnoreCase))
                ?? _options.Wallets[0];

            if (string.IsNullOrWhiteSpace(wallet.Address))
            {
                throw new InvalidOperationException($"Configured wallet for {wallet.Currency} has no address.");
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                Method = PaymentMethod.CryptoManual,
                Status = PaymentStatus.Pending,
                AmountUsd = amountUsd,
                CryptoCurrency = wallet.Currency,
                CryptoNetwork = wallet.Network,
                CryptoWalletAddress = wallet.Address,
                ProviderReference = $"crypto:{wallet.Currency}:{order.Id}:{DateTime.UtcNow:yyyyMMddHHmmss}",
            };
            _context.Payment.Add(payment);
            await _context.SaveChangesAsync(ct);

            return new PaymentInitResult
            {
                Payment = payment,
                CryptoCurrency = wallet.Currency,
                CryptoNetwork = wallet.Network,
                CryptoWalletAddress = wallet.Address,
                FinalizedImmediately = false,
            };
        }
    }
}

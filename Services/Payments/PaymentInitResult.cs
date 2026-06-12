using ecommerce.Models;

namespace ecommerce.Services.Payments
{
    public class PaymentInitResult
    {
        public Payment Payment { get; set; } = null!;

        public string? StripeClientSecret { get; set; }
        public string? StripePublishableKey { get; set; }

        public string? CryptoWalletAddress { get; set; }
        public string? CryptoCurrency { get; set; }
        public string? CryptoNetwork { get; set; }
        public decimal? CryptoExpectedAmount { get; set; }

        public bool FinalizedImmediately { get; set; }
    }
}

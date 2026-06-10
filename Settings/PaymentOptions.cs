namespace ecommerce.Settings
{
    public class PaymentOptions
    {
        public const string SectionName = "Payments";

        public StripeOptions Stripe { get; set; } = new();
        public CryptoOptions Crypto { get; set; } = new();
    }

    public class StripeOptions
    {
        public bool Enabled { get; set; } = false;
        public string PublishableKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string WebhookSecret { get; set; } = "";
        public string Currency { get; set; } = "usd";
    }

    public class CryptoOptions
    {
        public bool Enabled { get; set; } = false;
        public List<CryptoWalletOption> Wallets { get; set; } = new();
    }

    public class CryptoWalletOption
    {
        public string Currency { get; set; } = "";
        public string Network { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Label { get; set; }
    }
}

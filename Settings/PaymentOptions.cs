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
        public CryptoVerificationOptions Verification { get; set; } = new();
    }

    public class CryptoWalletOption
    {
        public string Currency { get; set; } = "";
        public string Network { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Label { get; set; }
    }

    public class CryptoVerificationOptions
    {
        public bool AutoVerifyEnabled { get; set; } = false;
        public int PollIntervalMinutes { get; set; } = 2;
        public decimal AmountTolerance { get; set; } = 0.02m;
        public int MinConfirmationsBitcoin { get; set; } = 1;
        public int MinConfirmationsEthereum { get; set; } = 6;
        public int MaxAttempts { get; set; } = 60;

        public string? EtherscanApiKey { get; set; }
        public string MempoolSpaceBaseUrl { get; set; } = "https://mempool.space";
        public string EtherscanBaseUrl { get; set; } = "https://api.etherscan.io";
        public string CoinGeckoBaseUrl { get; set; } = "https://api.coingecko.com";
    }
}

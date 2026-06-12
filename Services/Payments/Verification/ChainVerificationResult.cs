namespace ecommerce.Services.Payments.Verification
{
    public enum ChainVerificationOutcome
    {
        Unknown = 0,
        Confirmed = 1,
        Pending = 2,
        Rejected = 3,
        TxNotFound = 4,
    }

    public class ChainVerificationResult
    {
        public ChainVerificationOutcome Outcome { get; set; }
        public decimal? ReceivedAmount { get; set; }
        public int? Confirmations { get; set; }
        public bool RecipientMatched { get; set; }
        public bool AmountMatched { get; set; }
        public string? Reason { get; set; }
    }
}

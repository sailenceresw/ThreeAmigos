using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Models
{
    [Table("Payment")]
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public decimal AmountUsd { get; set; }

        public string? ProviderReference { get; set; }

        public string? CryptoCurrency { get; set; }
        public string? CryptoNetwork { get; set; }
        public string? CryptoWalletAddress { get; set; }
        public string? CryptoTxHash { get; set; }
        public decimal? CryptoExpectedAmount { get; set; }
        public decimal? CryptoReceivedAmount { get; set; }
        public int? CryptoConfirmations { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public int VerificationAttempts { get; set; } = 0;

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        public string? ConfirmedByUserId { get; set; }

        public DateTime? RefundedAt { get; set; }
        public string? RefundedByUserId { get; set; }
        public string? RefundReason { get; set; }
        public string? RefundProviderReference { get; set; }
    }
}

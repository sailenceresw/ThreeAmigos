using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Models
{
    [Table("PaymentEvent")]
    public class PaymentEvent
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Payment")]
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        [Required]
        public string Source { get; set; } = "";

        [Required]
        public string ExternalEventId { get; set; } = "";

        public string? EventType { get; set; }

        public string? RawPayload { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}

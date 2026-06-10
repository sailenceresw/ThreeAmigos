namespace ecommerce.Models
{
    public enum PaymentStatus
    {
        Pending = 0,
        AwaitingConfirmation = 1,
        Succeeded = 2,
        Failed = 3,
        Canceled = 4,
        Refunded = 5,
    }
}

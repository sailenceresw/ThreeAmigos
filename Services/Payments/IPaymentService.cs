using ecommerce.Models;

namespace ecommerce.Services.Payments
{
    public interface IPaymentService
    {
        Task<PaymentInitResult> BeginAsync(int orderId, PaymentMethod method, ApplicationUser user, IDictionary<string, string?>? providerArgs, CancellationToken ct);

        Task<Payment?> GetByIdAsync(int paymentId, CancellationToken ct);
        Task<Payment?> GetByOrderAsync(int orderId, CancellationToken ct);
        Task<Payment?> GetByProviderReferenceAsync(string providerRef, CancellationToken ct);

        Task<List<Payment>> ListAwaitingConfirmationAsync(PaymentMethod method, CancellationToken ct);
        Task<List<Payment>> ListRecentSucceededAsync(int take, CancellationToken ct);

        Task AttachCryptoTxAsync(int paymentId, string txHash, CancellationToken ct);

        Task<RefundResult> RefundAsync(int paymentId, string reason, string? adminUserId, CancellationToken ct);
        Task MarkRefundedFromExternalAsync(int paymentId, string? providerRefundReference, string reason, CancellationToken ct);

        Task MarkSucceededAsync(int paymentId, string? confirmedByUserId, CancellationToken ct);
        Task MarkFailedAsync(int paymentId, string reason, CancellationToken ct);

        Task<bool> RecordExternalEventAsync(string source, string externalEventId, string? eventType, string? rawPayload, int? paymentId, CancellationToken ct);
    }
}

using ecommerce.Models;

namespace ecommerce.Services.Payments
{
    public interface IPaymentProvider
    {
        PaymentMethod Method { get; }

        Task<PaymentInitResult> InitiateAsync(Order order, decimal amountUsd, ApplicationUser user, IDictionary<string, string?>? providerArgs, CancellationToken ct);
    }
}

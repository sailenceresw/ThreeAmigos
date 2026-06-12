namespace ecommerce.Services.Payments.Pricing
{
    public interface IPriceOracle
    {
        Task<decimal?> GetUsdPriceAsync(string currency, CancellationToken ct);
    }
}

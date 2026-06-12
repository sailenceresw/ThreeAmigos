using ecommerce.Models;

namespace ecommerce.Services.Stock
{
    public interface IStockReservationService
    {
        Task ReserveForOrderAsync(int orderId, IEnumerable<(int productId, int quantity)> items, CancellationToken ct);

        Task ConsumeForOrderAsync(int orderId, CancellationToken ct);

        Task ReleaseForOrderAsync(int orderId, CancellationToken ct);

        Task<int> GetAvailableQuantityAsync(int productId, CancellationToken ct);

        Task<int> ReleaseExpiredAsync(CancellationToken ct);
    }
}

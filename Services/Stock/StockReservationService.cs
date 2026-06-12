using ecommerce.Models;
using ecommerce.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Stock
{
    public class StockReservationService : IStockReservationService
    {
        private readonly Context _context;
        private readonly StockOptions _options;

        public StockReservationService(Context context, IOptions<StockOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public async Task ReserveForOrderAsync(int orderId, IEnumerable<(int productId, int quantity)> items, CancellationToken ct)
        {
            var expires = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.ReservationTtlMinutes));
            foreach (var (productId, quantity) in items)
            {
                if (quantity <= 0) continue;
                _context.Add(new StockReservation
                {
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    ExpiresAt = expires,
                });
            }
            await _context.SaveChangesAsync(ct);
        }

        public async Task ConsumeForOrderAsync(int orderId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var rows = await _context.Set<StockReservation>()
                .Where(r => r.OrderId == orderId && r.ConsumedAt == null && r.ReleasedAt == null)
                .ToListAsync(ct);
            foreach (var r in rows)
            {
                r.ConsumedAt = now;
            }
            await _context.SaveChangesAsync(ct);
        }

        public async Task ReleaseForOrderAsync(int orderId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var rows = await _context.Set<StockReservation>()
                .Where(r => r.OrderId == orderId && r.ConsumedAt == null && r.ReleasedAt == null)
                .ToListAsync(ct);
            foreach (var r in rows)
            {
                r.ReleasedAt = now;
            }
            await _context.SaveChangesAsync(ct);
        }

        public async Task<int> GetAvailableQuantityAsync(int productId, CancellationToken ct)
        {
            var product = await _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (product == null) return 0;

            var now = DateTime.UtcNow;
            var held = await _context.Set<StockReservation>()
                .Where(r => r.ProductId == productId
                            && r.ConsumedAt == null
                            && r.ReleasedAt == null
                            && r.ExpiresAt > now)
                .SumAsync(r => (int?)r.Quantity, ct) ?? 0;
            return Math.Max(0, product.Quantity - held);
        }

        public async Task<int> ReleaseExpiredAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var expired = await _context.Set<StockReservation>()
                .Where(r => r.ConsumedAt == null && r.ReleasedAt == null && r.ExpiresAt <= now)
                .ToListAsync(ct);
            foreach (var r in expired)
            {
                r.ReleasedAt = now;
            }
            await _context.SaveChangesAsync(ct);
            return expired.Count;
        }
    }
}

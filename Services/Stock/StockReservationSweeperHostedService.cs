using ecommerce.Settings;
using Microsoft.Extensions.Options;

namespace ecommerce.Services.Stock
{
    public class StockReservationSweeperHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockReservationSweeperHostedService> _logger;
        private readonly StockOptions _options;

        public StockReservationSweeperHostedService(
            IServiceProvider serviceProvider,
            IOptions<StockOptions> options,
            ILogger<StockReservationSweeperHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.SweepIntervalMinutes));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IStockReservationService>();
                    var released = await svc.ReleaseExpiredAsync(stoppingToken);
                    if (released > 0)
                    {
                        _logger.LogInformation("Released {Count} expired stock reservation(s).", released);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stock reservation sweeper iteration failed.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (TaskCanceledException) { }
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Services
{
    public class TimedHostedService : BackgroundService
    {
        private Timer _timer_stock_status;
        private Timer _timer_product_visibility;
        private Timer _timer_product_price;


        private readonly Context _context;

        private readonly IServiceProvider _serviceProvider;

        public TimedHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer_stock_status = new Timer(ProductPrice, null, TimeSpan.FromSeconds(10), TimeSpan.FromDays(1));
            _timer_stock_status = new Timer(UpdateStockStatus, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
            _timer_product_price = new Timer(ProductVisibilityChange, null, TimeSpan.FromSeconds(60), TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        private void UpdateStockStatus(object state)
        {
            var jobId = Guid.NewGuid();
            var startTime = DateTime.Now;
            Console.WriteLine($"Job ID: {jobId} - Stock Status Update Job - Starting At: {startTime}");

            using (var scope = _serviceProvider.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<Context>();

                var products = _context.Product.ToListAsync().Result;

                foreach (var product in products)
                {
                    var stocks = _context.Stock.Where(s => s.ProductId == product.Id).ToListAsync().Result;
                    var finalStock = 0;
                    foreach (var stock in stocks)
                    {
                        finalStock += stock.Quantity;
                    }

                    if (finalStock > 0)
                    {
                        product.StockStatus = "IN STOCK";
                    }
                    else
                    {
                        product.StockStatus = "OUT OF STOCK";
                    }
                    _context.Product.Update(product);
                    _context.SaveChanges();
                }
                var endTime = DateTime.Now;
                Console.WriteLine($"Job ID: {jobId} - Stock Status Update Job - Completed At: {endTime}");
            }
        }

        private void ProductVisibilityChange(object state)
        {
            var jobId = Guid.NewGuid();
            var startTime = DateTime.Now;
            Console.WriteLine($"Job ID: {jobId} - Product Visibility Job - Starting At: {startTime}");

            using (var scope = _serviceProvider.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<Context>();

                var products = _context.Product.ToListAsync().Result;

                // Group products by normalized name (lowercase and trimmed)
                var groupedProducts = products
                    .GroupBy(p => p.Name.ToLower().Trim())
                    .ToList();

                foreach (var group in groupedProducts)
                {
                    // Find the product with the lowest price in each group
                    var productWithLowestPrice = group.OrderBy(p => p.Price).FirstOrDefault();

                    if (productWithLowestPrice != null)
                    {
                        // Set IsParent to true for the product with the lowest price
                        productWithLowestPrice.IsParent = true;
                        _context.Product.Update(productWithLowestPrice);
                        _context.SaveChanges();

                        // Set IsParent to false for other products in the group
                        foreach (var product in group.Where(p => p.Id != productWithLowestPrice.Id))
                        {
                            product.IsParent = false;
                            _context.Product.Update(product);
                        }
                        _context.SaveChanges();
                    }
                }

                var endTime = DateTime.Now;
                Console.WriteLine($"Job ID: {jobId} - Product Visibility Job - Completed At: {endTime}");
            }
        }

        private void ProductPrice(object state)
        {
            var jobId = Guid.NewGuid();
            var startTime = DateTime.Now;
            Console.WriteLine($"Job ID: {jobId} - Update Product Price Job - Starting At: {startTime}");

            using (var scope = _serviceProvider.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<Context>();

                var yesterday = DateTime.Now.AddDays(-1).Date;
                var today = DateTime.Now.Date;

                // Get all orders from the previous day
                var orderDetails = _context.OrderItem
                    .Include(od => od.Order)
                    .Where(od => od.Order.OrderDate >= yesterday && od.Order.OrderDate < today)
                    .Select(od => od.ProductId)
                    .Distinct()
                    .ToList();

                var products = _context.Product.ToList();

                foreach (var product in products)
                {
                    if (orderDetails.Contains(product.Id))
                    {
                        // Increase price by 10% if the product was purchased the previous day
                        product.Price *= 1.10m;
                    }
                    // Update the product in the context
                    _context.Product.Update(product);
                }

                _context.SaveChanges();

                var endTime = DateTime.Now;
                Console.WriteLine($"Job ID: {jobId} - Update Product Price Job - Completed At: {endTime}");
            }
        }

        public override void Dispose()
        {
            _timer_stock_status?.Dispose();
            _timer_product_visibility?.Dispose();
            _timer_product_price?.Dispose();
            base.Dispose();
        }
    }
}
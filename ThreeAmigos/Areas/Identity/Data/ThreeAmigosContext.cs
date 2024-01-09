using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThreeAmigos.Areas.Identity.Data;
using ThreeAmigos.Models.AccountBalances;
using ThreeAmigos.Models.OrderItems;
using ThreeAmigos.Models.Orders;
using ThreeAmigos.Models.OrdersHistory;
using ThreeAmigos.Models.Products;

namespace ThreeAmigos.Areas.Identity.Data
{
    public class ThreeAmigosContext : IdentityDbContext<ThreeAmigosUser>
    {
        public ThreeAmigosContext(DbContextOptions<ThreeAmigosContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
         public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<AccountBalance> AccountBalances { get; set; }
        public DbSet<OrderHistory> OrdersHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}

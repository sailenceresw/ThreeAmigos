using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Models
{
    public class Context : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Stock> Stock { get; set; }
        public DbSet<Statement> Statement { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<CartItem> CartItem { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderItem> OrderItem { get; set; }
        public DbSet<Shipment> Shipment { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }

        public Context() : base() { }

        public Context(DbContextOptions<Context> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>().HasIndex(appUser => appUser.Email)
                .IsUnique();

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Balance)
                    .HasPrecision(18, 2);
            });
            builder.Entity<Order>(entity =>
            {
                entity.Property(e => e.TotalValue)
                    .HasPrecision(18, 2);
            });
            builder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.Price)
                    .HasPrecision(18, 2);
            });
            builder.Entity<Statement>(entity =>
            {
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
            });
        }
    }
}

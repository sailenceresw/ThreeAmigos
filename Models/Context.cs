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
        public DbSet<Payment> Payment { get; set; }
        public DbSet<PaymentEvent> PaymentEvent { get; set; }
        public DbSet<StockReservation> StockReservation { get; set; }

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
            builder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.AmountUsd).HasPrecision(18, 2);
                entity.Property(e => e.CryptoExpectedAmount).HasPrecision(28, 12);
                entity.Property(e => e.CryptoReceivedAmount).HasPrecision(28, 12);
                entity.Property(e => e.Method).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProviderReference);

                entity.HasOne(p => p.Order)
                    .WithMany()
                    .HasForeignKey(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<PaymentEvent>(entity =>
            {
                entity.HasIndex(e => new { e.Source, e.ExternalEventId }).IsUnique();

                entity.HasOne(e => e.Payment)
                    .WithMany()
                    .HasForeignKey(e => e.PaymentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            builder.Entity<Order>(entity =>
            {
                entity.Property(e => e.PaymentMethod).HasConversion<int?>();

                entity.HasOne(o => o.Payment)
                    .WithMany()
                    .HasForeignKey(o => o.PaymentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            builder.Entity<StockReservation>(entity =>
            {
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => new { e.ProductId, e.ConsumedAt, e.ReleasedAt, e.ExpiresAt });

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

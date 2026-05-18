using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Models;

namespace TasteAtDoor.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Caterer> Caterers { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CustomizationGroup> CustomizationGroups { get; set; }
        public DbSet<CustomizationOption> CustomizationOptions { get; set; }
        public DbSet<OrderItemCustomization> OrderItemCustomizations { get; set; }
        public DbSet<OrderItemReview> OrderItemReviews { get; set; }
        public DbSet<AppLog> AppLogs { get; set; }
        public DbSet<OrderChatMessage> OrderChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MenuItem>()
                .HasOne(m => m.Caretaker)
                .WithMany(u => u.MenuItems)
                .HasForeignKey(m => m.CaretakerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomizationGroup>()
                .HasOne(g => g.MenuItem)
                .WithMany(m => m.CustomizationGroups)
                .HasForeignKey(g => g.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomizationOption>()
                .HasOne(o => o.CustomizationGroup)
                .WithMany(g => g.Options)
                .HasForeignKey(o => o.CustomizationGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItemCustomization>()
                .HasOne(c => c.OrderItem)
                .WithMany(i => i.SelectedCustomizations)
                .HasForeignKey(c => c.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItemReview>()
                .HasOne(r => r.OrderItem)
                .WithMany(i => i.Reviews)
                .HasForeignKey(r => r.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItemReview>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OrderItemReview>()
                .HasOne(r => r.MenuItem)
                .WithMany()
                .HasForeignKey(r => r.MenuItemId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OrderItemReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OrderItemReview>()
                .HasOne(r => r.Caterer)
                .WithMany()
                .HasForeignKey(r => r.CatererId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            builder.Entity<CustomizationOption>()
                .Property(o => o.PriceChange)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(o => o.BaseUnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(o => o.FinalUnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(o => o.LineTotal)
                .HasPrecision(18, 2);

            builder.Entity<OrderItemCustomization>()
                .Property(o => o.PriceChange)
                .HasPrecision(18, 2);

            builder.Entity<AppLog>()
                .HasIndex(l => l.CreatedAt);

            builder.Entity<OrderChatMessage>()
                .HasOne(m => m.Order)
                .WithMany()
                .HasForeignKey(m => m.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderChatMessage>()
                .HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OrderChatMessage>()
                .HasIndex(m => new { m.OrderId, m.SentAt });
        }
    }
}

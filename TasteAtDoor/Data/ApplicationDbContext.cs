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
		}
	}
}
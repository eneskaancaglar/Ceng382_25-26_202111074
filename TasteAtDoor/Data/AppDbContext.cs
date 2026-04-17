using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Models;

namespace TasteAtDoor.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		public DbSet<ApplicationUser> ApplicationUsers { get; set; }
		public DbSet<Caterer> Caterers { get; set; }
		public DbSet<MenuItem> MenuItems { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderItem> OrderItems { get; set; }
	}
}
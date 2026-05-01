using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class UserDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalItemsPurchased { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}
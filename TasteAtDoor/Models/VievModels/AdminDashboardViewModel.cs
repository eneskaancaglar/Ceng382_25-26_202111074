using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalCaretakers { get; set; }
        public int TotalAdmins { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public int TotalMenuItems { get; set; }
        public int TotalReviews { get; set; }
        public int TotalLogs { get; set; }

        public double AverageMenuRating { get; set; }
        public double AverageCatererRating { get; set; }

        public List<Order> RecentOrders { get; set; } = new();
        public List<AppLog> RecentLogs { get; set; } = new();
    }
}


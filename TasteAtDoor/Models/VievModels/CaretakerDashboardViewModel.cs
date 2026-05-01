using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class CaretakerDashboardViewModel
    {
        public int TotalMenus { get; set; }
        public int TotalReceivedOrders { get; set; }
        public int TotalCompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public double AverageCatererRating { get; set; }
        public int TotalReviewCount { get; set; }

        public List<OrderItem> RecentOrderItems { get; set; } = new();
    }
}
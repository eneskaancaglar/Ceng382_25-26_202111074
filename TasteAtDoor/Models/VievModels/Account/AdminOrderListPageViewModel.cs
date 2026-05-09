using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class AdminOrderListPageViewModel
    {
        public List<Order> Orders { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int Page { get; set; }

        public int TotalPages { get; set; }
    }
}
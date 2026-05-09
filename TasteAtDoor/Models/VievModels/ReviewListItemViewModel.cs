namespace TasteAtDoor.Models.ViewModels
{
    public class ReviewListItemViewModel
    {
        public int ReviewId { get; set; }

        public int OrderId { get; set; }

        public int OrderItemId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string CatererName { get; set; } = string.Empty;

        public string? CatererEmail { get; set; }

        public string MenuItemName { get; set; } = string.Empty;

        public int MenuRating { get; set; }

        public int CatererRating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }
}
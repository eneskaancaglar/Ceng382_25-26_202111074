namespace TasteAtDoor.Models.ViewModels
{
    public class OrderChatRoomViewModel
    {
        public int OrderId { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public string CurrentUserId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string CatererName { get; set; } = string.Empty;

        public string? CatererEmail { get; set; }

        public List<OrderChatMessageItemViewModel> Messages { get; set; } = new();
    }

    public class OrderChatMessageItemViewModel
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string SenderName { get; set; } = string.Empty;

        public string MessageText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}


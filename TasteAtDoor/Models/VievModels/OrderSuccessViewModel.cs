namespace TasteAtDoor.Models.ViewModels
{
    public class OrderSuccessViewModel
    {
        public int OrderId { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public DateTime? EventDate { get; set; }

        public int GuestCount { get; set; }

        public string EventAddress { get; set; } = string.Empty;
    }
}
namespace TasteAtDoor.Models.ViewModels
{
    public class OrderSuccessViewModel
    {
        public int OrderId { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
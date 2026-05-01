namespace TasteAtDoor.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        public int Quantity { get; set; }

        public decimal BaseUnitPrice { get; set; }

        public decimal FinalUnitPrice { get; set; }

        public decimal LineTotal { get; set; }

        public ICollection<OrderItemCustomization> SelectedCustomizations { get; set; } = new List<OrderItemCustomization>();
        public ICollection<OrderItemReview> Reviews { get; set; } = new List<OrderItemReview>();
    }
}
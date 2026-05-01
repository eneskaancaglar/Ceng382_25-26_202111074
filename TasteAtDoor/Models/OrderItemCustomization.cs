namespace TasteAtDoor.Models
{
    public class OrderItemCustomization
    {
        public int Id { get; set; }

        public int OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }

        public int CustomizationGroupId { get; set; }
        public int CustomizationOptionId { get; set; }

        public string GroupTitle { get; set; } = string.Empty;
        public string OptionName { get; set; } = string.Empty;

        public decimal PriceChange { get; set; }
    }
}
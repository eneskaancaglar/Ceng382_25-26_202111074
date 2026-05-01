namespace TasteAtDoor.Models
{
    public class CartItem
    {
        public string Signature { get; set; } = string.Empty;
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal BaseUnitPrice { get; set; }
        public decimal FinalUnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImageContentType { get; set; } = string.Empty;
        public string ImageBase64 { get; set; } = string.Empty;

        public List<CustomizationSelection> SelectedOptions { get; set; } = new();

        public decimal LineTotal => FinalUnitPrice * Quantity;
    }
}
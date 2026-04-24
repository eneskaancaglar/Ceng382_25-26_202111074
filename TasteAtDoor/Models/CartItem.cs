namespace TasteAtDoor.Models
{
	public class CartItem
	{
		public int MenuItemId { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal UnitPrice { get; set; }
		public int Quantity { get; set; }
		public string ImageContentType { get; set; } = string.Empty;
		public string ImageBase64 { get; set; } = string.Empty;

		public decimal LineTotal => UnitPrice * Quantity;
	}
}
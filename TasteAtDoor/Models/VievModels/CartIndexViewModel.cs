using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class CartIndexViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => i.LineTotal);
    }
}


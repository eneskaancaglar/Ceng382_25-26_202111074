using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class MenuCustomizationIndexViewModel
    {
        public int MenuItemId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public List<CustomizationGroup> Groups { get; set; } = new();
    }
}


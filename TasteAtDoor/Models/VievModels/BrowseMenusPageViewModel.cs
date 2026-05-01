using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class BrowseMenusPageViewModel
    {
        public List<BrowseMenuItemViewModel> Items { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class CaretakerMenuListViewModel
    {
        public List<MenuItem> MenuItems { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
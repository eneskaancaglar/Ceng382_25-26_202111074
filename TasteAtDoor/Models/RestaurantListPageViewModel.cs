namespace TasteAtDoor.Models.ViewModels
{
    public class RestaurantListPageViewModel
    {
        public bool UserLocationSaved { get; set; }

        public string Search { get; set; } = string.Empty;

        public List<RestaurantListItemViewModel> Restaurants { get; set; } = new();
    }
}
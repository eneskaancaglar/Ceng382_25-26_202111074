namespace TasteAtDoor.Models.ViewModels
{
    public class RestaurantListPageViewModel
    {
        public bool UserLocationSaved { get; set; }

        public string Search { get; set; } = string.Empty;

        public double? UserLatitude { get; set; }

        public double? UserLongitude { get; set; }

        public string? UserAddress { get; set; }

        public List<RestaurantListItemViewModel> Restaurants { get; set; } = new();
    }
}



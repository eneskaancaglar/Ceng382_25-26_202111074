using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class BrowseMenusPageViewModel
    {
        public List<BrowseMenuItemViewModel> Items { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public string UserLocationText { get; set; } = string.Empty;

        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }

        public double RadiusKm { get; set; } = 10;

        public int Page { get; set; }
        public int TotalPages { get; set; }

        public string GoogleMapsApiKey { get; set; } = string.Empty;
    }
}


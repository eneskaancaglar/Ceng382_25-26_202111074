using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class RestaurantMenuPageViewModel
    {
        public string RestaurantId { get; set; } = string.Empty;

        public string RestaurantName { get; set; } = string.Empty;

        public string? RestaurantAddress { get; set; }

        public string? RestaurantBio { get; set; }

        public byte[]? RestaurantLogoImageData { get; set; }

        public string? RestaurantLogoImageContentType { get; set; }

        public double DistanceKm { get; set; }

        public bool CanOrder => DistanceKm <= 5;

        public List<MenuItem> MenuItems { get; set; } = new();
    }
}
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

        public double AverageCatererRating { get; set; }

        public int CatererReviewCount { get; set; }

        public bool CanOrder => DistanceKm <= 15;

        public List<MenuItem> MenuItems { get; set; } = new();

        public Dictionary<int, double> AverageMenuRatings { get; set; } = new();

        public Dictionary<int, int> MenuReviewCounts { get; set; } = new();
    }
}



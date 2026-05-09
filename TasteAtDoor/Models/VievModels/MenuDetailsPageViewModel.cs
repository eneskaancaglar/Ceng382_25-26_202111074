using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class MenuDetailsPageViewModel
    {
        public MenuItem MenuItem { get; set; } = null!;

        public string RestaurantName { get; set; } = string.Empty;

        public string? RestaurantAddress { get; set; }

        public double DistanceKm { get; set; }

        public bool CanOrder => DistanceKm <= 5;

        public double AverageMenuRating { get; set; }

        public double AverageCatererRating { get; set; }

        public int ReviewCount { get; set; }

        public List<ReviewListItemViewModel> Reviews { get; set; } = new();
    }
}
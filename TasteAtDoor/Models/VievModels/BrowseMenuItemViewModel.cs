using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class BrowseMenuItemViewModel
    {
        public MenuItem MenuItem { get; set; } = new();

        public double AverageMenuRating { get; set; }
        public double AverageCatererRating { get; set; }

        public int MenuReviewCount { get; set; }
        public int CatererReviewCount { get; set; }

        public double? DistanceKm { get; set; }
    }
}


namespace TasteAtDoor.Models.ViewModels
{
    public class RestaurantListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string RestaurantName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? Bio { get; set; }

        public byte[]? LogoImageData { get; set; }

        public string? LogoImageContentType { get; set; }

        public int MenuCount { get; set; }

        public double DistanceKm { get; set; }

        public double AverageCatererRating { get; set; }

        public int CatererReviewCount { get; set; }

        public bool CanOrder => DistanceKm <= 15;
    }
}

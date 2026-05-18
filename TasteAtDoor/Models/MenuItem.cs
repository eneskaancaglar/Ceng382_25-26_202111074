using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Package Name")]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        [Display(Name = "Price Per Person")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Package Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "General Event";

        [Required]
        [StringLength(80)]
        [Display(Name = "Package Category")]
        public string PackageCategory { get; set; } = "Standard";

        [Range(1, 100000)]
        [Display(Name = "Minimum Guest Count")]
        public int MinGuestCount { get; set; } = 10;

        [Range(1, 100000)]
        [Display(Name = "Maximum Guest Count")]
        public int MaxGuestCount { get; set; } = 500;

        [Display(Name = "Includes Main Course")]
        public bool IncludesMainCourse { get; set; } = true;

        [Display(Name = "Includes Dessert")]
        public bool IncludesDessert { get; set; }

        [Display(Name = "Includes Snacks")]
        public bool IncludesSnacks { get; set; }

        [Display(Name = "Includes Drinks")]
        public bool IncludesDrinks { get; set; }

        [StringLength(1000)]
        [Display(Name = "Included Items")]
        public string? IncludedItems { get; set; }

        [StringLength(1000)]
        [Display(Name = "Service Details")]
        public string? ServiceDetails { get; set; }

        [Required]
        [StringLength(250)]
        [Display(Name = "Caterer Location / Address")]
        public string LocationText { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Required]
        public string CaretakerId { get; set; } = string.Empty;

        public ApplicationUser? Caretaker { get; set; }

        [Required]
        public string ImageFileName { get; set; } = string.Empty;

        [Required]
        public string ImageContentType { get; set; } = string.Empty;

        [Required]
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        public ICollection<CustomizationGroup> CustomizationGroups { get; set; } = new List<CustomizationGroup>();
    }
}


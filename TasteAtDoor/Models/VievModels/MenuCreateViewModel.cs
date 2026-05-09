using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TasteAtDoor.Models.ViewModels
{
    public class MenuCreateViewModel
    {
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

        [StringLength(250)]
        [Display(Name = "Caterer Location / Address")]
        public string? LocationText { get; set; }

        [Required]
        [Display(Name = "Package Image")]
        public IFormFile? ImageFile { get; set; }
    }
}
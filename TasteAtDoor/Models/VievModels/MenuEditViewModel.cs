using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TasteAtDoor.Models.ViewModels
{
    public class MenuEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Menu Location / Address")]
        public string? LocationText { get; set; }

        [Display(Name = "New Menu Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageBase64 { get; set; }

        public string? ExistingImageContentType { get; set; }
    }
}
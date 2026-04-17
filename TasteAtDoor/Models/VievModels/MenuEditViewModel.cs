using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TasteAtDoor.Models.ViewModels
{
    public class MenuEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 999999.0)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }

        public string ExistingImageBase64 { get; set; } = string.Empty;

        public string ExistingImageContentType { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TasteAtDoor.Models.ViewModels
{
    public class MenuCreateViewModel
    {
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        [Display(Name = "Restaurant Address / Location")]
        public string LocationText { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Menu Image")]
        public IFormFile? ImageFile { get; set; }
    }
}
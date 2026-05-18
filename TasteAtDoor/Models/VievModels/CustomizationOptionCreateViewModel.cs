using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels
{
    public class CustomizationOptionCreateViewModel
    {
        public int CustomizationGroupId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(-999999.0, 999999.0)]
        public decimal PriceChange { get; set; }

        public bool IsDefault { get; set; }

        public int DisplayOrder { get; set; }
    }
}


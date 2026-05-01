using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class MenuItem
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

        [Required]
        [StringLength(250)]
        public string LocationText { get; set; } = string.Empty;

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
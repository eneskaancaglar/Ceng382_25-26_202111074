using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class CustomizationGroup
    {
        public int Id { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        public MenuItem? MenuItem { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string GroupType { get; set; } = "SingleSelect";

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }

        public ICollection<CustomizationOption> Options { get; set; } = new List<CustomizationOption>();
    }
}
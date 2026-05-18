using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels
{
    public class CustomizationGroupCreateViewModel
    {
        public int MenuItemId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string GroupType { get; set; } = "SingleSelect";

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }
    }
}


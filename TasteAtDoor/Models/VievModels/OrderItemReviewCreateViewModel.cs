using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels
{
    public class OrderItemReviewCreateViewModel
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }

        public string MenuItemName { get; set; } = string.Empty;
        public string CatererName { get; set; } = string.Empty;

        [Range(1, 5)]
        public int MenuRating { get; set; } = 5;

        [Range(1, 5)]
        public int CatererRating { get; set; } = 5;

        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}


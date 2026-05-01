using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class OrderItemReview
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public int OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        [Required]
        public string CatererId { get; set; } = string.Empty;
        public ApplicationUser? Caterer { get; set; }

        [Range(1, 5)]
        public int MenuRating { get; set; }

        [Range(1, 5)]
        public int CatererRating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
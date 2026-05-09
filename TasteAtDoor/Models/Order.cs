using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Pending";

        [StringLength(80)]
        public string EventType { get; set; } = string.Empty;

        public DateTime? EventDate { get; set; }

        [Range(1, 100000)]
        public int GuestCount { get; set; }

        [StringLength(500)]
        public string EventAddress { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? EventNote { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
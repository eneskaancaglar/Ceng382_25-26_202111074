using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class OrderChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [Required]
        public string SenderUserId { get; set; } = string.Empty;

        public ApplicationUser? SenderUser { get; set; }

        [Required]
        [StringLength(50)]
        public string SenderRole { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}


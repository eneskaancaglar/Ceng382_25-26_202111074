using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
    public class AppLog
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(30)]
        public string Level { get; set; } = "Info";

        [Required]
        [StringLength(80)]
        public string EventType { get; set; } = "General";

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public string? UserId { get; set; }

        [StringLength(256)]
        public string? UserEmail { get; set; }

        public string? Details { get; set; }
    }
}


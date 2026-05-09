using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        public string CartJson { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "Wedding";

        [Required]
        [Display(Name = "Event Date")]
        public DateTime? EventDate { get; set; }

        [Required]
        [Range(1, 100000)]
        [Display(Name = "Total Guest Count")]
        public int GuestCount { get; set; } = 50;

        [Required]
        [StringLength(500)]
        [Display(Name = "Event Address")]
        public string EventAddress { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Event Note")]
        public string? EventNote { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Card Holder Name")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        [CreditCard]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        [Display(Name = "Expiry Date")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3)]
        [Display(Name = "CVV")]
        public string CVV { get; set; } = string.Empty;
    }
}
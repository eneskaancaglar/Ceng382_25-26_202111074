using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        public string CartJson { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        [CreditCard]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3)]
        public string CVV { get; set; } = string.Empty;
    }
}
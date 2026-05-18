using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels.Account
{
    public class VerifyEmailCodeViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }
}


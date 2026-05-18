using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TasteAtDoor.Models.ViewModels
{
	public class ProfileViewModel
	{
		[Required(ErrorMessage = "Name is required.")]
		[StringLength(100)]
		public string FullName { get; set; } = string.Empty;

		public string? Email { get; set; }

		[Phone]
		public string? PhoneNumber { get; set; }

		[StringLength(500)]
		public string? Bio { get; set; }

		public string? Address { get; set; }

		public double? Latitude { get; set; }

		public double? Longitude { get; set; }

		public IFormFile? ProfileImageFile { get; set; }

		public string? ExistingImageBase64 { get; set; }

		public string? ExistingImageContentType { get; set; }

		public bool HasExistingImage =>
			!string.IsNullOrWhiteSpace(ExistingImageBase64) &&
			!string.IsNullOrWhiteSpace(ExistingImageContentType);
	}
}


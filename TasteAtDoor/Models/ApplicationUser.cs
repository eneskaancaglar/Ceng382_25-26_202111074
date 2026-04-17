using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models
{
	public class ApplicationUser : IdentityUser
	{
		[StringLength(100)]
		public string FullName { get; set; } = string.Empty;

		public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
	}
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TasteAtDoor.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public byte[]? ProfileImageData { get; set; }

    [StringLength(100)]
    public string? ProfileImageContentType { get; set; }

    [StringLength(255)]
    public string? ProfileImageFileName { get; set; }

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}


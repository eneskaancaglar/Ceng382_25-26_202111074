using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models;

public class Caterer
{
    public int Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string? Address { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}


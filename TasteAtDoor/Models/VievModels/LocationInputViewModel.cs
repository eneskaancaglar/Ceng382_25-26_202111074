using System.ComponentModel.DataAnnotations;

namespace TasteAtDoor.Models.ViewModels
{
    public class LocationInputViewModel
    {
        [Required(ErrorMessage = "Please select a location from the map.")]
        [Range(-90, 90, ErrorMessage = "Latitude value is invalid.")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "Please select a location from the map.")]
        [Range(-180, 180, ErrorMessage = "Longitude value is invalid.")]
        public double? Longitude { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }
    }
}
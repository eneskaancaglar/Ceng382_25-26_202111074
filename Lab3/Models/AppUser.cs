using System.ComponentModel.DataAnnotations;

namespace Lab3.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Surname { get; set; } = "";

    [Required]
    public string Address { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public string PhotoPath { get; set; } = "";

    public string Initials
    {
        get
        {
            var firstName = string.IsNullOrWhiteSpace(Name) ? "" : Name.Substring(0, 1);
            var firstSurname = string.IsNullOrWhiteSpace(Surname) ? "" : Surname.Substring(0, 1);
            return (firstName + firstSurname).ToUpper();
        }
    }
}
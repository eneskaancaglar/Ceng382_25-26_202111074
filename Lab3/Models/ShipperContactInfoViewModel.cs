namespace Lab3.Models;

public class ShipperContactInfoViewModel
{
    public int ContactInfoID { get; set; }
    public int ShipperID { get; set; }
    public string CompanyName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Postcode { get; set; }
    public string? Address { get; set; }
}
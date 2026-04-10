using Lab3.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Lab3.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<ShipperContactInfoViewModel> ContactInfos { get; set; } = new();

    public void OnGet()
    {
        string? connectionString = _configuration.GetConnectionString("DefaultConnection");

        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        string query = @"
            SELECT 
                sci.ContactInfoID,
                sci.ShipperID,
                s.CompanyName,
                sci.Email,
                sci.Website,
                sci.Phone,
                sci.City,
                sci.Country,
                sci.Postcode,
                sci.Address
            FROM ShippersContactInfo sci
            INNER JOIN Shippers s ON sci.ShipperID = s.ShipperID
            ORDER BY sci.ContactInfoID";

        using SqlCommand command = new SqlCommand(query, connection);
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            ContactInfos.Add(new ShipperContactInfoViewModel
            {
                ContactInfoID = Convert.ToInt32(reader["ContactInfoID"]),
                ShipperID = Convert.ToInt32(reader["ShipperID"]),
                CompanyName = reader["CompanyName"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? "",
                Website = reader["Website"]?.ToString(),
                Phone = reader["Phone"]?.ToString(),
                City = reader["City"]?.ToString(),
                Country = reader["Country"]?.ToString(),
                Postcode = reader["Postcode"]?.ToString(),
                Address = reader["Address"]?.ToString()
            });
        }
    }
}
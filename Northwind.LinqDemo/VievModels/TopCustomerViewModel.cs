namespace Northwind.LinqDemo.ViewModels;

public class TopCustomerViewModel
{
    public string CustomerId { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public decimal TotalSpend { get; set; }
    public int OrderCount { get; set; }
}
namespace Northwind.LinqDemo.ViewModels;

public class ReportViewModel
{
    public string? Query { get; set; }

    public int OverallProductCount { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public List<CategoryGroupViewModel> CategoryGroups { get; set; } = new();
    public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
}
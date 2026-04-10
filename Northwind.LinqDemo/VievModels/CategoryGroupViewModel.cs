namespace Northwind.LinqDemo.ViewModels;

public class CategoryGroupViewModel
{
    public string CategoryName { get; set; } = "";
    public List<ProductRowViewModel> Products { get; set; } = new();
    public decimal Subtotal { get; set; }
}
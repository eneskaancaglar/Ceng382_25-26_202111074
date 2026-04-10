namespace Northwind.LinqDemo.ViewModels;

public class ProductRowViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public short UnitsInStock { get; set; }
    public decimal InventoryValue { get; set; }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.LinqDemo.Models;
using Northwind.LinqDemo.ViewModels;

namespace Northwind.LinqDemo.Controllers;

public class LinqController : Controller
{
	private readonly NorthwindContext _db;

	public LinqController(NorthwindContext db)
	{
		_db = db;
	}

	public IActionResult Index()
	{
		return RedirectToAction(nameof(Report));
	}

	public async Task<IActionResult> Report(string? q)
	{
		IQueryable<Product> productsQuery = _db.Products
			.AsNoTracking()
			.Include(p => p.Category);

		if (!string.IsNullOrWhiteSpace(q))
		{
			string lowered = q.ToLower();

			productsQuery = productsQuery.Where(p =>
				p.ProductName != null &&
				p.ProductName.ToLower().Contains(lowered));
		}

		var productRows = await productsQuery
			.Select(p => new
			{
				p.ProductId,
				p.ProductName,
				CategoryName = p.Category != null ? p.Category.CategoryName : "Unknown",
				UnitPrice = p.UnitPrice ?? 0m,
				UnitsInStock = p.UnitsInStock ?? (short)0,
				InventoryValue = (p.UnitPrice ?? 0m) * (p.UnitsInStock ?? 0)
			})
			.OrderBy(x => x.CategoryName)
			.ThenBy(x => x.ProductName)
			.ToListAsync();

		var categoryGroups = productRows
			.GroupBy(x => x.CategoryName)
			.Select(g => new CategoryGroupViewModel
			{
				CategoryName = g.Key ?? "Unknown",
				Products = g.Select(p => new ProductRowViewModel
				{
					ProductId = p.ProductId,
					ProductName = p.ProductName ?? "",
					UnitPrice = p.UnitPrice,
					UnitsInStock = p.UnitsInStock,
					InventoryValue = p.InventoryValue
				}).ToList(),
				Subtotal = g.Sum(p => p.InventoryValue)
			})
			.OrderBy(g => g.CategoryName)
			.ToList();

		var summaryQuery = _db.Products
			.AsNoTracking()
			.Where(p => string.IsNullOrWhiteSpace(q) ||
						(p.ProductName != null && p.ProductName.ToLower().Contains(q.ToLower())));

		int overallProductCount = await summaryQuery.CountAsync();
		decimal averagePrice = overallProductCount > 0
			? await summaryQuery.AverageAsync(p => p.UnitPrice ?? 0m)
			: 0m;
		decimal? maxPrice = overallProductCount > 0
			? await summaryQuery.MaxAsync(p => p.UnitPrice)
			: 0m;

		var topCustomers = await _db.Orders
			.AsNoTracking()
			.Where(o => o.Customer != null)
			.GroupBy(o => new
			{
				o.CustomerId,
				CompanyName = o.Customer != null ? o.Customer.CompanyName : "",
			})
			.Select(g => new TopCustomerViewModel
			{
				CustomerId = g.Key.CustomerId ?? "",
				CompanyName = g.Key.CompanyName ?? "",
				OrderCount = g.Count(),
				TotalSpend = g.SelectMany(o => o.OrderDetails)
							  .Sum(od => od.UnitPrice * od.Quantity)
			})
			.OrderByDescending(x => x.TotalSpend)
			.Take(5)
			.ToListAsync();

		var vm = new ReportViewModel
		{
			Query = q,
			OverallProductCount = overallProductCount,
			AveragePrice = averagePrice,
			MaxPrice = maxPrice,
			CategoryGroups = categoryGroups,
			TopCustomers = topCustomers
		};

		return View(vm);
	}
}
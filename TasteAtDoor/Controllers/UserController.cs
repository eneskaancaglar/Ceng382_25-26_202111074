using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "User,Caretaker,Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "User,Caretaker,Admin")]
        public async Task<IActionResult> Index(string search = "", int page = 1)
        {
            const int pageSize = 6;

            var query = _context.MenuItems
                .Include(m => m.Caretaker)
                .Include(m => m.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.Name.Contains(search) ||
                    m.Description.Contains(search) ||
                    (m.Caretaker != null &&
                     ((m.Caretaker.FullName != null && m.Caretaker.FullName.Contains(search)) ||
                      (m.Caretaker.Email != null && m.Caretaker.Email.Contains(search)))));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var menus = await query
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var menuIds = menus.Select(m => m.Id).ToList();
            var catererIds = menus.Select(m => m.CaretakerId).Distinct().ToList();

            var allReviews = await _context.OrderItemReviews
                .Where(r => menuIds.Contains(r.MenuItemId) || catererIds.Contains(r.CatererId))
                .ToListAsync();

            var menuRatingMap = allReviews
                .Where(r => menuIds.Contains(r.MenuItemId))
                .GroupBy(r => r.MenuItemId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Average = g.Average(x => x.MenuRating),
                        Count = g.Count()
                    });

            var catererRatingMap = allReviews
                .Where(r => catererIds.Contains(r.CatererId))
                .GroupBy(r => r.CatererId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Average = g.Average(x => x.CatererRating),
                        Count = g.Count()
                    });

            var items = menus.Select(m => new BrowseMenuItemViewModel
            {
                MenuItem = m,
                AverageMenuRating = menuRatingMap.ContainsKey(m.Id) ? menuRatingMap[m.Id].Average : 0,
                MenuReviewCount = menuRatingMap.ContainsKey(m.Id) ? menuRatingMap[m.Id].Count : 0,
                AverageCatererRating = catererRatingMap.ContainsKey(m.CaretakerId) ? catererRatingMap[m.CaretakerId].Average : 0,
                CatererReviewCount = catererRatingMap.ContainsKey(m.CaretakerId) ? catererRatingMap[m.CaretakerId].Count : 0
            }).ToList();

            var model = new BrowseMenusPageViewModel
            {
                Items = items,
                Search = search,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
                return Challenge();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.ApplicationUserId == currentUser.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var model = new UserDashboardViewModel
            {
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalPrice),
                TotalItemsPurchased = orders.SelectMany(o => o.OrderItems).Sum(i => i.Quantity),
                RecentOrders = orders.Take(5).ToList()
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Orders(string search = "", string status = "", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
                return Challenge();

            const int pageSize = 5;

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Reviews)
                .Where(o => o.ApplicationUserId == currentUser.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    o.OrderItems.Any(oi => oi.MenuItem != null && oi.MenuItem.Name.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new OrderHistoryPageViewModel
            {
                Orders = orders,
                Search = search,
                Status = status,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Receipt(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
                return Challenge();

            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedCustomizations)
                .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == currentUser.Id);

            if (order is null)
                return NotFound();

            return View(order);
        }
    }
}
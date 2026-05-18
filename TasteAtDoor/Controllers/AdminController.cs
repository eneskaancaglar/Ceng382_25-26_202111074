using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels;
using TasteAtDoor.Services;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAppLogService _appLogService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAppLogService appLogService)
        {
            _userManager = userManager;
            _context = context;
            _appLogService = appLogService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();

            var caretakers = await _userManager.GetUsersInRoleAsync("Caretaker");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var customers = await _userManager.GetUsersInRoleAsync("User");

            var reviews = await _context.OrderItemReviews.ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalCaretakers = caretakers.Count,
                TotalAdmins = admins.Count,
                TotalCustomers = customers.Count,

                TotalOrders = await _context.Orders.CountAsync(),
                CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed"),

                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.TotalPrice),

                TotalMenuItems = await _context.MenuItems.CountAsync(),
                TotalReviews = reviews.Count,
                TotalLogs = await _context.AppLogs.CountAsync(),

                AverageMenuRating = reviews.Any()
                    ? reviews.Average(r => r.MenuRating)
                    : 0,

                AverageCatererRating = reviews.Any()
                    ? reviews.Average(r => r.CatererRating)
                    : 0,

                RecentOrders = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(6)
                    .ToListAsync(),

                RecentLogs = await _context.AppLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(8)
                    .ToListAsync()
            };

            await _appLogService.LogAsync(
                eventType: "AdminDashboardViewed",
                message: "Admin viewed dashboard.",
                userId: _userManager.GetUserId(User),
                userEmail: User.Identity?.Name);

            return View(model);
        }

        public async Task<IActionResult> Users(string search = "", string role = "", int page = 1)
        {
            const int pageSize = 10;

            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var orderStats = await _context.Orders
                .GroupBy(o => o.ApplicationUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalPrice)
                })
                .ToDictionaryAsync(x => x.UserId, x => x);

            var menuStats = await _context.MenuItems
                .GroupBy(m => m.CaretakerId)
                .Select(g => new
                {
                    CaretakerId = g.Key,
                    MenuCount = g.Count()
                })
                .ToDictionaryAsync(x => x.CaretakerId, x => x.MenuCount);

            var ratingStats = await _context.OrderItemReviews
                .GroupBy(r => r.CatererId)
                .Select(g => new
                {
                    CatererId = g.Key,
                    AverageRating = g.Average(r => r.CatererRating)
                })
                .ToDictionaryAsync(x => x.CatererId, x => x.AverageRating);

            var items = new List<AdminUserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var rolesText = roles.Any() ? string.Join(", ", roles) : "No Role";

                if (!string.IsNullOrWhiteSpace(role) &&
                    !roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var matches =
                        user.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(user.Email) &&
                         user.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(user.PhoneNumber) &&
                         user.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        rolesText.Contains(search, StringComparison.OrdinalIgnoreCase);

                    if (!matches)
                    {
                        continue;
                    }
                }

                orderStats.TryGetValue(user.Id, out var userOrderStats);
                menuStats.TryGetValue(user.Id, out var menuCount);
                ratingStats.TryGetValue(user.Id, out var averageRating);

                items.Add(new AdminUserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    RolesText = rolesText,
                    OrderCount = userOrderStats?.OrderCount ?? 0,
                    TotalSpent = userOrderStats?.TotalSpent ?? 0,
                    MenuCount = menuCount,
                    AverageCatererRating = averageRating
                });
            }

            var totalCount = items.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var pagedItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new AdminUserListPageViewModel
            {
                Users = pagedItems,
                Search = search,
                Role = role,
                Page = page,
                TotalPages = totalPages
            };

            await _appLogService.LogAsync(
                eventType: "AdminUsersViewed",
                message: "Admin viewed users page.",
                userId: _userManager.GetUserId(User),
                userEmail: User.Identity?.Name,
                details: $"Search: {search} | Role: {role} | Page: {page}");

            return View(model);
        }

        public async Task<IActionResult> Orders(
            string search = "",
            string status = "",
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            const int pageSize = 10;

            var query = _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m!.Caretaker)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    o.EventType.Contains(search) ||
                    o.EventAddress.Contains(search) ||
                    (o.EventNote != null && o.EventNote.Contains(search)) ||
                    (o.ApplicationUser != null && o.ApplicationUser.FullName.Contains(search)) ||
                    (o.ApplicationUser != null && o.ApplicationUser.Email != null && o.ApplicationUser.Email.Contains(search)) ||
                    o.OrderItems.Any(oi =>
                        oi.MenuItem != null &&
                        oi.MenuItem.Name.Contains(search)) ||
                    o.OrderItems.Any(oi =>
                        oi.MenuItem != null &&
                        oi.MenuItem.Caretaker != null &&
                        oi.MenuItem.Caretaker.FullName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < exclusiveEndDate);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new AdminOrderListPageViewModel
            {
                Orders = orders,
                Search = search,
                Status = status,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                TotalPages = totalPages
            };

            await _appLogService.LogAsync(
                eventType: "AdminOrdersViewed",
                message: "Admin viewed catering requests page.",
                userId: _userManager.GetUserId(User),
                userEmail: User.Identity?.Name,
                details: $"Search: {search} | Status: {status} | StartDate: {startDate:yyyy-MM-dd} | EndDate: {endDate:yyyy-MM-dd} | Page: {page}");

            return View(model);
        }

        public async Task<IActionResult> Ratings(
            string search = "",
            int? menuRating = null,
            int? catererRating = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            const int pageSize = 10;

            var query = _context.OrderItemReviews
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                .Include(r => r.MenuItem)
                .Include(r => r.User)
                .Include(r => r.Caterer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.OrderId.ToString().Contains(search) ||
                    r.Comment.Contains(search) ||
                    (r.MenuItem != null && r.MenuItem.Name.Contains(search)) ||
                    (r.User != null && r.User.FullName.Contains(search)) ||
                    (r.User != null && r.User.Email != null && r.User.Email.Contains(search)) ||
                    (r.Caterer != null && r.Caterer.FullName.Contains(search)) ||
                    (r.Caterer != null && r.Caterer.Email != null && r.Caterer.Email.Contains(search)));
            }

            if (menuRating.HasValue)
            {
                query = query.Where(r => r.MenuRating == menuRating.Value);
            }

            if (catererRating.HasValue)
            {
                query = query.Where(r => r.CatererRating == catererRating.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.Date.AddDays(1);
                query = query.Where(r => r.CreatedAt < exclusiveEndDate);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewListItemViewModel
                {
                    ReviewId = r.Id,
                    OrderId = r.OrderId,
                    OrderItemId = r.OrderItemId,
                    CustomerName = r.User != null ? r.User.FullName : "Customer",
                    CustomerEmail = r.User != null ? r.User.Email : null,
                    CatererName = r.Caterer != null ? r.Caterer.FullName : "Caterer",
                    CatererEmail = r.Caterer != null ? r.Caterer.Email : null,
                    MenuItemName = r.MenuItem != null ? r.MenuItem.Name : "Catering Package",
                    MenuRating = r.MenuRating,
                    CatererRating = r.CatererRating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    OrderStatus = r.Order != null ? r.Order.Status : "",
                    Quantity = r.OrderItem != null ? r.OrderItem.Quantity : 0,
                    LineTotal = r.OrderItem != null ? r.OrderItem.LineTotal : 0
                })
                .ToListAsync();

            var model = new ReviewListPageViewModel
            {
                Reviews = reviews,
                Search = search,
                MenuRating = menuRating,
                CatererRating = catererRating,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                TotalPages = totalPages,
                PageTitle = "All Ratings and Comments"
            };

            return View(model);
        }

        public async Task<IActionResult> Logs(
            string search = "",
            string level = "",
            string eventType = "",
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            const int pageSize = 15;

            var query = _context.AppLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    l.Message.Contains(search) ||
                    (l.UserEmail != null && l.UserEmail.Contains(search)) ||
                    (l.Details != null && l.Details.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(l => l.Level == level);
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(l => l.EventType.Contains(eventType));
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedAt < exclusiveEndDate);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new AppLogListViewModel
            {
                Logs = logs,
                Search = search,
                Level = level,
                EventType = eventType,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = _userManager.Users.Count(),
                TotalCaretakers = _userManager.GetUsersInRoleAsync("Caretaker").Result.Count,
                TotalOrders = _context.Orders.Count(),
                TotalMenuItems = _context.MenuItems.Count()
            };

            return View(model);
        }

        public async Task<IActionResult> Logs(string search = "", string level = "", string eventType = "", int page = 1)
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
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }
    }
}
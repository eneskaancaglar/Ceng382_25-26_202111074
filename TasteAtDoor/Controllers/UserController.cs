using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var menus = await _context.MenuItems
                .Include(m => m.Caretaker)
                .Include(m => m.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .OrderByDescending(m => m.Id)
                .ToListAsync();

            return View(menus);
        }
    }
}
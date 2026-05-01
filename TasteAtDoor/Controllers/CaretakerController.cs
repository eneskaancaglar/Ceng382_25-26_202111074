using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "Caretaker")]
    public class CaretakerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CaretakerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string search = "", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            const int pageSize = 6;

            var query = _context.MenuItems
                .Where(m => m.CaretakerId == currentUser.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.Name.Contains(search) ||
                    m.Description.Contains(search) ||
                    m.LocationText.Contains(search));
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

            var model = new CaretakerMenuListViewModel
            {
                MenuItems = menus,
                Search = search,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var ownedMenuIds = await _context.MenuItems
                .Where(m => m.CaretakerId == currentUser.Id)
                .Select(m => m.Id)
                .ToListAsync();

            var relatedOrderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.MenuItem)
                .Where(oi => ownedMenuIds.Contains(oi.MenuItemId))
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync();

            var relatedReviews = await _context.OrderItemReviews
                .Where(r => r.CatererId == currentUser.Id)
                .ToListAsync();

            var model = new CaretakerDashboardViewModel
            {
                TotalMenus = ownedMenuIds.Count,
                TotalReceivedOrders = relatedOrderItems.Count,
                TotalCompletedOrders = relatedOrderItems.Count(oi => oi.Order != null && oi.Order.Status == "Completed"),
                TotalRevenue = relatedOrderItems.Sum(oi => oi.LineTotal),
                AverageCatererRating = relatedReviews.Any() ? relatedReviews.Average(r => r.CatererRating) : 0,
                TotalReviewCount = relatedReviews.Count,
                RecentOrderItems = relatedOrderItems.Take(8).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MenuCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            if (model.ImageFile is null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Image is required.");
                return View(model);
            }

            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await model.ImageFile.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var menuItem = new MenuItem
            {
                Name = model.Name,
                Price = model.Price,
                Description = model.Description,
                LocationText = model.LocationText,
                CaretakerId = currentUser.Id,
                ImageFileName = model.ImageFile.FileName,
                ImageContentType = model.ImageFile.ContentType,
                ImageData = imageBytes
            };

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.CaretakerId == currentUser.Id);

            if (menuItem is null)
            {
                return NotFound();
            }

            var model = new MenuEditViewModel
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Price = menuItem.Price,
                Description = menuItem.Description,
                LocationText = menuItem.LocationText,
                ExistingImageBase64 = Convert.ToBase64String(menuItem.ImageData),
                ExistingImageContentType = menuItem.ImageContentType
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == model.Id && m.CaretakerId == currentUser.Id);

            if (menuItem is null)
            {
                return NotFound();
            }

            menuItem.Name = model.Name;
            menuItem.Price = model.Price;
            menuItem.Description = model.Description;
            menuItem.LocationText = model.LocationText;

            if (model.ImageFile is not null && model.ImageFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await model.ImageFile.CopyToAsync(memoryStream);

                menuItem.ImageData = memoryStream.ToArray();
                menuItem.ImageFileName = model.ImageFile.FileName;
                menuItem.ImageContentType = model.ImageFile.ContentType;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.CaretakerId == currentUser.Id);

            if (menuItem is null)
            {
                return NotFound();
            }

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
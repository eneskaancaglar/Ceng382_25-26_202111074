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
    public class MenuCustomizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MenuCustomizationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int menuItemId)
        {
            var menuItem = await GetOwnedMenuItemAsync(menuItemId);

            if (menuItem is null)
            {
                return NotFound();
            }

            var groups = await _context.CustomizationGroups
                .Include(g => g.Options)
                .Where(g => g.MenuItemId == menuItemId)
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Id)
                .ToListAsync();

            var model = new MenuCustomizationIndexViewModel
            {
                MenuItemId = menuItem.Id,
                MenuName = menuItem.Name,
                Groups = groups
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateGroup(int menuItemId)
        {
            var menuItem = await GetOwnedMenuItemAsync(menuItemId);

            if (menuItem is null)
            {
                return NotFound();
            }

            return View(new CustomizationGroupCreateViewModel
            {
                MenuItemId = menuItemId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CustomizationGroupCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var menuItem = await GetOwnedMenuItemAsync(model.MenuItemId);

            if (menuItem is null)
            {
                return NotFound();
            }

            var group = new CustomizationGroup
            {
                MenuItemId = model.MenuItemId,
                Title = model.Title,
                GroupType = model.GroupType,
                IsRequired = model.IsRequired,
                DisplayOrder = model.DisplayOrder
            };

            _context.CustomizationGroups.Add(group);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Customization group created successfully.";
            return RedirectToAction(nameof(Index), new { menuItemId = model.MenuItemId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateOption(int groupId)
        {
            var group = await GetOwnedGroupAsync(groupId);

            if (group is null)
            {
                return NotFound();
            }

            ViewBag.GroupTitle = group.Title;
            ViewBag.MenuItemId = group.MenuItemId;

            return View(new CustomizationOptionCreateViewModel
            {
                CustomizationGroupId = groupId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOption(CustomizationOptionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var invalidGroup = await GetOwnedGroupAsync(model.CustomizationGroupId);
                ViewBag.GroupTitle = invalidGroup?.Title ?? "";
                ViewBag.MenuItemId = invalidGroup?.MenuItemId ?? 0;
                return View(model);
            }

            var group = await GetOwnedGroupAsync(model.CustomizationGroupId);

            if (group is null)
            {
                return NotFound();
            }

            var option = new CustomizationOption
            {
                CustomizationGroupId = model.CustomizationGroupId,
                Name = model.Name,
                PriceChange = model.PriceChange,
                IsDefault = model.IsDefault,
                DisplayOrder = model.DisplayOrder
            };

            _context.CustomizationOptions.Add(option);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Customization option created successfully.";
            return RedirectToAction(nameof(Index), new { menuItemId = group.MenuItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var group = await GetOwnedGroupAsync(id);

            if (group is null)
            {
                return NotFound();
            }

            var menuItemId = group.MenuItemId;

            _context.CustomizationGroups.Remove(group);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Customization group deleted successfully.";
            return RedirectToAction(nameof(Index), new { menuItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOption(int id)
        {
            var option = await _context.CustomizationOptions
                .Include(o => o.CustomizationGroup)
                .ThenInclude(g => g!.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (option is null || option.CustomizationGroup is null || option.CustomizationGroup.MenuItem is null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null || option.CustomizationGroup.MenuItem.CaretakerId != currentUser.Id)
            {
                return NotFound();
            }

            var menuItemId = option.CustomizationGroup.MenuItemId;

            _context.CustomizationOptions.Remove(option);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Customization option deleted successfully.";
            return RedirectToAction(nameof(Index), new { menuItemId });
        }

        private async Task<MenuItem?> GetOwnedMenuItemAsync(int menuItemId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return null;
            }

            return await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == menuItemId && m.CaretakerId == currentUser.Id);
        }

        private async Task<CustomizationGroup?> GetOwnedGroupAsync(int groupId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return null;
            }

            return await _context.CustomizationGroups
                .Include(g => g.MenuItem)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.MenuItem!.CaretakerId == currentUser.Id);
        }
    }
}
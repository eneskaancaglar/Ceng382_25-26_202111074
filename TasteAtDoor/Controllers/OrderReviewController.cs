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
    [Authorize(Roles = "User")]
    public class OrderReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppLogService _appLogService;

        public OrderReviewController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppLogService appLogService)
        {
            _context = context;
            _userManager = userManager;
            _appLogService = appLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int orderItemId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.MenuItem)
                    .ThenInclude(m => m!.Caretaker)
                .Include(oi => oi.Reviews)
                .FirstOrDefaultAsync(oi =>
                    oi.Id == orderItemId &&
                    oi.Order != null &&
                    oi.Order.ApplicationUserId == currentUser.Id);

            if (orderItem is null)
            {
                return NotFound();
            }

            if (orderItem.Reviews.Any())
            {
                TempData["Error"] = "You have already reviewed this catering package.";
                return RedirectToAction("Orders", "User");
            }

            var model = new OrderItemReviewCreateViewModel
            {
                OrderItemId = orderItem.Id,
                OrderId = orderItem.OrderId,
                MenuItemName = orderItem.MenuItem?.Name ?? "Catering Package",
                CatererName = orderItem.MenuItem?.Caretaker?.FullName
                    ?? orderItem.MenuItem?.Caretaker?.Email
                    ?? "Caterer",
                MenuRating = 5,
                CatererRating = 5
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderItemReviewCreateViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.MenuItem)
                    .ThenInclude(m => m!.Caretaker)
                .Include(oi => oi.Reviews)
                .FirstOrDefaultAsync(oi =>
                    oi.Id == model.OrderItemId &&
                    oi.Order != null &&
                    oi.Order.ApplicationUserId == currentUser.Id);

            if (orderItem is null)
            {
                return NotFound();
            }

            if (orderItem.Reviews.Any())
            {
                TempData["Error"] = "You have already reviewed this catering package.";
                return RedirectToAction("Orders", "User");
            }

            model.MenuItemName = orderItem.MenuItem?.Name ?? "Catering Package";
            model.CatererName = orderItem.MenuItem?.Caretaker?.FullName
                ?? orderItem.MenuItem?.Caretaker?.Email
                ?? "Caterer";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var catererId = orderItem.MenuItem?.CaretakerId ?? string.Empty;

            if (string.IsNullOrWhiteSpace(catererId))
            {
                ModelState.AddModelError(string.Empty, "Caterer could not be detected for this package.");
                return View(model);
            }

            var review = new OrderItemReview
            {
                OrderId = orderItem.OrderId,
                OrderItemId = orderItem.Id,
                UserId = currentUser.Id,
                MenuItemId = orderItem.MenuItemId,
                CatererId = catererId,
                MenuRating = model.MenuRating,
                CatererRating = model.CatererRating,
                Comment = model.Comment ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            _context.OrderItemReviews.Add(review);
            await _context.SaveChangesAsync();

            await _appLogService.LogAsync(
                eventType: "RatingSubmitted",
                message: "User submitted a catering package and caterer review.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {review.OrderId} | OrderItemId: {review.OrderItemId} | PackageRating: {review.MenuRating} | CatererRating: {review.CatererRating}");

            TempData["Success"] = "Your review has been submitted.";
            return RedirectToAction("Orders", "User");
        }
    }
}


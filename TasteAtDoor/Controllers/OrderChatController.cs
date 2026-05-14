using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels;

namespace TasteAtDoor.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class OrderChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderChatController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("Room")]
        [HttpGet("Room/{rawId?}")]
        public async Task<IActionResult> Room(string? rawId)
        {
            var finalOrderId = ResolveOrderId(rawId);

            if (finalOrderId <= 0)
            {
                TempData["Error"] = "Chat could not be opened because the catering request id was missing or invalid.";
                return RedirectToSafePage();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m!.Caretaker)
                .FirstOrDefaultAsync(o => o.Id == finalOrderId);

            if (order is null)
            {
                TempData["Error"] = $"Catering request #{finalOrderId} was not found.";
                return RedirectToSafePage();
            }

            var canAccess = await CanAccessOrderAsync(order, currentUser);

            if (!canAccess)
            {
                TempData["Error"] = "You are not allowed to access this catering request chat.";
                return RedirectToSafePage();
            }

            var messages = await _context.OrderChatMessages
                .Include(m => m.SenderUser)
                .Where(m => m.OrderId == finalOrderId)
                .OrderBy(m => m.SentAt)
                .Select(m => new OrderChatMessageItemViewModel
                {
                    Id = m.Id,
                    OrderId = m.OrderId,
                    SenderId = m.SenderUserId,
                    SenderName = m.SenderUser != null
                        ? m.SenderUser.FullName
                        : "User",
                    MessageText = m.Message,
                    CreatedAt = m.SentAt
                })
                .ToListAsync();

            var caterers = order.OrderItems
                .Where(oi => oi.MenuItem?.Caretaker != null)
                .Select(oi => oi.MenuItem!.Caretaker!)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToList();

            var catererName = caterers.Any()
                ? string.Join(", ", caterers.Select(c => c.FullName))
                : "Caterer";

            var catererEmail = caterers.Any()
                ? string.Join(", ", caterers
                    .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                    .Select(c => c.Email))
                : null;

            var model = new OrderChatRoomViewModel
            {
                OrderId = order.Id,
                OrderStatus = order.Status,
                OrderDate = order.OrderDate,
                CurrentUserId = currentUser.Id,
                CustomerName = order.ApplicationUser?.FullName ?? "Customer",
                CustomerEmail = order.ApplicationUser?.Email,
                CatererName = catererName,
                CatererEmail = catererEmail,
                Messages = messages
            };

            return View(model);
        }

        [HttpPost("SendMessage")]
        [HttpPost("SendMessage/{rawId?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string? rawId, string messageText)
        {
            var finalOrderId = ResolveOrderId(rawId);

            if (finalOrderId <= 0)
            {
                TempData["ChatError"] = "Message could not be sent because the request id was missing.";
                return RedirectToSafePage();
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                TempData["ChatError"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Room), new { rawId = finalOrderId });
            }

            if (messageText.Length > 1000)
            {
                TempData["ChatError"] = "Message cannot be longer than 1000 characters.";
                return RedirectToAction(nameof(Room), new { rawId = finalOrderId });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == finalOrderId);

            if (order is null)
            {
                TempData["ChatError"] = "Catering request was not found.";
                return RedirectToSafePage();
            }

            var canAccess = await CanAccessOrderAsync(order, currentUser);

            if (!canAccess)
            {
                TempData["ChatError"] = "You are not allowed to send messages in this catering request chat.";
                return RedirectToSafePage();
            }

            var senderRole = await GetSenderRoleAsync(currentUser);

            var message = new OrderChatMessage
            {
                OrderId = order.Id,
                SenderUserId = currentUser.Id,
                SenderRole = senderRole,
                Message = messageText.Trim(),
                SentAt = DateTime.Now
            };

            _context.OrderChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Room), new { rawId = finalOrderId });
        }

        private int ResolveOrderId(string? rawId)
        {
            if (int.TryParse(rawId, out var routeId) && routeId > 0)
            {
                return routeId;
            }

            if (int.TryParse(Request.Query["id"], out var queryId) && queryId > 0)
            {
                return queryId;
            }

            if (int.TryParse(Request.Query["orderId"], out var queryOrderId) && queryOrderId > 0)
            {
                return queryOrderId;
            }

            if (Request.HasFormContentType &&
                int.TryParse(Request.Form["orderId"], out var formOrderId) &&
                formOrderId > 0)
            {
                return formOrderId;
            }

            return 0;
        }

        private IActionResult RedirectToSafePage()
        {
            if (User.IsInRole("User"))
            {
                return RedirectToAction("Orders", "User");
            }

            if (User.IsInRole("Caretaker"))
            {
                return RedirectToAction("Dashboard", "Caretaker");
            }

            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Orders", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<bool> CanAccessOrderAsync(Order order, ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return true;
            }

            if (order.ApplicationUserId == user.Id)
            {
                return true;
            }

            var isCatererForThisOrder = order.OrderItems.Any(oi =>
                oi.MenuItem != null &&
                oi.MenuItem.CaretakerId == user.Id);

            return isCatererForThisOrder;
        }

        private async Task<string> GetSenderRoleAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return "Admin";
            }

            if (await _userManager.IsInRoleAsync(user, "Caretaker"))
            {
                return "Caterer";
            }

            return "Customer";
        }
    }
}
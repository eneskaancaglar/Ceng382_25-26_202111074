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

        [HttpGet("Room/{id:int?}")]
        public async Task<IActionResult> Room(int? id, int? orderId)
        {
            int finalOrderId = orderId ?? id ?? 0;

            if (finalOrderId <= 0)
            {
                TempData["Error"] = "Chat could not be opened because the request id was missing.";

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
                return NotFound($"Catering request #{finalOrderId} was not found.");
            }

            var canAccess = await CanAccessOrderAsync(order, currentUser);

            if (!canAccess)
            {
                return Forbid();
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int orderId, string messageText)
        {
            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    error = "Order id is required."
                });
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest(new
                {
                    error = "Message cannot be empty."
                });
            }

            if (messageText.Length > 1000)
            {
                return BadRequest(new
                {
                    error = "Message cannot be longer than 1000 characters."
                });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Unauthorized();
            }

            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null)
            {
                return NotFound(new
                {
                    error = "Catering request was not found."
                });
            }

            var canAccess = await CanAccessOrderAsync(order, currentUser);

            if (!canAccess)
            {
                return Forbid();
            }

            var message = new OrderChatMessage
            {
                OrderId = order.Id,
                SenderUserId = currentUser.Id,
                Message = messageText.Trim(),
                SentAt = DateTime.Now
            };

            _context.OrderChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var senderName = string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.Email ?? "User"
                : currentUser.FullName;

            return Json(new
            {
                id = message.Id,
                orderId = message.OrderId,
                senderId = message.SenderUserId,
                senderName = senderName,
                messageText = message.Message,
                createdAt = message.SentAt,
                createdAtText = message.SentAt.ToString("dd MMM yyyy HH:mm")
            });
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
    }
}
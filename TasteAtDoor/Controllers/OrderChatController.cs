using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Services;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "User,Caretaker,Admin")]
    public class OrderChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppLogService _appLogService;

        public OrderChatController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppLogService appLogService)
        {
            _context = context;
            _userManager = userManager;
            _appLogService = appLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Room(int orderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await LoadOrderAsync(orderId);

            if (order is null)
            {
                return NotFound();
            }

            if (order.Status != "Completed")
            {
                TempData["Error"] = "Chat is available only after the purchase is completed.";
                return RedirectToAction("Index", "Home");
            }

            if (!CanAccessOrder(currentUser, order))
            {
                return Forbid();
            }

            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.CurrentUserName = currentUser.FullName;
            ViewBag.CurrentUserRole = GetCurrentRole();
            ViewBag.RestaurantNames = string.Join(", ",
                order.OrderItems
                    .Where(oi => oi.MenuItem?.Caretaker != null)
                    .Select(oi => oi.MenuItem!.Caretaker!.FullName)
                    .Distinct());

            await _appLogService.LogAsync(
                eventType: "OrderChatOpened",
                message: "Order chat room opened.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id}");

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Messages(int orderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Unauthorized();
            }

            var order = await LoadOrderAsync(orderId);

            if (order is null)
            {
                return NotFound();
            }

            if (!CanAccessOrder(currentUser, order))
            {
                return Forbid();
            }

            var messages = await _context.OrderChatMessages
                .Include(m => m.SenderUser)
                .Where(m => m.OrderId == orderId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    id = m.Id,
                    senderUserId = m.SenderUserId,
                    senderName = m.SenderUser != null ? m.SenderUser.FullName : "User",
                    senderRole = m.SenderRole,
                    message = m.Message,
                    sentAt = m.SentAt.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int orderId, string message)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Unauthorized();
            }

            var order = await LoadOrderAsync(orderId);

            if (order is null)
            {
                return NotFound();
            }

            if (!CanAccessOrder(currentUser, order))
            {
                return Forbid();
            }

            if (order.Status != "Completed")
            {
                return BadRequest(new { success = false, error = "Chat is available only for completed orders." });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { success = false, error = "Message cannot be empty." });
            }

            var chatMessage = new OrderChatMessage
            {
                OrderId = order.Id,
                SenderUserId = currentUser.Id,
                SenderRole = GetCurrentRole(),
                Message = message.Trim(),
                SentAt = DateTime.Now
            };

            _context.OrderChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await _appLogService.LogAsync(
                eventType: "OrderChatMessageSent",
                message: "Order chat message sent.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id}");

            return Json(new { success = true });
        }

        private async Task<Order?> LoadOrderAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m!.Caretaker)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        private bool CanAccessOrder(ApplicationUser currentUser, Order order)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            if (User.IsInRole("User") && order.ApplicationUserId == currentUser.Id)
            {
                return true;
            }

            if (User.IsInRole("Caretaker"))
            {
                return order.OrderItems.Any(oi =>
                    oi.MenuItem != null &&
                    oi.MenuItem.CaretakerId == currentUser.Id);
            }

            return false;
        }

        private string GetCurrentRole()
        {
            if (User.IsInRole("Admin"))
            {
                return "Admin";
            }

            if (User.IsInRole("Caretaker"))
            {
                return "Restaurant";
            }

            return "Customer";
        }
    }
}
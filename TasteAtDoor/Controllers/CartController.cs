using System.Text;
using System.Text.Json;
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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppLogService _appLogService;
        private readonly IEmailService _emailService;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppLogService appLogService,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _appLogService = appLogService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return View(new CheckoutViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CartJson))
            {
                ModelState.AddModelError(string.Empty, "Your cart is empty.");
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<CartItem> clientCart = new();

            if (!string.IsNullOrWhiteSpace(model.CartJson))
            {
                try
                {
                    clientCart = JsonSerializer.Deserialize<List<CartItem>>(model.CartJson, jsonOptions) ?? new List<CartItem>();
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Invalid cart data.");
                }
            }

            if (!clientCart.Any())
            {
                ModelState.AddModelError(string.Empty, "Your cart is empty.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuIds = clientCart.Select(c => c.MenuItemId).Distinct().ToList();

            var menuItems = await _context.MenuItems
                .Where(m => menuIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            var order = new Order
            {
                ApplicationUserId = currentUser.Id,
                OrderDate = DateTime.Now,
                Status = "Completed"
            };

            decimal totalPrice = 0;

            foreach (var cartItem in clientCart)
            {
                if (!menuItems.TryGetValue(cartItem.MenuItemId, out var dbMenuItem))
                {
                    continue;
                }

                if (cartItem.Quantity <= 0)
                {
                    continue;
                }

                var selectedOptionIds = cartItem.SelectedOptions
                    .Select(o => o.OptionId)
                    .Distinct()
                    .ToList();

                var dbOptions = await _context.CustomizationOptions
                    .Include(o => o.CustomizationGroup)
                    .Where(o =>
                        selectedOptionIds.Contains(o.Id) &&
                        o.CustomizationGroup != null &&
                        o.CustomizationGroup.MenuItemId == dbMenuItem.Id)
                    .ToListAsync();

                var finalUnitPrice = dbMenuItem.Price + dbOptions.Sum(o => o.PriceChange);
                var lineTotal = finalUnitPrice * cartItem.Quantity;

                var orderItem = new OrderItem
                {
                    MenuItemId = dbMenuItem.Id,
                    Quantity = cartItem.Quantity,
                    BaseUnitPrice = dbMenuItem.Price,
                    FinalUnitPrice = finalUnitPrice,
                    LineTotal = lineTotal
                };

                foreach (var option in dbOptions)
                {
                    orderItem.SelectedCustomizations.Add(new OrderItemCustomization
                    {
                        CustomizationGroupId = option.CustomizationGroupId,
                        CustomizationOptionId = option.Id,
                        GroupTitle = option.CustomizationGroup?.Title ?? string.Empty,
                        OptionName = cartItem.SelectedOptions
                            .FirstOrDefault(x => x.OptionId == option.Id)?.OptionName ?? option.Name,
                        PriceChange = option.PriceChange
                    });
                }

                order.OrderItems.Add(orderItem);
                totalPrice += lineTotal;
            }

            if (!order.OrderItems.Any())
            {
                ModelState.AddModelError(string.Empty, "No valid items were found in the cart.");
                return View(model);
            }

            order.TotalPrice = totalPrice;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _appLogService.LogAsync(
                eventType: "PaymentAction",
                message: "Simulated payment completed successfully.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id} | Total: {order.TotalPrice:0.00}");

            await _appLogService.LogAsync(
                eventType: "OrderCreated",
                message: "Order created successfully.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id} | ItemCount: {order.OrderItems.Count}");

            var savedOrder = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m!.Caretaker)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedCustomizations)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (savedOrder is not null)
            {
                if (!string.IsNullOrWhiteSpace(savedOrder.ApplicationUser?.Email))
                {
                    await _emailService.SendAsync(
                        savedOrder.ApplicationUser.Email!,
                        $"Taste At Door - Order Confirmation #{savedOrder.Id}",
                        BuildUserOrderEmail(savedOrder));
                }

                var caretakerEmails = savedOrder.OrderItems
                    .Where(oi => oi.MenuItem?.Caretaker?.Email != null)
                    .Select(oi => oi.MenuItem!.Caretaker!.Email!)
                    .Distinct()
                    .ToList();

                foreach (var caretakerEmail in caretakerEmails)
                {
                    var caretakerBody = BuildCaretakerOrderEmail(savedOrder, caretakerEmail);

                    if (!string.IsNullOrWhiteSpace(caretakerBody))
                    {
                        await _emailService.SendAsync(
                            caretakerEmail,
                            $"Taste At Door - New Order #{savedOrder.Id}",
                            caretakerBody);
                    }
                }
            }

            return RedirectToAction(nameof(Success), new { orderId = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == currentUser.Id);

            if (order is null)
            {
                return NotFound();
            }

            var model = new OrderSuccessViewModel
            {
                OrderId = order.Id,
                TotalPrice = order.TotalPrice,
                OrderDate = order.OrderDate,
                Status = order.Status
            };

            return View(model);
        }

        private string BuildUserOrderEmail(Order order)
        {
            var sb = new StringBuilder();

            sb.Append($"<p>Hello {(order.ApplicationUser?.FullName ?? "User")},</p>");
            sb.Append($"<p>Your order <strong>#{order.Id}</strong> has been completed successfully.</p>");
            sb.Append($"<p><strong>Order Date:</strong> {order.OrderDate}</p>");
            sb.Append($"<p><strong>Total Price:</strong> {order.TotalPrice:0.00}</p>");
            sb.Append("<h3>Order Items</h3>");
            sb.Append("<ul>");

            foreach (var item in order.OrderItems)
            {
                sb.Append("<li>");
                sb.Append($"{item.MenuItem?.Name ?? "Menu Item"} - Qty: {item.Quantity} - Total: {item.LineTotal:0.00}");

                if (item.SelectedCustomizations.Any())
                {
                    sb.Append("<ul>");
                    foreach (var customization in item.SelectedCustomizations)
                    {
                        sb.Append($"<li>{customization.GroupTitle}: {customization.OptionName}</li>");
                    }
                    sb.Append("</ul>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
            sb.Append("<p>Thank you for using Taste At Door.</p>");

            return sb.ToString();
        }

        private string BuildCaretakerOrderEmail(Order order, string caretakerEmail)
        {
            var relatedItems = order.OrderItems
                .Where(oi => oi.MenuItem?.Caretaker?.Email == caretakerEmail)
                .ToList();

            if (!relatedItems.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            sb.Append("<p>Hello,</p>");
            sb.Append($"<p>A new order <strong>#{order.Id}</strong> has been placed for your menu items.</p>");
            sb.Append($"<p><strong>Customer:</strong> {(order.ApplicationUser?.FullName ?? "Customer")} ({order.ApplicationUser?.Email ?? "No email"})</p>");
            sb.Append("<h3>Your Items in This Order</h3>");
            sb.Append("<ul>");

            foreach (var item in relatedItems)
            {
                sb.Append("<li>");
                sb.Append($"{item.MenuItem?.Name ?? "Menu Item"} - Qty: {item.Quantity} - Line Total: {item.LineTotal:0.00}");

                if (item.SelectedCustomizations.Any())
                {
                    sb.Append("<ul>");
                    foreach (var customization in item.SelectedCustomizations)
                    {
                        sb.Append($"<li>{customization.GroupTitle}: {customization.OptionName}</li>");
                    }
                    sb.Append("</ul>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
            sb.Append("<p>Please review the order in the system.</p>");

            return sb.ToString();
        }
    }
}
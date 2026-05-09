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
        private readonly IGoogleMapsService _googleMapsService;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppLogService appLogService,
            IEmailService emailService,
            IGoogleMapsService googleMapsService)
        {
            _context = context;
            _userManager = userManager;
            _appLogService = appLogService;
            _emailService = emailService;
            _googleMapsService = googleMapsService;
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
                    clientCart = JsonSerializer.Deserialize<List<CartItem>>(model.CartJson, jsonOptions)
                                 ?? new List<CartItem>();
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

            var menuIds = clientCart
                .Select(c => c.MenuItemId)
                .Distinct()
                .ToList();

            var menuItems = await _context.MenuItems
                .Include(m => m.Caretaker)
                .Where(m => menuIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Please save your location before checkout.");
                return View(model);
            }

            foreach (var cartItem in clientCart)
            {
                if (!menuItems.TryGetValue(cartItem.MenuItemId, out var menuForDistance))
                {
                    continue;
                }

                if (menuForDistance.Caretaker is null ||
                    !menuForDistance.Caretaker.Latitude.HasValue ||
                    !menuForDistance.Caretaker.Longitude.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Restaurant location is missing for '{menuForDistance.Name}'.");

                    return View(model);
                }

                var distanceKm = await _googleMapsService.GetRouteDistanceKmAsync(
                    currentUser.Latitude.Value,
                    currentUser.Longitude.Value,
                    menuForDistance.Caretaker.Latitude.Value,
                    menuForDistance.Caretaker.Longitude.Value);

                if (!distanceKm.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Distance for '{menuForDistance.Name}' could not be verified with Google Maps. Please try again.");

                    return View(model);
                }

                if (distanceKm.Value > 5)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"You cannot order '{menuForDistance.Name}' because the restaurant is {distanceKm.Value:0.0} km away by Google Maps route distance. Maximum distance is 5 km.");

                    return View(model);
                }
            }

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
                try
                {
                    if (!string.IsNullOrWhiteSpace(savedOrder.ApplicationUser?.Email))
                    {
                        await _emailService.SendAsync(
                            savedOrder.ApplicationUser.Email!,
                            $"Taste At Door - Order Confirmation #{savedOrder.Id}",
                            BuildUserOrderEmail(savedOrder));

                        await _appLogService.LogAsync(
                            eventType: "OrderEmailSentToUser",
                            message: "Order confirmation email sent to customer.",
                            userId: currentUser.Id,
                            userEmail: currentUser.Email,
                            details: $"OrderId: {savedOrder.Id} | To: {savedOrder.ApplicationUser.Email}");
                    }
                    else
                    {
                        await _appLogService.LogAsync(
                            eventType: "OrderEmailSkippedForUser",
                            message: "Customer email is missing, order email was not sent.",
                            level: "Warning",
                            userId: currentUser.Id,
                            userEmail: currentUser.Email,
                            details: $"OrderId: {savedOrder.Id}");
                    }
                }
                catch (Exception ex)
                {
                    await _appLogService.LogAsync(
                        eventType: "OrderEmailFailedForUser",
                        message: "Order confirmation email could not be sent to customer.",
                        level: "Error",
                        userId: currentUser.Id,
                        userEmail: currentUser.Email,
                        details: $"OrderId: {savedOrder.Id} | Error: {ex.Message}");
                }

                var caretakerGroups = savedOrder.OrderItems
                    .Where(oi =>
                        oi.MenuItem?.Caretaker != null &&
                        !string.IsNullOrWhiteSpace(oi.MenuItem.Caretaker.Email))
                    .GroupBy(oi => oi.MenuItem!.CaretakerId)
                    .ToList();

                foreach (var caretakerGroup in caretakerGroups)
                {
                    var caretaker = caretakerGroup.First().MenuItem!.Caretaker!;

                    if (string.IsNullOrWhiteSpace(caretaker.Email))
                    {
                        await _appLogService.LogAsync(
                            eventType: "OrderEmailSkippedForCaterer",
                            message: "Caterer email is missing, order email was not sent.",
                            level: "Warning",
                            userId: currentUser.Id,
                            userEmail: currentUser.Email,
                            details: $"OrderId: {savedOrder.Id} | CatererId: {caretaker.Id}");

                        continue;
                    }

                    var relatedItems = caretakerGroup.ToList();
                    var caretakerBody = BuildCaretakerOrderEmail(savedOrder, caretaker, relatedItems);

                    try
                    {
                        await _emailService.SendAsync(
                            caretaker.Email!,
                            $"Taste At Door - New Order #{savedOrder.Id}",
                            caretakerBody);

                        await _appLogService.LogAsync(
                            eventType: "OrderEmailSentToCaterer",
                            message: "New order email sent to caterer.",
                            userId: currentUser.Id,
                            userEmail: currentUser.Email,
                            details: $"OrderId: {savedOrder.Id} | Restaurant: {caretaker.FullName} | To: {caretaker.Email}");
                    }
                    catch (Exception ex)
                    {
                        await _appLogService.LogAsync(
                            eventType: "OrderEmailFailedForCaterer",
                            message: "New order email could not be sent to caterer.",
                            level: "Error",
                            userId: currentUser.Id,
                            userEmail: currentUser.Email,
                            details: $"OrderId: {savedOrder.Id} | Restaurant: {caretaker.FullName} | To: {caretaker.Email} | Error: {ex.Message}");
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
            static string H(string? value)
            {
                return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
            }

            var sb = new StringBuilder();

            sb.Append($"<p>Hello <strong>{H(order.ApplicationUser?.FullName ?? "User")}</strong>,</p>");
            sb.Append($"<p>Your order <strong>#{order.Id}</strong> has been completed successfully.</p>");

            sb.Append("<h3>Order Summary</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Order ID:</strong> #{order.Id}</li>");
            sb.Append($"<li><strong>Order Date:</strong> {order.OrderDate:dd.MM.yyyy HH:mm}</li>");
            sb.Append($"<li><strong>Status:</strong> {H(order.Status)}</li>");
            sb.Append($"<li><strong>Total Price:</strong> {order.TotalPrice:0.00}</li>");
            sb.Append("</ul>");

            sb.Append("<h3>Ordered Items</h3>");
            sb.Append("<table style='border-collapse:collapse;width:100%;font-family:Arial,sans-serif;'>");
            sb.Append("<thead>");
            sb.Append("<tr>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Restaurant</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Menu Item</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Quantity</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Customizations</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Line Total</th>");
            sb.Append("</tr>");
            sb.Append("</thead>");
            sb.Append("<tbody>");

            foreach (var item in order.OrderItems)
            {
                var restaurantName = item.MenuItem?.Caretaker?.FullName ?? "Restaurant";
                var menuName = item.MenuItem?.Name ?? "Menu Item";

                var customizationText = "None";

                if (item.SelectedCustomizations.Any())
                {
                    customizationText = string.Join("<br />",
                        item.SelectedCustomizations.Select(c =>
                            $"{H(c.GroupTitle)}: {H(c.OptionName)}" +
                            (c.PriceChange != 0 ? $" ({c.PriceChange:0.00})" : "")));
                }

                sb.Append("<tr>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{H(restaurantName)}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{H(menuName)}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.Quantity}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{customizationText}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.LineTotal:0.00}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
            sb.Append("</table>");

            var restaurantNames = order.OrderItems
                .Where(i => i.MenuItem?.Caretaker != null)
                .Select(i => i.MenuItem!.Caretaker!.FullName)
                .Distinct()
                .ToList();

            if (restaurantNames.Any())
            {
                sb.Append("<h3>Restaurant Information</h3>");
                sb.Append("<ul>");

                foreach (var restaurantName in restaurantNames)
                {
                    sb.Append($"<li>{H(restaurantName)}</li>");
                }

                sb.Append("</ul>");
            }

            sb.Append("<p>Thank you for using TasteAtDoor.</p>");

            return sb.ToString();
        }

        private string BuildCaretakerOrderEmail(
    Order order,
    ApplicationUser caretaker,
    List<OrderItem> relatedItems)
        {
            static string H(string? value)
            {
                return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
            }

            var restaurantTotal = relatedItems.Sum(i => i.LineTotal);

            var sb = new StringBuilder();

            sb.Append($"<p>Hello <strong>{H(caretaker.FullName)}</strong>,</p>");
            sb.Append($"<p>A new order <strong>#{order.Id}</strong> has been placed for your restaurant.</p>");

            sb.Append("<h3>Restaurant Information</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Restaurant:</strong> {H(caretaker.FullName)}</li>");
            sb.Append($"<li><strong>Restaurant Email:</strong> {H(caretaker.Email)}</li>");

            if (!string.IsNullOrWhiteSpace(caretaker.Address))
            {
                sb.Append($"<li><strong>Restaurant Address:</strong> {H(caretaker.Address)}</li>");
            }

            sb.Append("</ul>");

            sb.Append("<h3>Customer Information</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Customer:</strong> {H(order.ApplicationUser?.FullName ?? "Customer")}</li>");
            sb.Append($"<li><strong>Customer Email:</strong> {H(order.ApplicationUser?.Email ?? "No email")}</li>");
            sb.Append("</ul>");

            sb.Append("<h3>Order Summary</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Order ID:</strong> #{order.Id}</li>");
            sb.Append($"<li><strong>Order Date:</strong> {order.OrderDate:dd.MM.yyyy HH:mm}</li>");
            sb.Append($"<li><strong>Status:</strong> {H(order.Status)}</li>");
            sb.Append($"<li><strong>Your Restaurant Total:</strong> {restaurantTotal:0.00}</li>");
            sb.Append("</ul>");

            sb.Append("<h3>Your Items in This Order</h3>");
            sb.Append("<table style='border-collapse:collapse;width:100%;font-family:Arial,sans-serif;'>");
            sb.Append("<thead>");
            sb.Append("<tr>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Menu Item</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Quantity</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Customizations</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Line Total</th>");
            sb.Append("</tr>");
            sb.Append("</thead>");
            sb.Append("<tbody>");

            foreach (var item in relatedItems)
            {
                var menuName = item.MenuItem?.Name ?? "Menu Item";

                var customizationText = "None";

                if (item.SelectedCustomizations.Any())
                {
                    customizationText = string.Join("<br />",
                        item.SelectedCustomizations.Select(c =>
                            $"{H(c.GroupTitle)}: {H(c.OptionName)}" +
                            (c.PriceChange != 0 ? $" ({c.PriceChange:0.00})" : "")));
                }

                sb.Append("<tr>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{H(menuName)}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.Quantity}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{customizationText}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.LineTotal:0.00}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
            sb.Append("</table>");

            sb.Append("<p>Please login to TasteAtDoor to review the order and communicate with the customer.</p>");

            return sb.ToString();
        }
    }
}
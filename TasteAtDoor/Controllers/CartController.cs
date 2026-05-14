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
        public async Task<IActionResult> Checkout()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var model = new CheckoutViewModel
            {
                EventType = "Wedding",
                EventDate = DateTime.Now.AddDays(7),
                GuestCount = 50,
                EventAddress = currentUser?.Address ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CartJson))
            {
                ModelState.AddModelError(string.Empty, "Your package cart is empty.");
            }

            if (model.EventDate.HasValue && model.EventDate.Value < DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.EventDate), "Event date cannot be in the past.");
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

            clientCart = clientCart
                .Where(c => c.MenuItemId > 0 && c.Quantity > 0)
                .ToList();

            if (!clientCart.Any())
            {
                ModelState.AddModelError(string.Empty, "Your package cart is empty.");
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

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Please save your location before checkout.");
                return View(model);
            }

            var menuIds = clientCart
                .Select(c => c.MenuItemId)
                .Distinct()
                .ToList();

            var menuItems = await _context.MenuItems
                .Include(m => m.Caretaker)
                .Where(m => menuIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            const double maxAllowedDistanceKm = 15;

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
                        $"Caterer location is missing for '{menuForDistance.Name}'.");

                    return View(model);
                }

                double distanceKm;

                var googleDistanceKm = await _googleMapsService.GetRouteDistanceKmAsync(
                    currentUser.Latitude.Value,
                    currentUser.Longitude.Value,
                    menuForDistance.Caretaker.Latitude.Value,
                    menuForDistance.Caretaker.Longitude.Value);

                if (googleDistanceKm.HasValue)
                {
                    distanceKm = googleDistanceKm.Value;
                }
                else
                {
                    distanceKm = CalculateFallbackDistanceKmForCheckout(
                        currentUser.Latitude.Value,
                        currentUser.Longitude.Value,
                        menuForDistance.Caretaker.Latitude.Value,
                        menuForDistance.Caretaker.Longitude.Value);
                }

                if (distanceKm > maxAllowedDistanceKm)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"You cannot request '{menuForDistance.Name}' because this caterer is {distanceKm:0.0} km away. Maximum allowed distance is {maxAllowedDistanceKm:0} km.");

                    return View(model);
                }
            }

            var totalGuestCountFromCart = clientCart.Sum(c => c.Quantity);

            if (model.GuestCount <= 0)
            {
                model.GuestCount = totalGuestCountFromCart;
            }

            var order = new Order
            {
                ApplicationUserId = currentUser.Id,
                OrderDate = DateTime.Now,
                Status = "Pending",
                EventType = model.EventType.Trim(),
                EventDate = model.EventDate,
                GuestCount = model.GuestCount,
                EventAddress = model.EventAddress.Trim(),
                EventNote = model.EventNote?.Trim()
            };

            decimal totalPrice = 0;

            foreach (var cartItem in clientCart)
            {
                if (!menuItems.TryGetValue(cartItem.MenuItemId, out var dbMenuItem))
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
                var guestCount = cartItem.Quantity;
                var lineTotal = finalUnitPrice * guestCount;

                var orderItem = new OrderItem
                {
                    MenuItemId = dbMenuItem.Id,
                    Quantity = guestCount,
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
                ModelState.AddModelError(string.Empty, "No valid catering packages were found in the cart.");
                return View(model);
            }

            order.TotalPrice = totalPrice;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _appLogService.LogAsync(
                eventType: "PaymentAction",
                message: "Simulated catering payment completed successfully.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id} | Total: {order.TotalPrice:0.00} | Guests: {order.GuestCount}");

            await _appLogService.LogAsync(
                eventType: "CateringOrderCreated",
                message: "Catering request created successfully.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id} | PackageCount: {order.OrderItems.Count} | EventType: {order.EventType} | EventDate: {order.EventDate}");

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
                await SendOrderEmailsAsync(savedOrder, currentUser);
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
                Status = order.Status,
                EventType = order.EventType,
                EventDate = order.EventDate,
                GuestCount = order.GuestCount,
                EventAddress = order.EventAddress
            };

            return View(model);
        }

        private async Task SendOrderEmailsAsync(Order savedOrder, ApplicationUser currentUser)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(savedOrder.ApplicationUser?.Email))
                {
                    await _emailService.SendAsync(
                        savedOrder.ApplicationUser.Email!,
                        $"TasteAtDoor - Catering Request Confirmation #{savedOrder.Id}",
                        BuildUserOrderEmail(savedOrder));

                    await _appLogService.LogAsync(
                        eventType: "OrderEmailSentToUser",
                        message: "Catering request confirmation email sent to customer.",
                        userId: currentUser.Id,
                        userEmail: currentUser.Email,
                        details: $"OrderId: {savedOrder.Id} | To: {savedOrder.ApplicationUser.Email}");
                }
                else
                {
                    await _appLogService.LogAsync(
                        eventType: "OrderEmailSkippedForUser",
                        message: "Customer email is missing, catering email was not sent.",
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
                    message: "Catering request confirmation email could not be sent to customer.",
                    level: "Error",
                    userId: currentUser.Id,
                    userEmail: currentUser.Email,
                    details: $"OrderId: {savedOrder.Id} | Error: {ex.Message}");
            }

            var catererGroups = savedOrder.OrderItems
                .Where(oi =>
                    oi.MenuItem?.Caretaker != null &&
                    !string.IsNullOrWhiteSpace(oi.MenuItem.Caretaker.Email))
                .GroupBy(oi => oi.MenuItem!.CaretakerId)
                .ToList();

            foreach (var catererGroup in catererGroups)
            {
                var caterer = catererGroup.First().MenuItem!.Caretaker!;

                if (string.IsNullOrWhiteSpace(caterer.Email))
                {
                    await _appLogService.LogAsync(
                        eventType: "OrderEmailSkippedForCaterer",
                        message: "Caterer email is missing, catering request email was not sent.",
                        level: "Warning",
                        userId: currentUser.Id,
                        userEmail: currentUser.Email,
                        details: $"OrderId: {savedOrder.Id} | CatererId: {caterer.Id}");

                    continue;
                }

                var relatedItems = catererGroup.ToList();
                var catererBody = BuildCatererOrderEmail(savedOrder, caterer, relatedItems);

                try
                {
                    await _emailService.SendAsync(
                        caterer.Email!,
                        $"TasteAtDoor - New Catering Request #{savedOrder.Id}",
                        catererBody);

                    await _appLogService.LogAsync(
                        eventType: "OrderEmailSentToCaterer",
                        message: "New catering request email sent to caterer.",
                        userId: currentUser.Id,
                        userEmail: currentUser.Email,
                        details: $"OrderId: {savedOrder.Id} | Caterer: {caterer.FullName} | To: {caterer.Email}");
                }
                catch (Exception ex)
                {
                    await _appLogService.LogAsync(
                        eventType: "OrderEmailFailedForCaterer",
                        message: "New catering request email could not be sent to caterer.",
                        level: "Error",
                        userId: currentUser.Id,
                        userEmail: currentUser.Email,
                        details: $"OrderId: {savedOrder.Id} | Caterer: {caterer.FullName} | To: {caterer.Email} | Error: {ex.Message}");
                }
            }
        }

        private string BuildUserOrderEmail(Order order)
        {
            static string H(string? value)
            {
                return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
            }

            var sb = new StringBuilder();

            sb.Append($"<p>Hello <strong>{H(order.ApplicationUser?.FullName ?? "Customer")}</strong>,</p>");
            sb.Append($"<p>Your catering request <strong>#{order.Id}</strong> has been created successfully.</p>");

            sb.Append("<h3>Event Information</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Event Type:</strong> {H(order.EventType)}</li>");
            sb.Append($"<li><strong>Event Date:</strong> {(order.EventDate.HasValue ? order.EventDate.Value.ToString("dd.MM.yyyy HH:mm") : "Not specified")}</li>");
            sb.Append($"<li><strong>Total Guest Count:</strong> {order.GuestCount}</li>");
            sb.Append($"<li><strong>Event Address:</strong> {H(order.EventAddress)}</li>");

            if (!string.IsNullOrWhiteSpace(order.EventNote))
            {
                sb.Append($"<li><strong>Event Note:</strong> {H(order.EventNote)}</li>");
            }

            sb.Append("</ul>");

            sb.Append("<h3>Request Summary</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Request ID:</strong> #{order.Id}</li>");
            sb.Append($"<li><strong>Request Date:</strong> {order.OrderDate:dd.MM.yyyy HH:mm}</li>");
            sb.Append($"<li><strong>Status:</strong> {H(order.Status)}</li>");
            sb.Append($"<li><strong>Total Estimated Price:</strong> {order.TotalPrice:0.00} ₺</li>");
            sb.Append("</ul>");

            sb.Append("<h3>Selected Catering Packages</h3>");
            sb.Append("<table style='border-collapse:collapse;width:100%;font-family:Arial,sans-serif;'>");
            sb.Append("<thead>");
            sb.Append("<tr>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Caterer</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Package</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Guest Count</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Options</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Line Total</th>");
            sb.Append("</tr>");
            sb.Append("</thead>");
            sb.Append("<tbody>");

            foreach (var item in order.OrderItems)
            {
                var catererName = item.MenuItem?.Caretaker?.FullName ?? "Caterer";
                var packageName = item.MenuItem?.Name ?? "Catering Package";

                var customizationText = "None";

                if (item.SelectedCustomizations.Any())
                {
                    customizationText = string.Join("<br />",
                        item.SelectedCustomizations.Select(c =>
                            $"{H(c.GroupTitle)}: {H(c.OptionName)}" +
                            (c.PriceChange != 0 ? $" ({c.PriceChange:0.00} ₺ / person)" : "")));
                }

                sb.Append("<tr>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{H(catererName)}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{H(packageName)}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.Quantity}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{customizationText}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.LineTotal:0.00} ₺</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
            sb.Append("</table>");

            var catererNames = order.OrderItems
                .Where(i => i.MenuItem?.Caretaker != null)
                .Select(i => i.MenuItem!.Caretaker!.FullName)
                .Distinct()
                .ToList();

            if (catererNames.Any())
            {
                sb.Append("<h3>Caterer Information</h3>");
                sb.Append("<ul>");

                foreach (var catererName in catererNames)
                {
                    sb.Append($"<li>{H(catererName)}</li>");
                }

                sb.Append("</ul>");
            }

            sb.Append("<p>Thank you for using TasteAtDoor Catering.</p>");

            return sb.ToString();
        }

        private string BuildCatererOrderEmail(
            Order order,
            ApplicationUser caterer,
            List<OrderItem> relatedItems)
        {
            static string H(string? value)
            {
                return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
            }

            var catererTotal = relatedItems.Sum(i => i.LineTotal);

            var sb = new StringBuilder();

            sb.Append($"<p>Hello <strong>{H(caterer.FullName)}</strong>,</p>");
            sb.Append($"<p>A new catering request <strong>#{order.Id}</strong> has been placed for your company.</p>");

            sb.Append("<h3>Caterer Information</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Caterer:</strong> {H(caterer.FullName)}</li>");
            sb.Append($"<li><strong>Caterer Email:</strong> {H(caterer.Email)}</li>");

            if (!string.IsNullOrWhiteSpace(caterer.Address))
            {
                sb.Append($"<li><strong>Service Area:</strong> {H(caterer.Address)}</li>");
            }

            sb.Append("</ul>");

            sb.Append("<h3>Customer Information</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Customer:</strong> {H(order.ApplicationUser?.FullName ?? "Customer")}</li>");
            sb.Append($"<li><strong>Customer Email:</strong> {H(order.ApplicationUser?.Email ?? "No email")}</li>");
            sb.Append("</ul>");

            sb.Append("<h3>Event Information</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Event Type:</strong> {H(order.EventType)}</li>");
            sb.Append($"<li><strong>Event Date:</strong> {(order.EventDate.HasValue ? order.EventDate.Value.ToString("dd.MM.yyyy HH:mm") : "Not specified")}</li>");
            sb.Append($"<li><strong>Total Guest Count:</strong> {order.GuestCount}</li>");
            sb.Append($"<li><strong>Event Address:</strong> {H(order.EventAddress)}</li>");

            if (!string.IsNullOrWhiteSpace(order.EventNote))
            {
                sb.Append($"<li><strong>Event Note:</strong> {H(order.EventNote)}</li>");
            }

            sb.Append("</ul>");

            sb.Append("<h3>Request Summary</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Request ID:</strong> #{order.Id}</li>");
            sb.Append($"<li><strong>Request Date:</strong> {order.OrderDate:dd.MM.yyyy HH:mm}</li>");
            sb.Append($"<li><strong>Status:</strong> {H(order.Status)}</li>");
            sb.Append($"<li><strong>Your Estimated Total:</strong> {catererTotal:0.00} ₺</li>");
            sb.Append("</ul>");

            sb.Append("<h3>Your Packages in This Request</h3>");
            sb.Append("<table style='border-collapse:collapse;width:100%;font-family:Arial,sans-serif;'>");
            sb.Append("<thead>");
            sb.Append("<tr>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Package</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Guest Count</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Options</th>");
            sb.Append("<th style='border:1px solid #ddd;padding:8px;text-align:left;'>Line Total</th>");
            sb.Append("</tr>");
            sb.Append("</thead>");
            sb.Append("<tbody>");

            foreach (var item in relatedItems)
            {
                var packageName = item.MenuItem?.Name ?? "Catering Package";

                var customizationText = "None";

                if (item.SelectedCustomizations.Any())
                {
                    customizationText = string.Join("<br />",
                        item.SelectedCustomizations.Select(c =>
                            $"{H(c.GroupTitle)}: {H(c.OptionName)}" +
                            (c.PriceChange != 0 ? $" ({c.PriceChange:0.00} ₺ / person)" : "")));
                }

                sb.Append("<tr>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{H(packageName)}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.Quantity}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{customizationText}</td>");
                sb.Append($"<td style='border:1px solid #ddd;padding:8px;'>{item.LineTotal:0.00} ₺</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
            sb.Append("</table>");

            sb.Append("<p>Please login to TasteAtDoor to review the request and communicate with the customer.</p>");

            return sb.ToString();
        }

        private async Task<double> GetVerifiedDistanceKmAsync(
            double startLatitude,
            double startLongitude,
            double endLatitude,
            double endLongitude)
        {
            var googleDistanceKm = await _googleMapsService.GetRouteDistanceKmAsync(
                startLatitude,
                startLongitude,
                endLatitude,
                endLongitude);

            if (googleDistanceKm.HasValue)
            {
                return googleDistanceKm.Value;
            }

            return CalculateStraightLineDistanceKm(
                startLatitude,
                startLongitude,
                endLatitude,
                endLongitude);
        }

        private static double CalculateStraightLineDistanceKm(
            double startLatitude,
            double startLongitude,
            double endLatitude,
            double endLongitude)
        {
            const double earthRadiusKm = 6371;

            double dLat = DegreesToRadians(endLatitude - startLatitude);
            double dLon = DegreesToRadians(endLongitude - startLongitude);

            double lat1 = DegreesToRadians(startLatitude);
            double lat2 = DegreesToRadians(endLatitude);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
        private static double CalculateFallbackDistanceKmForCheckout(
            double startLatitude,
            double startLongitude,
            double endLatitude,
            double endLongitude)
        {
            const double earthRadiusKm = 6371;

            var dLat = DegreesToRadiansForCheckout(endLatitude - startLatitude);
            var dLon = DegreesToRadiansForCheckout(endLongitude - startLongitude);

            var lat1 = DegreesToRadiansForCheckout(startLatitude);
            var lat2 = DegreesToRadiansForCheckout(endLatitude);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadiansForCheckout(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasteAtDoor.Data;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "User")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            List<CartItem> clientCart = new();

            if (!string.IsNullOrWhiteSpace(model.CartJson))
            {
                try
                {
                    clientCart = JsonSerializer.Deserialize<List<CartItem>>(model.CartJson) ?? new List<CartItem>();
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

                var lineTotal = dbMenuItem.Price * cartItem.Quantity;
                totalPrice += lineTotal;

                order.OrderItems.Add(new OrderItem
                {
                    MenuItemId = dbMenuItem.Id,
                    Quantity = cartItem.Quantity,
                    UnitPrice = dbMenuItem.Price,
                    LineTotal = lineTotal
                });
            }

            if (!order.OrderItems.Any())
            {
                ModelState.AddModelError(string.Empty, "No valid items were found in the cart.");
                return View(model);
            }

            order.TotalPrice = totalPrice;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

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
    }
}
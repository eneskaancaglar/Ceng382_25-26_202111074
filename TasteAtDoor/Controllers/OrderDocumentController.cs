using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using TasteAtDoor.Data;
using TasteAtDoor.Documents;
using TasteAtDoor.Models;
using TasteAtDoor.Services;

namespace TasteAtDoor.Controllers
{
    [Authorize(Roles = "User,Caretaker,Admin")]
    public class OrderDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppLogService _appLogService;

        public OrderDocumentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppLogService appLogService)
        {
            _context = context;
            _userManager = userManager;
            _appLogService = appLogService;
        }

        [HttpGet]
        public async Task<IActionResult> ReceiptPdf(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await LoadOrderForDocumentAsync(id);

            if (order is null)
            {
                return NotFound();
            }

            var canAccess = await CanAccessOrderAsync(currentUser, order);

            if (!canAccess)
            {
                return Forbid();
            }

            var document = new OrderReceiptDocument(order);
            var pdfBytes = document.GeneratePdf();

            await _appLogService.LogAsync(
                eventType: "ReceiptPdfGenerated",
                message: "Receipt PDF generated.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id}");

            return File(
                pdfBytes,
                "application/pdf",
                $"receipt-order-{order.Id}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> AgreementPdf(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await LoadOrderForDocumentAsync(id);

            if (order is null)
            {
                return NotFound();
            }

            var canAccess = await CanAccessOrderAsync(currentUser, order);

            if (!canAccess)
            {
                return Forbid();
            }

            var document = new OrderAgreementDocument(order);
            var pdfBytes = document.GeneratePdf();

            await _appLogService.LogAsync(
                eventType: "AgreementPdfGenerated",
                message: "Agreement PDF generated.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id}");

            return File(
                pdfBytes,
                "application/pdf",
                $"agreement-order-{order.Id}.pdf");
        }

        private async Task<Order?> LoadOrderForDocumentAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m!.Caretaker)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedCustomizations)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        private async Task<bool> CanAccessOrderAsync(ApplicationUser currentUser, Order order)
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
    }
}
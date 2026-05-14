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
    [Route("[controller]")]
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

        [HttpGet("ReceiptPdf")]
        [HttpGet("ReceiptPdf/{rawId?}")]
        public async Task<IActionResult> ReceiptPdf(string? rawId)
        {
            var finalOrderId = ResolveOrderId(rawId);

            if (finalOrderId <= 0)
            {
                TempData["Error"] = "Receipt PDF could not be opened because the catering request id was missing or invalid.";
                return RedirectToSafePage();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await LoadOrderForDocumentAsync(finalOrderId);

            if (order is null)
            {
                TempData["Error"] = $"Catering request #{finalOrderId} was not found.";
                return RedirectToSafePage();
            }

            var canAccess = await CanAccessOrderAsync(currentUser, order);

            if (!canAccess)
            {
                TempData["Error"] = "You are not allowed to access this catering request document.";
                return RedirectToSafePage();
            }

            var document = new OrderReceiptDocument(order);
            var pdfBytes = document.GeneratePdf();

            await _appLogService.LogAsync(
                eventType: "ReceiptPdfGenerated",
                message: "Catering receipt PDF generated.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id}");

            return File(
                pdfBytes,
                "application/pdf",
                $"CateringReceipt_Order_{order.Id}.pdf");
        }

        [HttpGet("AgreementPdf")]
        [HttpGet("AgreementPdf/{rawId?}")]
        public async Task<IActionResult> AgreementPdf(string? rawId)
        {
            var finalOrderId = ResolveOrderId(rawId);

            if (finalOrderId <= 0)
            {
                TempData["Error"] = "Agreement PDF could not be opened because the catering request id was missing or invalid.";
                return RedirectToSafePage();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await LoadOrderForDocumentAsync(finalOrderId);

            if (order is null)
            {
                TempData["Error"] = $"Catering request #{finalOrderId} was not found.";
                return RedirectToSafePage();
            }

            var canAccess = await CanAccessOrderAsync(currentUser, order);

            if (!canAccess)
            {
                TempData["Error"] = "You are not allowed to access this catering request agreement.";
                return RedirectToSafePage();
            }

            var document = new OrderAgreementDocument(order);
            var pdfBytes = document.GeneratePdf();

            await _appLogService.LogAsync(
                eventType: "AgreementPdfGenerated",
                message: "Serious Turkish catering service agreement PDF generated.",
                userId: currentUser.Id,
                userEmail: currentUser.Email,
                details: $"OrderId: {order.Id}");

            return File(
                pdfBytes,
                "application/pdf",
                $"CateringAgreement_Order_{order.Id}.pdf");
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

            return 0;
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
    }
}
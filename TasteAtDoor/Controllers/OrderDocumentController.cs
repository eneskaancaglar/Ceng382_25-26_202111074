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
    [Authorize(Roles = "User")]
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
        public async Task<IActionResult> AgreementPdf(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedCustomizations)
                .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == currentUser.Id);

            if (order is null)
            {
                return NotFound();
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
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TasteAtDoor.Models;

namespace TasteAtDoor.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account", new { fresh = 1 });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (User.IsInRole("Caterer") || User.IsInRole("Caretaker"))
            {
                return RedirectToAction("Dashboard", "Caretaker");
            }

            if (User.IsInRole("User"))
            {
                return RedirectToAction("Dashboard", "User");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}


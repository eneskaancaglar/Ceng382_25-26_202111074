using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TasteAtDoor.Models;

namespace TasteAtDoor.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (User.IsInRole("Caretaker"))
            {
                return RedirectToAction("Dashboard", "Caretaker");
            }

            if (User.IsInRole("User"))
            {
                return RedirectToAction("Dashboard", "User");
            }
        }

        return RedirectToAction("Login", "Account");
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
}
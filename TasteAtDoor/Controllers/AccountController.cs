using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels.Account;
using TasteAtDoor.Services;

namespace TasteAtDoor.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAppLogService _appLogService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAppLogService appLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appLogService = appLogService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Role != "User" && model.Role != "Caretaker")
            {
                ModelState.AddModelError(string.Empty, "Invalid role selection.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                await _signInManager.SignInAsync(user, isPersistent: false);

                await _appLogService.LogAsync(
                    eventType: "RegisterSuccess",
                    message: "User registered successfully.",
                    userId: user.Id,
                    userEmail: user.Email,
                    details: $"Role: {model.Role}");

                return await RedirectByRole(user);
            }

            await _appLogService.LogAsync(
                eventType: "RegisterFailure",
                message: "User registration failed.",
                level: "Warning",
                userEmail: model.Email,
                details: string.Join(" | ", result.Errors.Select(e => e.Description)));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                false,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                await _appLogService.LogAsync(
                    eventType: "LoginFailure",
                    message: "Failed login attempt.",
                    level: "Warning",
                    userEmail: model.Email);

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
            {
                await _signInManager.SignOutAsync();

                await _appLogService.LogAsync(
                    eventType: "LoginFailure",
                    message: "Login succeeded but user could not be loaded.",
                    level: "Error",
                    userEmail: model.Email);

                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            await _appLogService.LogAsync(
                eventType: "LoginSuccess",
                message: "User logged in successfully.",
                userId: user.Id,
                userEmail: user.Email);

            return await RedirectByRole(user);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                return RedirectToAction(nameof(Register));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new AccountDetailsViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is not null)
            {
                await _appLogService.LogAsync(
                    eventType: "Logout",
                    message: "User logged out.",
                    userId: user.Id,
                    userEmail: user.Email);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchAccount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is not null)
            {
                await _appLogService.LogAsync(
                    eventType: "SwitchAccount",
                    message: "User switched account.",
                    userId: user.Id,
                    userEmail: user.Email);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<IActionResult> RedirectByRole(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (await _userManager.IsInRoleAsync(user, "Caretaker"))
            {
                return RedirectToAction("Index", "Caretaker");
            }

            return RedirectToAction("Index", "User");
        }
    }
}
using System.Net;
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
        private const string PendingLoginUserIdKey = "PendingLoginUserId";
        private const string PendingLoginEmailKey = "PendingLoginEmail";
        private const string PendingLoginCodeKey = "PendingLoginCode";
        private const string PendingLoginExpiresKey = "PendingLoginExpires";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAppLogService _appLogService;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAppLogService appLogService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appLogService = appLogService;
            _emailService = emailService;
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
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
            }

            ClearPendingLoginSession();
            HttpContext.Session.Clear();

            Response.Cookies.Delete(".AspNetCore.Identity.Application");

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

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
            {
                await _appLogService.LogAsync(
                    eventType: "LoginFailure",
                    message: "Failed login attempt. User not found.",
                    level: "Warning",
                    userEmail: model.Email);

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var passwordResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                model.Password,
                lockoutOnFailure: false);

            if (!passwordResult.Succeeded)
            {
                await _appLogService.LogAsync(
                    eventType: "LoginFailure",
                    message: "Failed login attempt. Invalid password.",
                    level: "Warning",
                    userId: user.Id,
                    userEmail: user.Email);

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var isRegularUser = await _userManager.IsInRoleAsync(user, "User");

            if (!isRegularUser)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);

                await _appLogService.LogAsync(
                    eventType: "LoginSuccess",
                    message: "Admin/Caretaker logged in without email verification.",
                    userId: user.Id,
                    userEmail: user.Email);

                return await RedirectByRole(user);
            }

            var code = GenerateVerificationCode();
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            HttpContext.Session.SetString(PendingLoginUserIdKey, user.Id);
            HttpContext.Session.SetString(PendingLoginEmailKey, user.Email ?? model.Email);
            HttpContext.Session.SetString(PendingLoginCodeKey, code);
            HttpContext.Session.SetString(PendingLoginExpiresKey, expiresAt.ToString("O"));

            try
            {
                await _emailService.SendAsync(
                    user.Email ?? model.Email,
                    "TasteAtDoor Login Verification Code",
                    BuildVerificationEmailBody(user.FullName, code));
            }
            catch (Exception ex)
            {
                await _appLogService.LogAsync(
                    eventType: "LoginVerificationEmailFailed",
                    message: "Login verification email could not be sent.",
                    level: "Error",
                    userId: user.Id,
                    userEmail: user.Email,
                    details: ex.Message);

                ModelState.AddModelError(
                    string.Empty,
                    "Verification email could not be sent. Please check SMTP settings.");

                return View(model);
            }

            await _appLogService.LogAsync(
                eventType: "LoginVerificationCodeSent",
                message: "Email verification code sent for regular user login.",
                userId: user.Id,
                userEmail: user.Email,
                details: "Code expires in 10 minutes. Applied only to User role.");

            return RedirectToAction(nameof(VerifyEmailCode));
        }

        [HttpGet]
        public IActionResult VerifyEmailCode()
        {
            var email = HttpContext.Session.GetString(PendingLoginEmailKey);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new VerifyEmailCodeViewModel
            {
                Email = email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmailCode(VerifyEmailCodeViewModel model)
        {
            var pendingUserId = HttpContext.Session.GetString(PendingLoginUserIdKey);
            var pendingEmail = HttpContext.Session.GetString(PendingLoginEmailKey);
            var expectedCode = HttpContext.Session.GetString(PendingLoginCodeKey);
            var expiresAtText = HttpContext.Session.GetString(PendingLoginExpiresKey);

            if (string.IsNullOrWhiteSpace(pendingUserId) ||
                string.IsNullOrWhiteSpace(pendingEmail) ||
                string.IsNullOrWhiteSpace(expectedCode) ||
                string.IsNullOrWhiteSpace(expiresAtText))
            {
                ModelState.AddModelError(string.Empty, "Verification session expired. Please login again.");
                return View(model);
            }

            model.Email = pendingEmail;

            if (!DateTime.TryParse(expiresAtText, out var expiresAt) ||
                DateTime.UtcNow > expiresAt)
            {
                ClearPendingLoginSession();

                await _appLogService.LogAsync(
                    eventType: "LoginVerificationExpired",
                    message: "Login verification code expired.",
                    level: "Warning",
                    userEmail: pendingEmail);

                ModelState.AddModelError(string.Empty, "Verification code expired. Please login again.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.Equals(model.Code.Trim(), expectedCode, StringComparison.Ordinal))
            {
                await _appLogService.LogAsync(
                    eventType: "LoginVerificationFailure",
                    message: "Invalid login verification code entered.",
                    level: "Warning",
                    userId: pendingUserId,
                    userEmail: pendingEmail);

                ModelState.AddModelError(nameof(model.Code), "Invalid verification code.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(pendingUserId);

            if (user is null)
            {
                ClearPendingLoginSession();
                ModelState.AddModelError(string.Empty, "User not found. Please login again.");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            ClearPendingLoginSession();

            await _appLogService.LogAsync(
                eventType: "LoginSuccess",
                message: "User logged in successfully with email verification.",
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
                return RedirectToAction("Dashboard", "Admin");
            }

            if (await _userManager.IsInRoleAsync(user, "Caretaker"))
            {
                return RedirectToAction("Dashboard", "Caretaker");
            }

            return RedirectToAction("Dashboard", "User");
        }

        private static string GenerateVerificationCode()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        private static string BuildVerificationEmailBody(string fullName, string code)
        {
            var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "User" : fullName);
            var safeCode = WebUtility.HtmlEncode(code);

            return $"""
                <p>Hello {safeName},</p>
                <p>Your TasteAtDoor login verification code is:</p>
                <div style="font-size:28px;font-weight:bold;letter-spacing:6px;padding:14px 18px;background:#f4f4f4;border-radius:10px;display:inline-block;">
                    {safeCode}
                </div>
                <p>This code expires in 10 minutes.</p>
                <p>If you did not try to login, you can ignore this email.</p>
            """;
        }

        private void ClearPendingLoginSession()
        {
            HttpContext.Session.Remove(PendingLoginUserIdKey);
            HttpContext.Session.Remove(PendingLoginEmailKey);
            HttpContext.Session.Remove(PendingLoginCodeKey);
            HttpContext.Session.Remove(PendingLoginExpiresKey);
        }
    }
}
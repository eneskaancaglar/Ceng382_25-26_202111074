using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using TasteAtDoor.Models;
using TasteAtDoor.Models.ViewModels.Account;
using TasteAtDoor.Services;

namespace TasteAtDoor.Controllers
{
    public class AccountController : Controller
    {
        private const string VerifyUserIdKey = "LoginVerifyUserId";
        private const string VerifyEmailKey = "LoginVerifyEmail";
        private const string VerifyCodeKey = "LoginVerifyCode";
        private const string VerifyExpiresKey = "LoginVerifyExpires";

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IWebHostEnvironment environment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Login(string? fresh = null)
        {
            await _signInManager.SignOutAsync();
            ClearSessionSafely();
            DisablePageCache();

            ModelState.Clear();

            return View(new LoginViewModel
            {
                Email = string.Empty,
                Password = string.Empty,
                RememberMe = false
            });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            DisablePageCache();

            if (!ModelState.IsValid)
            {
                model.Email = string.Empty;
                model.Password = string.Empty;
                model.RememberMe = false;
                return View(model);
            }

            var email = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                model.Email = string.Empty;
                model.Password = string.Empty;
                model.RememberMe = false;
                return View(model);
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(
                user,
                model.Password,
                lockoutOnFailure: false);

            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                model.Email = string.Empty;
                model.Password = string.Empty;
                model.RememberMe = false;
                return View(model);
            }

            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                await StartSixDigitLoginVerificationAsync(user);

                TempData["Success"] = "A 6-digit login verification code has been sent to your email.";
                return RedirectToAction(nameof(VerifyEmailCode));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await _signInManager.SignOutAsync();
            ClearSessionSafely();
            DisablePageCache();

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(
            string fullName,
            string email,
            string password,
            string confirmPassword,
            string role,
            string? phoneNumber = null)
        {
            DisablePageCache();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(string.Empty, "Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Password is required.");
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
            }

            var normalizedRole = string.IsNullOrWhiteSpace(role)
                ? "User"
                : role.Trim();

            if (normalizedRole == "Customer")
            {
                normalizedRole = "User";
            }

            if (normalizedRole != "User" && normalizedRole != "Caretaker")
            {
                normalizedRole = "User";
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var existingUser = await _userManager.FindByEmailAsync(email.Trim());

            if (existingUser is not null)
            {
                ModelState.AddModelError(string.Empty, "This email address is already registered.");
                return View();
            }

            if (!await _roleManager.RoleExistsAsync(normalizedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(normalizedRole));
            }

            var user = new ApplicationUser
            {
                UserName = email.Trim(),
                Email = email.Trim(),
                EmailConfirmed = normalizedRole != "User",
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber
            };

            var createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View();
            }

            await _userManager.AddToRoleAsync(user, normalizedRole);

            if (normalizedRole == "User")
            {
                await StartSixDigitLoginVerificationAsync(user);

                TempData["Success"] = "Account created. Enter the 6-digit code to continue to your dashboard.";
                return RedirectToAction(nameof(VerifyEmailCode));
            }

            TempData["Success"] = "Caterer account created successfully. You can login.";
            return RedirectToAction(nameof(Login), new { fresh = 1 });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyEmailCode()
        {
            DisablePageCache();

            var email = HttpContext.Session.GetString(VerifyEmailKey);
            var devCode = _environment.IsDevelopment()
                ? HttpContext.Session.GetString(VerifyCodeKey)
                : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Verification session expired. Please login again.";
                return RedirectToAction(nameof(Login), new { fresh = 1 });
            }

            ViewBag.Email = email;
            ViewBag.DevCode = devCode;

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> VerifyEmailCode(string code)
        {
            DisablePageCache();

            var userId = HttpContext.Session.GetString(VerifyUserIdKey);
            var expectedCode = HttpContext.Session.GetString(VerifyCodeKey);
            var expiresRaw = HttpContext.Session.GetString(VerifyExpiresKey);

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(expectedCode) ||
                string.IsNullOrWhiteSpace(expiresRaw))
            {
                TempData["Error"] = "Verification session expired. Please login again.";
                return RedirectToAction(nameof(Login), new { fresh = 1 });
            }

            if (!DateTime.TryParse(expiresRaw, out var expiresAt) || DateTime.Now > expiresAt)
            {
                TempData["Error"] = "Verification code expired. Please login again to receive a new code.";
                ClearEmailVerificationSession();
                return RedirectToAction(nameof(Login), new { fresh = 1 });
            }

            var submittedCode = (code ?? string.Empty).Trim();

            if (submittedCode != expectedCode)
            {
                TempData["Error"] = "Invalid verification code.";
                return RedirectToAction(nameof(VerifyEmailCode));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                TempData["Error"] = "User could not be found.";
                ClearEmailVerificationSession();
                return RedirectToAction(nameof(Login), new { fresh = 1 });
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            ClearEmailVerificationSession();

            return RedirectToAction("Dashboard", "User");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResendEmailCode()
        {
            DisablePageCache();

            var userId = HttpContext.Session.GetString(VerifyUserIdKey);

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Verification session expired. Please login again.";
                return RedirectToAction(nameof(Login), new { fresh = 1 });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                TempData["Error"] = "User could not be found.";
                ClearEmailVerificationSession();
                return RedirectToAction(nameof(Login), new { fresh = 1 });
            }

            await StartSixDigitLoginVerificationAsync(user);

            TempData["Success"] = "A new 6-digit login verification code has been sent.";
            return RedirectToAction(nameof(VerifyEmailCode));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            ClearSessionSafely();

            return RedirectToAction(nameof(Login), new { fresh = 1 });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task StartSixDigitLoginVerificationAsync(ApplicationUser user)
        {
            ClearEmailVerificationSession();

            var code = GenerateSixDigitCode();
            var expiresAt = DateTime.Now.AddMinutes(10);

            HttpContext.Session.SetString(VerifyUserIdKey, user.Id);
            HttpContext.Session.SetString(VerifyEmailKey, user.Email ?? string.Empty);
            HttpContext.Session.SetString(VerifyCodeKey, code);
            HttpContext.Session.SetString(VerifyExpiresKey, expiresAt.ToString("O"));

            try
            {
                await SendSixDigitCodeEmailAsync(user, code);
            }
            catch
            {
                TempData["Error"] = "Verification email could not be sent. In development mode, use the test code shown on screen.";
            }
        }

        private async Task SendSixDigitCodeEmailAsync(ApplicationUser user, string code)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var safeName = System.Net.WebUtility.HtmlEncode(user.FullName);

            var body = $@"
                <h2>TasteAtDoor Login Verification</h2>
                <p>Hello {safeName},</p>
                <p>Your 6-digit login verification code is:</p>
                <div style=""font-size:28px;font-weight:bold;letter-spacing:6px;background:#f3f4f6;padding:16px;border-radius:10px;text-align:center;"">
                    {code}
                </div>
                <p>This code is valid for 10 minutes.</p>
            ";

            await _emailService.SendAsync(
                user.Email,
                "TasteAtDoor - Login Verification Code",
                body);
        }

        private static string GenerateSixDigitCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private void ClearEmailVerificationSession()
        {
            HttpContext.Session.Remove(VerifyUserIdKey);
            HttpContext.Session.Remove(VerifyEmailKey);
            HttpContext.Session.Remove(VerifyCodeKey);
            HttpContext.Session.Remove(VerifyExpiresKey);
        }

        private void DisablePageCache()
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
        }

        private void ClearSessionSafely()
        {
            var sessionFeature = HttpContext.Features.Get<ISessionFeature>();

            if (sessionFeature?.Session is not null)
            {
                sessionFeature.Session.Clear();
            }
        }
    }
}
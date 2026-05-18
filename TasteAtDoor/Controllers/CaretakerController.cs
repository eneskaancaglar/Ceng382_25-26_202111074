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
    [Authorize(Roles = "Caretaker")]
    public class CaretakerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IConfiguration _configuration;

        public CaretakerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IGoogleMapsService googleMapsService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _googleMapsService = googleMapsService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string search = "", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            const int pageSize = 6;

            var query = _context.MenuItems
                .Where(m => m.CaretakerId == currentUser.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.Name.Contains(search) ||
                    m.Description.Contains(search) ||
                    m.LocationText.Contains(search) ||
                    m.EventType.Contains(search) ||
                    m.PackageCategory.Contains(search) ||
                    (m.IncludedItems != null && m.IncludedItems.Contains(search)) ||
                    (m.ServiceDetails != null && m.ServiceDetails.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var menus = await query
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new CaretakerMenuListViewModel
            {
                MenuItems = menus,
                Search = search,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var ownedMenuIds = await _context.MenuItems
                .Where(m => m.CaretakerId == currentUser.Id)
                .Select(m => m.Id)
                .ToListAsync();

            var relatedOrderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.MenuItem)
                .Where(oi => ownedMenuIds.Contains(oi.MenuItemId))
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync();

            var relatedReviews = await _context.OrderItemReviews
                .Where(r => r.CatererId == currentUser.Id)
                .ToListAsync();

            var model = new CaretakerDashboardViewModel
            {
                TotalMenus = ownedMenuIds.Count,
                TotalReceivedOrders = relatedOrderItems.Count,
                TotalCompletedOrders = relatedOrderItems.Count(oi =>
                    oi.Order != null && oi.Order.Status == "Completed"),
                TotalRevenue = relatedOrderItems.Sum(oi => oi.LineTotal),
                AverageCatererRating = relatedReviews.Any()
                    ? relatedReviews.Average(r => r.CatererRating)
                    : 0,
                TotalReviewCount = relatedReviews.Count,
                RecentOrderItems = relatedOrderItems.Take(8).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var model = BuildProfileViewModel(currentUser);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                FillExistingProfileImage(model, currentUser);
                return View(model);
            }

            currentUser.FullName = model.FullName;
            currentUser.PhoneNumber = model.PhoneNumber;
            currentUser.Bio = model.Bio;

            var imageResult = await TryUpdateProfileImageAsync(currentUser, model.ProfileImageFile);

            if (!imageResult.Success)
            {
                ModelState.AddModelError(
                    nameof(model.ProfileImageFile),
                    imageResult.ErrorMessage ?? "Image could not be uploaded.");

                FillExistingProfileImage(model, currentUser);
                return View(model);
            }

            var result = await _userManager.UpdateAsync(currentUser);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                FillExistingProfileImage(model, currentUser);
                return View(model);
            }

            TempData["Success"] = "Caterer profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> Reviews(
            string search = "",
            int? menuRating = null,
            int? catererRating = null,
            int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            const int pageSize = 10;

            var query = _context.OrderItemReviews
                .Include(r => r.Order)
                    .ThenInclude(o => o!.ApplicationUser)
                .Include(r => r.OrderItem)
                .Include(r => r.MenuItem)
                .Include(r => r.User)
                .Include(r => r.Caterer)
                .Where(r => r.CatererId == currentUser.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.OrderId.ToString().Contains(search) ||
                    r.Comment.Contains(search) ||
                    (r.MenuItem != null && r.MenuItem.Name.Contains(search)) ||
                    (r.User != null && r.User.FullName.Contains(search)) ||
                    (r.User != null && r.User.Email != null && r.User.Email.Contains(search)));
            }

            if (menuRating.HasValue)
            {
                query = query.Where(r => r.MenuRating == menuRating.Value);
            }

            if (catererRating.HasValue)
            {
                query = query.Where(r => r.CatererRating == catererRating.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewListItemViewModel
                {
                    ReviewId = r.Id,
                    OrderId = r.OrderId,
                    OrderItemId = r.OrderItemId,
                    CustomerName = r.User != null ? r.User.FullName : "Customer",
                    CustomerEmail = r.User != null ? r.User.Email : null,
                    CatererName = r.Caterer != null ? r.Caterer.FullName : "Caterer",
                    CatererEmail = r.Caterer != null ? r.Caterer.Email : null,
                    MenuItemName = r.MenuItem != null ? r.MenuItem.Name : "Package",
                    MenuRating = r.MenuRating,
                    CatererRating = r.CatererRating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    OrderStatus = r.Order != null ? r.Order.Status : "",
                    Quantity = r.OrderItem != null ? r.OrderItem.Quantity : 0,
                    LineTotal = r.OrderItem != null ? r.OrderItem.LineTotal : 0
                })
                .ToListAsync();

            var model = new ReviewListPageViewModel
            {
                Reviews = reviews,
                Search = search,
                MenuRating = menuRating,
                CatererRating = catererRating,
                Page = page,
                TotalPages = totalPages,
                PageTitle = "Customer Reviews"
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> RestaurantLocation()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            var model = new LocationInputViewModel
            {
                Latitude = currentUser.Latitude,
                Longitude = currentUser.Longitude,
                Address = currentUser.Address
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurantLocation(LocationInputViewModel model)
        {
            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            currentUser.Latitude = model.Latitude;
            currentUser.Longitude = model.Longitude;
            currentUser.Address = model.Address;

            var result = await _userManager.UpdateAsync(currentUser);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["Success"] = "Caterer location saved successfully.";
            return RedirectToAction(nameof(RestaurantLocation));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                TempData["Error"] = "Please save your caterer location before creating catering packages.";
                return RedirectToAction(nameof(RestaurantLocation));
            }

            return View(new MenuCreateViewModel
            {
                LocationText = currentUser.Address,
                EventType = "Wedding",
                PackageCategory = "Standard",
                MinGuestCount = 10,
                MaxGuestCount = 500,
                IncludesMainCourse = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuCreateViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            ValidateGuestRange(model.MinGuestCount, model.MaxGuestCount);

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.LocationText),
                    "Please save your caterer location first.");
            }

            if (model.ImageFile is null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Package image is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var finalLocationText = !string.IsNullOrWhiteSpace(model.LocationText)
                ? model.LocationText.Trim()
                : currentUser.Address;

            if (string.IsNullOrWhiteSpace(finalLocationText))
            {
                finalLocationText = "Caterer saved location";
            }

            double latitude = currentUser.Latitude!.Value;
            double longitude = currentUser.Longitude!.Value;

            if (!string.IsNullOrWhiteSpace(model.LocationText))
            {
                var geocoded = await _googleMapsService.GeocodeAddressAsync(model.LocationText);

                if (geocoded.Latitude.HasValue && geocoded.Longitude.HasValue)
                {
                    latitude = geocoded.Latitude.Value;
                    longitude = geocoded.Longitude.Value;
                }
            }

            var imageResult = await ConvertImageFileToBytesAsync(model.ImageFile);

            if (!imageResult.Success)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    imageResult.ErrorMessage ?? "Package image could not be uploaded.");

                return View(model);
            }

            var menuItem = new MenuItem
            {
                Name = model.Name.Trim(),
                Price = model.Price,
                Description = model.Description.Trim(),
                EventType = model.EventType.Trim(),
                PackageCategory = model.PackageCategory.Trim(),
                MinGuestCount = model.MinGuestCount,
                MaxGuestCount = model.MaxGuestCount,
                IncludesMainCourse = model.IncludesMainCourse,
                IncludesDessert = model.IncludesDessert,
                IncludesSnacks = model.IncludesSnacks,
                IncludesDrinks = model.IncludesDrinks,
                IncludedItems = model.IncludedItems?.Trim(),
                ServiceDetails = model.ServiceDetails?.Trim(),
                LocationText = finalLocationText,
                Latitude = latitude,
                Longitude = longitude,
                CaretakerId = currentUser.Id,
                ImageFileName = model.ImageFile!.FileName,
                ImageContentType = model.ImageFile.ContentType,
                ImageData = imageResult.ImageBytes!
            };

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Catering package created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.CaretakerId == currentUser.Id);

            if (menuItem is null)
            {
                return NotFound();
            }

            var model = new MenuEditViewModel
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Price = menuItem.Price,
                Description = menuItem.Description,
                EventType = menuItem.EventType,
                PackageCategory = menuItem.PackageCategory,
                MinGuestCount = menuItem.MinGuestCount,
                MaxGuestCount = menuItem.MaxGuestCount,
                IncludesMainCourse = menuItem.IncludesMainCourse,
                IncludesDessert = menuItem.IncludesDessert,
                IncludesSnacks = menuItem.IncludesSnacks,
                IncludesDrinks = menuItem.IncludesDrinks,
                IncludedItems = menuItem.IncludedItems,
                ServiceDetails = menuItem.ServiceDetails,
                LocationText = menuItem.LocationText,
                ExistingImageBase64 = Convert.ToBase64String(menuItem.ImageData),
                ExistingImageContentType = menuItem.ImageContentType
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == model.Id && m.CaretakerId == currentUser.Id);

            if (menuItem is null)
            {
                return NotFound();
            }

            ValidateGuestRange(model.MinGuestCount, model.MaxGuestCount);

            if (!ModelState.IsValid)
            {
                FillExistingPackageImage(model, menuItem);
                return View(model);
            }

            var finalLocationText = !string.IsNullOrWhiteSpace(model.LocationText)
                ? model.LocationText.Trim()
                : currentUser.Address;

            if (string.IsNullOrWhiteSpace(finalLocationText))
            {
                finalLocationText = menuItem.LocationText;
            }

            double? latitude = menuItem.Latitude;
            double? longitude = menuItem.Longitude;

            if (!string.IsNullOrWhiteSpace(model.LocationText))
            {
                var geocoded = await _googleMapsService.GeocodeAddressAsync(model.LocationText);

                if (geocoded.Latitude.HasValue && geocoded.Longitude.HasValue)
                {
                    latitude = geocoded.Latitude.Value;
                    longitude = geocoded.Longitude.Value;
                }
            }

            if ((!latitude.HasValue || !longitude.HasValue) &&
                currentUser.Latitude.HasValue &&
                currentUser.Longitude.HasValue)
            {
                latitude = currentUser.Latitude.Value;
                longitude = currentUser.Longitude.Value;
            }

            menuItem.Name = model.Name.Trim();
            menuItem.Price = model.Price;
            menuItem.Description = model.Description.Trim();
            menuItem.EventType = model.EventType.Trim();
            menuItem.PackageCategory = model.PackageCategory.Trim();
            menuItem.MinGuestCount = model.MinGuestCount;
            menuItem.MaxGuestCount = model.MaxGuestCount;
            menuItem.IncludesMainCourse = model.IncludesMainCourse;
            menuItem.IncludesDessert = model.IncludesDessert;
            menuItem.IncludesSnacks = model.IncludesSnacks;
            menuItem.IncludesDrinks = model.IncludesDrinks;
            menuItem.IncludedItems = model.IncludedItems?.Trim();
            menuItem.ServiceDetails = model.ServiceDetails?.Trim();
            menuItem.LocationText = finalLocationText;
            menuItem.Latitude = latitude;
            menuItem.Longitude = longitude;

            if (model.ImageFile is not null && model.ImageFile.Length > 0)
            {
                var imageResult = await ConvertImageFileToBytesAsync(model.ImageFile);

                if (!imageResult.Success)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        imageResult.ErrorMessage ?? "Package image could not be uploaded.");

                    FillExistingPackageImage(model, menuItem);
                    return View(model);
                }

                menuItem.ImageData = imageResult.ImageBytes!;
                menuItem.ImageFileName = model.ImageFile.FileName;
                menuItem.ImageContentType = model.ImageFile.ContentType;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Catering package updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.CaretakerId == currentUser.Id);

            if (menuItem is null)
            {
                return NotFound();
            }

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Catering package deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private ProfileViewModel BuildProfileViewModel(ApplicationUser user)
        {
            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                Address = user.Address,
                Latitude = user.Latitude,
                Longitude = user.Longitude
            };

            FillExistingProfileImage(model, user);

            return model;
        }

        private void FillExistingProfileImage(ProfileViewModel model, ApplicationUser user)
        {
            if (user.ProfileImageData is not null && user.ProfileImageData.Length > 0)
            {
                model.ExistingImageBase64 = Convert.ToBase64String(user.ProfileImageData);
                model.ExistingImageContentType = user.ProfileImageContentType;
            }
        }

        private void FillExistingPackageImage(MenuEditViewModel model, MenuItem menuItem)
        {
            if (menuItem.ImageData.Length > 0)
            {
                model.ExistingImageBase64 = Convert.ToBase64String(menuItem.ImageData);
                model.ExistingImageContentType = menuItem.ImageContentType;
            }
        }

        private void ValidateGuestRange(int minGuestCount, int maxGuestCount)
        {
            if (maxGuestCount < minGuestCount)
            {
                ModelState.AddModelError(
                    "MaxGuestCount",
                    "Maximum guest count cannot be smaller than minimum guest count.");
            }
        }

        private async Task<(bool Success, byte[]? ImageBytes, string? ErrorMessage)> ConvertImageFileToBytesAsync(
            IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return (false, null, "Please upload a package image.");
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Please upload a valid image file.");
            }

            const long maxFileSize = 4 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return (false, null, "Image size must be smaller than 4 MB.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            return (true, memoryStream.ToArray(), null);
        }

        private async Task<(bool Success, string? ErrorMessage)> TryUpdateProfileImageAsync(
            ApplicationUser user,
            IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return (true, null);
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Please upload a valid image file.");
            }

            const long maxFileSize = 2 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return (false, "Image size must be smaller than 2 MB.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            user.ProfileImageData = memoryStream.ToArray();
            user.ProfileImageContentType = file.ContentType;
            user.ProfileImageFileName = file.FileName;

            return (true, null);
        }
    }
}


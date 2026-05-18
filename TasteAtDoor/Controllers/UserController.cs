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
    [Authorize(Roles = "User,Caretaker,Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IConfiguration _configuration;

        public UserController(
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

        [Authorize(Roles = "User,Caretaker,Admin")]
        public async Task<IActionResult> Index(string search = "")
        {
            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                return View(new RestaurantListPageViewModel
                {
                    UserLocationSaved = false,
                    Search = search,
                    UserLatitude = currentUser.Latitude,
                    UserLongitude = currentUser.Longitude,
                    UserAddress = currentUser.Address
                });
            }

            var caretakers = await _userManager.GetUsersInRoleAsync("Caretaker");

            var restaurantQuery = caretakers
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                restaurantQuery = restaurantQuery.Where(c =>
                    c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(c.Email) &&
                     c.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(c.Address) &&
                     c.Address.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(c.Bio) &&
                     c.Bio.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var caretakerList = restaurantQuery.ToList();
            var caretakerIds = caretakerList.Select(c => c.Id).ToList();

            var menuCountMap = await _context.MenuItems
                .Where(m => caretakerIds.Contains(m.CaretakerId))
                .GroupBy(m => m.CaretakerId)
                .Select(g => new
                {
                    CaretakerId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.CaretakerId, x => x.Count);

            var catererRatingMap = await _context.OrderItemReviews
                .Where(r => caretakerIds.Contains(r.CatererId))
                .GroupBy(r => r.CatererId)
                .Select(g => new
                {
                    CatererId = g.Key,
                    AverageRating = g.Average(r => r.CatererRating),
                    ReviewCount = g.Count()
                })
                .ToDictionaryAsync(x => x.CatererId);

            var restaurants = new List<RestaurantListItemViewModel>();

            foreach (var caretaker in caretakerList)
            {
                double distanceKm;

                var googleDistanceKm = await _googleMapsService.GetRouteDistanceKmAsync(
                    currentUser.Latitude.Value,
                    currentUser.Longitude.Value,
                    caretaker.Latitude!.Value,
                    caretaker.Longitude!.Value);

                if (googleDistanceKm.HasValue)
                {
                    distanceKm = googleDistanceKm.Value;
                }
                else
                {
                    distanceKm = CalculateStraightLineDistanceKm(
                        currentUser.Latitude.Value,
                        currentUser.Longitude.Value,
                        caretaker.Latitude.Value,
                        caretaker.Longitude.Value);
                }

                var hasRating = catererRatingMap.TryGetValue(caretaker.Id, out var ratingInfo);

                restaurants.Add(new RestaurantListItemViewModel
                {
                    Id = caretaker.Id,
                    RestaurantName = caretaker.FullName,
                    Email = caretaker.Email,
                    Address = caretaker.Address,
                    Bio = caretaker.Bio,
                    LogoImageData = caretaker.ProfileImageData,
                    LogoImageContentType = caretaker.ProfileImageContentType,
                    MenuCount = menuCountMap.ContainsKey(caretaker.Id) ? menuCountMap[caretaker.Id] : 0,
                    DistanceKm = distanceKm,
                    AverageCatererRating = hasRating ? ratingInfo!.AverageRating : 0,
                    CatererReviewCount = hasRating ? ratingInfo!.ReviewCount : 0
                });
            }

            restaurants = restaurants
                .OrderBy(r => r.DistanceKm)
                .ToList();

            var model = new RestaurantListPageViewModel
            {
                UserLocationSaved = true,
                Search = search,
                UserLatitude = currentUser.Latitude,
                UserLongitude = currentUser.Longitude,
                UserAddress = currentUser.Address,
                Restaurants = restaurants
            };

            return View(model);
        }

        [Authorize(Roles = "User,Caretaker,Admin")]
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

        [Authorize(Roles = "User,Caretaker,Admin")]
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
                ModelState.AddModelError(nameof(model.ProfileImageFile), imageResult.ErrorMessage ?? "Image could not be uploaded.");
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

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize(Roles = "User,Caretaker,Admin")]
        [HttpGet]
        public async Task<IActionResult> MyLocation()
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

        [Authorize(Roles = "User,Caretaker,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyLocation(LocationInputViewModel model, string? returnUrl = null)
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

            TempData["Success"] = "Your location was saved successfully.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(MyLocation));
        }

        [Authorize(Roles = "User,Caretaker,Admin")]
        [HttpGet]
        public async Task<IActionResult> RestaurantMenu(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                TempData["Error"] = "Please save your location before viewing caterer packages.";
                return RedirectToAction(nameof(MyLocation));
            }

            var restaurant = await _context.Users
                .Include(u => u.MenuItems)
                    .ThenInclude(m => m.CustomizationGroups)
                        .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (restaurant is null)
            {
                return NotFound();
            }

            var isCaretaker = await _userManager.IsInRoleAsync(restaurant, "Caretaker");

            if (!isCaretaker)
            {
                return NotFound();
            }

            if (!restaurant.Latitude.HasValue || !restaurant.Longitude.HasValue)
            {
                TempData["Error"] = "This caterer has not saved its location yet.";
                return RedirectToAction(nameof(Index));
            }

            double distanceKm;

            var googleDistanceKm = await _googleMapsService.GetRouteDistanceKmAsync(
                currentUser.Latitude.Value,
                currentUser.Longitude.Value,
                restaurant.Latitude.Value,
                restaurant.Longitude.Value);

            if (googleDistanceKm.HasValue)
            {
                distanceKm = googleDistanceKm.Value;
            }
            else
            {
                distanceKm = CalculateStraightLineDistanceKm(
                    currentUser.Latitude.Value,
                    currentUser.Longitude.Value,
                    restaurant.Latitude.Value,
                    restaurant.Longitude.Value);
            }

            var menuItems = restaurant.MenuItems
                .OrderByDescending(m => m.Id)
                .ToList();

            var menuIds = menuItems
                .Select(m => m.Id)
                .ToList();

            var menuRatingStats = await _context.OrderItemReviews
                .Where(r => menuIds.Contains(r.MenuItemId))
                .GroupBy(r => r.MenuItemId)
                .Select(g => new
                {
                    MenuItemId = g.Key,
                    AverageMenuRating = g.Average(r => r.MenuRating),
                    ReviewCount = g.Count()
                })
                .ToListAsync();

            var averageMenuRatings = menuRatingStats
                .ToDictionary(x => x.MenuItemId, x => x.AverageMenuRating);

            var menuReviewCounts = menuRatingStats
                .ToDictionary(x => x.MenuItemId, x => x.ReviewCount);

            var catererRatingStats = await _context.OrderItemReviews
                .Where(r => r.CatererId == restaurant.Id)
                .GroupBy(r => r.CatererId)
                .Select(g => new
                {
                    AverageRating = g.Average(r => r.CatererRating),
                    ReviewCount = g.Count()
                })
                .FirstOrDefaultAsync();

            var model = new RestaurantMenuPageViewModel
            {
                RestaurantId = restaurant.Id,
                RestaurantName = restaurant.FullName,
                RestaurantAddress = restaurant.Address,
                RestaurantBio = restaurant.Bio,
                RestaurantLogoImageData = restaurant.ProfileImageData,
                RestaurantLogoImageContentType = restaurant.ProfileImageContentType,
                DistanceKm = distanceKm,
                AverageCatererRating = catererRatingStats?.AverageRating ?? 0,
                CatererReviewCount = catererRatingStats?.ReviewCount ?? 0,
                MenuItems = menuItems,
                AverageMenuRatings = averageMenuRatings,
                MenuReviewCounts = menuReviewCounts
            };

            return View(model);
        }

        [Authorize(Roles = "User,Caretaker,Admin")]
        [HttpGet]
        public async Task<IActionResult> MenuDetails(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                TempData["Error"] = "Please save your location before viewing package details.";
                return RedirectToAction(nameof(MyLocation));
            }

            var menuItem = await _context.MenuItems
                .Include(m => m.Caretaker)
                .Include(m => m.CustomizationGroups)
                    .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem is null)
            {
                return NotFound();
            }

            if (menuItem.Caretaker is null)
            {
                return NotFound();
            }

            if (!menuItem.Caretaker.Latitude.HasValue || !menuItem.Caretaker.Longitude.HasValue)
            {
                TempData["Error"] = "This caterer has not saved its location yet.";
                return RedirectToAction(nameof(Index));
            }

            double distanceKm;

            var googleDistanceKm = await _googleMapsService.GetRouteDistanceKmAsync(
                currentUser.Latitude.Value,
                currentUser.Longitude.Value,
                menuItem.Caretaker.Latitude.Value,
                menuItem.Caretaker.Longitude.Value);

            if (googleDistanceKm.HasValue)
            {
                distanceKm = googleDistanceKm.Value;
            }
            else
            {
                distanceKm = CalculateStraightLineDistanceKm(
                    currentUser.Latitude.Value,
                    currentUser.Longitude.Value,
                    menuItem.Caretaker.Latitude.Value,
                    menuItem.Caretaker.Longitude.Value);
            }

            var reviewQuery = _context.OrderItemReviews
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                .Include(r => r.MenuItem)
                .Include(r => r.User)
                .Include(r => r.Caterer)
                .Where(r => r.MenuItemId == id);

            var reviewCount = await reviewQuery.CountAsync();

            var averageMenuRating = reviewCount > 0
                ? await reviewQuery.AverageAsync(r => r.MenuRating)
                : 0;

            var averageCatererRating = reviewCount > 0
                ? await reviewQuery.AverageAsync(r => r.CatererRating)
                : 0;

            var reviews = await reviewQuery
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewListItemViewModel
                {
                    ReviewId = r.Id,
                    OrderId = r.OrderId,
                    OrderItemId = r.OrderItemId,
                    CustomerName = r.User != null ? r.User.FullName : "Customer",
                    CustomerEmail = r.User != null ? r.User.Email : null,
                    CatererName = r.Caterer != null ? r.Caterer.FullName : "Caterer",
                    CatererEmail = r.Caterer != null ? r.Caterer.Email : null,
                    MenuItemName = r.MenuItem != null ? r.MenuItem.Name : "Catering Package",
                    MenuRating = r.MenuRating,
                    CatererRating = r.CatererRating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    OrderStatus = r.Order != null ? r.Order.Status : "",
                    Quantity = r.OrderItem != null ? r.OrderItem.Quantity : 0,
                    LineTotal = r.OrderItem != null ? r.OrderItem.LineTotal : 0
                })
                .ToListAsync();

            var model = new MenuDetailsPageViewModel
            {
                MenuItem = menuItem,
                RestaurantName = menuItem.Caretaker.FullName,
                RestaurantAddress = menuItem.Caretaker.Address,
                DistanceKm = distanceKm,
                AverageMenuRating = averageMenuRating,
                AverageCatererRating = averageCatererRating,
                ReviewCount = reviewCount,
                Reviews = reviews
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.ApplicationUserId == currentUser.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var model = new UserDashboardViewModel
            {
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalPrice),
                TotalItemsPurchased = orders.SelectMany(o => o.OrderItems).Sum(i => i.Quantity),
                RecentOrders = orders.Take(5).ToList()
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Orders(string search = "", string status = "", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            const int pageSize = 5;

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m!.Caretaker)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Reviews)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedCustomizations)
                .Where(o => o.ApplicationUserId == currentUser.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    (!string.IsNullOrWhiteSpace(o.EventType) && o.EventType.Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(o.EventAddress) && o.EventAddress.Contains(search)) ||
                    o.OrderItems.Any(oi =>
                        oi.MenuItem != null &&
                        oi.MenuItem.Name.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
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

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new OrderHistoryPageViewModel
            {
                Orders = orders,
                Search = search,
                Status = status,
                Page = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> MyReviews(string search = "", int? menuRating = null, int? catererRating = null, int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser is null)
            {
                return Challenge();
            }

            const int pageSize = 10;

            var query = _context.OrderItemReviews
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                .Include(r => r.MenuItem)
                .Include(r => r.User)
                .Include(r => r.Caterer)
                .Where(r => r.UserId == currentUser.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.OrderId.ToString().Contains(search) ||
                    r.Comment.Contains(search) ||
                    (r.MenuItem != null && r.MenuItem.Name.Contains(search)) ||
                    (r.Caterer != null && r.Caterer.FullName.Contains(search)) ||
                    (r.Caterer != null && r.Caterer.Email != null && r.Caterer.Email.Contains(search)));
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
                    MenuItemName = r.MenuItem != null ? r.MenuItem.Name : "Catering Package",
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
                PageTitle = "My Reviews"
            };

            return View(model);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Receipt(int id)
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
                        .ThenInclude(m => m!.Caretaker)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SelectedCustomizations)
                .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == currentUser.Id);

            if (order is null)
            {
                return NotFound();
            }

            return View(order);
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

        private static double CalculateStraightLineDistanceKm(
            double startLatitude,
            double startLongitude,
            double endLatitude,
            double endLongitude)
        {
            const double earthRadiusKm = 6371;

            var dLat = DegreesToRadians(endLatitude - startLatitude);
            var dLon = DegreesToRadians(endLongitude - startLongitude);

            var lat1 = DegreesToRadians(startLatitude);
            var lat2 = DegreesToRadians(endLatitude);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}



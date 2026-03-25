using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Lab3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lab3.Pages;

public class RegisterInputModel
{
    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Surname { get; set; } = "";

    [Required]
    public string Address { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    [Required]
    public string ConfirmPassword { get; set; } = "";
}

public class RegisterModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public RegisterModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? ProfilePhoto { get; set; }

    public string SuccessMessage { get; set; } = "";
    public string ErrorMessage { get; set; } = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Lütfen tüm alanları doğru şekilde doldurun.";
            return Page();
        }

        if (ProfilePhoto == null || ProfilePhoto.Length == 0)
        {
            ErrorMessage = "Lütfen bir profil fotoğrafı seçin.";
            return Page();
        }

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Password ve Confirm Password aynı olmalıdır.";
            return Page();
        }

        if (Input.Password.Length < 8)
        {
            ErrorMessage = "Password en az 8 karakter olmalıdır.";
            return Page();
        }

        if (!Regex.IsMatch(Input.Password, "[A-Z]"))
        {
            ErrorMessage = "Password en az 1 büyük harf içermelidir.";
            return Page();
        }

        if (!Regex.IsMatch(Input.Password, "[0-9]"))
        {
            ErrorMessage = "Password en az 1 rakam içermelidir.";
            return Page();
        }

        string usersFilePath = Path.Combine(_environment.ContentRootPath, "App_Data", "users.json");
        var existingUsers = UserStorage.GetUsers(usersFilePath);

        bool emailExists = existingUsers.Any(x => x.Email.ToLower() == Input.Email.ToLower());

        if (emailExists)
        {
            ErrorMessage = "Bu email adresi zaten kayıtlı.";
            return Page();
        }

        string extension = Path.GetExtension(ProfilePhoto.FileName).ToLower();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (!allowedExtensions.Contains(extension))
        {
            ErrorMessage = "Sadece resim dosyaları yükleyebilirsiniz.";
            return Page();
        }

        string photoFolder = Path.Combine(_environment.WebRootPath, "profilephotos");

        if (!Directory.Exists(photoFolder))
        {
            Directory.CreateDirectory(photoFolder);
        }

        string uniqueFileName = Guid.NewGuid().ToString() + extension;
        string photoPath = Path.Combine(photoFolder, uniqueFileName);

        using (var stream = new FileStream(photoPath, FileMode.Create))
        {
            await ProfilePhoto.CopyToAsync(stream);
        }

        var newUser = new UserItem
        {
            Name = Input.Name,
            Surname = Input.Surname,
            Email = Input.Email,
            Address = Input.Address,
            PhotoPath = "/profilephotos/" + uniqueFileName
        };

        UserStorage.AddUser(usersFilePath, newUser);

        return RedirectToPage("/Index");
    }
}
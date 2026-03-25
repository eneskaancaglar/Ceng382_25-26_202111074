using Lab3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lab3.Pages;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public List<UserItem> Users { get; set; } = new List<UserItem>();
    public List<string> CSharpImages { get; set; } = new List<string>();

    public void OnGet()
    {
        LoadUsersFromFile();
        LoadSavedImages();
    }

    public async Task OnPostUploadCSharpAsync(List<IFormFile> CSharpFiles)
    {
        LoadUsersFromFile();

        string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");

        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        if (CSharpFiles != null && CSharpFiles.Count > 0)
        {
            foreach (var file in CSharpFiles)
            {
                if (file.Length > 0)
                {
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                    if (!allowedExtensions.Contains(extension))
                    {
                        continue;
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }
        }

        LoadSavedImages();
    }

    private void LoadUsersFromFile()
    {
        string usersFilePath = Path.Combine(_environment.ContentRootPath, "App_Data", "users.json");
        Users = UserStorage.GetUsers(usersFilePath);
    }

    private void LoadSavedImages()
    {
        string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");

        CSharpImages = new List<string>();

        if (Directory.Exists(uploadFolder))
        {
            var files = Directory.GetFiles(uploadFolder);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                CSharpImages.Add("/uploads/" + fileName);
            }
        }
    }
}
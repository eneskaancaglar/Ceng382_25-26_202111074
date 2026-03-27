using Lab3.Data;
using Lab3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lab3.Pages;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _context;

    public IndexModel(IWebHostEnvironment environment, ApplicationDbContext context)
    {
        _environment = environment;
        _context = context;
    }

    public List<AppUser> Users { get; set; } = new();
    public List<string> CSharpImages { get; set; } = new();

    public void OnGet()
    {
        Users = _context.Users.ToList();
        LoadSavedImages();
    }

    public async Task OnPostUploadCSharpAsync(List<IFormFile> CSharpFiles)
    {
        Users = _context.Users.ToList();

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
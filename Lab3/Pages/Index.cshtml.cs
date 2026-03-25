using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyRazorAuthApp.Pages;

public class IndexModel : PageModel
{
    public string[] StudentNames { get; set; } = [];
    public int[] MidtermGrades { get; set; } = [];
    public int[] FinalGrades { get; set; } = [];
    public string[] LetterGrades { get; set; } = [];

    public void OnGet()
    {
        StudentNames = new[] { "Ayşe", "Mehmet", "Zeynep" };
        MidtermGrades = new[] { 70, 55, 90 };
        FinalGrades = new[] { 80, 60, 75 };
        LetterGrades = new[] { "-", "-", "-" };
    }

    public void OnPostCalculate()
    {
        StudentNames = new[] { "Ayşe", "Mehmet", "Zeynep" };
        MidtermGrades = new[] { 70, 55, 90 };
        FinalGrades = new[] { 80, 60, 75 };

        LetterGrades = new string[StudentNames.Length];

        for (int i = 0; i < StudentNames.Length; i++)
        {
            double average = MidtermGrades[i] * 0.40 + FinalGrades[i] * 0.60;

            if (average >= 90)
                LetterGrades[i] = "AA";
            else if (average >= 85)
                LetterGrades[i] = "BA";
            else if (average >= 80)
                LetterGrades[i] = "BB";
            else if (average >= 70)
                LetterGrades[i] = "CB";
            else if (average >= 60)
                LetterGrades[i] = "CC";
            else
                LetterGrades[i] = "FF";
        }
    }
}
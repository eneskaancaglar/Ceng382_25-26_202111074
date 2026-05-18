namespace TasteAtDoor.Models.ViewModels
{
    public class AdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string RolesText { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public int MenuCount { get; set; }

        public double AverageCatererRating { get; set; }
    }
}


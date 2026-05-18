namespace TasteAtDoor.Models.ViewModels
{
	public class AdminUserListPageViewModel
	{
		public List<AdminUserListItemViewModel> Users { get; set; } = new();

		public string Search { get; set; } = string.Empty;

		public string Role { get; set; } = string.Empty;

		public int Page { get; set; }

		public int TotalPages { get; set; }
	}
}


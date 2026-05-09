namespace TasteAtDoor.Models.ViewModels
{
    public class ReviewListPageViewModel
    {
        public List<ReviewListItemViewModel> Reviews { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public int? MenuRating { get; set; }

        public int? CatererRating { get; set; }

        public int Page { get; set; }

        public int TotalPages { get; set; }

        public string PageTitle { get; set; } = "Reviews";
    }
}
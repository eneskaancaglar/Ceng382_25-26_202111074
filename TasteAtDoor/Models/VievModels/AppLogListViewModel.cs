using TasteAtDoor.Models;

namespace TasteAtDoor.Models.ViewModels
{
    public class AppLogListViewModel
    {
        public List<AppLog> Logs { get; set; } = new();

        public string Search { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;

        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
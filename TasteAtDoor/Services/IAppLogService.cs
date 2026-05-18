namespace TasteAtDoor.Services
{
    public interface IAppLogService
    {
        Task LogAsync(
            string eventType,
            string message,
            string level = "Info",
            string? userId = null,
            string? userEmail = null,
            string? details = null);
    }
}


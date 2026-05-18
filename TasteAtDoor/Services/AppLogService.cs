using TasteAtDoor.Data;
using TasteAtDoor.Models;

namespace TasteAtDoor.Services
{
    public class AppLogService : IAppLogService
    {
        private readonly ApplicationDbContext _context;

        public AppLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            string eventType,
            string message,
            string level = "Info",
            string? userId = null,
            string? userEmail = null,
            string? details = null)
        {
            try
            {
                var log = new AppLog
                {
                    EventType = eventType,
                    Message = message,
                    Level = level,
                    UserId = userId,
                    UserEmail = userEmail,
                    Details = details
                };

                _context.AppLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
            }
        }
    }
}


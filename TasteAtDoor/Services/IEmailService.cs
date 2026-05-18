namespace TasteAtDoor.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }
}


namespace TasteAtDoor.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string to, string subject, string htmlBody);
    }
}
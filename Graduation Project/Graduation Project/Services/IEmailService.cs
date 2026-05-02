namespace Graduation_Project.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
    }
}

namespace Graduation_Project.Interfaces
{
    public interface IDoctorNotificationService
    {
        Task NotifyAsync(int doctorId, string title, string message, string type, string? actionUrl = null, bool sendPush = true);
    }
}

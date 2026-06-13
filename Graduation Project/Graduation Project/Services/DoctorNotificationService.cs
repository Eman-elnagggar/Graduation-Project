using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class DoctorNotificationService : IDoctorNotificationService
    {
        private readonly AppDbContext _context;
        private readonly IPushNotificationService _push;

        public DoctorNotificationService(AppDbContext context, IPushNotificationService push)
        {
            _context = context;
            _push = push;
        }

        public async Task NotifyAsync(int doctorId, string title, string message, string type, string? actionUrl = null)
        {
            _context.DoctorNotifications.Add(new DoctorNotification
            {
                DoctorID = doctorId,
                Title = title,
                Message = message,
                NotificationType = type,
                DateCreated = DateTime.Now,
                IsRead = false,
                ActionUrl = actionUrl
            });
            await _context.SaveChangesAsync();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorID == doctorId);
            if (!string.IsNullOrEmpty(doctor?.UserID))
                _ = _push.SendToUserAsync(doctor.UserID, title, message, actionUrl ?? "/Doctor/Index");
        }
    }
}

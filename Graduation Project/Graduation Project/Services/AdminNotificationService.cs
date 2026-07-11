using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Identity;

namespace Graduation_Project.Services
{
    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly AppDbContext _context;
        private readonly IPushNotificationService _push;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminNotificationService(
            AppDbContext context,
            IPushNotificationService push,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _push = push;
            _userManager = userManager;
        }

        public async Task NotifyAsync(string title, string message, string type, string? actionUrl = null, string severity = "info", bool sendPush = true)
        {
            _context.AdminNotifications.Add(new AdminNotification
            {
                Title = title,
                Message = message,
                NotificationType = type,
                Severity = severity,
                DateCreated = DateTime.Now,
                IsRead = false,
                ActionUrl = actionUrl
            });
            await _context.SaveChangesAsync();

            if (!sendPush) return;

            // The feed is shared, so every admin gets the push.
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
                _ = _push.SendToUserAsync(admin.Id, title, message, actionUrl ?? "/Admin/Index");
        }
    }
}

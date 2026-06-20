using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        private readonly IChatMessageCrypto _chatMessageCrypto;
        private readonly IPushNotificationService _push;
        private readonly IPatientNotificationService _patientNotifications;
        private readonly IDoctorNotificationService _doctorNotifications;

        public ChatHub(AppDbContext db, IChatMessageCrypto chatMessageCrypto,
            IPushNotificationService push, IPatientNotificationService patientNotifications,
            IDoctorNotificationService doctorNotifications)
        {
            _db = db;
            _chatMessageCrypto = chatMessageCrypto;
            _push = push;
            _patientNotifications = patientNotifications;
            _doctorNotifications = doctorNotifications;
        }

        public async Task SendMessage(string receiverId, string message)
        {
            if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(message))
                return;

            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(senderId))
                return;

            var text = message.Trim();
            if (text.Length == 0)
                return;

            var chatMessage = new ChatMessage
            {
                SenderUserId = senderId,
                ReceiverUserId = receiverId,
                Message = _chatMessageCrypto.Encrypt(text),
                SentAtUtc = DateTime.UtcNow,
                IsRead = false
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, text, chatMessage.SentAtUtc, (string?)null, (string?)null, (string?)null);
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, text, chatMessage.SentAtUtc, (string?)null, (string?)null, (string?)null);

            _ = SendMessagePushAsync(senderId, receiverId, text);
        }

        public async Task SendFileMessage(string receiverId, string text, string attachmentUrl, string attachmentType, string attachmentName)
        {
            if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(attachmentUrl))
                return;

            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(senderId))
                return;

            var safeText = (text ?? string.Empty).Trim();

            var chatMessage = new ChatMessage
            {
                SenderUserId = senderId,
                ReceiverUserId = receiverId,
                Message = _chatMessageCrypto.Encrypt(string.IsNullOrEmpty(safeText) ? "[attachment]" : safeText),
                AttachmentUrl = attachmentUrl,
                AttachmentType = attachmentType,
                AttachmentName = attachmentName,
                SentAtUtc = DateTime.UtcNow,
                IsRead = false
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, safeText, chatMessage.SentAtUtc, attachmentUrl, attachmentType, attachmentName);
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, safeText, chatMessage.SentAtUtc, attachmentUrl, attachmentType, attachmentName);

            var notifBody = string.IsNullOrEmpty(safeText) ? "sent you a file" : safeText;
            _ = SendMessagePushAsync(senderId, receiverId, notifBody);
        }

        private async Task SendMessagePushAsync(string senderId, string receiverId, string text)
        {
            try
            {
                var sender = await _db.Users.FindAsync(senderId);
                var senderName = BuildDisplayName(sender);

                var receiverPatient = await _db.Patients.FirstOrDefaultAsync(p => p.UserID == receiverId);
                var isPatient = receiverPatient != null;
                var url = isPatient ? "/Patient/Messages" : "/Doctor/Messages";

                var preview = text.Length > 80 ? text[..80] + "…" : text;
                var title = $"New message from {senderName}";

                // Persist a bell notification for the recipient (push is sent below, so these
                // pass sendPush:false to avoid a duplicate push).
                if (receiverPatient != null)
                {
                    _patientNotifications.Notify(receiverPatient.PatientID,
                        title, preview, PatientNotificationTypes.Message, url, sendPush: false);
                }
                else
                {
                    var receiverDoctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserID == receiverId);
                    if (receiverDoctor != null)
                    {
                        await _doctorNotifications.NotifyAsync(receiverDoctor.DoctorID,
                            title, preview, PatientNotificationTypes.Message, url, sendPush: false);
                    }
                }

                await _push.SendToUserAsync(receiverId, title, preview, url);
            }
            catch { }
        }

        private static string BuildDisplayName(ApplicationUser? user)
        {
            if (user == null) return "Someone";
            var full = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrEmpty(full) ? user.UserName ?? "Someone" : full;
        }
    }
}

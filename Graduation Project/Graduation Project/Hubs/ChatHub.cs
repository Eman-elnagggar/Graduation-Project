using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Graduation_Project.Data;
using Graduation_Project.Models;

namespace Graduation_Project.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;

        public ChatHub(AppDbContext db)
        {
            _db = db;
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
                Message = text,
                SentAtUtc = DateTime.Now,
                IsRead = false
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, text, chatMessage.SentAtUtc);
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, text, chatMessage.SentAtUtc);
        }
    }
}

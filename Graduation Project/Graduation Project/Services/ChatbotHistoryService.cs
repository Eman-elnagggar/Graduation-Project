using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class ChatbotHistoryService : IChatbotHistoryService
    {
        private readonly AppDbContext _context;

        public ChatbotHistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ChatbotMessage>> GetHistoryAsync(int patientId, CancellationToken cancellationToken = default)
        {
            if (patientId <= 0)
            {
                throw new ArgumentException("Patient id is required.", nameof(patientId));
            }

            return await _context.ChatbotMessages
                .AsNoTracking()
                .Where(m => m.PatientID == patientId)
                .OrderBy(m => m.SentAtUtc)
                .ThenBy(m => m.ChatbotMessageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<ChatbotMessage> SaveMessageAsync(ChatbotMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _context.ChatbotMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);
            return message;
        }

        public async Task ClearHistoryAsync(int patientId, CancellationToken cancellationToken = default)
        {
            if (patientId <= 0)
            {
                throw new ArgumentException("Patient id is required.", nameof(patientId));
            }

            var messages = await _context.ChatbotMessages
                .Where(m => m.PatientID == patientId)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                return;
            }

            _context.ChatbotMessages.RemoveRange(messages);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

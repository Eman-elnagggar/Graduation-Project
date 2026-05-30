using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IChatbotHistoryService
    {
        Task<IReadOnlyList<ChatbotMessage>> GetHistoryAsync(int patientId, CancellationToken cancellationToken = default);
        Task<ChatbotMessage> SaveMessageAsync(ChatbotMessage message, CancellationToken cancellationToken = default);
        Task ClearHistoryAsync(int patientId, CancellationToken cancellationToken = default);
    }
}

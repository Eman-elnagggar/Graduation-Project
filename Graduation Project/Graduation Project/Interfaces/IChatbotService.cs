using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IChatbotService
    {
        Task<ChatbotApiResponse> GetReplyAsync(string message, CancellationToken cancellationToken = default);
    }
}

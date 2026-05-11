using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Graduation_Project.Services
{
    public interface IUltrasoundAIService
    {
        Task<UltrasoundAIResult> AnalyzeAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
    }
}

using Graduation_Project.ViewModels.Analysis;

namespace Graduation_Project.Interfaces
{
    public interface IAnalysisService
    {
        Task<AnalysisUploadResponse> UploadAndExtractAsync(AnalysisUploadRequest request, CancellationToken cancellationToken = default);
        Task<AnalysisUploadResponse> ConfirmAsync(int labTestId, AnalysisConfirmRequest request, CancellationToken cancellationToken = default);
        Task ProcessAnalysisAsync(int labTestId, CancellationToken cancellationToken = default);
        Task<AnalysisResultResponse?> GetAnalysisResultAsync(int labTestId, CancellationToken cancellationToken = default);
        Task<OcrOnlyResponse> OcrOnlyAsync(OcrOnlyRequest request, CancellationToken cancellationToken = default);
        Task<BatchSubmitResponse> BatchSubmitAsync(BatchSubmitRequest request, CancellationToken cancellationToken = default);
    }
}

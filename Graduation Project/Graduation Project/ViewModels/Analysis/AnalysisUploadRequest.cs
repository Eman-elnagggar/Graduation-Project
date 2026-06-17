using Microsoft.AspNetCore.Http;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisUploadRequest
    {
        public int PatientId { get; set; }
        public string TestType { get; set; } = string.Empty;
        public int? ReportId { get; set; }
        public IFormFile? Image { get; set; }
    }
}

using System.Collections.Generic;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisUploadResponse
    {
        public int LabTestId { get; set; }
        public int? ReportId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public Dictionary<string, object> ExtractedValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

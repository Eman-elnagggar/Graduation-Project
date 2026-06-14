using System.Collections.Generic;

namespace Graduation_Project.ViewModels.Analysis
{
    public class OcrOnlyResponse
    {
        public string TestName { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public Dictionary<string, object> ExtractedValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string? TempImagePath { get; set; }
    }
}

using System.Collections.Generic;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisOcrResponse
    {
        public string? TestName { get; set; }
        public decimal? Confidence { get; set; }
        public Dictionary<string, object> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

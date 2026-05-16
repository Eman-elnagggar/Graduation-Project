using System.Collections.Generic;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisConfirmRequest
    {
        public Dictionary<string, object> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

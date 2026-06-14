using System.Collections.Generic;

namespace Graduation_Project.ViewModels.Analysis
{
    public class BatchSubmitRequest
    {
        public int PatientId { get; set; }
        public List<BatchTestItem> Tests { get; set; } = new();
    }

    public class BatchTestItem
    {
        public string? TempImagePath { get; set; }
        public string TestType { get; set; } = string.Empty;
        public string? TestName { get; set; }
        public Dictionary<string, object> ConfirmedValues { get; set; } = new();
    }

    public class BatchSubmitResponse
    {
        public int ReportId { get; set; }
        public int LabTestId { get; set; }
    }
}

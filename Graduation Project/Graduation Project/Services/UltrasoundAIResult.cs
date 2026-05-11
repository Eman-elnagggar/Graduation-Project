namespace Graduation_Project.Services
{
    public class UltrasoundAIResult
    {
        public byte[] ProcessedImageBytes { get; set; }
        public string Prediction { get; set; }
        public double? ConfidenceScore { get; set; }
        public string RawJson { get; set; }
    }
}

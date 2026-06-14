using Microsoft.AspNetCore.Http;

namespace Graduation_Project.ViewModels.Analysis
{
    public class OcrOnlyRequest
    {
        public string TestType { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}

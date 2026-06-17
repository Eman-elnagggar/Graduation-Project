using Graduation_Project.Interfaces;

namespace Graduation_Project.Services
{
    public class AnalysisBackgroundJob
    {
        private readonly IAnalysisService _analysisService;
        private readonly ILogger<AnalysisBackgroundJob> _logger;

        public AnalysisBackgroundJob(IAnalysisService analysisService, ILogger<AnalysisBackgroundJob> logger)
        {
            _analysisService = analysisService;
            _logger = logger;
        }

        public async Task ProcessAsync(int labTestId)
        {
            try
            {
                await _analysisService.ProcessAnalysisAsync(labTestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background processing failed for lab test {LabTestId}.", labTestId);
                throw;
            }
        }
    }
}

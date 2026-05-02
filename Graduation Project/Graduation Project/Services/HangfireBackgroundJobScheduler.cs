using Microsoft.Extensions.DependencyInjection;

namespace Graduation_Project.Services
{
    public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HangfireBackgroundJobScheduler> _logger;

        public HangfireBackgroundJobScheduler(IServiceProvider serviceProvider, ILogger<HangfireBackgroundJobScheduler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void EnqueueAnalysis(int labTestId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<AnalysisBackgroundJob>();
                    await job.ProcessAsync(labTestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process analysis in background for lab test {LabTestId}.", labTestId);
                }
            });
        }
    }
}

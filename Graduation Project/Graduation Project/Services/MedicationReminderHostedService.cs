using Microsoft.Extensions.Hosting;

namespace Graduation_Project.Services
{
    public class MedicationReminderHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public MedicationReminderHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<MedicationReminderService>();
                reminderService.EvaluateReminders(DateTime.Now);

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}

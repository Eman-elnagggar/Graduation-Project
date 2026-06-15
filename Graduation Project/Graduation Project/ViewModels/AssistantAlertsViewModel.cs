using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AssistantAlertsViewModel
    {
        public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public List<Alert> Alerts { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}

using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AlertsViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";

        // All alerts ordered newest-first
        public List<Alert> Alerts { get; set; } = new();

        // Quick-stat counts
        // "danger" and "critical" both map to the Critical bucket
        public int CriticalCount => Alerts.Count(a =>
            a.AlertType?.ToLower() == "danger" ||
            a.AlertType?.ToLower() == "critical");
        public int WarningCount  => Alerts.Count(a => a.AlertType?.ToLower() == "warning");
        public int InfoCount     => Alerts.Count(a =>
            a.AlertType?.ToLower() != "danger"   &&
            a.AlertType?.ToLower() != "critical" &&
            a.AlertType?.ToLower() != "warning");
        public int UnreadCount   => Alerts.Count(a => !a.IsRead);
        public int TotalCount    => Alerts.Count;
    }
}

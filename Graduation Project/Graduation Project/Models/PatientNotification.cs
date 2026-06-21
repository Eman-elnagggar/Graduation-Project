using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    /// <summary>
    /// A patient-facing (or clinic-operational) notification — reminders, status
    /// updates and operational events. Distinct from <see cref="Alert"/>, which is
    /// reserved for clinical health alerts that require the patient's attention.
    /// NotificationType: "medication" | "appointment" | "ultrasound" | "operational".
    /// "operational" notifications are surfaced to clinic staff (assistants), all
    /// other types are surfaced to the patient.
    /// </summary>
    public class PatientNotification
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;

        /// <summary>Drives icon/color in the UI: "info" | "warning" | "danger" | "success".</summary>
        public string Severity { get; set; } = "info";

        public DateTime DateCreated { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; } = null!;
    }
}

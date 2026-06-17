using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class MedicationReminderSettings
    {
        [Key]
        public int MedicationReminderSettingsId { get; set; }

        [ForeignKey(nameof(Patient))]
        public int PatientID { get; set; }

        public int LeadTimeMinutes { get; set; } = 30;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual Patient Patient { get; set; } = null!;
    }
}

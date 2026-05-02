using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Medication
    {
        [Key]
        public int MedicationId { get; set; }

        [ForeignKey(nameof(Patient))]
        public int PatientID { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public MedicationSource Source { get; set; } = MedicationSource.Prescription;
        public int? ReminderLeadTimeMinutes { get; set; }

        [ForeignKey(nameof(PrescriptionItem))]
        public int? PrescriptionItemId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Patient Patient { get; set; } = null!;
        public virtual PrescriptionItem? PrescriptionItem { get; set; }
        public virtual ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();
        public virtual ICollection<MedicationLog> Logs { get; set; } = new List<MedicationLog>();
    }
}

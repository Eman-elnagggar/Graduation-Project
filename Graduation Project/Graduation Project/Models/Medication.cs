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

        /// <summary>Pharmaceutical form — Tablet, Syrup, Injection… (display only).</summary>
        public string? Form { get; set; }

        /// <summary>Human-readable frequency label, e.g. "3 times daily".</summary>
        public string Frequency { get; set; } = string.Empty;

        /// <summary>Catalogue code from <see cref="MedicationFrequencies"/>; drives the Edit form.</summary>
        public string? FrequencyCode { get; set; }

        /// <summary>Doses per active day. 0 means the medication is taken only as needed.</summary>
        public int TimesPerDay { get; set; } = 1;

        /// <summary>Days between active days — 1 = daily, 2 = every other day, 7 = weekly.</summary>
        public int IntervalDays { get; set; } = 1;

        public string Instructions { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public MedicationSource Source { get; set; } = MedicationSource.Prescription;
        public int? ReminderLeadTimeMinutes { get; set; }
        public int? TotalPills { get; set; }
        public int? PillsPerDose { get; set; }

        [ForeignKey(nameof(PrescriptionItem))]
        public int? PrescriptionItemId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Patient Patient { get; set; } = null!;
        public virtual PrescriptionItem? PrescriptionItem { get; set; }
        public virtual ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();
        public virtual ICollection<MedicationLog> Logs { get; set; } = new List<MedicationLog>();
    }
}

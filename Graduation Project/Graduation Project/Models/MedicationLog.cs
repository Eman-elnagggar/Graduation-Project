using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class MedicationLog
    {
        [Key]
        public int MedicationLogId { get; set; }

        [ForeignKey(nameof(Medication))]
        public int MedicationId { get; set; }

        public DateTime ScheduledAt { get; set; }
        public DateTime? TakenAt { get; set; }
        public MedicationLogStatus Status { get; set; } = MedicationLogStatus.Scheduled;
        public string? Notes { get; set; }

        public virtual Medication Medication { get; set; } = null!;
    }
}

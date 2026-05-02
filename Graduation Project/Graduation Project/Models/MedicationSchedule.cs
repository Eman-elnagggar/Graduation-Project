using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class MedicationSchedule
    {
        [Key]
        public int MedicationScheduleId { get; set; }

        [ForeignKey(nameof(Medication))]
        public int MedicationId { get; set; }

        public TimeSpan TimeOfDay { get; set; }
        public int FrequencyPerDay { get; set; }
        public string? Notes { get; set; }

        public virtual Medication Medication { get; set; } = null!;
    }
}

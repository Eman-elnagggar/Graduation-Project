using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        [ForeignKey("User")]
        public string UserID { get; set; }

        public string Address { get; set; }
        public DateTime? DateOfPregnancy { get; set; }
        public DateTime? LastPregnancyStartedAt { get; set; }
        public DateTime? PregnancyEndedAt { get; set; }
        public int PregnancyCount { get; set; }
        public int GestationalWeeks { get; set; }
        public bool IsFirstPregnancy { get; set; }
        public int PreviousPregnancies { get; set; }
        public int Abortions { get; set; }
        public int Births { get; set; }
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }

        // Health Habits
        public bool BloodPressureIssue { get; set; }
        public bool Smoking { get; set; }
        public bool AlcoholUse { get; set; }

        // Navigation
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<PatientDrug> PatientDrugs { get; set; }
        public virtual ICollection<PregnancyRecord> PregnancyRecords { get; set; }
    }
}

using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PatientProfileViewModel
    {
        // Core patient/user objects
    public Patient Patient { get; set; }
   public User User { get; set; }

        // Convenience display fields
        public string UserName { get; set; }
        public string FullName { get; set; }
      public int Age { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

  // Pregnancy
     public int PregnancyWeek { get; set; }
        public int PregnancyDays { get; set; }
        public int PregnancyProgressPercent { get; set; }
        public string Trimester { get; set; }
        public string DueDate { get; set; }
        public int DaysRemaining { get; set; }

        // BMI
        public double Bmi { get; set; }
        public string BmiStatus { get; set; }

        // Medications
        public List<PatientDrug> Medications { get; set; } = new();
    }
}

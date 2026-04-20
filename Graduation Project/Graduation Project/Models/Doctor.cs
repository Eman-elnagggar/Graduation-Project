using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorID { get; set; }

        [ForeignKey("User")]
        public string UserID { get; set; }

        public string Specialization { get; set; }
        public string LicenseNumber { get; set; }
        public string LicenseImagePath { get; set; }
        public string VerificationStatus { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string Address { get; set; }

        // Navigation
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<ClinicDoctor> ClinicDoctors { get; set; }
        public virtual ICollection<AssistantDoctor> AssistantDoctors { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}

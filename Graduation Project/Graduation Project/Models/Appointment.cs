using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        [ForeignKey("Patient")]
        public int? PatientID { get; set; }

        [ForeignKey("Clinic")]
        public int ClinicID { get; set; }

        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }

        public bool isBooked { get; set; }

        // Navigation
        public virtual Doctor Doctor { get; set; }
        public virtual Patient? Patient { get; set; }
        public virtual Clinic Clinic { get; set; }
        public virtual Booking Booking { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [ForeignKey("Appointment")]
        public int AppointmentID { get; set; }

        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int ClinicID { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }

        // Navigation
        public virtual Appointment Appointment { get; set; }
    }
}

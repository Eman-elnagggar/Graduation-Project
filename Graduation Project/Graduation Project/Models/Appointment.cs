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

        [Timestamp]
        public byte[] RowVersion { get; set; }

        [ForeignKey("CreatedByAssistant")]
        public int? CreatedByAssistantID { get; set; }

        // Navigation
        public virtual Doctor Doctor { get; set; }
        public virtual Patient? Patient { get; set; }
        public virtual Clinic Clinic { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        [NotMapped]
        public virtual Booking? Booking
        {
            get => Bookings
                .OrderByDescending(b => b.IsActive)
                .ThenByDescending(b => b.BookingID)
                .FirstOrDefault();
            set
            {
                if (value == null)
                    return;

                foreach (var existing in Bookings.Where(b => b.IsActive))
                    existing.IsActive = false;

                value.IsActive = true;

                if (!Bookings.Contains(value))
                    Bookings.Add(value);
            }
        }

        public virtual Assistant? CreatedByAssistant { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Note
    {
        [Key]
        public int NoteID { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public DateTime CreatedDate { get; set; }
        public string Content { get; set; }

        // Navigation
        public virtual Doctor Doctor { get; set; }
        public virtual Patient Patient { get; set; }
    }
}

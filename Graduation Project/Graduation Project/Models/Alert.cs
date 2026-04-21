using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Alert
    {
        [Key]
        public int AlertID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string AlertType { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsRead { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
    }
}

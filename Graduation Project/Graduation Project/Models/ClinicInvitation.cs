using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class ClinicInvitation
    {
        [Key]
        public int ClinicInvitationID { get; set; }

        [ForeignKey(nameof(Doctor))]
        public int DoctorID { get; set; }

        [ForeignKey(nameof(Clinic))]
        public int ClinicID { get; set; }

        [ForeignKey(nameof(Assistant))]
        public int AssistantID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string AssistantEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Pending";

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAtUtc { get; set; }

        [StringLength(500)]
        public string? ResponseMessage { get; set; }

        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Clinic Clinic { get; set; } = null!;
        public virtual Assistant Assistant { get; set; } = null!;
    }
}

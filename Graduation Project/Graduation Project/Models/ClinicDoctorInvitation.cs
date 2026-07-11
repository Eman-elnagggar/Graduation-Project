using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    // A clinic owner inviting another doctor to join the clinic.
    // Kept separate from ClinicInvitation (assistant invites), whose AssistantID
    // is required and drives the assistant leave-approval flow.
    public class ClinicDoctorInvitation
    {
        [Key]
        public int ClinicDoctorInvitationID { get; set; }

        [ForeignKey(nameof(Clinic))]
        public int ClinicID { get; set; }

        [ForeignKey(nameof(Inviter))]
        public int InviterDoctorID { get; set; }

        [ForeignKey(nameof(Invitee))]
        public int InviteeDoctorID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string InviteeEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Pending";

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAtUtc { get; set; }

        public virtual Clinic Clinic { get; set; } = null!;
        public virtual Doctor Inviter { get; set; } = null!;
        public virtual Doctor Invitee { get; set; } = null!;
    }
}

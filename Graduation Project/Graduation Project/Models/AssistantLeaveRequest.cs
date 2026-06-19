using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    // Raised when an assistant accepts an invitation from a DIFFERENT clinic.
    // The clinic switch is held until every linked doctor in her current
    // ("old") clinic approves. A single denial rejects the whole request.
    public class AssistantLeaveRequest
    {
        [Key]
        public int AssistantLeaveRequestID { get; set; }

        [ForeignKey(nameof(Assistant))]
        public int AssistantID { get; set; }

        // Clinic the assistant is leaving.
        [ForeignKey(nameof(OldClinic))]
        public int OldClinicID { get; set; }

        // Destination clinic / inviting doctor (copied from the invitation so the
        // switch can be executed later even if the invitation row is touched).
        [ForeignKey(nameof(NewClinic))]
        public int NewClinicID { get; set; }

        [ForeignKey(nameof(NewDoctor))]
        public int NewDoctorID { get; set; }

        // The invitation that triggered this leave request.
        [ForeignKey(nameof(Invitation))]
        public int ClinicInvitationID { get; set; }

        // Pending | Approved | Denied | Cancelled
        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAtUtc { get; set; }

        [StringLength(500)]
        public string? ResolutionMessage { get; set; }

        // Optimistic-concurrency guard so two doctors approving/denying at the
        // same instant cannot both execute the switch.
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public virtual Assistant Assistant { get; set; } = null!;
        public virtual Clinic OldClinic { get; set; } = null!;
        public virtual Clinic NewClinic { get; set; } = null!;
        public virtual Doctor NewDoctor { get; set; } = null!;
        public virtual ClinicInvitation Invitation { get; set; } = null!;

        public virtual ICollection<AssistantLeaveApproval> Approvals { get; set; } = new List<AssistantLeaveApproval>();
    }

    // One row per doctor whose approval is required for the leave request.
    public class AssistantLeaveApproval
    {
        [Key]
        public int AssistantLeaveApprovalID { get; set; }

        [ForeignKey(nameof(LeaveRequest))]
        public int AssistantLeaveRequestID { get; set; }

        [ForeignKey(nameof(Doctor))]
        public int DoctorID { get; set; }

        // Pending | Approved | Denied
        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Pending";

        public DateTime? RespondedAtUtc { get; set; }

        public virtual AssistantLeaveRequest LeaveRequest { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
    }
}

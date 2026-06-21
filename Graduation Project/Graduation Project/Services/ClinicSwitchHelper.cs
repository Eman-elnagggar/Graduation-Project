using Graduation_Project.Data;
using Graduation_Project.Models;

namespace Graduation_Project.Services
{
    // Shared execution of an assistant clinic switch so the assistant-side
    // immediate path and the doctor-side leave-approval path stay identical.
    // Mutates the tracked entities only; the caller owns SaveChanges and any
    // notifications.
    public static class ClinicSwitchHelper
    {
        public static void ExecuteSwitch(
            AppDbContext context,
            Assistant trackedAssistant,
            ClinicInvitation invitation,
            bool removeOldLinks)
        {
            if (removeOldLinks)
            {
                // Drop every existing doctor link so she starts fresh in the new clinic.
                var oldDoctorLinks = context.AssistantDoctors
                    .Where(ad => ad.AssistantID == trackedAssistant.AssistantID)
                    .ToList();
                context.AssistantDoctors.RemoveRange(oldDoctorLinks);

                // Supersede any other still-pending invitations she was carrying.
                var otherPending = context.ClinicInvitations
                    .Where(ci => ci.AssistantID == trackedAssistant.AssistantID
                              && ci.Status == "Pending"
                              && ci.ClinicInvitationID != invitation.ClinicInvitationID)
                    .ToList();
                foreach (var old in otherPending)
                {
                    old.Status = "Superseded";
                    old.RespondedAtUtc = DateTime.UtcNow;
                    old.ResponseMessage = "Superseded by clinic switch";
                }
            }

            // Assign the new clinic.
            trackedAssistant.ClinicID = invitation.ClinicID;

            // Link to the inviting doctor (if not already linked).
            var alreadyLinked = context.AssistantDoctors.Any(ad =>
                ad.DoctorID == invitation.DoctorID && ad.AssistantID == trackedAssistant.AssistantID);
            if (!alreadyLinked)
            {
                context.AssistantDoctors.Add(new AssistantDoctor
                {
                    DoctorID = invitation.DoctorID,
                    AssistantID = trackedAssistant.AssistantID
                });
            }

            invitation.Status = "Accepted";
            invitation.RespondedAtUtc = DateTime.UtcNow;
            invitation.ResponseMessage = "Accepted by assistant";
        }
    }
}

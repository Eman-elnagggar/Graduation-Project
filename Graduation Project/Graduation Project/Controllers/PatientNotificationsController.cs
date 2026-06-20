using Graduation_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    /// <summary>
    /// Patient-facing notifications (reminders / status updates) shown in the top-bar
    /// bell. Distinct from <see cref="PatientAlertsController"/>, which serves clinical
    /// health alerts. Operational notifications are excluded here (clinic-facing).
    /// </summary>
    [Authorize(Roles = "Patient")]
    public class PatientNotificationsController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPatientNotificationService _notifications;

        public PatientNotificationsController(
            IPatient patientRepository,
            IPatientNotificationService notifications)
        {
            _patientRepository = patientRepository;
            _notifications = notifications;
        }

        [HttpGet]
        public IActionResult GetNotifications(int patientId, int take = 20)
        {
            if (patientId <= 0)
                return BadRequest(new { success = false, message = "Invalid patient id." });

            var (patient, failure) = AuthorizePatientAccess(patientId);
            if (failure != null)
                return failure;

            var notifications = _notifications.GetForPatient(patientId, take);

            var response = new
            {
                success = true,
                userName = patient!.User?.FirstName ?? "Patient",
                unreadCount = notifications.Count(n => !n.IsRead),
                alerts = notifications.Select(n => new
                {
                    alertId = n.Id,
                    title = n.Title,
                    message = n.Message,
                    alertType = n.Severity,
                    dateCreated = n.DateCreated,
                    isRead = n.IsRead
                })
            };

            return Json(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAlertRead(int alertId, int patientId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId);
            if (failure != null)
                return failure;

            var notifications = _notifications.GetForPatient(patientId, 100);
            if (notifications.All(n => n.Id != alertId))
                return Json(new { success = false });

            _notifications.MarkRead(alertId);
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllAlertsRead(int patientId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId);
            if (failure != null)
                return failure;

            var count = _notifications.MarkAllReadForPatient(patientId);
            return Json(new { success = true, count });
        }

        private (Models.Patient? patient, IActionResult? failure) AuthorizePatientAccess(int patientId)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return (null, NotFound());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return (null, Unauthorized(new { success = false, message = "Unauthorized." }));

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." }));

            return (patient, null);
        }
    }
}

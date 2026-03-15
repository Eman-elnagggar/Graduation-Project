using Graduation_Project.Interfaces;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class PatientAlertsController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IAlert _alertRepository;

        public PatientAlertsController(IPatient patientRepository, IAlert alertRepository)
        {
            _patientRepository = patientRepository;
            _alertRepository = alertRepository;
        }

        public IActionResult Alerts(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var alerts = _alertRepository
                .GetByPatientId(id)
                .OrderByDescending(a => a.DateCreated)
                .ToList();

            var viewModel = new AlertsViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Alerts = alerts
            };

            return View("~/Views/Patient/Alerts.cshtml", viewModel);
        }

        [HttpGet]
        public IActionResult GetNotifications(int patientId, int take = 20)
        {
            if (patientId <= 0)
                return BadRequest(new { success = false, message = "Invalid patient id." });

            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return NotFound(new { success = false, message = "Patient not found." });

            var alerts = _alertRepository
                .GetByPatientId(patientId)
                .OrderByDescending(a => a.DateCreated)
                .Take(Math.Clamp(take, 1, 50))
                .ToList();

            var response = new
            {
                success = true,
                userName = patient.User?.FirstName ?? "Patient",
                unreadCount = alerts.Count(a => !a.IsRead),
                alerts = alerts.Select(a => new
                {
                    alertId = a.AlertID,
                    title = a.Title,
                    message = a.Message,
                    alertType = a.AlertType,
                    dateCreated = a.DateCreated,
                    isRead = a.IsRead
                })
            };

            return Json(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAlertRead(int alertId, int patientId)
        {
            var alert = _alertRepository.GetById(alertId);
            if (alert == null || alert.PatientID != patientId)
                return Json(new { success = false });

            alert.IsRead = true;
            _alertRepository.Update(alert);
            _alertRepository.Save();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllAlertsRead(int patientId)
        {
            var unread = _alertRepository
                .GetByPatientId(patientId)
                .Where(a => !a.IsRead)
                .ToList();

            foreach (var alert in unread)
            {
                alert.IsRead = true;
                _alertRepository.Update(alert);
            }

            _alertRepository.Save();
            return Json(new { success = true, count = unread.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAlert(int alertId, int patientId)
        {
            var alert = _alertRepository.GetById(alertId);
            if (alert == null || alert.PatientID != patientId)
                return Json(new { success = false });

            _alertRepository.Delete(alertId);
            _alertRepository.Save();

            return Json(new { success = true });
        }
    }
}

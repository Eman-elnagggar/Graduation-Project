using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPatientBloodPressure _patientBloodPressure;
        private readonly IPatientBloodSugar _patientBloodSugar;
        private readonly ILabTest _labTest;
        private readonly IAppointment _appointment;
        private readonly IUltrasoundImage _ultrasoundImage;
        private readonly IAlert _alertRepository;
        private readonly AlertService _alertService;

        public PatientController(
            IPatient patientRepository,
            IPatientBloodPressure patientBloodPressure,
            IPatientBloodSugar patientBloodSugar,
            ILabTest labTest,
            IAppointment appointment,
            IUltrasoundImage ultrasoundImage,
            IAlert alertRepository,
            AlertService alertService)
        {
            _patientRepository = patientRepository;
            _patientBloodPressure = patientBloodPressure;
            _patientBloodSugar = patientBloodSugar;
            _labTest = labTest;
            _appointment = appointment;
            _ultrasoundImage = ultrasoundImage;
            _alertRepository = alertRepository;
            _alertService = alertService;
        }

        public IActionResult Index(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            // Calculate current pregnancy week
            int currentWeek = 0;
            if (patient.DateOfPregnancy.HasValue)
            {
                int daysSinceStart = (int)(DateTime.Today - patient.DateOfPregnancy.Value.Date).TotalDays;
                currentWeek = Math.Clamp(daysSinceStart / 7, 0, 40);
            }
            else if (patient.GestationalWeeks > 0)
            {
                currentWeek = Math.Clamp(patient.GestationalWeeks, 0, 40);
            }

            // Calculate due date (280 days = 40 weeks from start)
            string dueDate = patient.DateOfPregnancy.HasValue
                ? patient.DateOfPregnancy.Value.AddDays(280).ToString("MMM dd, yyyy")
                : "N/A";

            // Fetch latest health readings
            var lastBP = _patientBloodPressure.GetLastBloodPressureValue(id);
            var lastBS = _patientBloodSugar.GetLastBloodSugarValue(id);
            var lastLab = _labTest.GetLastLabTestByPatientId(id);
            var nextAppt = _appointment.GetNextAppointmentForPatient(id);

            // Fetch recent readings for the tracker panels
            var recentBPReadings = _patientBloodPressure.GetRecentByPatientId(id, 10).ToList();
            var recentBSReadings = _patientBloodSugar.GetRecentByPatientId(id, 10).ToList();

            // Fetch a larger window for weekly chart aggregation
            var weeklyBPReadings = _patientBloodPressure.GetRecentByPatientId(id, 40).ToList();
            var weeklyBSReadings = _patientBloodSugar.GetRecentByPatientId(id, 40).ToList();

            // Evaluate patient data and persist any new critical alerts.
            // Pass ALL recent readings so every abnormal value generates an alert,
            // not just whichever reading happens to be "last".
            _alertService.EvaluateAndSaveAlerts(id, patient, recentBPReadings, recentBSReadings, lastLab, nextAppt);

            // Load unread alerts for the dashboard (most recent 5)
            var healthAlerts = _alertRepository
                .GetByPatientId(id)
                .Where(a => !a.IsRead)
                .Take(5)
                .ToList();

            // Build recent activity feed
            var activities = new List<RecentActivityItem>();

            if (lastBP != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Blood Pressure Recorded",
                    Description = $"{lastBP.BloodPressure} mmHg",
                    DateTime = lastBP.DateTime,
                    IconClass = "fas fa-heartbeat",
                    IconBgColor = "#e3f2fd",
                    IconColor = "#2196f3"
                });
            }

            if (lastBS != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Blood Sugar Recorded",
                    Description = $"{lastBS.BloodSugar} mg/dL",
                    DateTime = lastBS.DateTime,
                    IconClass = "fas fa-tint",
                    IconBgColor = "#fce4ec",
                    IconColor = "#e91e63"
                });
            }

            if (lastLab != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = $"{lastLab.TestType} Test Uploaded",
                    Description = "AI Analysis Complete",
                    DateTime = lastLab.UploadDate,
                    IconClass = "fas fa-flask",
                    IconBgColor = "#e8f5e9",
                    IconColor = "#4caf50"
                });
            }

            var lastUltrasound = _ultrasoundImage.GetLastUltrasoundByPatientId(id);
            if (lastUltrasound != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Ultrasound Analyzed",
                    Description = string.IsNullOrWhiteSpace(lastUltrasound.DetectedAnomaly)
                        ? "No anomalies detected"
                        : lastUltrasound.DetectedAnomaly,
                    DateTime = lastUltrasound.UploadDate,
                    IconClass = "fas fa-baby",
                    IconBgColor = "#f3e5f5",
                    IconColor = "#9c27b0"
                });
            }

            if (nextAppt != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Upcoming Appointment",
                    Description = $"Dr. {nextAppt.Doctor?.User?.FirstName} - {nextAppt.Date:MMM dd, yyyy}",
                    DateTime = DateTime.Now,
                    OverrideTime = nextAppt.Date.ToString("MMM dd, yyyy"),
                    IconClass = "fas fa-calendar-check",
                    IconBgColor = "#fff3e0",
                    IconColor = "#ff9800"
                });
            }

            // Sort by most recent first, keep top 5
            activities = activities
                .OrderByDescending(a => a.DateTime)
                .Take(5)
                .ToList();

            var viewModel = new PatientDashboardViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                PregnancyWeek = currentWeek,
                PregnancyProgressPercent = (int)Math.Round(currentWeek / 40.0 * 100),
                Trimester = currentWeek <= 13 ? "1st Trimester"
                          : currentWeek <= 26 ? "2nd Trimester"
                          : "3rd Trimester",
                DueDate = dueDate,
                LastBloodPressureValue = lastBP?.BloodPressure ?? "N/A",
                LastBloodSugarValue = lastBS?.BloodSugar ?? 0,
                LastLabTest = lastLab,
                NextAppointment = nextAppt,
                RecentBloodPressureReadings = recentBPReadings,
                RecentBloodSugarReadings = recentBSReadings,
                WeeklyBloodPressureReadings = weeklyBPReadings,
                WeeklyBloodSugarReadings = weeklyBSReadings,
                RecentActivities = activities,
                HealthAlerts = healthAlerts
            };

            return View(viewModel);
        }

        // ---------------------------------------------------------------
        // POST: /Patient/SaveBloodPressure
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveBloodPressure(int patientId, string systolic, string diastolic, string? pulse, string? measurementTime)
        {
            if (string.IsNullOrWhiteSpace(systolic) || string.IsNullOrWhiteSpace(diastolic))
                return BadRequest(new { success = false, message = "Systolic and diastolic values are required." });

            var reading = new PatientBloodPressure
            {
                PatientID = patientId,
                BloodPressure = $"{systolic}/{diastolic}",
                DateTime = DateTime.Now,
                MeasurementTime = measurementTime
            };

            _patientBloodPressure.Add(reading);
            _patientBloodPressure.Save();

            // Evaluate and persist alerts for the new reading immediately
            var patient = _patientRepository.GetById(patientId);
            if (patient != null)
            {
                var lastBS = _patientBloodSugar.GetLastBloodSugarValue(patientId);
                var lastLab = _labTest.GetLastLabTestByPatientId(patientId);
                var nextAppt = _appointment.GetNextAppointmentForPatient(patientId);
                _alertService.EvaluateAndSaveAlerts(patientId, patient, reading, lastBS, lastLab, nextAppt);
            }

            return Json(new
            {
                success = true,
                id = reading.ID,
                bloodPressure = reading.BloodPressure,
                dateTime = reading.DateTime.ToString("MMM dd, yyyy hh:mm tt"),
                day = reading.DateTime.Day.ToString(),
                month = reading.DateTime.ToString("MMM"),
                time = reading.DateTime.ToString("h:mm tt"),
                measurementTime = reading.MeasurementTime
            });
        }

        // ---------------------------------------------------------------
        // POST: /Patient/SaveBloodSugar
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveBloodSugar(int patientId, double bloodSugar, string? measurementTime)
        {
            if (bloodSugar <= 0)
                return BadRequest(new { success = false, message = "Blood sugar value is required." });

            var reading = new PatientBloodSugar
            {
                PatientID = patientId,
                BloodSugar = bloodSugar,
                DateTime = DateTime.Now,
                MeasurementTime = measurementTime
            };

            _patientBloodSugar.Add(reading);
            _patientBloodSugar.Save();

            // Evaluate and persist alerts for the new reading immediately
            var patient = _patientRepository.GetById(patientId);
            if (patient != null)
            {
                var lastBP = _patientBloodPressure.GetLastBloodPressureValue(patientId);
                var lastLab = _labTest.GetLastLabTestByPatientId(patientId);
                var nextAppt = _appointment.GetNextAppointmentForPatient(patientId);
                _alertService.EvaluateAndSaveAlerts(patientId, patient, lastBP, reading, lastLab, nextAppt);
            }

            return Json(new
            {
                success = true,
                id = reading.ID,
                bloodSugar = reading.BloodSugar,
                dateTime = reading.DateTime.ToString("MMM dd, yyyy hh:mm tt"),
                day = reading.DateTime.Day.ToString(),
                month = reading.DateTime.ToString("MMM"),
                time = reading.DateTime.ToString("h:mm tt"),
                measurementTime = reading.MeasurementTime
            });
        }
    }
}

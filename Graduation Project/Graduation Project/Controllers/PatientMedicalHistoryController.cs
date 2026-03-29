using Graduation_Project.Interfaces;
using Graduation_Project.Data;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientMedicalHistoryController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPatientBloodPressure _patientBloodPressure;
        private readonly IPatientBloodSugar _patientBloodSugar;
        private readonly ILabTest _labTest;
        private readonly IUltrasoundImage _ultrasoundImage;
        private readonly IAppointment _appointment;
        private readonly IAlert _alertRepository;
        private readonly INote _noteRepository;
        private readonly IPrescription _prescriptionRepository;
        private readonly AppDbContext _context;

        public PatientMedicalHistoryController(
            IPatient patientRepository,
            IPatientBloodPressure patientBloodPressure,
            IPatientBloodSugar patientBloodSugar,
            ILabTest labTest,
            IUltrasoundImage ultrasoundImage,
            IAppointment appointment,
            IAlert alertRepository,
            INote noteRepository,
            IPrescription prescriptionRepository,
            AppDbContext context)
        {
            _patientRepository = patientRepository;
            _patientBloodPressure = patientBloodPressure;
            _patientBloodSugar = patientBloodSugar;
            _labTest = labTest;
            _ultrasoundImage = ultrasoundImage;
            _appointment = appointment;
            _alertRepository = alertRepository;
            _noteRepository = noteRepository;
            _prescriptionRepository = prescriptionRepository;
            _context = context;
        }

        public IActionResult MedicalHistory(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return Forbid();

            var bpReadings = _patientBloodPressure.GetRecentByPatientId(id, 200).ToList();
            var bsReadings = _patientBloodSugar.GetRecentByPatientId(id, 200).ToList();
            var labTests = _labTest.GetLabTestsByPatientId(id).ToList();
            var ultrasounds = _ultrasoundImage.GetUltrasoundsByPatientId(id).ToList();
            var appointments = _appointment.GetByPatientId(id).ToList();
            var alerts = _alertRepository.GetByPatientId(id).ToList();
            var notes = _noteRepository.GetByPatientId(id).ToList();
            var prescriptions = _prescriptionRepository.GetByPatientId(id).ToList();
            var pregnancyRecords = _context.PregnancyRecords
                .Where(r => r.PatientID == id)
                .ToList();

            var entries = new List<MedicalHistoryEntry>();

            foreach (var bp in bpReadings)
            {
                var parts = bp.BloodPressure?.Split('/');
                string status = "normal";
                if (parts?.Length == 2 &&
                    int.TryParse(parts[0], out int sys) &&
                    int.TryParse(parts[1], out int dia))
                {
                    if (sys >= 160 || dia >= 110) status = "critical";
                    else if (sys >= 140 || dia >= 90) status = "attention";
                }

                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = bp.DateTime,
                    EventType = "bp-reading",
                    Status = status,
                    Title = "Blood Pressure Reading",
                    SubTitle = $"{bp.BloodPressure} mmHg{(bp.MeasurementTime != null ? $" · {bp.MeasurementTime}" : "")}",
                    BloodPressure = bp
                });
            }

            foreach (var bs in bsReadings)
            {
                string status = bs.BloodSugar >= 200 ? "critical"
                              : bs.BloodSugar >= 140 ? "attention"
                              : "normal";

                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = bs.DateTime,
                    EventType = "blood-sugar",
                    Status = status,
                    Title = "Blood Sugar Reading",
                    SubTitle = $"{bs.BloodSugar} mg/dL{(bs.MeasurementTime != null ? $" · {bs.MeasurementTime}" : "")}",
                    BloodSugar = bs
                });
            }

            foreach (var lab in labTests)
            {
                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = lab.UploadDate,
                    EventType = "lab-test",
                    Status = "normal",
                    Title = $"{lab.TestType} Test",
                    SubTitle = "AI Analysis Available",
                    LabTest = lab
                });
            }

            foreach (var us in ultrasounds)
            {
                bool hasAnomaly = !string.IsNullOrWhiteSpace(us.DetectedAnomaly);
                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = us.UploadDate,
                    EventType = "ultrasound",
                    Status = hasAnomaly ? "attention" : "normal",
                    Title = "Ultrasound Scan",
                    SubTitle = hasAnomaly ? us.DetectedAnomaly : "No anomalies detected",
                    DoctorName = us.Doctor?.User != null
                        ? $"Dr. {us.Doctor.User.FirstName} {us.Doctor.User.LastName}"
                        : null,
                    Ultrasound = us
                });
            }

            foreach (var appt in appointments)
            {
                bool isPast = appt.Date < DateTime.Now;
                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = appt.Date,
                    EventType = "appointment",
                    Status = "normal",
                    Title = isPast ? "Appointment - Completed" : "Upcoming Appointment",
                    DoctorName = appt.Doctor?.User != null
                        ? $"Dr. {appt.Doctor.User.FirstName} {appt.Doctor.User.LastName}"
                        : null,
                    ClinicName = appt.Clinic?.Name,
                    Appointment = appt
                });
            }

            foreach (var alert in alerts)
            {
                string status = alert.AlertType?.ToLower() is "danger" or "critical" ? "critical"
                              : alert.AlertType?.ToLower() == "warning" ? "attention"
                              : "normal";

                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = alert.DateCreated,
                    EventType = "alert",
                    Status = status,
                    Title = alert.Title,
                    SubTitle = alert.Message,
                    Alert = alert
                });
            }

            foreach (var note in notes)
            {
                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = note.CreatedDate,
                    EventType = "doctor-note",
                    Status = "normal",
                    Title = "Doctor's Note",
                    SubTitle = note.Content?.Length > 120
                        ? note.Content[..120] + "..."
                        : note.Content,
                    DoctorName = note.Doctor?.User != null
                        ? $"Dr. {note.Doctor.User.FirstName} {note.Doctor.User.LastName}"
                        : null,
                    DoctorNote = note
                });
            }

            foreach (var rx in prescriptions)
            {
                int itemCount = rx.Items?.Count ?? 0;
                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = rx.PrescriptionDate,
                    EventType = "medication",
                    Status = "normal",
                    Title = "Prescription Issued",
                    SubTitle = itemCount > 0
                        ? $"{itemCount} medication{(itemCount != 1 ? "s" : "")} prescribed"
                        : rx.Notes,
                    DoctorName = rx.Doctor?.User != null
                        ? $"Dr. {rx.Doctor.User.FirstName} {rx.Doctor.User.LastName}"
                        : null,
                    Prescription = rx
                });
            }

            foreach (var pregnancy in pregnancyRecords)
            {
                entries.Add(new MedicalHistoryEntry
                {
                    DateTime = pregnancy.StartDate,
                    EventType = "pregnancy-started",
                    Status = "normal",
                    Title = "Pregnancy Started",
                    SubTitle = "Pregnancy tracking was started by the patient."
                });

                if (pregnancy.EndDate.HasValue)
                {
                    entries.Add(new MedicalHistoryEntry
                    {
                        DateTime = pregnancy.EndDate.Value,
                        EventType = "pregnancy-ended",
                        Status = "normal",
                        Title = "Pregnancy Ended",
                        SubTitle = "This pregnancy was marked as ended by the patient."
                    });
                }
            }

            entries = entries.OrderByDescending(e => e.DateTime).ToList();

            var viewModel = new MedicalHistoryViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                TimelineEntries = entries,
                LabTestCount = labTests.Count,
                UltrasoundCount = ultrasounds.Count,
                AppointmentCount = appointments.Count,
                BloodPressureCount = bpReadings.Count,
                AlertCount = alerts.Count
            };

            return View("~/Views/Patient/MedicalHistory.cshtml", viewModel);
        }
    }
}

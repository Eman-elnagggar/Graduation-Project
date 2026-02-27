using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class AssistantController : Controller
    {
        private readonly IAssistant _assistantRepository;
        private readonly IClinic _clinicRepository;
        private readonly IAppointment _appointmentRepository;
        private readonly IBooking _bookingRepository;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly IAlert _alertRepository;
        private readonly ILabTest _labTestRepository;

        public AssistantController(
            IAssistant assistantRepository,
            IClinic clinicRepository,
            IAppointment appointmentRepository,
            IBooking bookingRepository,
            IPatientDoctor patientDoctorRepository,
            IAlert alertRepository,
            ILabTest labTestRepository)
        {
            _assistantRepository = assistantRepository;
            _clinicRepository = clinicRepository;
            _appointmentRepository = appointmentRepository;
            _bookingRepository = bookingRepository;
            _patientDoctorRepository = patientDoctorRepository;
            _alertRepository = alertRepository;
            _labTestRepository = labTestRepository;
        }

        public IActionResult Index(int id, int? doctorId)
        {
            // Fast initial load — only 2 DB queries (assistant + clinic)
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null)
                return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null)
                return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var selectedDoctorName = isFiltered
                ? doctorSummaries.FirstOrDefault(d => d.DoctorID == doctorId.Value)?.FullName ?? "Doctor"
                : "All Doctors";

            // Return page skeleton — heavy data (stats, schedule) loaded via AJAX
            var viewModel = new AssistantDashboardViewModel
            {
                Assistant = assistant,
                AssistantName = assistant.User != null
                    ? $"{assistant.User.FirstName} {assistant.User.LastName}".Trim()
                    : "Assistant",
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : null,
                SelectedDoctorName = selectedDoctorName
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetDashboardStats(int id, int? doctorId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var activeDoctorIds = isFiltered ? new List<int> { doctorId.Value } : relevantDoctorIds;

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var allTodaysAppointments = _appointmentRepository
                .GetByClinicAndDate(clinic.ClinicID, today).ToList();
            var allApprovedPatientDoctors = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds).ToList();

            // Pre-aggregate counts per doctor
            var appointmentCountsByDoctor = allTodaysAppointments
                .GroupBy(a => a.DoctorID)
                .ToDictionary(g => g.Key, g => g.Count());
            var patientCountsByDoctor = allApprovedPatientDoctors
                .GroupBy(pd => pd.DoctorID)
                .ToDictionary(g => g.Key, g => g.Count());

            // Filtered counts
            var filteredAppointmentCount = isFiltered
                ? allTodaysAppointments.Count(a => a.DoctorID == doctorId!.Value)
                : allTodaysAppointments.Count;

            var filteredPatientDoctors = isFiltered
                ? allApprovedPatientDoctors.Where(pd => pd.DoctorID == doctorId!.Value)
                : allApprovedPatientDoctors;
            var uniquePatientIds = filteredPatientDoctors
                .Select(pd => pd.PatientID).Distinct().ToList();

            var pendingAlertsCount = _alertRepository
                .GetUnreadByPatientIds(uniquePatientIds, 5).Count();

            var testsThisWeek = isFiltered
                ? _labTestRepository.CountByDoctorSince(doctorId!.Value, weekStart)
                : _labTestRepository.CountByDoctorsSince(activeDoctorIds, weekStart);

            return Json(new
            {
                todayAppointmentsCount = filteredAppointmentCount,
                totalPatients = uniquePatientIds.Count,
                pendingAlertsCount,
                testsThisWeek,
                doctorCounts = relevantDoctorIds.Select(dId => new
                {
                    doctorId = dId,
                    todayAppointments = appointmentCountsByDoctor.GetValueOrDefault(dId),
                    totalPatients = patientCountsByDoctor.GetValueOrDefault(dId)
                }).ToList()
            });
        }

        [HttpGet]
        public IActionResult GetTodaysSchedule(int id, int? doctorId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);

            var allTodaysAppointments = _appointmentRepository
                .GetByClinicAndDate(clinic.ClinicID, DateTime.Today).ToList();

            var filteredAppointments = isFiltered
                ? allTodaysAppointments.Where(a => a.DoctorID == doctorId!.Value).ToList()
                : allTodaysAppointments;

            ViewBag.ClinicName = clinic.Name ?? "Clinic";
            ViewBag.SelectedDoctorID = isFiltered ? doctorId : null;
            ViewBag.SelectedDoctorName = isFiltered
                ? BuildDoctorSummaries(assistant, clinic, relevantDoctorIds)
                    .FirstOrDefault(d => d.DoctorID == doctorId!.Value)?.FullName ?? "Doctor"
                : "All Doctors";
            ViewBag.HasDoctors = relevantDoctorIds.Any();

            return PartialView("_TodaysSchedule", filteredAppointments);
        }

        private List<int> GetRelevantDoctorIds(Assistant assistant, Clinic clinic)
        {
            var assistantDoctorIds = assistant.AssistantDoctors?
                .Select(ad => ad.DoctorID).ToHashSet() ?? new HashSet<int>();
            var clinicDoctorIds = clinic.ClinicDoctors?
                .Select(cd => cd.DoctorID).ToHashSet() ?? new HashSet<int>();
            var relevantDoctorIds = assistantDoctorIds.Intersect(clinicDoctorIds).ToList();
            if (!relevantDoctorIds.Any())
                relevantDoctorIds = clinicDoctorIds.ToList();
            return relevantDoctorIds;
        }

        private List<AssistantDoctorSummary> BuildDoctorSummaries(
            Assistant assistant, Clinic clinic, List<int> relevantDoctorIds)
        {
            var summaries = new List<AssistantDoctorSummary>();
            foreach (var dId in relevantDoctorIds)
            {
                var clinicDoctor = clinic.ClinicDoctors?.FirstOrDefault(cd => cd.DoctorID == dId);
                var doctor = clinicDoctor?.Doctor;
                var fullName = doctor?.User != null
                    ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "Doctor";

                if (doctor == null)
                {
                    var ad = assistant.AssistantDoctors?.FirstOrDefault(a => a.DoctorID == dId);
                    if (ad?.Doctor?.User != null)
                    {
                        doctor = ad.Doctor;
                        fullName = $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim();
                    }
                }

                summaries.Add(new AssistantDoctorSummary
                {
                    DoctorID = dId,
                    FullName = fullName,
                    Specialization = doctor?.Specialization ?? "General Practitioner"
                });
            }
            return summaries;
        }

        public IActionResult Appointments(int id, int? doctorId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);

            var viewModel = new AssistantAppointmentsViewModel
            {
                Assistant = assistant,
                AssistantName = assistant.User != null
                    ? $"{assistant.User.FirstName} {assistant.User.LastName}".Trim()
                    : "Assistant",
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : null
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetAppointments(int id, int? doctorId, string status = "Confirmed")
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var activeDoctorIds = isFiltered ? new List<int> { doctorId!.Value } : relevantDoctorIds;

            var appointments = _appointmentRepository.GetByClinicDoctorsAndStatus(clinic.ClinicID, activeDoctorIds, status);

            var result = appointments.Select(a => new
            {
                appointmentId = a.AppointmentID,
                patientName = a.Patient?.User != null
                    ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}" : "Unknown",
                patientPhone = a.Patient?.User?.Phone ?? string.Empty,
                doctorName = a.Doctor?.User != null
                    ? $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}" : "Unknown",
                doctorSpecialization = a.Doctor?.Specialization ?? string.Empty,
                date = a.Date.ToString("yyyy-MM-dd"),
                time = a.Time.ToString(@"hh\:mm"),
                status = a.Booking?.Status ?? "Confirmed",
                reason = a.Booking?.Reason ?? string.Empty,
                notes = a.Booking?.Notes ?? string.Empty,
                isToday = a.Date.Date == DateTime.Today
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetAppointmentDetail(int id, int appointmentId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Forbid();

            return Json(new
            {
                appointmentId = appointment.AppointmentID,
                doctorId = appointment.DoctorID,
                patientName = appointment.Patient?.User != null
                    ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Unknown",
                doctorName = appointment.Doctor?.User != null
                    ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "Unknown",
                date = appointment.Date.ToString("yyyy-MM-dd"),
                time = appointment.Time.ToString(@"hh\:mm"),
                status = appointment.Booking?.Status ?? "Confirmed",
                reason = appointment.Booking?.Reason ?? string.Empty
            });
        }

        [HttpPost]
        public IActionResult ModifyAppointment(int id, int appointmentId, string newDate, string newTime, string reason)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(newDate, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            if (!TimeSpan.TryParse(newTime, out var parsedTime))
                return Json(new { success = false, message = "Invalid time." });

            appointment.Date = parsedDate;
            appointment.Time = parsedTime;
            _appointmentRepository.Update(appointment);

            if (appointment.Booking != null)
            {
                appointment.Booking.Status = "Modified";
                if (!string.IsNullOrWhiteSpace(reason))
                    appointment.Booking.Notes = reason;
                _bookingRepository.Update(appointment.Booking);
            }

            _appointmentRepository.Save();

            return Json(new { success = true, message = "Appointment modified successfully." });
        }

        [HttpPost]
        public IActionResult CancelAppointment(int id, int appointmentId, string reason)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            appointment.isBooked = false;
            _appointmentRepository.Update(appointment);

            if (appointment.Booking != null)
            {
                appointment.Booking.Status = "Cancelled";
                if (!string.IsNullOrWhiteSpace(reason))
                    appointment.Booking.Notes = reason;
                _bookingRepository.Update(appointment.Booking);
            }

            _appointmentRepository.Save();

            return Json(new { success = true, message = "Appointment cancelled successfully." });
        }

        public IActionResult Details(int id)
        {
            throw new NotImplementedException();
        }

        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Assistant assistant)
        {
            throw new NotImplementedException();
        }

        public IActionResult Edit(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Assistant assistant)
        {
            throw new NotImplementedException();
        }

        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            throw new NotImplementedException();
        }
    }
}

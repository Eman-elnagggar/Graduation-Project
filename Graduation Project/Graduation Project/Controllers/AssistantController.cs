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

        /// <summary>
        /// Lightweight endpoint that returns only the counts per status,
        /// avoiding the cost of serializing full appointment objects.
        /// </summary>
        [HttpGet]
        public IActionResult GetAppointmentCounts(int id, int? doctorId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var activeDoctorIds = isFiltered ? new List<int> { doctorId!.Value } : relevantDoctorIds;

            var confirmed = _appointmentRepository
                .GetByClinicDoctorsAndStatus(clinic.ClinicID, activeDoctorIds, "Confirmed").Count();
            var modified = _appointmentRepository
                .GetByClinicDoctorsAndStatus(clinic.ClinicID, activeDoctorIds, "Modified").Count();
            var cancelled = _appointmentRepository
                .GetByClinicDoctorsAndStatus(clinic.ClinicID, activeDoctorIds, "Cancelled").Count();

            return Json(new
            {
                confirmed,
                modified,
                cancelled,
                total = confirmed + modified + cancelled
            });
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

            if (parsedDate.Date < DateTime.Today)
                return Json(new { success = false, message = "Cannot schedule an appointment in the past." });

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

        // ── Availability ────────────────────────────────────────────────────

        public IActionResult Availability(int id, int? doctorId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);

            var viewModel = new AssistantAvailabilityViewModel
            {
                Assistant = assistant,
                AssistantName = assistant.User != null
                    ? $"{assistant.User.FirstName} {assistant.User.LastName}".Trim()
                    : "Assistant",
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : (relevantDoctorIds.Count == 1 ? relevantDoctorIds[0] : null)
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetAvailabilitySlots(int id, int doctorId, string date)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Forbid();

            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest("Invalid date.");

            var appointments = _appointmentRepository.GetByClinicDoctorAndDate(clinic.ClinicID, doctorId, parsedDate);

            var result = appointments.Select(a => new
            {
                appointmentId = a.AppointmentID,
                time = a.Time.ToString(@"hh\:mm"),
                isBooked = a.isBooked,
                patientName = a.Patient?.User != null
                    ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}" : null
            }).ToList();

            return Json(result);
        }

        [HttpPost]
        public IActionResult CreateAvailabilitySlot(int id, int doctorId, string date, string time)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            if (!TimeSpan.TryParse(time, out var parsedTime))
                return Json(new { success = false, message = "Invalid time." });

            if (parsedDate.Date < DateTime.Today)
                return Json(new { success = false, message = "Cannot create slots in the past." });

            var existing = _appointmentRepository
                .GetByClinicDoctorAndDate(clinic.ClinicID, doctorId, parsedDate)
                .Any(a => a.Time == parsedTime);
            if (existing)
                return Json(new { success = false, message = "Slot already exists." });

            var slot = new Appointment
            {
                DoctorID = doctorId,
                ClinicID = clinic.ClinicID,
                PatientID = null,
                Date = parsedDate,
                Time = parsedTime,
                isBooked = false
            };
            _appointmentRepository.Add(slot);
            _appointmentRepository.Save();

            return Json(new { success = true, message = "Slot created.", appointmentId = slot.AppointmentID });
        }

        [HttpPost]
        public IActionResult DeleteAvailabilitySlot(int id, int appointmentId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var appointment = _appointmentRepository.GetById(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Slot not found." });

            if (appointment.isBooked)
                return Json(new { success = false, message = "Cannot remove a booked slot." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            _appointmentRepository.Delete(appointmentId);
            _appointmentRepository.Save();

            return Json(new { success = true, message = "Slot removed." });
        }

        [HttpPost]
        public IActionResult SetAllSlotsAvailable(int id, int doctorId, string date,
            string startTime, string endTime, int slotDuration)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            if (parsedDate.Date < DateTime.Today)
                return Json(new { success = false, message = "Cannot create slots in the past." });

            if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
                return Json(new { success = false, message = "Invalid time range." });

            var existingTimes = _appointmentRepository
                .GetByClinicDoctorAndDate(clinic.ClinicID, doctorId, parsedDate)
                .Select(a => a.Time).ToHashSet();

            var newSlots = new List<Appointment>();
            var current = start;
            while (current < end)
            {
                if (!existingTimes.Contains(current))
                {
                    newSlots.Add(new Appointment
                    {
                        DoctorID = doctorId,
                        ClinicID = clinic.ClinicID,
                        PatientID = null,
                        Date = parsedDate,
                        Time = current,
                        isBooked = false
                    });
                }
                current = current.Add(TimeSpan.FromMinutes(slotDuration));
            }

            if (newSlots.Any())
            {
                _appointmentRepository.AddRange(newSlots);
                _appointmentRepository.Save();
            }

            return Json(new { success = true, message = $"{newSlots.Count} slot(s) created." });
        }

        [HttpPost]
        public IActionResult BlockAllAvailabilitySlots(int id, int doctorId, string date)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            var slots = _appointmentRepository
                .GetByClinicDoctorAndDate(clinic.ClinicID, doctorId, parsedDate)
                .Where(a => !a.isBooked).ToList();

            foreach (var slot in slots)
                _appointmentRepository.Delete(slot.AppointmentID);

            _appointmentRepository.Save();

            return Json(new { success = true, message = $"{slots.Count} slot(s) blocked." });
        }

        [HttpPost]
        public IActionResult ApplyQuickSetupSchedule(int id, [FromBody] QuickSetupRequest request)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null) return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(request.DoctorId))
                return Json(new { success = false, message = "Access denied." });

            if (request.WorkingDays == null || !request.WorkingDays.Any())
                return Json(new { success = false, message = "Please select at least one working day." });

            if (!TimeSpan.TryParse(request.StartTime, out var start) || !TimeSpan.TryParse(request.EndTime, out var end))
                return Json(new { success = false, message = "Invalid time range." });

            var today = DateTime.Today;
            var endDate = today.AddDays(request.WeeksAhead * 7);

            var existingSet = _appointmentRepository
                .GetByClinicDoctorAndDateRange(clinic.ClinicID, request.DoctorId, today, endDate)
                .Select(a => (a.Date.Date, a.Time))
                .ToHashSet();

            var newSlots = new List<Appointment>();
            for (var d = today; d <= endDate; d = d.AddDays(1))
            {
                if (!request.WorkingDays.Contains((int)d.DayOfWeek)) continue;

                var current = start;
                while (current < end)
                {
                    if (!existingSet.Contains((d, current)))
                    {
                        newSlots.Add(new Appointment
                        {
                            DoctorID = request.DoctorId,
                            ClinicID = clinic.ClinicID,
                            PatientID = null,
                            Date = d,
                            Time = current,
                            isBooked = false
                        });
                    }
                    current = current.Add(TimeSpan.FromMinutes(request.SlotDuration));
                }
            }

            if (newSlots.Any())
            {
                _appointmentRepository.AddRange(newSlots);
                _appointmentRepository.Save();
            }

            return Json(new { success = true, message = $"Schedule applied. {newSlots.Count} slot(s) created." });
        }

        // ── CRUD stubs ───────────────────────────────────────────────────────

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

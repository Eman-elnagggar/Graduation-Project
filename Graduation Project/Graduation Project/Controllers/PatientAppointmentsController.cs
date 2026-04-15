using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientAppointmentsController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IAppointment _appointment;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly IAlert _alertRepository;
        private readonly IDoctor _doctorRepository;
        private readonly IBooking _bookingRepository;

        public PatientAppointmentsController(
            IPatient patientRepository,
            IAppointment appointment,
            IPatientDoctor patientDoctorRepository,
            IAlert alertRepository,
            IDoctor doctorRepository,
            IBooking bookingRepository)
        {
            _patientRepository = patientRepository;
            _appointment = appointment;
            _patientDoctorRepository = patientDoctorRepository;
            _alertRepository = alertRepository;
            _doctorRepository = doctorRepository;
            _bookingRepository = bookingRepository;
        }

        public IActionResult Appointments(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null) return failure;

            var allAppointments = _appointment.GetByPatientId(id).ToList();
            var today = DateTime.Today;

            var completedAppointments = allAppointments
                .Where(a => a.isBooked
                         && string.Equals(a.Booking?.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var upcoming = allAppointments
                .Where(a => a.Date.Date >= today
                         && a.isBooked
                         && !string.Equals(a.Booking?.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .ToList();

            var past = _appointment.GetPastByPatientId(id).ToList();

            var pastIds = past.Select(a => a.AppointmentID).ToHashSet();
            foreach (var completed in completedAppointments)
            {
                if (!pastIds.Contains(completed.AppointmentID))
                    past.Add(completed);
            }

            past = past
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.Time)
                .ToList();

            var myDoctors = _patientDoctorRepository.GetByPatientId(id)
                .Where(pd => pd.Status == "Approved")
                .ToList();
            var primaryDoctor = myDoctors.FirstOrDefault(pd => pd.IsPrimary);
            var unreadAlerts = _alertRepository.GetByPatientId(id).Count(a => !a.IsRead);

            var viewModel = new PatientAppointmentsViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                UpcomingAppointments = upcoming,
                PastAppointments = past,
                MyDoctors = myDoctors,
                PrimaryDoctor = primaryDoctor,
                UnreadAlertsCount = unreadAlerts
            };

            return View("~/Views/Patient/Appointments.cshtml", viewModel);
        }

        public IActionResult BookAppointment(int id, int? doctorId = null)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null) return failure;

            var allDoctors = _doctorRepository.GetAllWithDetails().ToList();
            var availableDoctors = allDoctors
                .Select(d =>
                {
                    var clinicDoctor = d.ClinicDoctors?.FirstOrDefault();
                    var firstAvailable = _appointment.GetFirstAvailableForDoctor(d.DoctorID);
                    var clinics = d.ClinicDoctors?.Select(cd => new ClinicInfo
                    {
                        ClinicID = cd.ClinicID,
                        ClinicName = cd.Clinic?.Name ?? "Clinic",
                        ClinicLocation = cd.Clinic?.Location ?? string.Empty
                    }).ToList() ?? new List<ClinicInfo>();

                    return new DoctorBookingInfo
                    {
                        DoctorID = d.DoctorID,
                        FullName = d.User != null ? $"Dr. {d.User.FirstName} {d.User.LastName}".Trim() : "Doctor",
                        Specialization = d.Specialization ?? string.Empty,
                        ClinicID = clinicDoctor?.ClinicID ?? 0,
                        ClinicName = clinicDoctor?.Clinic?.Name ?? "Clinic",
                        ClinicLocation = clinicDoctor?.Clinic?.Location ?? string.Empty,
                        NextAvailableDate = firstAvailable?.Date,
                        NextAvailableTime = firstAvailable?.Time,
                        Clinics = clinics
                    };
                })
                .ToList();

            var viewModel = new PatientBookAppointmentViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                AvailableDoctors = availableDoctors,
                PreSelectedDoctorId = doctorId
            };

            return View("~/Views/Patient/BookAppointment.cshtml", viewModel);
        }

        [HttpGet]
        public IActionResult GetDoctorSlots(int patientId, int doctorId, string date, int? clinicId = null)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest();

            var slots = clinicId.HasValue
                ? _appointment.GetAvailableByDoctorClinicAndDate(doctorId, clinicId.Value, parsedDate)
                : _appointment.GetAvailableByDoctorAndDate(doctorId, parsedDate);

            if (parsedDate.Date == DateTime.Today)
            {
                var nowTime = DateTime.Now.TimeOfDay;
                slots = slots.Where(a => a.Time > nowTime).ToList();
            }

            return Json(slots.Select(a => new
            {
                appointmentId = a.AppointmentID,
                time = a.Time.ToString(@"hh\:mm"),
                timeDisplay = DateTime.Today.Add(a.Time).ToString("hh:mm tt"),
                hourOf24 = (int)a.Time.TotalHours,
                clinicId = a.ClinicID
            }));
        }

        [HttpGet]
        public IActionResult GetAvailableDates(int patientId, int doctorId, int year, int month, int? clinicId = null)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var dates = clinicId.HasValue
                ? _appointment.GetAvailableDatesByDoctorAndClinic(doctorId, clinicId.Value, year, month)
                : _appointment.GetAvailableDatesByDoctor(doctorId, year, month);

            var today = DateTime.Today;
            dates = dates.Where(d => d.Date >= today).ToList();

            if (dates.Any(d => d.Date == today))
            {
                var nowTime = DateTime.Now.TimeOfDay;
                var todaySlots = clinicId.HasValue
                    ? _appointment.GetAvailableByDoctorClinicAndDate(doctorId, clinicId.Value, today)
                    : _appointment.GetAvailableByDoctorAndDate(doctorId, today);

                if (!todaySlots.Any(a => a.Time > nowTime))
                    dates = dates.Where(d => d.Date != today).ToList();
            }

            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmBooking(int patientId, int appointmentId, string reason, string? notes)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var appointment = _appointment.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.isBooked)
                return Json(new { success = false, message = "This slot is no longer available." });

            var now = DateTime.Now;
            if (appointment.Date.Date < now.Date
                || (appointment.Date.Date == now.Date && appointment.Time <= now.TimeOfDay))
            {
                return Json(new { success = false, message = "This slot time has already passed. Please choose a future time." });
            }

            if (_appointment.HasDoctorConflict(appointment.DoctorID, appointment.Date, appointment.Time, appointmentId))
                return Json(new { success = false, message = "This doctor is already booked at this time at another clinic. Please choose a different slot." });

            appointment.PatientID = patientId;
            appointment.isBooked = true;

            Booking booking;
            if (appointment.Booking != null)
            {
                booking = appointment.Booking;
                booking.PatientID = patientId;
                booking.DoctorID = appointment.DoctorID;
                booking.ClinicID = appointment.ClinicID;
                booking.Status = "Confirmed";
                booking.Reason = reason ?? string.Empty;
                booking.Notes = notes ?? string.Empty;
                _bookingRepository.Update(booking);
            }
            else
            {
                booking = new Booking
                {
                    AppointmentID = appointmentId,
                    PatientID = patientId,
                    DoctorID = appointment.DoctorID,
                    ClinicID = appointment.ClinicID,
                    Status = "Confirmed",
                    Reason = reason ?? string.Empty,
                    Notes = notes ?? string.Empty
                };
                _bookingRepository.Add(booking);
            }

            try
            {
                _bookingRepository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "This slot was just booked by someone else. Please choose another." });
            }

            return Json(new
            {
                success = true,
                message = "Appointment booked successfully!",
                bookingId = booking.BookingID,
                appointmentId = appointment.AppointmentID,
                doctorName = appointment.Doctor?.User != null
                    ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}".Trim()
                    : "Doctor",
                clinicName = appointment.Clinic?.Name ?? string.Empty,
                clinicLocation = appointment.Clinic?.Location ?? string.Empty,
                date = appointment.Date.ToString("yyyy-MM-dd"),
                dateDisplay = appointment.Date.ToString("MMM dd, yyyy"),
                time = appointment.Time.ToString(@"hh\:mm"),
                timeDisplay = DateTime.Today.Add(appointment.Time).ToString("hh:mm tt"),
                status = booking.Status,
                reason = booking.Reason,
                notes = booking.Notes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int patientId, int appointmentId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var appointment = _appointment.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.PatientID != patientId)
                return Json(new { success = false, message = "Appointment not found." });

            var bookingStatus = appointment.Booking?.Status ?? "Confirmed";
            var isCancellableStatus = string.Equals(bookingStatus, "Confirmed", StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(bookingStatus, "Modified", StringComparison.OrdinalIgnoreCase);

            if (!isCancellableStatus)
                return Json(new { success = false, message = "Only confirmed or modified appointments can be cancelled." });

            appointment.isBooked = false;
            appointment.PatientID = null;
            _appointment.Update(appointment);

            if (appointment.Booking != null)
            {
                appointment.Booking.Status = "Cancelled";
                _bookingRepository.Update(appointment.Booking);
            }

            _appointment.Save();
            return Json(new
            {
                success = true,
                message = "Appointment cancelled successfully.",
                appointmentId = appointment.AppointmentID,
                doctorName = appointment.Doctor?.User != null
                    ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}".Trim()
                    : "Doctor",
                date = appointment.Date.ToString("yyyy-MM-dd"),
                time = appointment.Time.ToString(@"hh\:mm"),
                status = appointment.Booking?.Status ?? "Cancelled"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RescheduleAppointment(int patientId, int currentAppointmentId, int newAppointmentId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var current = _appointment.GetByIdWithBooking(currentAppointmentId);
            if (current == null || !current.isBooked || current.PatientID != patientId)
                return Json(new { success = false, message = "Appointment not found." });

            var currentStatus = current.Booking?.Status ?? "Confirmed";
            var canReschedule = string.Equals(currentStatus, "Modified", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(currentStatus, "Confirmed", StringComparison.OrdinalIgnoreCase);
            if (!canReschedule)
                return Json(new { success = false, message = "Only confirmed or modified appointments can be rescheduled." });

            var newSlot = _appointment.GetAvailableSlotById(newAppointmentId, current.DoctorID, current.ClinicID);
            if (newSlot == null)
                return Json(new { success = false, message = "Selected time is no longer available." });

            var now = DateTime.Now;
            if (newSlot.Date.Date < now.Date || (newSlot.Date.Date == now.Date && newSlot.Time <= now.TimeOfDay))
                return Json(new { success = false, message = "Please choose a future time slot." });

            if (current.Booking == null)
                return Json(new { success = false, message = "Booking data is missing for this appointment." });

            var booking = current.Booking;

            current.isBooked = false;
            current.PatientID = null;
            _appointment.Update(current);

            newSlot.PatientID = patientId;
            newSlot.isBooked = true;
            _appointment.Update(newSlot);

            booking.AppointmentID = newSlot.AppointmentID;
            booking.ClinicID = newSlot.ClinicID;
            booking.DoctorID = newSlot.DoctorID;
            booking.PatientID = patientId;
            booking.Status = "Confirmed";
            _bookingRepository.Update(booking);

            try
            {
                _bookingRepository.Save();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "This slot was just taken. Please choose another one." });
            }

            return Json(new { success = true, message = "Appointment rescheduled successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetPrimaryDoctor(int patientId, int doctorId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var approvedLinks = _patientDoctorRepository
                .GetByPatientId(patientId)
                .Where(pd => string.Equals(pd.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!approvedLinks.Any())
                return Json(new { success = false, message = "No approved doctors found for this patient." });

            var targetLink = approvedLinks.FirstOrDefault(pd => pd.DoctorID == doctorId);
            if (targetLink == null)
                return Json(new { success = false, message = "Doctor is not in your approved doctors list." });

            if (targetLink.IsPrimary)
                return Json(new { success = true, message = "This doctor is already your primary doctor.", doctorId });

            foreach (var link in approvedLinks)
            {
                var shouldBePrimary = link.DoctorID == doctorId;
                if (link.IsPrimary == shouldBePrimary)
                    continue;

                link.IsPrimary = shouldBePrimary;
                _patientDoctorRepository.Update(link);
            }

            _patientDoctorRepository.Save();

            return Json(new
            {
                success = true,
                message = "Primary doctor updated successfully.",
                doctorId
            });
        }

        private (Patient? patient, IActionResult? failure) AuthorizePatientAccess(int patientId, bool returnJsonOnFailure = false)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return (null, NotFound());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                if (returnJsonOnFailure)
                    return (null, Unauthorized(new { success = false, message = "Unauthorized." }));

                return (null, Unauthorized());
            }

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
            {
                if (returnJsonOnFailure)
                    return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." }));

                return (null, Forbid());
            }

            return (patient, null);
        }
    }
}

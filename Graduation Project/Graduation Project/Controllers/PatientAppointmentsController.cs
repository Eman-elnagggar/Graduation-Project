using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Controllers
{
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
            var patient = _patientRepository.GetById(id);
            if (patient == null) return NotFound();

            var allAppointments = _appointment.GetByPatientId(id).ToList();
            var upcoming = allAppointments
                .Where(a => a.Date.Date >= DateTime.Today && a.isBooked)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .ToList();
            var past = _appointment.GetPastByPatientId(id).ToList();

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
            var patient = _patientRepository.GetById(id);
            if (patient == null) return NotFound();

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
            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest();

            var slots = clinicId.HasValue
                ? _appointment.GetAvailableByDoctorClinicAndDate(doctorId, clinicId.Value, parsedDate)
                : _appointment.GetAvailableByDoctorAndDate(doctorId, parsedDate);

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
            var dates = clinicId.HasValue
                ? _appointment.GetAvailableDatesByDoctorAndClinic(doctorId, clinicId.Value, year, month)
                : _appointment.GetAvailableDatesByDoctor(doctorId, year, month);

            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmBooking(int patientId, int appointmentId, string reason, string? notes)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return Json(new { success = false, message = "Patient not found." });

            var appointment = _appointment.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.isBooked)
                return Json(new { success = false, message = "This slot is no longer available." });

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
            var appointment = _appointment.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.PatientID != patientId)
                return Json(new { success = false, message = "Appointment not found." });

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
    }
}

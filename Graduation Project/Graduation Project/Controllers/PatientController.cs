using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
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
        private readonly INote _noteRepository;
        private readonly IPrescription _prescriptionRepository;
        private readonly IMedicalHistory _medicalHistoryRepository;
        private readonly IPlace _placeRepository;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly IDoctor _doctorRepository;
        private readonly IBooking _bookingRepository;

        public PatientController(
            IPatient patientRepository,
            IPatientBloodPressure patientBloodPressure,
            IPatientBloodSugar patientBloodSugar,
            ILabTest labTest,
            IAppointment appointment,
            IUltrasoundImage ultrasoundImage,
            IAlert alertRepository,
            AlertService alertService,
            INote noteRepository,
            IPrescription prescriptionRepository,
            IMedicalHistory medicalHistoryRepository,
            IPlace placeRepository,
            IPatientDoctor patientDoctorRepository,
            IDoctor doctorRepository,
            IBooking bookingRepository)
        {
            _patientRepository = patientRepository;
            _patientBloodPressure = patientBloodPressure;
            _patientBloodSugar = patientBloodSugar;
            _labTest = labTest;
            _appointment = appointment;
            _ultrasoundImage = ultrasoundImage;
            _alertRepository = alertRepository;
            _alertService = alertService;
            _noteRepository = noteRepository;
            _prescriptionRepository = prescriptionRepository;
            _medicalHistoryRepository = medicalHistoryRepository;
            _placeRepository = placeRepository;
            _patientDoctorRepository = patientDoctorRepository;
            _doctorRepository = doctorRepository;
            _bookingRepository = bookingRepository;
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
                    DateTime = nextAppt.Date,
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

        public IActionResult MedicalHistory(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            // ?? collect all record sets ??????????????????????????????????
            var bpReadings = _patientBloodPressure.GetRecentByPatientId(id, 200).ToList();
            var bsReadings = _patientBloodSugar.GetRecentByPatientId(id, 200).ToList();
            var labTests = _labTest.GetLabTestsByPatientId(id).ToList();
            var ultrasounds = _ultrasoundImage.GetUltrasoundsByPatientId(id).ToList();
            var appointments = _appointment.GetByPatientId(id).ToList();
            var alerts = _alertRepository.GetByPatientId(id).ToList();
            var notes = _noteRepository.GetByPatientId(id).ToList();
            var prescriptions = _prescriptionRepository.GetByPatientId(id).ToList();

            // ?? build flat timeline ??????????????????????????????????????
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
                    Title = isPast ? "Appointment – Completed" : "Upcoming Appointment",
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
                        ? note.Content[..120] + "…"
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

            // ?? sort newest-first ????????????????????????????????????????
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

            return View(viewModel);
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

            return View(viewModel);
        }

        // ---------------------------------------------------------------
        // POST: /Patient/MarkAlertRead
        // ---------------------------------------------------------------
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

        // ---------------------------------------------------------------
        // POST: /Patient/MarkAllAlertsRead
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllAlertsRead(int patientId)
        {
            var unread = _alertRepository
                .GetByPatientId(patientId)
                .Where(a => !a.IsRead)
                .ToList();

            foreach (var a in unread)
            {
                a.IsRead = true;
                _alertRepository.Update(a);
            }
            _alertRepository.Save();

            return Json(new { success = true, count = unread.Count });
        }

        // ---------------------------------------------------------------
        // POST: /Patient/DeleteAlert
        // ---------------------------------------------------------------
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

        // ---------------------------------------------------------------
        // GET: /Patient/Places/5
        // ---------------------------------------------------------------
        [HttpGet]
        public IActionResult Places(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var places = _placeRepository.GetByPatientId(id).ToList();

            var viewModel = new PlacesViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Places = places
            };

            return View(viewModel);
        }

        // ---------------------------------------------------------------
        // POST: /Patient/Places  (form-POST kept as stub for future use)
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Places(Patient patient)
        {
            throw new NotImplementedException();
        }

        // ---------------------------------------------------------------
        // POST: /Patient/SavePlace  (AJAX)
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePlace(int patientId, string name, string type, string? address, string? phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                return BadRequest(new { success = false, message = "Name and type are required." });

            var place = new Place
            {
                PatientID = patientId,
                Name = name,
                Type = type,
                Address = address ?? string.Empty,
                Phone = phone ?? string.Empty,
                ImageURL = string.Empty
            };

            _placeRepository.Add(place);
            _placeRepository.Save();

            return Json(new
            {
                success = true,
                id = place.PlaceID,
                name = place.Name,
                type = place.Type,
                address = place.Address,
                phone = place.Phone,
                imageUrl = place.ImageURL
            });
        }

        // ---------------------------------------------------------------
        // POST: /Patient/UpdatePlace  (AJAX)
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePlace(int placeId, int patientId, string name, string type, string? address, string? phone)
        {
            var place = _placeRepository.GetById(placeId);
            if (place == null || place.PatientID != patientId)
                return Json(new { success = false });

            place.Name = name;
            place.Type = type;
            place.Address = address ?? string.Empty;
            place.Phone = phone ?? string.Empty;

            _placeRepository.Update(place);
            _placeRepository.Save();

            return Json(new { success = true });
        }

        // ---------------------------------------------------------------
        // POST: /Patient/DeletePlace  (AJAX)
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePlace(int placeId, int patientId)
        {
            var place = _placeRepository.GetById(placeId);
            if (place == null || place.PatientID != patientId)
                return Json(new { success = false });

            _placeRepository.Delete(placeId);
            _placeRepository.Save();

            return Json(new { success = true });
        }

        // ---------------------------------------------------------------
        // GET: /Patient/Appointments/5
        // ---------------------------------------------------------------
        public IActionResult Appointments(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null) return NotFound();

            var allAppointments = _appointment.GetByPatientId(id).ToList();
            var upcoming = allAppointments
                .Where(a => a.Date.Date >= DateTime.Today && a.isBooked)
                .OrderBy(a => a.Date).ThenBy(a => a.Time)
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

            return View(viewModel);
        }

        // ---------------------------------------------------------------
        // GET: /Patient/BookAppointment/5
        // ---------------------------------------------------------------
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

            return View(viewModel);
        }

        // ---------------------------------------------------------------
        // GET: /Patient/GetDoctorSlots  (AJAX)
        // ---------------------------------------------------------------
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

        // ---------------------------------------------------------------
        // GET: /Patient/GetAvailableDates  (AJAX)
        // ---------------------------------------------------------------
        [HttpGet]
        public IActionResult GetAvailableDates(int patientId, int doctorId, int year, int month, int? clinicId = null)
        {
            var dates = clinicId.HasValue
                ? _appointment.GetAvailableDatesByDoctorAndClinic(doctorId, clinicId.Value, year, month)
                : _appointment.GetAvailableDatesByDoctor(doctorId, year, month);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        // ---------------------------------------------------------------
        // POST: /Patient/ConfirmBooking  (AJAX)
        // ---------------------------------------------------------------
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

            // Check if the doctor is already booked at the same date/time at another clinic
            if (_appointment.HasDoctorConflict(appointment.DoctorID, appointment.Date, appointment.Time, appointmentId))
                return Json(new { success = false, message = "This doctor is already booked at this time at another clinic. Please choose a different slot." });

            appointment.PatientID = patientId;
            appointment.isBooked = true;
            _appointment.Update(appointment);

            var booking = new Booking
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
            _bookingRepository.Save();

            return Json(new { success = true, message = "Appointment booked successfully!" });
        }

        // ---------------------------------------------------------------
        // POST: /Patient/CancelAppointment  (AJAX)
        // ---------------------------------------------------------------
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
            return Json(new { success = true, message = "Appointment cancelled successfully." });
        }

        public IActionResult Edit(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Patient patient)
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

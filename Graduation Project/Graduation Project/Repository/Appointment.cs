using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class AppointmentRepository : IAppointment
    {
        private readonly AppDbContext _context;

        public AppointmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Appointment> GetAll() => _context.Appointments.ToList();

        public Appointment GetById(int id) => _context.Appointments.Find(id);

        public void Add(Appointment appointment) => _context.Appointments.Add(appointment);

        public void Update(Appointment appointment) => _context.Appointments.Update(appointment);

        public void Delete(int id)
        {
            var entity = _context.Appointments.Find(id);
            if (entity != null)
                _context.Appointments.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public Appointment GetNextAppointmentForPatient(int patientId)
        {
            return _context.Appointments
                .Where(a => a.PatientID == patientId && a.isBooked && a.Date.Date >= DateTime.Today)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Clinic)
                .OrderBy(a => a.Date)
                .FirstOrDefault();
        }

        public IEnumerable<Appointment> GetByPatientId(int patientId) =>
            _context.Appointments
                .Where(a => a.PatientID == patientId)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Clinic)
                .Include(a => a.Bookings)
                .OrderByDescending(a => a.Date)
                .ToList();

        public IEnumerable<Appointment> GetByClinicAndDate(int clinicId, DateTime date) =>
            _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Bookings)
                .Where(a => a.ClinicID == clinicId && a.Date.Date == date.Date)
                .OrderBy(a => a.Time)
                .AsSplitQuery()
                .ToList();

        public IEnumerable<Appointment> GetByClinicDoctorAndDate(int clinicId, int doctorId, DateTime date) =>
            _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Bookings)
                .Where(a => a.ClinicID == clinicId && a.DoctorID == doctorId && a.Date.Date == date.Date)
                .OrderBy(a => a.Time)
                .AsSplitQuery()
                .ToList();

        public Dictionary<int, DateTime> GetLastVisitDates(IEnumerable<int> patientIds, int doctorId) =>
            _context.Appointments
                .Where(a => a.PatientID.HasValue
                         && patientIds.Contains(a.PatientID.Value)
                         && a.DoctorID == doctorId
                         && a.Date < DateTime.Now)
                .GroupBy(a => a.PatientID!.Value)
                .Select(g => new { PatientID = g.Key, LastDate = g.Max(a => a.Date) })
                .ToDictionary(x => x.PatientID, x => x.LastDate);

        public Dictionary<int, DateTime> GetLastVisitDatesForDoctors(IEnumerable<int> patientIds, IEnumerable<int> doctorIds) =>
            _context.Appointments
                .Where(a => a.PatientID.HasValue
                         && patientIds.Contains(a.PatientID.Value)
                         && doctorIds.Contains(a.DoctorID)
                         && a.Date < DateTime.Now)
                .GroupBy(a => a.PatientID!.Value)
                .Select(g => new { PatientID = g.Key, LastDate = g.Max(a => a.Date) })
                .ToDictionary(x => x.PatientID, x => x.LastDate);

        public IEnumerable<Appointment> GetByClinicDoctorsStatusAndDate(int clinicId, IEnumerable<int> doctorIds, string status, DateTime date) =>
            HydrateMissingPatientsFromBooking(
                _context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Include(a => a.Doctor).ThenInclude(d => d.User)
                    .Include(a => a.Bookings)
                    .Where(a => a.ClinicID == clinicId
                             && doctorIds.Contains(a.DoctorID)
                             && a.Bookings.Any(b => b.IsActive && b.Status == status)
                             && a.Date.Date == date.Date)
                    .OrderBy(a => a.Time)
                    .AsSplitQuery()
                    .ToList());

        public Dictionary<string, int> GetStatusCountsByClinicDoctorsAndDate(int clinicId, IEnumerable<int> doctorIds, DateTime date, IEnumerable<string> statuses)
        {
            var normalizedStatuses = statuses
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var counts = _context.Appointments
                .AsNoTracking()
                .Where(a => a.ClinicID == clinicId
                         && doctorIds.Contains(a.DoctorID)
                         && a.Bookings.Any(b => b.IsActive)
                         && a.Date.Date == date.Date
                         && a.Bookings.Any(b => b.IsActive && normalizedStatuses.Contains(b.Status)))
                .Select(a => a.Bookings.Where(b => b.IsActive).OrderByDescending(b => b.BookingID).FirstOrDefault())
                .Where(b => b != null)
                .GroupBy(b => b!.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            foreach (var status in normalizedStatuses)
            {
                if (!counts.ContainsKey(status))
                    counts[status] = 0;
            }

            return counts;
        }

        public IEnumerable<Appointment> GetPagedByClinicDoctorsStatusAndDate(int clinicId, IEnumerable<int> doctorIds, string status, DateTime date, string? search, int page, int pageSize)
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Bookings)
                .Where(a => a.ClinicID == clinicId
                         && doctorIds.Contains(a.DoctorID)
                         && a.Bookings.Any(b => b.IsActive && b.Status == status)
                         && a.Date.Date == date.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    ((a.Patient != null && a.Patient.User != null) &&
                        (((a.Patient.User.FirstName ?? "") + " " + (a.Patient.User.LastName ?? "")).Contains(term)
                         || (a.Patient.User.Phone ?? "").Contains(term)))
                    || ((a.Doctor != null && a.Doctor.User != null) &&
                        (((a.Doctor.User.FirstName ?? "") + " " + (a.Doctor.User.LastName ?? "")).Contains(term))));
            }

            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 5, 100);

            var items = query
                .OrderBy(a => a.Time)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .AsSplitQuery()
                .ToList();

            return HydrateMissingPatientsFromBooking(items);
        }

        public int CountByClinicDoctorsStatusAndDate(int clinicId, IEnumerable<int> doctorIds, string status, DateTime date, string? search)
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.ClinicID == clinicId
                         && doctorIds.Contains(a.DoctorID)
                         && a.Bookings.Any(b => b.IsActive && b.Status == status)
                         && a.Date.Date == date.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    ((a.Patient != null && a.Patient.User != null) &&
                        (((a.Patient.User.FirstName ?? "") + " " + (a.Patient.User.LastName ?? "")).Contains(term)
                         || (a.Patient.User.Phone ?? "").Contains(term)))
                    || ((a.Doctor != null && a.Doctor.User != null) &&
                        (((a.Doctor.User.FirstName ?? "") + " " + (a.Doctor.User.LastName ?? "")).Contains(term))));
            }

            return query.Count();
        }

        public Appointment GetByIdWithBooking(int id) =>
            HydrateMissingPatientsFromBooking(new List<Appointment>
            {
                _context.Appointments
                .Include(a => a.Bookings)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Clinic)
                .FirstOrDefault(a => a.AppointmentID == id)
            })
            .FirstOrDefault();

        private List<Appointment> HydrateMissingPatientsFromBooking(List<Appointment> appointments)
        {
            var list = appointments.Where(a => a != null).ToList();
            if (list.Count == 0)
                return list;

            var missingPatientIds = list
                .Where(a => a.Patient == null && a.Booking != null && a.Booking.PatientID > 0)
                .Select(a => a.Booking!.PatientID)
                .Distinct()
                .ToList();

            if (missingPatientIds.Count == 0)
                return list;

            var patientMap = _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => missingPatientIds.Contains(p.PatientID))
                .ToDictionary(p => p.PatientID, p => p);

            foreach (var appointment in list)
            {
                if (appointment.Patient != null || appointment.Booking == null)
                    continue;

                if (patientMap.TryGetValue(appointment.Booking.PatientID, out var patient))
                    appointment.Patient = patient;
            }

            return list;
        }

        public void AddRange(IEnumerable<Appointment> appointments) =>
            _context.Appointments.AddRange(appointments);

        public IEnumerable<Appointment> GetByClinicDoctorAndDateRange(int clinicId, int doctorId, DateTime startDate, DateTime endDate) =>
            _context.Appointments
                .AsNoTracking()
                .Where(a => a.ClinicID == clinicId
                         && a.DoctorID == doctorId
                         && a.Date.Date >= startDate.Date
                         && a.Date.Date <= endDate.Date)
                .OrderBy(a => a.Date).ThenBy(a => a.Time)
                .ToList();

        public IEnumerable<Appointment> GetAvailableByDoctorAndDate(int doctorId, DateTime date)
        {
            // Collect times already booked for this doctor on this date (across all clinics)
            var bookedTimes = _context.Appointments
                .Where(a => a.DoctorID == doctorId && a.Date.Date == date.Date && a.isBooked)
                .Select(a => a.Time)
                .ToHashSet();

            return _context.Appointments
                .Where(a => a.DoctorID == doctorId && a.Date.Date == date.Date && !a.isBooked)
                .OrderBy(a => a.Time)
                .AsEnumerable()
                .Where(a => !bookedTimes.Contains(a.Time))
                .ToList();
        }

        public Appointment GetFirstAvailableForDoctor(int doctorId)
        {
            var today = DateTime.Today;
            return _context.Appointments
                .Where(a => a.DoctorID == doctorId
                         && !a.isBooked
                         && a.Date >= today
                         && !_context.Appointments.Any(b =>
                                b.DoctorID == doctorId
                             && b.isBooked
                             && b.Date.Date == a.Date.Date
                             && b.Time == a.Time))
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .FirstOrDefault();
        }

        public IEnumerable<DateTime> GetAvailableDatesByDoctor(int doctorId, int year, int month)
        {
            var allAppointments = _context.Appointments
                .Where(a => a.DoctorID == doctorId
                         && a.Date.Year == year && a.Date.Month == month
                         && a.Date.Date >= DateTime.Today)
                .ToList();

            var availableDates = new List<DateTime>();
            foreach (var group in allAppointments.GroupBy(a => a.Date.Date))
            {
                var bookedTimes = group.Where(a => a.isBooked).Select(a => a.Time).ToHashSet();
                if (group.Any(a => !a.isBooked && !bookedTimes.Contains(a.Time)))
                    availableDates.Add(group.Key);
            }

            return availableDates.OrderBy(d => d);
        }

        public IEnumerable<Appointment> GetPastByPatientId(int patientId) =>
            _context.Appointments
                .Where(a => a.PatientID == patientId
                         && a.isBooked
                         && a.Date.Date < DateTime.Today)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Clinic)
                .Include(a => a.Bookings)
                .OrderByDescending(a => a.Date).ThenBy(a => a.Time)
                .ToList();

        public bool HasDoctorConflict(int doctorId, DateTime date, TimeSpan time, int excludeAppointmentId) =>
            _context.Appointments
                .Any(a => a.DoctorID == doctorId
                       && a.Date.Date == date.Date
                       && a.Time == time
                       && a.isBooked
                       && a.AppointmentID != excludeAppointmentId);

        public IEnumerable<DateTime> GetAvailableDatesByDoctorAndClinic(int doctorId, int clinicId, int year, int month)
        {
            // All booked times for this doctor across ALL clinics in this month
            var allBookedTimes = _context.Appointments
                .Where(a => a.DoctorID == doctorId
                         && a.Date.Year == year && a.Date.Month == month
                         && a.Date.Date >= DateTime.Today
                         && a.isBooked)
                .Select(a => new { a.Date, a.Time })
                .ToList()
                .GroupBy(a => a.Date.Date)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Time).ToHashSet());

            // Unbooked slots at the requested clinic
            var clinicAppointments = _context.Appointments
                .Where(a => a.DoctorID == doctorId
                         && a.ClinicID == clinicId
                         && a.Date.Year == year && a.Date.Month == month
                         && a.Date.Date >= DateTime.Today
                         && !a.isBooked)
                .ToList();

            var availableDates = new List<DateTime>();
            foreach (var group in clinicAppointments.GroupBy(a => a.Date.Date))
            {
                allBookedTimes.TryGetValue(group.Key, out var bookedTimes);
                if (group.Any(a => bookedTimes == null || !bookedTimes.Contains(a.Time)))
                    availableDates.Add(group.Key);
            }

            return availableDates.OrderBy(d => d);
        }

        public IEnumerable<Appointment> GetAvailableByDoctorClinicAndDate(int doctorId, int clinicId, DateTime date)
        {
            // Times booked across ALL clinics for this doctor on this date
            var bookedTimes = _context.Appointments
                .Where(a => a.DoctorID == doctorId && a.Date.Date == date.Date && a.isBooked)
                .Select(a => a.Time)
                .ToHashSet();

            return _context.Appointments
                .Where(a => a.DoctorID == doctorId && a.ClinicID == clinicId
                         && a.Date.Date == date.Date && !a.isBooked)
                .OrderBy(a => a.Time)
                .AsEnumerable()
                .Where(a => !bookedTimes.Contains(a.Time))
                .ToList();
        }

        public IEnumerable<Appointment> GetByDoctorAndDate(int doctorId, DateTime date) =>
            _context.Appointments
                .AsNoTracking()
                .Include(a => a.Clinic)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.DoctorID == doctorId && a.Date.Date == date.Date)
                .OrderBy(a => a.Time)
                .ToList();

        public IEnumerable<Appointment> GetByDoctorAndDateRange(int doctorId, DateTime startDate, DateTime endDate) =>
            _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorID == doctorId
                         && a.Date.Date >= startDate.Date
                         && a.Date.Date <= endDate.Date)
                .OrderBy(a => a.Date).ThenBy(a => a.Time)
                .ToList();

        public Appointment? GetAvailableSlotById(int appointmentId, int doctorId, int? clinicId = null)
        {
            var query = _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Clinic)
                .Where(a => a.AppointmentID == appointmentId
                         && a.DoctorID == doctorId
                         && !a.isBooked);

            if (clinicId.HasValue)
                query = query.Where(a => a.ClinicID == clinicId.Value);

            return query.FirstOrDefault();
        }
    }
}

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
                .Where(a => a.PatientID == patientId && a.Date > DateTime.Now)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User) // Include doctor details
                .OrderBy(a => a.Date)
                .FirstOrDefault();
        }

        public IEnumerable<Appointment> GetByPatientId(int patientId) =>
            _context.Appointments
                .Where(a => a.PatientID == patientId)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Clinic)
                .OrderByDescending(a => a.Date)
                .ToList();

        public IEnumerable<Appointment> GetByClinicAndDate(int clinicId, DateTime date) =>
            _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Booking)
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
                .Include(a => a.Booking)
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

        public IEnumerable<Appointment> GetByClinicDoctorsAndStatus(int clinicId, IEnumerable<int> doctorIds, string status) =>
            _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Booking)
                .Where(a => a.ClinicID == clinicId
                         && doctorIds.Contains(a.DoctorID)
                         && a.Booking != null
                         && a.Booking.Status == status)
                .OrderByDescending(a => a.Date).ThenBy(a => a.Time)
                .AsSplitQuery()
                .ToList();

        public Appointment GetByIdWithBooking(int id) =>
            _context.Appointments
                .Include(a => a.Booking)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefault(a => a.AppointmentID == id);

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
    }
}

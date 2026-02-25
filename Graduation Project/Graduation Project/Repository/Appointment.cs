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
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class MedicationLogRepository : IMedicationLog
    {
        private readonly AppDbContext _context;

        public MedicationLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<MedicationLog> GetAll() => _context.MedicationLogs.ToList();

        public MedicationLog? GetById(int id) => _context.MedicationLogs.Find(id);

        public IEnumerable<MedicationLog> GetByMedicationId(int medicationId) =>
            _context.MedicationLogs
                .Include(l => l.Medication)
                .Where(l => l.MedicationId == medicationId)
                .OrderByDescending(l => l.ScheduledAt)
                .ToList();

        public IEnumerable<MedicationLog> GetByPatientId(int patientId, DateTime startDate, DateTime endDate) =>
            _context.MedicationLogs
                .Include(l => l.Medication)
                .Where(l => l.Medication.PatientID == patientId
                            && l.ScheduledAt >= startDate
                            && l.ScheduledAt <= endDate)
                .OrderByDescending(l => l.ScheduledAt)
                .ToList();

        public void Add(MedicationLog log) => _context.MedicationLogs.Add(log);

        public void Update(MedicationLog log) => _context.MedicationLogs.Update(log);

        public void Delete(int id)
        {
            var entity = _context.MedicationLogs.Find(id);
            if (entity != null)
                _context.MedicationLogs.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

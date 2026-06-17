using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class MedicationRepository : IMedication
    {
        private readonly AppDbContext _context;

        public MedicationRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Medication> GetAll() => _context.Medications.ToList();

        public Medication? GetById(int id) => _context.Medications.Find(id);

        public IEnumerable<Medication> GetByPatientId(int patientId) =>
            _context.Medications
                .Include(m => m.Schedules)
                .Include(m => m.Logs)
                .Where(m => m.PatientID == patientId)
                .OrderByDescending(m => m.IsActive)
                .ThenByDescending(m => m.StartDate)
                .ToList();

        public void Add(Medication medication) => _context.Medications.Add(medication);

        public void Update(Medication medication) => _context.Medications.Update(medication);

        public void Delete(int id)
        {
            var entity = _context.Medications.Find(id);
            if (entity != null)
                _context.Medications.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class MedicationScheduleRepository : IMedicationSchedule
    {
        private readonly AppDbContext _context;

        public MedicationScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<MedicationSchedule> GetAll() => _context.MedicationSchedules.ToList();

        public MedicationSchedule? GetById(int id) => _context.MedicationSchedules.Find(id);

        public IEnumerable<MedicationSchedule> GetByMedicationId(int medicationId) =>
            _context.MedicationSchedules
                .Where(s => s.MedicationId == medicationId)
                .OrderBy(s => s.TimeOfDay)
                .ToList();

        public void Add(MedicationSchedule schedule) => _context.MedicationSchedules.Add(schedule);

        public void Update(MedicationSchedule schedule) => _context.MedicationSchedules.Update(schedule);

        public void Delete(int id)
        {
            var entity = _context.MedicationSchedules.Find(id);
            if (entity != null)
                _context.MedicationSchedules.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

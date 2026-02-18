using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class MedicalHistoryRepository : IMedicalHistory
    {
        private readonly AppDbContext _context;

        public MedicalHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Models.MedicalHistory> GetAll() => _context.MedicalHistories.ToList();

        public Models.MedicalHistory GetById(int id) => _context.MedicalHistories.Find(id);

        public void Add(Models.MedicalHistory medicalHistory) => _context.MedicalHistories.Add(medicalHistory);

        public void Update(Models.MedicalHistory medicalHistory) => _context.MedicalHistories.Update(medicalHistory);

        public void Delete(int id)
        {
            var entity = _context.MedicalHistories.Find(id);
            if (entity != null)
                _context.MedicalHistories.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

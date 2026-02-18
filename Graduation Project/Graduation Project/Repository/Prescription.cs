using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PrescriptionRepository : IPrescription
    {
        private readonly AppDbContext _context;

        public PrescriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Prescription> GetAll() => _context.Prescriptions.ToList();

        public Prescription GetById(int id) => _context.Prescriptions.Find(id);

        public void Add(Prescription prescription) => _context.Prescriptions.Add(prescription);

        public void Update(Prescription prescription) => _context.Prescriptions.Update(prescription);

        public void Delete(int id)
        {
            var entity = _context.Prescriptions.Find(id);
            if (entity != null)
                _context.Prescriptions.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

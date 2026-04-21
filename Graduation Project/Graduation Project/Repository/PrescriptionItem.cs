using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PrescriptionItemRepository : IPrescriptionItem
    {
        private readonly AppDbContext _context;

        public PrescriptionItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PrescriptionItem> GetAll() => _context.PrescriptionItems.ToList();

        public PrescriptionItem GetById(int id) => _context.PrescriptionItems.Find(id);

        public void Add(PrescriptionItem prescriptionItem) => _context.PrescriptionItems.Add(prescriptionItem);

        public void Update(PrescriptionItem prescriptionItem) => _context.PrescriptionItems.Update(prescriptionItem);

        public void Delete(int id)
        {
            var entity = _context.PrescriptionItems.Find(id);
            if (entity != null)
                _context.PrescriptionItems.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

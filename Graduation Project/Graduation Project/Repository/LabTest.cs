using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class LabTestRepository : ILabTest
    {
        private readonly AppDbContext _context;

        public LabTestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<LabTest> GetAll() => _context.LabTests.ToList();

        public LabTest GetById(int id) => _context.LabTests.Find(id);

        public void Add(LabTest labTest) => _context.LabTests.Add(labTest);

        public void Update(LabTest labTest) => _context.LabTests.Update(labTest);

        public void Delete(int id)
        {
            var entity = _context.LabTests.Find(id);
            if (entity != null)
                _context.LabTests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

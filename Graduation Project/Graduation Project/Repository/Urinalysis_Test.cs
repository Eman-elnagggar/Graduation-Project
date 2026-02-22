using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class Urinalysis_TestRepository : IUrinalysis_Test
    {
        private readonly AppDbContext _context;

        public Urinalysis_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Urinalysis_Test> GetAll() => _context.Urinalysis_Tests.ToList();

        public Urinalysis_Test GetById(int labTestId) => _context.Urinalysis_Tests.Find(labTestId);

        public void Add(Urinalysis_Test urinalysisTest) => _context.Urinalysis_Tests.Add(urinalysisTest);

        public void Update(Urinalysis_Test urinalysisTest) => _context.Urinalysis_Tests.Update(urinalysisTest);

        public void Delete(int labTestId)
        {
            var entity = _context.Urinalysis_Tests.Find(labTestId);
            if (entity != null)
                _context.Urinalysis_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class Ferritin_TestRepository : IFerritin_Test
    {
        private readonly AppDbContext _context;

        public Ferritin_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Ferritin_Test> GetAll() => _context.Ferritin_Tests.ToList();

        public Ferritin_Test GetById(int labTestId) => _context.Ferritin_Tests.Find(labTestId);

        public void Add(Ferritin_Test ferritinTest) => _context.Ferritin_Tests.Add(ferritinTest);

        public void Update(Ferritin_Test ferritinTest) => _context.Ferritin_Tests.Update(ferritinTest);

        public void Delete(int labTestId)
        {
            var entity = _context.Ferritin_Tests.Find(labTestId);
            if (entity != null)
                _context.Ferritin_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

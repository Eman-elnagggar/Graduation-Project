using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class CBC_TestRepository : ICBC_Test
    {
        private readonly AppDbContext _context;

        public CBC_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<CBC_Test> GetAll() => _context.CBC_Tests.ToList();

        public CBC_Test GetById(int labTestId) => _context.CBC_Tests.Find(labTestId);

        public void Add(CBC_Test cbcTest) => _context.CBC_Tests.Add(cbcTest);

        public void Update(CBC_Test cbcTest) => _context.CBC_Tests.Update(cbcTest);

        public void Delete(int labTestId)
        {
            var entity = _context.CBC_Tests.Find(labTestId);
            if (entity != null)
                _context.CBC_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class TSH_TestRepository : ITSH_Test
    {
        private readonly AppDbContext _context;

        public TSH_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TSH_Test> GetAll() => _context.TSH_Tests.ToList();

        public TSH_Test GetById(int labTestId) => _context.TSH_Tests.Find(labTestId);

        public void Add(TSH_Test tshTest) => _context.TSH_Tests.Add(tshTest);

        public void Update(TSH_Test tshTest) => _context.TSH_Tests.Update(tshTest);

        public void Delete(int labTestId)
        {
            var entity = _context.TSH_Tests.Find(labTestId);
            if (entity != null)
                _context.TSH_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

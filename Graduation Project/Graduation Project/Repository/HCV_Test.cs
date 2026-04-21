using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class HCV_TestRepository : IHCV_Test
    {
        private readonly AppDbContext _context;

        public HCV_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Models.HCV_Test> GetAll() => _context.HCV_Tests.ToList();

        public Models.HCV_Test GetById(int labTestId) => _context.HCV_Tests.Find(labTestId);

        public void Add(Models.HCV_Test hcvTest) => _context.HCV_Tests.Add(hcvTest);

        public void Update(Models.HCV_Test hcvTest) => _context.HCV_Tests.Update(hcvTest);

        public void Delete(int labTestId)
        {
            var entity = _context.HCV_Tests.Find(labTestId);
            if (entity != null)
                _context.HCV_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

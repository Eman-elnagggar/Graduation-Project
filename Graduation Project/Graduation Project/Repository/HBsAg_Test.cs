using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class HBsAg_TestRepository : IHBsAg_Test
    {
        private readonly AppDbContext _context;

        public HBsAg_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<HBsAg_Test> GetAll() => _context.HBsAg_Tests.ToList();

        public HBsAg_Test GetById(int labTestId) => _context.HBsAg_Tests.Find(labTestId);

        public void Add(HBsAg_Test hbsAgTest) => _context.HBsAg_Tests.Add(hbsAgTest);

        public void Update(HBsAg_Test hbsAgTest) => _context.HBsAg_Tests.Update(hbsAgTest);

        public void Delete(int labTestId)
        {
            var entity = _context.HBsAg_Tests.Find(labTestId);
            if (entity != null)
                _context.HBsAg_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

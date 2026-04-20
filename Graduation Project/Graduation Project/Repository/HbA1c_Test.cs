using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class HbA1c_TestRepository : IHbA1c_Test
    {
        private readonly AppDbContext _context;

        public HbA1c_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Models.HbA1c_Test> GetAll() => _context.HbA1c_Tests.ToList();

        public Models.HbA1c_Test GetById(int labTestId) => _context.HbA1c_Tests.Find(labTestId);

        public void Add(Models.HbA1c_Test hbA1cTest) => _context.HbA1c_Tests.Add(hbA1cTest);

        public void Update(Models.HbA1c_Test hbA1cTest) => _context.HbA1c_Tests.Update(hbA1cTest);

        public void Delete(int labTestId)
        {
            var entity = _context.HbA1c_Tests.Find(labTestId);
            if (entity != null)
                _context.HbA1c_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class BloodGroup_TestRepository : IBloodGroup_Test
    {
        private readonly AppDbContext _context;

        public BloodGroup_TestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<BloodGroup_Test> GetAll() => _context.BloodGroup_Tests.ToList();

        public BloodGroup_Test GetById(int labTestId) => _context.BloodGroup_Tests.Find(labTestId);

        public void Add(BloodGroup_Test bloodGroupTest) => _context.BloodGroup_Tests.Add(bloodGroupTest);

        public void Update(BloodGroup_Test bloodGroupTest) => _context.BloodGroup_Tests.Update(bloodGroupTest);

        public void Delete(int labTestId)
        {
            var entity = _context.BloodGroup_Tests.Find(labTestId);
            if (entity != null)
                _context.BloodGroup_Tests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

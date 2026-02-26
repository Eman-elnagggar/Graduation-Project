using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class TestReportRepository : ITestReport
    {
        private readonly AppDbContext _context;

        public TestReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TestReport> GetAll() => _context.TestReports.ToList();

        public TestReport GetById(int id) => _context.TestReports.Find(id);

        public void Add(TestReport testReport) => _context.TestReports.Add(testReport);

        public void Update(TestReport testReport) => _context.TestReports.Update(testReport);

        public void Delete(int id)
        {
            var entity = _context.TestReports.Find(id);
            if (entity != null)
                _context.TestReports.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

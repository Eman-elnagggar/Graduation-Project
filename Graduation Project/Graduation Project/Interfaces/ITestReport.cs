using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface ITestReport
    {
        IEnumerable<TestReport> GetAll();
        TestReport GetById(int id);
        void Add(TestReport testReport);
        void Update(TestReport testReport);
        void Delete(int id);
        void Save();
    }
}

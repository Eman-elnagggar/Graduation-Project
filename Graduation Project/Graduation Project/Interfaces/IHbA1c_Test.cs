using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IHbA1c_Test
    {
        IEnumerable<HbA1c_Test> GetAll();
        HbA1c_Test GetById(int labTestId);
        void Add(HbA1c_Test hbA1cTest);
        void Update(HbA1c_Test hbA1cTest);
        void Delete(int labTestId);
        void Save();
    }
}

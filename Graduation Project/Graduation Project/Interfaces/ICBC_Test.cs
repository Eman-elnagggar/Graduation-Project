using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface ICBC_Test
    {
        IEnumerable<CBC_Test> GetAll();
        CBC_Test GetById(int labTestId);
        void Add(CBC_Test cbcTest);
        void Update(CBC_Test cbcTest);
        void Delete(int labTestId);
        void Save();
    }
}

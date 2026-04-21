using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface ITSH_Test
    {
        IEnumerable<TSH_Test> GetAll();
        TSH_Test GetById(int labTestId);
        void Add(TSH_Test tshTest);
        void Update(TSH_Test tshTest);
        void Delete(int labTestId);
        void Save();
    }
}

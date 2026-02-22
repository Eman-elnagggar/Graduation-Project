using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IHCV_Test
    {
        IEnumerable<HCV_Test> GetAll();
        HCV_Test GetById(int labTestId);
        void Add(HCV_Test hcvTest);
        void Update(HCV_Test hcvTest);
        void Delete(int labTestId);
        void Save();
    }
}

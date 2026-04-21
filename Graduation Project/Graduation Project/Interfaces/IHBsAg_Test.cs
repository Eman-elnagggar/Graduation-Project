using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IHBsAg_Test
    {
        IEnumerable<HBsAg_Test> GetAll();
        HBsAg_Test GetById(int labTestId);
        void Add(HBsAg_Test hbsAgTest);
        void Update(HBsAg_Test hbsAgTest);
        void Delete(int labTestId);
        void Save();
    }
}

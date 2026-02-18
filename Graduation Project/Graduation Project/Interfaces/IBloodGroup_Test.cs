using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IBloodGroup_Test
    {
        IEnumerable<BloodGroup_Test> GetAll();
        BloodGroup_Test GetById(int labTestId);
        void Add(BloodGroup_Test bloodGroupTest);
        void Update(BloodGroup_Test bloodGroupTest);
        void Delete(int labTestId);
        void Save();
    }
}

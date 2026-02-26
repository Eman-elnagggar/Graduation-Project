using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IFerritin_Test
    {
        IEnumerable<Ferritin_Test> GetAll();
        Ferritin_Test GetById(int labTestId);
        void Add(Ferritin_Test ferritinTest);
        void Update(Ferritin_Test ferritinTest);
        void Delete(int labTestId);
        void Save();
    }
}

using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IUrinalysis_Test
    {
        IEnumerable<Urinalysis_Test> GetAll();
        Urinalysis_Test GetById(int labTestId);
        void Add(Urinalysis_Test urinalysisTest);
        void Update(Urinalysis_Test urinalysisTest);
        void Delete(int labTestId);
        void Save();
    }
}

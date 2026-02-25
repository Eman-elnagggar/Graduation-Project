using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface ILabTest
    {
        IEnumerable<LabTest> GetAll();
        LabTest GetById(int id);
        void Add(LabTest labTest);
        void Update(LabTest labTest);
        void Delete(int id);
        void Save();
        IEnumerable<LabTest> GetLabTestsByPatientId(int patientId);
        LabTest GetLastLabTestByPatientId(int patientId);
    }
}

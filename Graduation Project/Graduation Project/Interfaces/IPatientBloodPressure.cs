using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPatientBloodPressure
    {
        IEnumerable<PatientBloodPressure> GetAll();
        PatientBloodPressure GetById(int id);
        void Add(PatientBloodPressure patientBloodPressure);
        void Update(PatientBloodPressure patientBloodPressure);
        void Delete(int id);
        void Save();
    }
}

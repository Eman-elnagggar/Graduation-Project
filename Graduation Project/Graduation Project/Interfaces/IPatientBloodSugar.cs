using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPatientBloodSugar
    {
        IEnumerable<PatientBloodSugar> GetAll();
        PatientBloodSugar GetById(int id);
        void Add(PatientBloodSugar patientBloodSugar);
        void Update(PatientBloodSugar patientBloodSugar);
        void Delete(int id);
        void Save();
    }
}

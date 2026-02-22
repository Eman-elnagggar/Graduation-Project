using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPatientDrug
    {
        IEnumerable<PatientDrug> GetAll();
        PatientDrug GetById(int id);
        void Add(PatientDrug patientDrug);
        void Update(PatientDrug patientDrug);
        void Delete(int id);
        void Save();
    }
}

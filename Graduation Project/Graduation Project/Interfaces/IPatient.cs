using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPatient
    {
        IEnumerable<Patient> GetAll();
        Patient GetById(int id);
        void Add(Patient patient);
        void Update(Patient patient);
        void Delete(int id);
        void Save();
    }
}

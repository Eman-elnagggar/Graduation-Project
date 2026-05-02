using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IMedication
    {
        IEnumerable<Medication> GetAll();
        Medication? GetById(int id);
        IEnumerable<Medication> GetByPatientId(int patientId);
        void Add(Medication medication);
        void Update(Medication medication);
        void Delete(int id);
        void Save();
    }
}

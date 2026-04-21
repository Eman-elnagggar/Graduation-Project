using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPrescription
    {
        IEnumerable<Prescription> GetAll();
        Prescription GetById(int id);
        void Add(Prescription prescription);
        void Update(Prescription prescription);
        void Delete(int id);
        void Save();
        IEnumerable<Prescription> GetByPatientId(int patientId);
    }
}

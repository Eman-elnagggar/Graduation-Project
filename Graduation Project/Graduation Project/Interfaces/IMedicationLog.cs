using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IMedicationLog
    {
        IEnumerable<MedicationLog> GetAll();
        MedicationLog? GetById(int id);
        IEnumerable<MedicationLog> GetByMedicationId(int medicationId);
        IEnumerable<MedicationLog> GetByPatientId(int patientId, DateTime startDate, DateTime endDate);
        void Add(MedicationLog log);
        void Update(MedicationLog log);
        void Delete(int id);
        void Save();
    }
}

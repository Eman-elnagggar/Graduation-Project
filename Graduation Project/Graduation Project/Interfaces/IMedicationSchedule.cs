using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IMedicationSchedule
    {
        IEnumerable<MedicationSchedule> GetAll();
        MedicationSchedule? GetById(int id);
        IEnumerable<MedicationSchedule> GetByMedicationId(int medicationId);
        void Add(MedicationSchedule schedule);
        void Update(MedicationSchedule schedule);
        void Delete(int id);
        void Save();
    }
}

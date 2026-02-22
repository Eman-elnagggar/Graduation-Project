using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPrescriptionItem
    {
        IEnumerable<PrescriptionItem> GetAll();
        PrescriptionItem GetById(int id);
        void Add(PrescriptionItem prescriptionItem);
        void Update(PrescriptionItem prescriptionItem);
        void Delete(int id);
        void Save();
    }
}

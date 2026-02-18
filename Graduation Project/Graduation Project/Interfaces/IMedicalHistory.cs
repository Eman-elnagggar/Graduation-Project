using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IMedicalHistory
    {
        IEnumerable<MedicalHistory> GetAll();
        MedicalHistory GetById(int id);
        void Add(MedicalHistory medicalHistory);
        void Update(MedicalHistory medicalHistory);
        void Delete(int id);
        void Save();
    }
}

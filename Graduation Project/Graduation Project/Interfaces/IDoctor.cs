using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IDoctor
    {
        IEnumerable<Doctor> GetAll();
        Doctor GetById(int id);
        void Add(Doctor doctor);
        void Update(Doctor doctor);
        void Delete(int id);
        void Save();
        IEnumerable<Doctor> GetAllWithDetails();
    }
}

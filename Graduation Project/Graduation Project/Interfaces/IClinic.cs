using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IClinic
    {
        IEnumerable<Clinic> GetAll();
        Clinic GetById(int id);
        Clinic GetByIdWithDoctor(int id);
        void Add(Clinic clinic);
        void Update(Clinic clinic);
        void Delete(int id);
        void Save();
    }
}

using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IAssistant
    {
        IEnumerable<Assistant> GetAll();
        Assistant GetById(int id);
        Assistant GetByIdWithUser(int id);
        Assistant GetByIdWithDoctors(int id);
        void Add(Assistant assistant);
        void Update(Assistant assistant);
        void Delete(int id);
        void Save();
    }
}

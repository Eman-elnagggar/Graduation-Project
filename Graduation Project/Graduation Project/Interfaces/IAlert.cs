using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IAlert
    {
        IEnumerable<Alert> GetAll();
        Alert GetById(int id);
        void Add(Alert alert);
        void Update(Alert alert);
        void Delete(int id);
        void Save();
    }
}

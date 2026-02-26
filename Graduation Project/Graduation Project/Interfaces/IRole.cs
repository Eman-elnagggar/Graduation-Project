using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IRole
    {
        IEnumerable<Role> GetAll();
        Role GetById(int id);
        void Add(Role role);
        void Update(Role role);
        void Delete(int id);
        void Save();
    }
}

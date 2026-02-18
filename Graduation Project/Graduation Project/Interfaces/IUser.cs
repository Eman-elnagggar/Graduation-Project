using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IUser
    {
        IEnumerable<User> GetAll();
        User GetById(int id);
        void Add(User user);
        void Update(User user);
        void Delete(int id);
        void Save();
    }
}

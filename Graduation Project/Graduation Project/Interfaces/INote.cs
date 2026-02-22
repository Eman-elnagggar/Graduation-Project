using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface INote
    {
        IEnumerable<Note> GetAll();
        Note GetById(int id);
        void Add(Note note);
        void Update(Note note);
        void Delete(int id);
        void Save();
    }
}

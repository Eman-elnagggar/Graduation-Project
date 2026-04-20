using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IAIModel
    {
        IEnumerable<AIModel> GetAll();
        AIModel GetById(int id);
        void Add(AIModel aiModel);
        void Update(AIModel aiModel);
        void Delete(int id);
        void Save();
    }
}

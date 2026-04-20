using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class AIModelRepository : IAIModel
    {
        private readonly AppDbContext _context;

        public AIModelRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<AIModel> GetAll() => _context.AIModels.ToList();

        public AIModel GetById(int id) => _context.AIModels.Find(id);

        public void Add(AIModel aiModel) => _context.AIModels.Add(aiModel);

        public void Update(AIModel aiModel) => _context.AIModels.Update(aiModel);

        public void Delete(int id)
        {
            var entity = _context.AIModels.Find(id);
            if (entity != null)
                _context.AIModels.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class AssistantRepository : IAssistant
    {
        private readonly AppDbContext _context;

        public AssistantRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Assistant> GetAll() => _context.Assistants.ToList();

        public Assistant GetById(int id) => _context.Assistants.Find(id);

        public void Add(Assistant assistant) => _context.Assistants.Add(assistant);

        public void Update(Assistant assistant) => _context.Assistants.Update(assistant);

        public void Delete(int id)
        {
            var entity = _context.Assistants.Find(id);
            if (entity != null)
                _context.Assistants.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

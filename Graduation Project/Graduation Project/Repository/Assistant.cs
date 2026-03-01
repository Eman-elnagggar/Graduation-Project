using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

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

        public Assistant GetByIdWithUser(int id) =>
            _context.Assistants
                .Include(a => a.User)
                .FirstOrDefault(a => a.AssistantID == id);

        public Assistant GetByIdWithDoctors(int id) =>
            _context.Assistants
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.AssistantDoctors)
                    .ThenInclude(ad => ad.Doctor)
                        .ThenInclude(d => d.User)
                .AsSplitQuery()
                .FirstOrDefault(a => a.AssistantID == id);

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

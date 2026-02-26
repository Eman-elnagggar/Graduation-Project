using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class RoleRepository : IRole
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Role> GetAll() => _context.Roles.ToList();

        public Role GetById(int id) => _context.Roles.Find(id);

        public void Add(Role role) => _context.Roles.Add(role);

        public void Update(Role role) => _context.Roles.Update(role);

        public void Delete(int id)
        {
            var entity = _context.Roles.Find(id);
            if (entity != null)
                _context.Roles.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

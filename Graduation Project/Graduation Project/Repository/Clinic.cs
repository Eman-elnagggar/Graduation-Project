using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class ClinicRepository : IClinic
    {
        private readonly AppDbContext _context;

        public ClinicRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Clinic> GetAll() => _context.Clinics.ToList();

        public Clinic GetById(int id) => _context.Clinics.Find(id);

        public Clinic GetByIdWithDoctor(int id) =>
            _context.Clinics
                .AsNoTracking()
                .Include(c => c.ClinicDoctors)
                    .ThenInclude(cd => cd.Doctor)
                        .ThenInclude(d => d.User)
                .AsSplitQuery()
                .FirstOrDefault(c => c.ClinicID == id);

        public void Add(Clinic clinic) => _context.Clinics.Add(clinic);

        public void Update(Clinic clinic) => _context.Clinics.Update(clinic);

        public void Delete(int id)
        {
            var entity = _context.Clinics.Find(id);
            if (entity != null)
                _context.Clinics.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

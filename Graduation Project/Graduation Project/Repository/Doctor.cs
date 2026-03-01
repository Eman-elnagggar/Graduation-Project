using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class DoctorRepository : IDoctor
    {
        private readonly AppDbContext _context;

        public DoctorRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Doctor> GetAll() => _context.Doctors.ToList();

        public Doctor GetById(int id) => _context.Doctors.Find(id);

        public void Add(Doctor doctor) => _context.Doctors.Add(doctor);

        public void Update(Doctor doctor) => _context.Doctors.Update(doctor);

        public void Delete(int id)
        {
            var entity = _context.Doctors.Find(id);
            if (entity != null)
                _context.Doctors.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public IEnumerable<Doctor> GetAllWithDetails() =>
            _context.Doctors
                .Include(d => d.User)
                .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
                .ToList();
    }
}

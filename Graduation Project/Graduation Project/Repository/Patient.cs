using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PatientRepository : IPatient
    {
        private readonly AppDbContext _context;

        public PatientRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Patient> GetAll() => _context.Patients.ToList();

        public Patient GetById(int id) => _context.Patients.Find(id);

        public void Add(Patient patient) => _context.Patients.Add(patient);

        public void Update(Patient patient) => _context.Patients.Update(patient);

        public void Delete(int id)
        {
            var entity = _context.Patients.Find(id);
            if (entity != null)
                _context.Patients.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

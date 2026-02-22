using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PatientDoctorRepository : IPatientDoctor
    {
        private readonly AppDbContext _context;

        public PatientDoctorRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PatientDoctor> GetAll() => _context.PatientDoctors.ToList();

        public PatientDoctor GetById(int doctorId, int patientId) =>
            _context.PatientDoctors.Find(doctorId, patientId);

        public void Add(PatientDoctor patientDoctor) => _context.PatientDoctors.Add(patientDoctor);

        public void Update(PatientDoctor patientDoctor) => _context.PatientDoctors.Update(patientDoctor);

        public void Delete(int doctorId, int patientId)
        {
            var entity = _context.PatientDoctors.Find(doctorId, patientId);
            if (entity != null)
                _context.PatientDoctors.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

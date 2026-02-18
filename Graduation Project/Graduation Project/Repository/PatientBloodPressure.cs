using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PatientBloodPressureRepository : IPatientBloodPressure
    {
        private readonly AppDbContext _context;

        public PatientBloodPressureRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PatientBloodPressure> GetAll() => _context.PatientBloodPressure.ToList();

        public PatientBloodPressure GetById(int id) => _context.PatientBloodPressure.Find(id);

        public void Add(PatientBloodPressure patientBloodPressure) => _context.PatientBloodPressure.Add(patientBloodPressure);

        public void Update(PatientBloodPressure patientBloodPressure) => _context.PatientBloodPressure.Update(patientBloodPressure);

        public void Delete(int id)
        {
            var entity = _context.PatientBloodPressure.Find(id);
            if (entity != null)
                _context.PatientBloodPressure.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

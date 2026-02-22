using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PatientBloodSugarRepository : IPatientBloodSugar
    {
        private readonly AppDbContext _context;

        public PatientBloodSugarRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PatientBloodSugar> GetAll() => _context.PatientBloodSugar.ToList();

        public PatientBloodSugar GetById(int id) => _context.PatientBloodSugar.Find(id);

        public void Add(PatientBloodSugar patientBloodSugar) => _context.PatientBloodSugar.Add(patientBloodSugar);

        public void Update(PatientBloodSugar patientBloodSugar) => _context.PatientBloodSugar.Update(patientBloodSugar);

        public void Delete(int id)
        {
            var entity = _context.PatientBloodSugar.Find(id);
            if (entity != null)
                _context.PatientBloodSugar.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public PatientBloodSugar GetLastBloodSugarValue(int patientId)
        {
            return _context.PatientBloodSugar
                .Where(x => x.PatientID == patientId)
                .OrderByDescending(x => x.DateTime)
                .FirstOrDefault();
        }

        public IEnumerable<PatientBloodSugar> GetRecentByPatientId(int patientId, int count = 10)
        {
            return _context.PatientBloodSugar
                .Where(x => x.PatientID == patientId)
                .OrderByDescending(x => x.DateTime)
                .Take(count)
                .ToList();
        }
    }
}

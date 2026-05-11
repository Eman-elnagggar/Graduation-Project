using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class LabTestRepository : ILabTest
    {
        private readonly AppDbContext _context;

        public LabTestRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<LabTest> GetAll() => _context.LabTests.ToList();

        public LabTest GetById(int id) => _context.LabTests.Find(id);

        public void Add(LabTest labTest) => _context.LabTests.Add(labTest);

        public void Update(LabTest labTest) => _context.LabTests.Update(labTest);

        public void Delete(int id)
        {
            var entity = _context.LabTests.Find(id);
            if (entity != null)
                _context.LabTests.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public IEnumerable<LabTest> GetLabTestsByPatientId(int patientId)
        {
            return _context.LabTests.Where(lt => lt.PatientID == patientId).ToList();
        }

        public LabTest GetLastLabTestByPatientId(int patientId)
        {
            return _context.LabTests
                .Where(lt => lt.PatientID == patientId)
                .OrderByDescending(lt => lt.UploadDate)
                .FirstOrDefault();
        }

        public int CountByDoctorSince(int doctorId, DateTime since) =>
            _context.LabTests.Count(lt => lt.DoctorID == doctorId && lt.UploadDate >= since);

        public int CountByDoctorsSince(IEnumerable<int> doctorIds, DateTime since) =>
            _context.LabTests.Count(lt => doctorIds.Contains(lt.DoctorID) && lt.UploadDate >= since);
    }
}

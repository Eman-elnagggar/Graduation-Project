using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class UltrasoundImageRepository : IUltrasoundImage
    {
        private readonly AppDbContext _context;

        public UltrasoundImageRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<UltrasoundImage> GetAll() => _context.UltrasoundImages.ToList();

        public UltrasoundImage GetById(int id) => _context.UltrasoundImages.Find(id);

        public void Add(UltrasoundImage ultrasoundImage) => _context.UltrasoundImages.Add(ultrasoundImage);

        public void Update(UltrasoundImage ultrasoundImage) => _context.UltrasoundImages.Update(ultrasoundImage);

        public void Delete(int id)
        {
            var entity = _context.UltrasoundImages.Find(id);
            if (entity != null)
                _context.UltrasoundImages.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public UltrasoundImage GetLastUltrasoundByPatientId(int patientId)
        {
            return _context.UltrasoundImages
                .Where(u => u.PatientID == patientId)
                .OrderByDescending(u => u.UploadDate)
                .FirstOrDefault();
        }

        public IEnumerable<UltrasoundImage> GetUltrasoundsByPatientId(int patientId) =>
            _context.UltrasoundImages
                .Where(u => u.PatientID == patientId)
                .Include(u => u.Doctor).ThenInclude(d => d.User)
                .OrderByDescending(u => u.UploadDate)
                .ToList();
    }
}

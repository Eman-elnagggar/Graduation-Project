using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

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
    }
}

using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PlaceRepository : IPlace
    {
        private readonly AppDbContext _context;

        public PlaceRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Place> GetAll() => _context.Places.ToList();

        public Place GetById(int id) => _context.Places.Find(id);

        public void Add(Place place) => _context.Places.Add(place);

        public void Update(Place place) => _context.Places.Update(place);

        public void Delete(int id)
        {
            var entity = _context.Places.Find(id);
            if (entity != null)
                _context.Places.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public IEnumerable<Place> GetByPatientId(int patientId) =>
            _context.Places.Where(p => p.PatientID == patientId).ToList();
    }
}

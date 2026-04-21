using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class WeightTrackingRepository : IWeightTracking
    {
        private readonly AppDbContext _context;

        public WeightTrackingRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<WeightTracking> GetAll() => _context.WeightTrackings.ToList();

        public WeightTracking GetById(int id) => _context.WeightTrackings.Find(id);

        public void Add(WeightTracking weightTracking) => _context.WeightTrackings.Add(weightTracking);

        public void Update(WeightTracking weightTracking) => _context.WeightTrackings.Update(weightTracking);

        public void Delete(int id)
        {
            var entity = _context.WeightTrackings.Find(id);
            if (entity != null)
                _context.WeightTrackings.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

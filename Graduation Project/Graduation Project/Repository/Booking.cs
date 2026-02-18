using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class BookingRepository : IBooking
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Booking> GetAll() => _context.Bookings.ToList();

        public Booking GetById(int id) => _context.Bookings.Find(id);

        public void Add(Booking booking) => _context.Bookings.Add(booking);

        public void Update(Booking booking) => _context.Bookings.Update(booking);

        public void Delete(int id)
        {
            var entity = _context.Bookings.Find(id);
            if (entity != null)
                _context.Bookings.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

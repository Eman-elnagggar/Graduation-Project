using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IBooking
    {
        IEnumerable<Booking> GetAll();
        Booking GetById(int id);
        void Add(Booking booking);
        void Update(Booking booking);
        void Delete(int id);
        void Save();
    }
}

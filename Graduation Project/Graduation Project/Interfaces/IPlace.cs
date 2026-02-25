using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPlace
    {
        IEnumerable<Place> GetAll();
        Place GetById(int id);
        IEnumerable<Place> GetByPatientId(int patientId);
        void Add(Place place);
        void Update(Place place);
        void Delete(int id);
        void Save();
    }
}

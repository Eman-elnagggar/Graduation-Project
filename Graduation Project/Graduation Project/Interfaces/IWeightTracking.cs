using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IWeightTracking
    {
        IEnumerable<WeightTracking> GetAll();
        WeightTracking GetById(int id);
        void Add(WeightTracking weightTracking);
        void Update(WeightTracking weightTracking);
        void Delete(int id);
        void Save();
    }
}

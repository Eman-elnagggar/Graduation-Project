using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IAlert
    {
        IEnumerable<Alert> GetAll();
        Alert GetById(int id);
        IEnumerable<Alert> GetByPatientId(int patientId);
        void Add(Alert alert);
        void Update(Alert alert);
        void Delete(int id);
        void Save();
        IEnumerable<Alert> GetUnreadByPatientIds(IEnumerable<int> patientIds, int count);
        IEnumerable<int> GetPatientIdsWithUnreadAlerts(IEnumerable<int> patientIds);
    }
}

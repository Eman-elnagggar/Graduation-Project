using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class AlertRepository : IAlert
    {
        private readonly AppDbContext _context;

        public AlertRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Alert> GetAll() => _context.Alerts.ToList();

        public Alert GetById(int id) => _context.Alerts.Find(id);

        public IEnumerable<Alert> GetByPatientId(int patientId) =>
            _context.Alerts
                .Where(a => a.PatientID == patientId)
                .OrderByDescending(a => a.DateCreated)
                .ToList();

        public void Add(Alert alert) => _context.Alerts.Add(alert);

        public void Update(Alert alert) => _context.Alerts.Update(alert);

        public void Delete(int id)
        {
            var entity = _context.Alerts.Find(id);
            if (entity != null)
                _context.Alerts.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

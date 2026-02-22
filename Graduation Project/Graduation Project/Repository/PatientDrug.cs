using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Repository
{
    public class PatientDrugRepository : IPatientDrug
    {
        private readonly AppDbContext _context;

        public PatientDrugRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PatientDrug> GetAll() => _context.PatientDrugs.ToList();

        public PatientDrug GetById(int id) => _context.PatientDrugs.Find(id);

        public void Add(PatientDrug patientDrug) => _context.PatientDrugs.Add(patientDrug);

        public void Update(PatientDrug patientDrug) => _context.PatientDrugs.Update(patientDrug);

        public void Delete(int id)
        {
            var entity = _context.PatientDrugs.Find(id);
            if (entity != null)
                _context.PatientDrugs.Remove(entity);
        }

        public void Save() => _context.SaveChanges();
    }
}

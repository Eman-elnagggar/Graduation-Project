using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPatientDoctor
    {
        IEnumerable<PatientDoctor> GetAll();
        PatientDoctor GetById(int doctorId, int patientId);
        void Add(PatientDoctor patientDoctor);
        void Update(PatientDoctor patientDoctor);
        void Delete(int doctorId, int patientId);
        void Save();
        IEnumerable<PatientDoctor> GetApprovedByDoctor(int doctorId);
        IEnumerable<PatientDoctor> GetApprovedByDoctors(IEnumerable<int> doctorIds);
    }
}

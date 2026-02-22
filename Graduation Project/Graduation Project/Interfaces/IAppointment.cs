using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IAppointment
    {
        IEnumerable<Appointment> GetAll();
        Appointment GetById(int id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(int id);
        void Save();
        Appointment GetNextAppointmentForPatient(int patientId);
        IEnumerable<Appointment> GetByPatientId(int patientId);
    }
}

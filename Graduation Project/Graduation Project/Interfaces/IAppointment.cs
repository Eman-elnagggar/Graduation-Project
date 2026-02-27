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
        IEnumerable<Appointment> GetByClinicAndDate(int clinicId, DateTime date);
        IEnumerable<Appointment> GetByClinicDoctorAndDate(int clinicId, int doctorId, DateTime date);
        Dictionary<int, DateTime> GetLastVisitDates(IEnumerable<int> patientIds, int doctorId);
        Dictionary<int, DateTime> GetLastVisitDatesForDoctors(IEnumerable<int> patientIds, IEnumerable<int> doctorIds);
        IEnumerable<Appointment> GetByClinicDoctorsAndStatus(int clinicId, IEnumerable<int> doctorIds, string status);
        Appointment GetByIdWithBooking(int id);
    }
}

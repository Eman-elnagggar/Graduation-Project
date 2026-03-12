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
        void AddRange(IEnumerable<Appointment> appointments);
        IEnumerable<Appointment> GetByClinicDoctorAndDateRange(int clinicId, int doctorId, DateTime startDate, DateTime endDate);
        IEnumerable<Appointment> GetAvailableByDoctorAndDate(int doctorId, DateTime date);
        Appointment GetFirstAvailableForDoctor(int doctorId);
        IEnumerable<DateTime> GetAvailableDatesByDoctor(int doctorId, int year, int month);
        IEnumerable<Appointment> GetPastByPatientId(int patientId);
        bool HasDoctorConflict(int doctorId, DateTime date, TimeSpan time, int excludeAppointmentId);
        IEnumerable<DateTime> GetAvailableDatesByDoctorAndClinic(int doctorId, int clinicId, int year, int month);
        IEnumerable<Appointment> GetAvailableByDoctorClinicAndDate(int doctorId, int clinicId, DateTime date);
        IEnumerable<Appointment> GetByDoctorAndDate(int doctorId, DateTime date);
        IEnumerable<Appointment> GetByDoctorAndDateRange(int doctorId, DateTime startDate, DateTime endDate);
    }
}

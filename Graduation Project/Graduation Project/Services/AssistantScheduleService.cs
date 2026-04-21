using Graduation_Project.Interfaces;

namespace Graduation_Project.Services
{
    public class AssistantScheduleService
    {
        private readonly IAssistant _assistantRepository;
        private readonly IClinic _clinicRepository;
        private readonly IAppointment _appointmentRepository;

        public AssistantScheduleService(
            IAssistant assistantRepository,
            IClinic clinicRepository,
            IAppointment appointmentRepository)
        {
            _assistantRepository = assistantRepository;
            _clinicRepository = clinicRepository;
            _appointmentRepository = appointmentRepository;
        }

        public AssistantScheduleScope? BuildScope(int assistantId, int? doctorId)
        {
            var assistant = _assistantRepository.GetByIdWithDoctors(assistantId);
            if (assistant == null || !assistant.ClinicID.HasValue) return null;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID.Value);
            if (clinic == null) return null;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var activeDoctorIds = isFiltered ? new List<int> { doctorId!.Value } : relevantDoctorIds;

            return new AssistantScheduleScope
            {
                AssistantId = assistantId,
                ClinicId = clinic.ClinicID,
                ActiveDoctorIds = activeDoctorIds
            };
        }

        public AssistantPagedAppointmentsResult GetAppointmentsPage(AssistantScheduleScope scope, string status, DateTime date, int page, int pageSize, string? search)
        {
            var total = _appointmentRepository.CountByClinicDoctorsStatusAndDate(
                scope.ClinicId, scope.ActiveDoctorIds, status, date, search);

            var items = _appointmentRepository
                .GetPagedByClinicDoctorsStatusAndDate(scope.ClinicId, scope.ActiveDoctorIds, status, date, search, page, pageSize)
                .Select(a => (object)new
                {
                    appointmentId = a.AppointmentID,
                    patientName = a.Patient?.User != null
                        ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}" : "Unknown",
                    patientPhone = a.Patient?.User?.Phone ?? string.Empty,
                    doctorName = a.Doctor?.User != null
                        ? $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}" : "Unknown",
                    doctorSpecialization = a.Doctor?.Specialization ?? string.Empty,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    time = a.Time.ToString(@"hh\:mm"),
                    status = a.Booking?.Status ?? "Confirmed",
                    reason = a.Booking?.Reason ?? string.Empty,
                    notes = a.Booking?.Notes ?? string.Empty,
                    isToday = a.Date.Date == DateTime.Today
                })
                .ToList();

            var safePageSize = Math.Clamp(pageSize, 5, 100);
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));

            return new AssistantPagedAppointmentsResult
            {
                Items = items,
                Total = total,
                Page = Math.Max(1, page),
                PageSize = safePageSize,
                TotalPages = totalPages
            };
        }

        public AssistantAppointmentStatusCounts GetCounts(AssistantScheduleScope scope, DateTime date)
        {
            var statuses = new[] { "Confirmed", "Modified", "Cancelled" };
            var counts = _appointmentRepository
                .GetStatusCountsByClinicDoctorsAndDate(scope.ClinicId, scope.ActiveDoctorIds, date, statuses);

            var confirmed = counts.GetValueOrDefault("Confirmed");
            var modified = counts.GetValueOrDefault("Modified");
            var cancelled = counts.GetValueOrDefault("Cancelled");

            return new AssistantAppointmentStatusCounts
            {
                Confirmed = confirmed,
                Modified = modified,
                Cancelled = cancelled,
                Total = confirmed + modified + cancelled
            };
        }

        private static List<int> GetRelevantDoctorIds(Models.Assistant assistant, Models.Clinic clinic)
        {
            var assistantDoctorIds = assistant.AssistantDoctors?
                .Select(ad => ad.DoctorID).ToHashSet() ?? new HashSet<int>();
            var clinicDoctorIds = clinic.ClinicDoctors?
                .Select(cd => cd.DoctorID).ToHashSet() ?? new HashSet<int>();
            var relevantDoctorIds = assistantDoctorIds.Intersect(clinicDoctorIds).ToList();

            if (!relevantDoctorIds.Any())
                relevantDoctorIds = clinicDoctorIds.ToList();

            return relevantDoctorIds;
        }
    }

    public class AssistantScheduleScope
    {
        public int AssistantId { get; set; }
        public int ClinicId { get; set; }
        public List<int> ActiveDoctorIds { get; set; } = new();
    }

    public class AssistantAppointmentStatusCounts
    {
        public int Confirmed { get; set; }
        public int Modified { get; set; }
        public int Cancelled { get; set; }
        public int Total { get; set; }
    }

    public class AssistantPagedAppointmentsResult
    {
        public List<object> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

}

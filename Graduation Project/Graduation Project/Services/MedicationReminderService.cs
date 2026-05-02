using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class MedicationReminderService
    {
        private readonly AppDbContext _context;
        private readonly IAlert _alertRepository;
        private readonly IMedicationLog _logRepository;

        public MedicationReminderService(AppDbContext context, IAlert alertRepository, IMedicationLog logRepository)
        {
            _context = context;
            _alertRepository = alertRepository;
            _logRepository = logRepository;
        }

        public List<MedicationDueSlot> GetDueSlots(int patientId, DateTime date)
        {
            var start = date.Date;
            var end = date.Date.AddDays(1);
            var medications = _context.Medications
                .Include(m => m.Schedules)
                .Where(m => m.PatientID == patientId
                            && m.IsActive
                            && (m.EndDate == null || m.EndDate.Value.Date >= date.Date))
                .ToList();

            var logs = _logRepository.GetByPatientId(patientId, start, end).ToList();
            var slots = new List<MedicationDueSlot>();

            foreach (var med in medications)
            {
                foreach (var schedule in med.Schedules)
                {
                    var scheduledAt = start.Add(schedule.TimeOfDay);
                    var existing = logs.FirstOrDefault(l => l.MedicationId == med.MedicationId && l.ScheduledAt == scheduledAt);

                    slots.Add(new MedicationDueSlot
                    {
                        MedicationId = med.MedicationId,
                        MedicationName = med.Name,
                        ScheduledAt = scheduledAt,
                        Status = existing?.Status ?? MedicationLogStatus.Scheduled,
                        Dosage = med.Dosage,
                        Instructions = med.Instructions
                    });
                }
            }

            return slots
                .OrderBy(s => s.ScheduledAt)
                .ToList();
        }

        public void EvaluateReminders(DateTime date)
        {
            var day = date.Date;
            var patientIds = _context.Medications
                .Where(m => m.IsActive && (m.EndDate == null || m.EndDate.Value.Date >= day))
                .Select(m => m.PatientID)
                .Distinct()
                .ToList();

            foreach (var patientId in patientIds)
            {
                var dueSlots = GetDueSlots(patientId, day);
                var leadTimeMinutes = _context.MedicationReminderSettings
                    .Where(s => s.PatientID == patientId)
                    .Select(s => (int?)s.LeadTimeMinutes)
                    .FirstOrDefault() ?? 30;
                var overdue = dueSlots
                    .Where(s => s.Status == MedicationLogStatus.Scheduled
                                && s.ScheduledAt <= DateTime.Now.AddMinutes(leadTimeMinutes))
                    .ToList();

                foreach (var slot in overdue)
                {
                    var medLeadTime = _context.Medications
                        .Where(m => m.MedicationId == slot.MedicationId)
                        .Select(m => m.ReminderLeadTimeMinutes)
                        .FirstOrDefault();
                    var effectiveLead = medLeadTime ?? leadTimeMinutes;

                    if (slot.ScheduledAt > DateTime.Now.AddMinutes(effectiveLead))
                        continue;

                    var message = $"It's time to take {slot.MedicationName} ({slot.Dosage}).";
                    var title = "Medication Reminder";

                    var existingToday = _alertRepository
                        .GetByPatientId(patientId)
                        .Any(a => a.DateCreated.Date == day
                                  && a.Title == title
                                  && a.Message == message);

                    if (existingToday)
                        continue;

                    _alertRepository.Add(new Alert
                    {
                        PatientID = patientId,
                        Title = title,
                        Message = message,
                        AlertType = AlertTypes.Info,
                        DateCreated = DateTime.Now,
                        IsRead = false
                    });
                }

                _alertRepository.Save();
            }
        }
    }

    public class MedicationDueSlot
    {
        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public MedicationLogStatus Status { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }
}

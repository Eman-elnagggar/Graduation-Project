using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class MedicationAdherenceService
    {
        private readonly AppDbContext _context;
        private readonly IMedicationLog _logRepository;

        public MedicationAdherenceService(AppDbContext context, IMedicationLog logRepository)
        {
            _context = context;
            _logRepository = logRepository;
        }

        public MedicationLog LogDose(int medicationId, DateTime scheduledAt, MedicationLogStatus status, string? notes)
        {
            var log = _context.MedicationLogs
                .FirstOrDefault(l => l.MedicationId == medicationId && l.ScheduledAt == scheduledAt);

            if (log == null)
            {
                log = new MedicationLog
                {
                    MedicationId = medicationId,
                    ScheduledAt = scheduledAt,
                    Status = status,
                    TakenAt = status == MedicationLogStatus.Taken ? DateTime.Now : null,
                    Notes = notes
                };
                _logRepository.Add(log);
            }
            else
            {
                log.Status = status;
                log.TakenAt = status == MedicationLogStatus.Taken ? DateTime.Now : null;
                log.Notes = notes;
                _logRepository.Update(log);
            }

            _logRepository.Save();
            return log;
        }

        public MedicationAdherenceSummary GetSummary(int patientId, DateTime startDate, DateTime endDate)
        {
            var logs = _logRepository
                .GetByPatientId(patientId, startDate, endDate)
                .ToList();

            var total = logs.Count;
            var taken = logs.Count(l => l.Status == MedicationLogStatus.Taken);
            var missed = logs.Count(l => l.Status == MedicationLogStatus.Missed);
            var skipped = logs.Count(l => l.Status == MedicationLogStatus.Skipped);
            var adherence = total > 0 ? (double)taken / total * 100d : 0d;

            return new MedicationAdherenceSummary
            {
                PatientId = patientId,
                StartDate = startDate,
                EndDate = endDate,
                TotalDoses = total,
                TakenDoses = taken,
                MissedDoses = missed,
                SkippedDoses = skipped,
                AdherencePercent = Math.Round(adherence, 1)
            };
        }
    }

    public class MedicationAdherenceSummary
    {
        public int PatientId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDoses { get; set; }
        public int TakenDoses { get; set; }
        public int MissedDoses { get; set; }
        public int SkippedDoses { get; set; }
        public double AdherencePercent { get; set; }
    }
}

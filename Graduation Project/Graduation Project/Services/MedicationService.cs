using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class MedicationService
    {
        private readonly AppDbContext _context;
        private readonly IMedication _medicationRepository;
        private readonly IMedicationSchedule _scheduleRepository;

        public MedicationService(
            AppDbContext context,
            IMedication medicationRepository,
            IMedicationSchedule scheduleRepository)
        {
            _context = context;
            _medicationRepository = medicationRepository;
            _scheduleRepository = scheduleRepository;
        }

        public Medication CreateFromPrescription(PrescriptionItem item, DateTime prescriptionDate)
        {
            var medication = new Medication
            {
                PatientID = item.Prescription.PatientID,
                Name = item.MedicineName ?? string.Empty,
                Dosage = item.Dosage ?? string.Empty,
                Frequency = item.Frequency ?? string.Empty,
                Instructions = item.Instructions ?? string.Empty,
                StartDate = prescriptionDate.Date,
                EndDate = item.DurationDays > 0 ? prescriptionDate.Date.AddDays(item.DurationDays) : null,
                Source = MedicationSource.Prescription,
                PrescriptionItemId = item.ItemID,
                IsActive = true
            };

            _medicationRepository.Add(medication);
            _medicationRepository.Save();

            EnsureDefaultSchedule(medication, item.Frequency);
            return medication;
        }

        public Medication AddSelfMedication(
            int patientId,
            string name,
            string dosage,
            string frequency,
            string instructions,
            DateTime startDate,
            int? durationDays)
        {
            var medication = new Medication
            {
                PatientID = patientId,
                Name = name.Trim(),
                Dosage = dosage.Trim(),
                Frequency = frequency.Trim(),
                Instructions = instructions.Trim(),
                StartDate = startDate.Date,
                EndDate = durationDays.HasValue && durationDays.Value > 0
                    ? startDate.Date.AddDays(durationDays.Value)
                    : null,
                Source = MedicationSource.Self,
                IsActive = true
            };

            _medicationRepository.Add(medication);
            _medicationRepository.Save();

            EnsureDefaultSchedule(medication, frequency);
            return medication;
        }

        public IEnumerable<Medication> GetActiveMedications(int patientId)
        {
            var today = DateTime.Today;
            return _context.Medications
                .Include(m => m.Schedules)
                .Include(m => m.Logs)
                .Where(m => m.PatientID == patientId
                            && m.IsActive
                            && (m.EndDate == null || m.EndDate.Value.Date >= today))
                .OrderByDescending(m => m.StartDate)
                .ToList();
        }

        public MedicationReminderSettings GetOrCreateReminderSettings(int patientId)
        {
            var settings = _context.MedicationReminderSettings
                .FirstOrDefault(s => s.PatientID == patientId);

            if (settings != null)
                return settings;

            settings = new MedicationReminderSettings
            {
                PatientID = patientId,
                LeadTimeMinutes = 30,
                UpdatedAt = DateTime.Now
            };

            _context.MedicationReminderSettings.Add(settings);
            _context.SaveChanges();
            return settings;
        }

        public void SaveReminderSettings(MedicationReminderSettings settings)
        {
            _context.MedicationReminderSettings.Update(settings);
            _context.SaveChanges();
        }

        public void UpdateMedicationLeadTime(int medicationId, int? leadTimeMinutes)
        {
            var medication = _medicationRepository.GetById(medicationId);
            if (medication == null)
                return;

            medication.ReminderLeadTimeMinutes = leadTimeMinutes;
            _medicationRepository.Update(medication);
            _medicationRepository.Save();
        }

        public void UpdateMedicationStatus(int medicationId, bool isActive)
        {
            var medication = _medicationRepository.GetById(medicationId);
            if (medication == null)
                return;

            medication.IsActive = isActive;
            _medicationRepository.Update(medication);
            _medicationRepository.Save();
        }

        /// <summary>
        /// Soft-removes a medication from the patient's active tracker (sets IsActive = false).
        /// </summary>
        public bool RemoveMedicationForPatient(int medicationId, int patientId)
        {
            var medication = _medicationRepository.GetById(medicationId);
            if (medication == null || medication.PatientID != patientId)
                return false;

            medication.IsActive = false;
            _medicationRepository.Update(medication);
            _medicationRepository.Save();
            return true;
        }

        private void EnsureDefaultSchedule(Medication medication, string? frequencyText)
        {
            var normalized = (frequencyText ?? string.Empty).ToLowerInvariant();
            var times = new List<TimeSpan>();

            if (normalized.Contains("three") || normalized.Contains("3"))
            {
                times.Add(new TimeSpan(8, 0, 0));
                times.Add(new TimeSpan(14, 0, 0));
                times.Add(new TimeSpan(20, 0, 0));
            }
            else if (normalized.Contains("twice") || normalized.Contains("2"))
            {
                times.Add(new TimeSpan(9, 0, 0));
                times.Add(new TimeSpan(21, 0, 0));
            }
            else
            {
                times.Add(new TimeSpan(9, 0, 0));
            }

            var frequencyPerDay = Math.Max(times.Count, 1);
            foreach (var time in times)
            {
                _scheduleRepository.Add(new MedicationSchedule
                {
                    MedicationId = medication.MedicationId,
                    TimeOfDay = time,
                    FrequencyPerDay = frequencyPerDay
                });
            }

            _scheduleRepository.Save();
        }
    }
}

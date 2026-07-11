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
            // A prescription only carries free text, so infer a schedulable spec from it.
            var spec = MedicationFrequencies.Parse(item.Frequency);
            if (!string.IsNullOrWhiteSpace(item.Frequency))
                spec.Label = item.Frequency.Trim();

            var medication = new Medication
            {
                PatientID = item.Prescription.PatientID,
                Name = item.MedicineName ?? string.Empty,
                Dosage = item.Dosage ?? string.Empty,
                Instructions = item.Instructions ?? string.Empty,
                StartDate = prescriptionDate.Date,
                EndDate = item.DurationDays > 0 ? prescriptionDate.Date.AddDays(item.DurationDays) : null,
                Source = MedicationSource.Prescription,
                PrescriptionItemId = item.ItemID,
                IsActive = true
            };

            _medicationRepository.Add(medication);
            _medicationRepository.Save();

            ApplyFrequency(medication, spec);
            return medication;
        }

        public Medication AddSelfMedication(
            int patientId,
            string name,
            string dosage,
            string? form,
            MedicationFrequencySpec frequency,
            string instructions,
            DateTime startDate,
            int? durationDays,
            int? totalPills,
            int? pillsPerDose)
        {
            var medication = new Medication
            {
                PatientID = patientId,
                Name = name.Trim(),
                Dosage = dosage.Trim(),
                Form = string.IsNullOrWhiteSpace(form) ? null : form.Trim(),
                Instructions = instructions.Trim(),
                StartDate = startDate.Date,
                EndDate = durationDays.HasValue && durationDays.Value > 0
                    ? startDate.Date.AddDays(durationDays.Value)
                    : null,
                TotalPills = totalPills,
                PillsPerDose = pillsPerDose,
                Source = MedicationSource.Self,
                IsActive = true
            };

            _medicationRepository.Add(medication);
            _medicationRepository.Save();

            ApplyFrequency(medication, frequency);
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

        public bool UpdateSelfMedication(
            int medicationId,
            int patientId,
            string name,
            string dosage,
            string? form,
            MedicationFrequencySpec frequency,
            string instructions,
            DateTime startDate,
            int? durationDays,
            int? totalPills,
            int? pillsPerDose)
        {
            var medication = _medicationRepository.GetById(medicationId);
            if (medication == null || medication.PatientID != patientId)
                return false;

            medication.Name = name.Trim();
            medication.Dosage = dosage.Trim();
            medication.Form = string.IsNullOrWhiteSpace(form) ? null : form.Trim();
            medication.Instructions = instructions.Trim();
            medication.StartDate = startDate.Date;
            medication.EndDate = durationDays.HasValue && durationDays.Value > 0
                ? startDate.Date.AddDays(durationDays.Value)
                : null;
            medication.TotalPills = totalPills;
            medication.PillsPerDose = pillsPerDose;

            _medicationRepository.Update(medication);
            _medicationRepository.Save();

            ApplyFrequency(medication, frequency);
            return true;
        }

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

        /// <summary>
        /// Writes the frequency onto the medication and rebuilds its dose schedule so
        /// the stored times always match the chosen frequency exactly.
        /// </summary>
        public void ApplyFrequency(Medication medication, MedicationFrequencySpec spec)
        {
            var times = spec.Times.OrderBy(t => t).ToList();

            medication.FrequencyCode = spec.Code;
            medication.Frequency = string.IsNullOrWhiteSpace(spec.Label)
                ? MedicationFrequencies.Find(spec.Code)?.Label ?? "Custom schedule"
                : spec.Label.Trim();
            medication.TimesPerDay = times.Count;
            medication.IntervalDays = Math.Max(spec.IntervalDays, 1);

            foreach (var existing in _scheduleRepository.GetByMedicationId(medication.MedicationId).ToList())
                _scheduleRepository.Delete(existing.MedicationScheduleId);
            _scheduleRepository.Save();

            foreach (var time in times)
            {
                _scheduleRepository.Add(new MedicationSchedule
                {
                    MedicationId = medication.MedicationId,
                    TimeOfDay = time,
                    FrequencyPerDay = times.Count
                });
            }

            _scheduleRepository.Save();

            _medicationRepository.Update(medication);
            _medicationRepository.Save();
        }
    }
}

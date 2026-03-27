using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientProfileController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPatientDrug _patientDrugRepository;

        public PatientProfileController(IPatient patientRepository, IPatientDrug patientDrugRepository)
        {
            _patientRepository = patientRepository;
            _patientDrugRepository = patientDrugRepository;
        }

        // GET: /PatientProfile/Index/5
        public IActionResult Index(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var user = patient.User;

            // ── Pregnancy calculations ──────────────────────────────
            int currentWeek = 0, remainingDays = 0, daysIntoWeek = 0;
            string dueDate = "N/A";

            if (patient.DateOfPregnancy.HasValue)
            {
                int totalDays = (int)(DateTime.Today - patient.DateOfPregnancy.Value.Date).TotalDays;
                currentWeek = Math.Clamp(totalDays / 7, 0, 40);
                daysIntoWeek = totalDays % 7;
                var dueDateValue = patient.DateOfPregnancy.Value.AddDays(280);
                dueDate = dueDateValue.ToString("MMM dd, yyyy");
                remainingDays = Math.Max(0, (int)(dueDateValue - DateTime.Today).TotalDays);
            }
            else if (patient.GestationalWeeks > 0)
            {
                currentWeek = Math.Clamp(patient.GestationalWeeks, 0, 40);
            }

            string trimester = currentWeek <= 13 ? "1st Trimester"
              : currentWeek <= 26 ? "2nd Trimester"
             : "3rd Trimester";

            int pregnancyPercent = (int)Math.Round(currentWeek / 40.0 * 100);

            // ── Age ─────────────────────────────────────────────────
            int age = 0;
            if (user?.DateOfBirth != default)
            {
                age = DateTime.Today.Year - user.DateOfBirth.Year;
                if (DateTime.Today < user.DateOfBirth.AddYears(age)) age--;
            }

            // ── BMI ─────────────────────────────────────────────────
            double bmi = 0;
            string bmiStatus = "N/A";
            if (patient.WeightKg > 0 && patient.HeightCm > 0)
            {
                double heightM = patient.HeightCm / 100.0;
                bmi = Math.Round(patient.WeightKg / (heightM * heightM), 1);
                bmiStatus = bmi < 18.5 ? "Underweight"
                       : bmi < 25   ? "Normal"
                 : bmi < 30   ? "Overweight"
                    : "Obese";
            }

            // ── Medications ─────────────────────────────────────────
            var medications = _patientDrugRepository.GetAll()
              .Where(d => d.PatientID == id)
                     .ToList();

            var viewModel = new PatientProfileViewModel
             {
              Patient            = patient,
            User     = user,
                   UserName      = user?.FirstName ?? "Patient",
           FullName           = user != null ? $"{user.FirstName} {user.LastName}" : "Patient",
           Age            = age,
                Email              = user?.Email ?? string.Empty,
        Phone              = user?.Phone ?? string.Empty,
         PregnancyWeek      = currentWeek,
          PregnancyDays      = daysIntoWeek,
      PregnancyProgressPercent = pregnancyPercent,
             Trimester      = trimester,
            DueDate      = dueDate,
            DaysRemaining    = remainingDays,
         Bmi           = bmi,
           BmiStatus     = bmiStatus,
               Medications        = medications
                };

         return View(viewModel);
        }

        // POST: /PatientProfile/SavePersonal
     [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePersonal(int patientId, string firstName, string lastName,
            string phone, string address, DateTime? dateOfBirth)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

     if (patient.User != null)
     {
     patient.User.FirstName  = firstName?.Trim() ?? patient.User.FirstName;
          patient.User.LastName   = lastName?.Trim()  ?? patient.User.LastName;
    patient.User.Phone = phone?.Trim()     ?? patient.User.Phone;
   if (dateOfBirth.HasValue)
         patient.User.DateOfBirth = dateOfBirth.Value;
  }

      patient.Address = address?.Trim() ?? patient.Address;

         _patientRepository.Update(patient);
     _patientRepository.Save();

            return Json(new { success = true });
   }

        // POST: /PatientProfile/SaveMeasurements
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveMeasurements(int patientId, double weightKg, double heightCm)
    {
     var (patient, failure) = AuthorizePatientAccess(patientId, true);
       if (failure != null)
           return failure;

          if (weightKg > 0) patient.WeightKg = weightKg;
            if (heightCm > 0) patient.HeightCm = heightCm;

       _patientRepository.Update(patient);
        _patientRepository.Save();

       double bmi = 0;
          string bmiStatus = "N/A";
            if (patient.WeightKg > 0 && patient.HeightCm > 0)
            {
        double h = patient.HeightCm / 100.0;
       bmi = Math.Round(patient.WeightKg / (h * h), 1);
         bmiStatus = bmi < 18.5 ? "Underweight" : bmi < 25 ? "Normal" : bmi < 30 ? "Overweight" : "Obese";
  }

 return Json(new { success = true, bmi, bmiStatus });
        }

        // POST: /PatientProfile/SavePregnancy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePregnancy(int patientId, DateTime? pregnancyDate,
            bool isFirstPregnancy, int previousPregnancies, int abortions, int births)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId, true);
    if (failure != null)
    return failure;

    if (pregnancyDate.HasValue) patient.DateOfPregnancy = pregnancyDate;
            patient.IsFirstPregnancy    = isFirstPregnancy;
            patient.PreviousPregnancies = previousPregnancies;
  patient.Abortions         = abortions;
patient.Births    = births;

       _patientRepository.Update(patient);
  _patientRepository.Save();

            return Json(new { success = true });
        }

    // POST: /PatientProfile/SaveMedical
   [HttpPost]
        [ValidateAntiForgeryToken]
     public IActionResult SaveMedical(int patientId, bool bloodPressureIssue,
            bool smoking, bool alcoholUse)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
         return failure;

   patient.BloodPressureIssue = bloodPressureIssue;
       patient.Smoking            = smoking;
    patient.AlcoholUse         = alcoholUse;

            _patientRepository.Update(patient);
      _patientRepository.Save();

      return Json(new { success = true });
        }

  // POST: /PatientProfile/AddMedication
      [HttpPost]
     [ValidateAntiForgeryToken]
  public IActionResult AddMedication(int patientId, string drugName, string reason,
  double doseMgPerDay, int durationMonths)
 {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

     if (string.IsNullOrWhiteSpace(drugName) || string.IsNullOrWhiteSpace(reason))
    return Json(new { success = false, message = "Drug name and reason are required." });

            var drug = new PatientDrug
        {
                PatientID      = patientId,
    DrugName   = drugName.Trim(),
      Reason         = reason.Trim(),
                DoseMgPerDay   = doseMgPerDay,
    DurationMonths = durationMonths
     };

     _patientDrugRepository.Add(drug);
          _patientDrugRepository.Save();

            return Json(new { success = true, id = drug.DrugID, name = drug.DrugName, reason = drug.Reason, dose = drug.DoseMgPerDay, duration = drug.DurationMonths });
        }

        // POST: /PatientProfile/DeleteMedication
  [HttpPost]
 [ValidateAntiForgeryToken]
      public IActionResult DeleteMedication(int drugId, int patientId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

       var drug = _patientDrugRepository.GetById(drugId);
          if (drug == null || drug.PatientID != patientId)
           return Json(new { success = false });

   _patientDrugRepository.Delete(drugId);
            _patientDrugRepository.Save();

      return Json(new { success = true });
        }

        private (Patient? patient, IActionResult? failure) AuthorizePatientAccess(int patientId, bool returnJsonOnFailure = false)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return (null, NotFound());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                if (returnJsonOnFailure)
                    return (null, Unauthorized(new { success = false, message = "Unauthorized." }));

                return (null, Unauthorized());
            }

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
            {
                if (returnJsonOnFailure)
                    return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." }));

                return (null, Forbid());
            }

            return (patient, null);
        }
    }
}

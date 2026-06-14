using Graduation_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientTestsController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly ILabTest _labTestRepository;
        private readonly IPatientDoctor _patientDoctorRepository;

        public PatientTestsController(IPatient patientRepository, ILabTest labTestRepository, IPatientDoctor patientDoctorRepository)
        {
            _patientRepository = patientRepository;
            _labTestRepository = labTestRepository;
            _patientDoctorRepository = patientDoctorRepository;
        }

        public IActionResult TestsUpload(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return Forbid();

            var previousTests = _labTestRepository.GetLabTestsByPatientId(id).ToList();

            var approvedDoctors = _patientDoctorRepository
                .GetByPatientId(id)
                .Where(pd => string.Equals(pd.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                          && pd.Doctor != null
                          && !string.IsNullOrWhiteSpace(pd.Doctor.UserID))
                .GroupBy(pd => pd.DoctorID)
                .Select(g => g.First())
                .Select(pd => new
                {
                    userId = pd.Doctor!.UserID!,
                    name = pd.Doctor.User != null
                        ? $"Dr. {pd.Doctor.User.FirstName} {pd.Doctor.User.LastName}".Trim()
                        : "Doctor"
                })
                .ToList();

            ViewBag.PatientId = id;
            ViewBag.UserName = patient.User?.FirstName ?? "Patient";
            ViewBag.PreviousTests = previousTests;
            ViewBag.ApprovedDoctorsJson = JsonSerializer.Serialize(approvedDoctors);

            return View("~/Views/Patient/TestsUpload.cshtml");
        }
    }
}

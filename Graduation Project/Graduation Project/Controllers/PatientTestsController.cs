using Graduation_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientTestsController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly ILabTest _labTestRepository;

        public PatientTestsController(IPatient patientRepository, ILabTest labTestRepository)
        {
            _patientRepository = patientRepository;
            _labTestRepository = labTestRepository;
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

            ViewBag.PatientId = id;
            ViewBag.UserName = patient.User?.FirstName ?? "Patient";
            ViewBag.PreviousTests = previousTests;

            return View("~/Views/Patient/TestsUpload.cshtml");
        }
    }
}

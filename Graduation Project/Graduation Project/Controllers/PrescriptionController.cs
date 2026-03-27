using Graduation_Project.Interfaces;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PrescriptionController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPrescription _prescriptionRepository;

        public PrescriptionController(
            IPatient patientRepository,
            IPrescription prescriptionRepository)
        {
            _patientRepository = patientRepository;
            _prescriptionRepository = prescriptionRepository;
        }

        // ---------------------------------------------------------------
        // GET: /Prescription/Index/5
        // ---------------------------------------------------------------
        public IActionResult Index(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return Forbid();

            var prescriptions = _prescriptionRepository.GetByPatientId(id).ToList();

            var viewModel = new PrescriptionsViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Prescriptions = prescriptions
            };

            return View(viewModel);
        }
    }
}

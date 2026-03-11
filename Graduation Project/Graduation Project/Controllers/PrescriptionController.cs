using Graduation_Project.Interfaces;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
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

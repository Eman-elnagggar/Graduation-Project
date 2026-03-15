using Graduation_Project.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class PatientTestsController : Controller
    {
        private readonly IPatient _patientRepository;

        public PatientTestsController(IPatient patientRepository)
        {
            _patientRepository = patientRepository;
        }

        public IActionResult TestsUpload(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            ViewBag.PatientId = id;
            ViewBag.UserName = patient.User?.FirstName ?? "Patient";
            return View("~/Views/Patient/TestsUpload.cshtml");
        }
    }
}

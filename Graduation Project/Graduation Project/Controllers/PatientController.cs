using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class PatientController : Controller
    {
        private readonly IPatient _patientRepository;

        public PatientController(IPatient patientRepository)
        {
            _patientRepository = patientRepository;
        }

        public IActionResult Index()
        {
            throw new NotImplementedException();
        }

        public IActionResult Details(int id)
        {
            throw new NotImplementedException();
        }

        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Patient patient)
        {
            throw new NotImplementedException();
        }

        public IActionResult Edit(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Patient patient)
        {
            throw new NotImplementedException();
        }

        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            throw new NotImplementedException();
        }
    }
}

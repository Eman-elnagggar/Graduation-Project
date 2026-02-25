using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class PlaceController : Controller
    {
        private readonly IPlace _placeRepo;

        public PlaceController(IPlace placeRepo)
        {
            _placeRepo = placeRepo;
        }

        // GET: /Place/Index?patientId=1
        public IActionResult Index(int patientId)
        {
            var places = _placeRepo.GetAll()
                                   .Where(p => p.PatientID == patientId)
                                   .ToList();
            ViewBag.PatientId = patientId;
            return View(places);
        }

        // POST: /Place/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Place place)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PatientId = place.PatientID;
                var places = _placeRepo.GetAll()
                                       .Where(p => p.PatientID == place.PatientID)
                                       .ToList();
                return View("Index", places);
            }

            _placeRepo.Add(place);
            _placeRepo.Save();

            return RedirectToAction("Index", new { patientId = place.PatientID });
        }

        // POST: /Place/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int patientId)
        {
            _placeRepo.Delete(id);
            _placeRepo.Save();

            return RedirectToAction("Index", new { patientId });
        }
    }
}

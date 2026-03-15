using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class PatientPlacesController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPlace _placeRepository;

        public PatientPlacesController(IPatient patientRepository, IPlace placeRepository)
        {
            _patientRepository = patientRepository;
            _placeRepository = placeRepository;
        }

        [HttpGet]
        public IActionResult Places(int id)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var places = _placeRepository.GetByPatientId(id).ToList();

            var viewModel = new PlacesViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Places = places
            };

            return View("~/Views/Patient/Places.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Places(Patient patient)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePlace(int patientId, string name, string type, string? address, string? phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                return BadRequest(new { success = false, message = "Name and type are required." });

            var place = new Place
            {
                PatientID = patientId,
                Name = name,
                Type = type,
                Address = address ?? string.Empty,
                Phone = phone ?? string.Empty,
                ImageURL = string.Empty
            };

            _placeRepository.Add(place);
            _placeRepository.Save();

            return Json(new
            {
                success = true,
                id = place.PlaceID,
                name = place.Name,
                type = place.Type,
                address = place.Address,
                phone = place.Phone,
                imageUrl = place.ImageURL
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePlace(int placeId, int patientId, string name, string type, string? address, string? phone)
        {
            var place = _placeRepository.GetById(placeId);
            if (place == null || place.PatientID != patientId)
                return Json(new { success = false });

            place.Name = name;
            place.Type = type;
            place.Address = address ?? string.Empty;
            place.Phone = phone ?? string.Empty;

            _placeRepository.Update(place);
            _placeRepository.Save();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePlace(int placeId, int patientId)
        {
            var place = _placeRepository.GetById(placeId);
            if (place == null || place.PatientID != patientId)
                return Json(new { success = false });

            _placeRepository.Delete(placeId);
            _placeRepository.Save();

            return Json(new { success = true });
        }
    }
}

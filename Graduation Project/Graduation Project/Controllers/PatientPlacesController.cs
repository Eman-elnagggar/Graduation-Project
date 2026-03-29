using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
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
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

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
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

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
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

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
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var place = _placeRepository.GetById(placeId);
            if (place == null || place.PatientID != patientId)
                return Json(new { success = false });

            _placeRepository.Delete(placeId);
            _placeRepository.Save();

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

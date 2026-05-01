using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{

    [Authorize(Roles = "Patient")]
    public class PublicDoctorController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPatient _patientRepository;

        public PublicDoctorController(AppDbContext context, IPatient patientRepository)
        {
            _context = context;
            _patientRepository = patientRepository;
        }

        [HttpGet]
        public IActionResult Details(int doctorId, int patientId)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId);
            if (failure != null) return failure;

            var doctor = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.ClinicDoctors)
                    .ThenInclude(cd => cd.Clinic)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Bookings)
                .AsNoTracking()
                .FirstOrDefault(d => d.DoctorID == doctorId);

            if (doctor == null || doctor.User == null)
                return NotFound();

            
            if (!doctor.User.IsActive)
                return NotFound();


            var now = DateTime.Now;

            var availableSlots = doctor.Appointments?
                .Count(a => a.Date.Add(a.Time) > now && !a.isBooked) ?? 0;

            var completedAppts = doctor.Appointments?
                .Count(a => a.Bookings.Any(b => b.IsActive &&
                    string.Equals(b.Status, "Completed", StringComparison.OrdinalIgnoreCase))) ?? 0;

            var clinics = doctor.ClinicDoctors?
                .Where(cd => cd.Clinic != null)
                .Select(cd => new PublicDoctorClinicSummary
                {
                    ClinicID = cd.ClinicID,
                    Name = cd.Clinic.Name ?? string.Empty,
                    Location = cd.Clinic.Location ?? string.Empty
                })
                .ToList() ?? new List<PublicDoctorClinicSummary>();

            var fullName = $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim();

            var vm = new PublicDoctorProfileViewModel
            {
                DoctorID            = doctor.DoctorID,
                FullName            = fullName,
                FirstName           = doctor.User.FirstName ?? string.Empty,
                Specialization      = doctor.Specialization ?? "General Practice",
                Address             = doctor.Address ?? string.Empty,
                VerificationStatus  = doctor.VerificationStatus ?? "Pending",
                VerificationDate    = doctor.VerificationDate,
                // Replace with doctor.Bio once you add that column to the model
                Bio                 = $"Dr. {doctor.User.FirstName} {doctor.User.LastName} is a licensed medical professional " +
                                      $"specializing in {doctor.Specialization ?? "general medicine"}. " +
                                      "They are committed to delivering compassionate and evidence-based patient care.",
                TotalAppointments   = availableSlots,
                CompletedAppointments = completedAppts,
                Clinics             = clinics,
                CurrentPatientId    = patientId
            };

            ViewData["Title"]         = vm.FullName + " — Doctor Profile";
            ViewData["PatientId"]     = patientId;
            ViewData["UserName"]      = patient!.User?.FirstName ?? "Patient";
            ViewData["PageTitleHtml"] = $"Doctor <span class='topbar-accent'>Profile</span>";
            ViewData["PageSubtitle"]  = vm.FullName;

            return View(vm);
        }


        private (Patient? patient, IActionResult? failure) AuthorizePatientAccess(int patientId)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return (null, NotFound());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return (null, Unauthorized());

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return (null, Forbid());

            return (patient, null);
        }
    }
}

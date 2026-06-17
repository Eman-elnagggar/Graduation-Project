using Graduation_Project.Interfaces;
using Graduation_Project.Data;
using Graduation_Project.Models;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PrescriptionController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPrescription _prescriptionRepository;
        private readonly AppDbContext _context;

        public PrescriptionController(
            IPatient patientRepository,
            IPrescription prescriptionRepository,
            AppDbContext context)
        {
            _patientRepository = patientRepository;
            _prescriptionRepository = prescriptionRepository;
            _context = context;
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

        [HttpGet]
        public IActionResult Print(int id, int prescriptionId)
        {
            var patient = _patientRepository.GetById(id);
            if (patient == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return Forbid();

            var prescription = _context.Prescriptions
                .Include(p => p.Items)
                .Include(p => p.Patient)
                    .ThenInclude(pt => pt.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefault(p => p.PrescriptionID == prescriptionId && p.PatientID == patient.PatientID);

            if (prescription == null)
                return NotFound();

            var doctor = prescription.Doctor;
            if (doctor == null)
                return NotFound();

            var clinic = _context.ClinicDoctors
                .Include(cd => cd.Clinic)
                .Where(cd => cd.DoctorID == doctor.DoctorID)
                .Select(cd => cd.Clinic)
                .FirstOrDefault();

            var followUp = _context.Appointments
                .Where(a => a.DoctorID == doctor.DoctorID
                         && a.PatientID == patient.PatientID
                         && a.Date.Date >= prescription.PrescriptionDate.Date
                         && a.isBooked)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .Select(a => (DateTime?)a.Date.Date.Add(a.Time))
                .FirstOrDefault();

            var doctorName = string.Join(" ", new[] { doctor.User?.FirstName, doctor.User?.LastName }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            var vm = new DoctorPrescriptionPrintViewModel
            {
                Doctor = doctor,
                DoctorName = string.IsNullOrWhiteSpace(doctorName) ? "Doctor" : doctorName,
                Patient = prescription.Patient,
                Prescription = prescription,
                ClinicName = clinic?.Name,
                ClinicAddress = clinic?.Location,
                ClinicPhone = doctor.User?.PhoneNumber,
                FollowUpDate = followUp,
                IsPrintedByPatient = true
            };

            return View("~/Views/Doctor/PrintPrescription.cshtml", vm);
        }
    }
}

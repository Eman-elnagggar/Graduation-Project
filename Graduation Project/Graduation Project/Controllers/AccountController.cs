using Graduation_Project.Data;
using Graduation_Project.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _environment = environment;
        }

        private static string? NormalizeBabyGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim() switch
            {
                "Male" => "Male",
                "Female" => "Female",
                "Unknown" => "Unknown",
                _ => null
            };
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            ViewBag.Email = email;
            ViewBag.RememberMe = rememberMe;

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Please enter your email and password.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AuthError"] = "Please enter your email address.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Please enter your password.";
                return View();
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                TempData["AuthError"] = "Please enter a valid email address.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["AuthError"] = "No account was found with this email.";
                return View();
            }

            if (!user.IsActive)
            {
                TempData["AuthError"] = "Your account is inactive. Please contact support.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    TempData["AuthError"] = "Your account is temporarily locked. Please try again later.";
                else if (result.IsNotAllowed)
                    TempData["AuthError"] = "Sign-in is not allowed for this account.";
                else
                    TempData["AuthError"] = "Incorrect password. Please try again.";
                return View();
            }

            return await RedirectToRoleLandingAsync(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterPatient()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatient(IFormCollection form)
        {
            var firstName = form["FirstName"].ToString().Trim();
            var lastName = form["LastName"].ToString().Trim();
            var email = form["Email"].ToString().Trim();
            var password = form["Password"].ToString();
            var confirmPassword = form["ConfirmPassword"].ToString();
            var hasDateOfBirth = DateTime.TryParse(form["DateOfBirth"], out var dob);
            var hasWeight = double.TryParse(form["Weight"], NumberStyles.Any, CultureInfo.InvariantCulture, out var weightKg)
                            || double.TryParse(form["Weight"], NumberStyles.Any, CultureInfo.CurrentCulture, out weightKg);
            var hasHeight = double.TryParse(form["Height"], NumberStyles.Any, CultureInfo.InvariantCulture, out var heightCm)
                            || double.TryParse(form["Height"], NumberStyles.Any, CultureInfo.CurrentCulture, out heightCm);
            var hasPregnancyStartDate = DateTime.TryParse(form["LastMenstrualPeriod"], out var dateOfPregnancy);
            var hasFirstPregnancySelection = bool.TryParse(form["IsFirstPregnancy"], out var isFirstPregnancy);
            var hasBloodPressureSelection = bool.TryParse(form["HasBloodPressureIssues"], out var bloodPressureIssue);

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Please fill all required fields.";
                return View();
            }

            if (!hasDateOfBirth)
            {
                TempData["AuthError"] = "Date of birth is required.";
                return View();
            }

            var age = DateTime.Today.Year - dob.Year;
            if (dob.Date > DateTime.Today.AddYears(-age))
                age--;

            if (age <= 0)
            {
                TempData["AuthError"] = "Please enter a valid date of birth.";
                return View();
            }

            if (!hasWeight || weightKg <= 0)
            {
                TempData["AuthError"] = "Weight (kg) is required.";
                return View();
            }

            if (!hasHeight || heightCm <= 0)
            {
                TempData["AuthError"] = "Height (cm) is required.";
                return View();
            }

            if (!hasBloodPressureSelection)
            {
                TempData["AuthError"] = "Please select blood pressure (Yes/No).";
                return View();
            }

            if (!hasPregnancyStartDate)
            {
                TempData["AuthError"] = "Pregnancy start date is required.";
                return View();
            }

            if (dateOfPregnancy.Date > DateTime.Today)
            {
                TempData["AuthError"] = "Pregnancy start date cannot be in the future.";
                return View();
            }

            if (!hasFirstPregnancySelection)
            {
                TempData["AuthError"] = "Please select if this is your first pregnancy.";
                return View();
            }

            var previousPregnancies = 0;
            var abortions = 0;
            var births = 0;

            if (!isFirstPregnancy)
            {
                if (!int.TryParse(form["PreviousPregnancies"], out previousPregnancies) || previousPregnancies <= 0)
                {
                    TempData["AuthError"] = "Please enter how many pregnancies you had before.";
                    return View();
                }

                if (!int.TryParse(form["Abortions"], out abortions) || abortions < 0)
                {
                    TempData["AuthError"] = "Please enter a valid abortion number.";
                    return View();
                }

                if (!int.TryParse(form["Births"], out births) || births < 0)
                {
                    TempData["AuthError"] = "Please enter a valid birth number.";
                    return View();
                }
            }

            if (password != confirmPassword)
            {
                TempData["AuthError"] = "Password and confirmation do not match.";
                return View();
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                TempData["AuthError"] = "Email is already registered.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = form["PhoneNumber"].ToString(),
                DateOfBirth = dob,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                TempData["AuthError"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return View();
            }

            await EnsureRoleAsync("Patient");
            await _userManager.AddToRoleAsync(user, "Patient");

            var babyGender = NormalizeBabyGender(form["BabyGender"].ToString());

            var baselinePregnancyCount = previousPregnancies + 1;

            var patient = new Patient
            {
                UserID = user.Id,
                Address = form["Address"].ToString(),
                DateOfPregnancy = dateOfPregnancy,
                GestationalWeeks = Math.Max(0, (int)((DateTime.Today - dateOfPregnancy.Date).TotalDays / 7)),
                IsFirstPregnancy = isFirstPregnancy,
                PreviousPregnancies = previousPregnancies,
                PregnancyCount = baselinePregnancyCount,
                LastPregnancyStartedAt = dateOfPregnancy,
                Abortions = abortions,
                Births = births,
                WeightKg = weightKg,
                HeightCm = heightCm,
                BloodPressureIssue = bloodPressureIssue,
                Smoking = bool.TryParse(form["Smoking"], out var smoking) && smoking,
                AlcoholUse = bool.TryParse(form["Alcohol"], out var alcohol) && alcohol
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            _context.PregnancyRecords.Add(new PregnancyRecord
            {
                PatientID = patient.PatientID,
                StartDate = dateOfPregnancy,
                BabyGender = babyGender,
                CreatedAt = DateTime.Now
            });

            var medicationNameKeys = form.Keys
                .Where(k => k.StartsWith("Medications[") && k.EndsWith("].Name"))
                .ToList();

            foreach (var nameKey in medicationNameKeys)
            {
                var prefix = nameKey[..(nameKey.Length - ".Name".Length)];
                var medName = form[nameKey].ToString().Trim();
                if (string.IsNullOrWhiteSpace(medName))
                    continue;

                var doseRaw = form[$"{prefix}.Dose"].ToString().Trim();
                var durationRaw = form[$"{prefix}.Duration"].ToString().Trim();

                var doseMatch = Regex.Match(doseRaw, @"\d+(\.\d+)?");
                var durationMatch = Regex.Match(durationRaw, @"\d+");

                var dose = doseMatch.Success && double.TryParse(doseMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDose)
                    ? parsedDose
                    : 0;

                var durationMonths = durationMatch.Success && int.TryParse(durationMatch.Value, out var parsedDuration)
                    ? parsedDuration
                    : 0;

                _context.PatientDrugs.Add(new PatientDrug
                {
                    PatientID = patient.PatientID,
                    DrugName = medName,
                    DoseMgPerDay = dose,
                    DurationMonths = durationMonths,
                    Reason = "Added during registration"
                });
            }

            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return await RedirectToRoleLandingAsync(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterDoctor()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(IFormCollection form)
        {
            var firstName = form["FirstName"].ToString().Trim();
            var lastName = form["LastName"].ToString().Trim();
            var email = form["Email"].ToString().Trim();
            var password = form["Password"].ToString();
            var confirmPassword = form["ConfirmPassword"].ToString();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Please fill all required fields.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["AuthError"] = "Password and confirmation do not match.";
                return View();
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                TempData["AuthError"] = "Email is already registered.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = form["PhoneNumber"].ToString(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                DateOfBirth = DateTime.UtcNow.Date
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                TempData["AuthError"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return View();
            }

            await EnsureRoleAsync("Doctor");
            await _userManager.AddToRoleAsync(user, "Doctor");

            var licensePath = await SaveDoctorLicenseAsync(form.Files["LicenseFile"]);

            var doctor = new Doctor
            {
                UserID = user.Id,
                Specialization = form["MedicalSpecialty"].ToString(),
                LicenseNumber = $"LIC-{DateTime.UtcNow:yyyyMMddHHmmss}",
                LicenseImagePath = licensePath,
                VerificationStatus = "Pending",
                VerificationDate = null,
                Address = string.Empty
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            TempData["AuthSuccess"] = "Doctor account created. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterAssistant()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAssistant(IFormCollection form)
        {
            var firstName = form["FirstName"].ToString().Trim();
            var lastName = form["LastName"].ToString().Trim();
            var email = form["Email"].ToString().Trim();
            var password = form["Password"].ToString();
            var confirmPassword = form["ConfirmPassword"].ToString();
            var agreeTerms = bool.TryParse(form["AgreeTerms"], out var agreed) && agreed;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Please fill all required fields.";
                return View();
            }

            if (!agreeTerms)
            {
                TempData["AuthError"] = "You must agree to terms and privacy policy.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["AuthError"] = "Password and confirmation do not match.";
                return View();
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                TempData["AuthError"] = "Email is already registered.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = form["PhoneNumber"].ToString(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                DateOfBirth = DateTime.UtcNow.Date
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                TempData["AuthError"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return View();
            }

            await EnsureRoleAsync("Assistant");
            await _userManager.AddToRoleAsync(user, "Assistant");

            var clinic = _context.Clinics.FirstOrDefault();
            if (clinic == null)
            {
                clinic = new Clinic
                {
                    Name = "Default Clinic",
                    Location = "TBD"
                };
                _context.Clinics.Add(clinic);
                await _context.SaveChangesAsync();
            }

            var assistant = new Assistant
            {
                UserID = user.Id,
                ClinicID = clinic.ClinicID
            };

            _context.Assistants.Add(assistant);
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return await RedirectToRoleLandingAsync(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> AccessDenied(string? returnUrl = null)
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var currentRole = roles.FirstOrDefault();

            if (string.Equals(currentRole, "Patient", StringComparison.OrdinalIgnoreCase) && user != null)
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient != null)
                    ViewBag.PatientId = patient.PatientID;
            }

            ViewBag.AttemptedUrl = string.IsNullOrWhiteSpace(returnUrl) ? Request.Path.Value : returnUrl;
            ViewBag.UserRole = currentRole;

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadMyData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserID == user.Id);
            var medications = patient == null
                ? new List<PatientDrug>()
                : await _context.PatientDrugs.AsNoTracking()
                    .Where(d => d.PatientID == patient.PatientID)
                    .ToListAsync();

            static string EscapeCsv(string? value)
            {
                if (string.IsNullOrEmpty(value))
                    return "";

                var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
                if (!needsQuotes)
                    return value;

                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            static string CsvRow(params string?[] columns)
                => string.Join(',', columns.Select(EscapeCsv));

            var csv = new StringBuilder();
            csv.AppendLine(CsvRow("Section", "Field", "Value"));
            csv.AppendLine(CsvRow("Meta", "ExportedAtUtc", DateTime.UtcNow.ToString("O")));

            csv.AppendLine(CsvRow("Account", "Email", user.Email));
            csv.AppendLine(CsvRow("Account", "UserName", user.UserName));
            csv.AppendLine(CsvRow("Account", "FirstName", user.FirstName));
            csv.AppendLine(CsvRow("Account", "LastName", user.LastName));
            csv.AppendLine(CsvRow("Account", "PhoneNumber", user.PhoneNumber));
            csv.AppendLine(CsvRow("Account", "DateOfBirth", user.DateOfBirth.ToString("yyyy-MM-dd")));
            csv.AppendLine(CsvRow("Account", "CreatedDate", user.CreatedDate.ToString("O")));
            csv.AppendLine(CsvRow("Account", "IsActive", user.IsActive.ToString()));
            csv.AppendLine(CsvRow("Account", "Roles", string.Join(" | ", roles)));

            if (patient != null)
            {
                csv.AppendLine(CsvRow("PatientProfile", "Address", patient.Address));
                csv.AppendLine(CsvRow("PatientProfile", "DateOfPregnancy", patient.DateOfPregnancy?.ToString("yyyy-MM-dd")));
                csv.AppendLine(CsvRow("PatientProfile", "GestationalWeeks", patient.GestationalWeeks.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "IsFirstPregnancy", patient.IsFirstPregnancy.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "PreviousPregnancies", patient.PreviousPregnancies.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "Abortions", patient.Abortions.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "Births", patient.Births.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "WeightKg", patient.WeightKg.ToString(CultureInfo.InvariantCulture)));
                csv.AppendLine(CsvRow("PatientProfile", "HeightCm", patient.HeightCm.ToString(CultureInfo.InvariantCulture)));
                csv.AppendLine(CsvRow("PatientProfile", "BloodPressureIssue", patient.BloodPressureIssue.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "Smoking", patient.Smoking.ToString()));
                csv.AppendLine(CsvRow("PatientProfile", "AlcoholUse", patient.AlcoholUse.ToString()));
            }

            for (var i = 0; i < medications.Count; i++)
            {
                var med = medications[i];
                var index = (i + 1).ToString();
                csv.AppendLine(CsvRow($"Medication #{index}", "DrugName", med.DrugName));
                csv.AppendLine(CsvRow($"Medication #{index}", "Reason", med.Reason));
                csv.AppendLine(CsvRow($"Medication #{index}", "DoseMgPerDay", med.DoseMgPerDay.ToString(CultureInfo.InvariantCulture)));
                csv.AppendLine(CsvRow($"Medication #{index}", "DurationMonths", med.DurationMonths.ToString()));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"my-data-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            user.IsActive = false;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = string.Join(" ", updateResult.Errors.Select(e => e.Description))
                });
            }

            await _signInManager.SignOutAsync();
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Login), "Account") });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return Json(new { success = false, message = "Please fill in all password fields." });

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                return Json(new { success = false, message = "New passwords do not match." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    message = string.Join(" ", result.Errors.Select(e => e.Description))
                });
            }

            await _signInManager.RefreshSignInAsync(user);
            return Json(new { success = true });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> LogoutGet()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private async Task<IActionResult> RedirectToRoleLandingAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Patient"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                if (patient != null)
                    return RedirectToAction("Index", "Patient", new { id = patient.PatientID });
            }

            if (await _userManager.IsInRoleAsync(user, "Assistant"))
            {
                var assistant = await _context.Assistants.FirstOrDefaultAsync(a => a.UserID == user.Id);
                if (assistant != null)
                    return RedirectToAction("Index", "Assistant", new { id = assistant.AssistantID });
            }

            if (await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                return RedirectToAction("Index", "Doctor");
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private async Task<string> SaveDoctorLicenseAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "licenses");
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"license_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/licenses/{fileName}";
        }
    }
}

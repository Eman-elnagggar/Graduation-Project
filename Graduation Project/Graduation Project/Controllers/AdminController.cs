using Graduation_Project.Data;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ─── Dashboard ────────────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["ActivePage"] = "Dashboard";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var today = DateTime.Today;
            var totalPatients = await _context.Patients.CountAsync();
            var totalDoctors = await _context.Doctors.CountAsync();
            var totalAssistants = await _context.Assistants.CountAsync();
            var totalClinics = await _context.Clinics.CountAsync();
            var appointmentsToday = await _context.Appointments
                .Where(a => a.Date.Date == today && a.isBooked)
                .CountAsync();
            var pendingInvitations = await _context.ClinicInvitations
                .Where(ci => ci.Status == "Pending")
                .CountAsync();
            var alertsToday = await _context.Alerts
                .Where(a => a.DateCreated.Date == today)
                .CountAsync();
            var pendingDoctors = await _context.Doctors
                .Where(d => d.VerificationStatus == "Pending")
                .CountAsync();

            var activeUsers = await _context.Users
                .Where(u => u.IsActive)
                .CountAsync();

            var weeklyAppts = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var d = today.AddDays(-i);
                var count = await _context.Appointments
                    .Where(a => a.Date.Date == d && a.isBooked)
                    .CountAsync();
                weeklyAppts.Add(count);
            }

            var monthlyGrowth = new List<int>();
            var monthLabels = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var count = await _context.Users
                    .Where(u => u.CreatedDate >= monthStart && u.CreatedDate < monthEnd)
                    .CountAsync();
                monthlyGrowth.Add(count);
                monthLabels.Add(monthStart.ToString("MMM"));
            }

            var recentAlerts = await _context.Alerts
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .OrderByDescending(a => a.DateCreated)
                .Take(6)
                .Select(a => new AdminAlertItem
                {
                    Title = a.Title,
                    Message = a.Message,
                    AlertType = a.AlertType,
                    DateCreated = a.DateCreated,
                    PatientName = a.Patient.User.FirstName + " " + a.Patient.User.LastName
                })
                .ToListAsync();

            var recentActivity = BuildRecentActivity();

            var vm = new AdminDashboardViewModel
            {
                TotalPatients = totalPatients,
                TotalDoctors = totalDoctors,
                TotalAssistants = totalAssistants,
                TotalClinics = totalClinics,
                AppointmentsToday = appointmentsToday,
                PendingClinicInvitations = pendingInvitations,
                AlertsToday = alertsToday,
                PendingDoctorVerifications = pendingDoctors,
                ActiveUsers = activeUsers,
                WeeklyAppointments = weeklyAppts,
                MonthlyUserGrowth = monthlyGrowth,
                MonthLabels = monthLabels,
                RecentAlerts = recentAlerts,
                RecentActivity = recentActivity
            };

            return View(vm);
        }

        // ─── User Management ──────────────────────────────────────────────────

        public async Task<IActionResult> Users(string search = "", string role = "", string status = "")
        {
            ViewData["Title"] = "User Management";
            ViewData["ActivePage"] = "Users";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var users = await _context.Users.ToListAsync();

            var userRows = new List<AdminUserRow>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "Unknown";

                if (!string.IsNullOrWhiteSpace(role) &&
                    !string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active" && !user.IsActive) continue;
                    if (status == "inactive" && user.IsActive) continue;
                }

                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.ToLowerInvariant();
                    if (!fullName.ToLowerInvariant().Contains(q) &&
                        !user.Email!.ToLowerInvariant().Contains(q))
                        continue;
                }

                int? entityId = null;
                if (userRole == "Doctor")
                {
                    var doc = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == user.Id);
                    entityId = doc?.DoctorID;
                }
                else if (userRole == "Patient")
                {
                    var pat = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);
                    entityId = pat?.PatientID;
                }
                else if (userRole == "Assistant")
                {
                    var ast = await _context.Assistants.FirstOrDefaultAsync(a => a.UserID == user.Id);
                    entityId = ast?.AssistantID;
                }

                userRows.Add(new AdminUserRow
                {
                    UserId = user.Id,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? user.UserName! : fullName,
                    Email = user.Email ?? "",
                    Role = userRole,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate,
                    PhoneNumber = user.PhoneNumber,
                    RoleEntityId = entityId
                });
            }

            var allUsers = await _context.Users.ToListAsync();
            var activeCount = allUsers.Count(u => u.IsActive);

            var vm = new AdminUserListViewModel
            {
                Users = userRows.OrderByDescending(u => u.CreatedDate).ToList(),
                SearchQuery = search,
                RoleFilter = role,
                StatusFilter = status,
                TotalCount = allUsers.Count,
                ActiveCount = activeCount,
                InactiveCount = allUsers.Count - activeCount
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["AdminSuccess"] = $"User {user.Email} has been {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, "Nabd@123");
            if (result.Succeeded)
                TempData["AdminSuccess"] = $"Password for {user.Email} has been reset to Nabd@123.";
            else
                TempData["AdminError"] = "Failed to reset password.";

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["AdminError"] = "Admin accounts cannot be soft-deleted.";
                return RedirectToAction(nameof(Users));
            }

            // Store original email before mangling so we can display/recover it
            if (string.IsNullOrWhiteSpace(user.OriginalEmail))
                user.OriginalEmail = user.Email;

            var mangledEmail = $"deleted_{user.Id}@nabd.deleted";
            user.IsActive = false;
            user.Email = mangledEmail;
            user.NormalizedEmail = mangledEmail.ToUpperInvariant();
            user.UserName = mangledEmail;
            user.NormalizedUserName = mangledEmail.ToUpperInvariant();
            // Lock the account so they cannot sign in
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);

            TempData["AdminSuccess"] = $"User ({user.OriginalEmail}) has been soft-deleted and locked out.";
            return RedirectToAction(nameof(Users));
        }

        // ─── Doctor Verification ──────────────────────────────────────────────

        public async Task<IActionResult> DoctorVerification()
        {
            ViewData["Title"] = "Doctor Verification";
            ViewData["ActivePage"] = "DoctorVerification";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
                .OrderByDescending(d => d.User.CreatedDate)
                .ToListAsync();

            var pending = doctors.Where(d => d.VerificationStatus == "Pending" || string.IsNullOrEmpty(d.VerificationStatus))
                .Select(d => MapDoctorRow(d)).ToList();
            var approved = doctors.Where(d => d.VerificationStatus == "Approved")
                .Select(d => MapDoctorRow(d)).ToList();
            var rejected = doctors.Where(d => d.VerificationStatus == "Rejected")
                .Select(d => MapDoctorRow(d)).ToList();

            var vm = new AdminDoctorVerificationViewModel
            {
                PendingDoctors = pending,
                ApprovedDoctors = approved,
                RejectedDoctors = rejected
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDoctor(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                TempData["AdminError"] = "Doctor not found.";
                return RedirectToAction(nameof(DoctorVerification));
            }

            doctor.VerificationStatus = "Approved";
            doctor.VerificationDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Send approval email
            var approvedUser = await _context.Users.FindAsync(doctor.UserID);
            if (approvedUser != null)
            {
                var name = $"{approvedUser.FirstName} {approvedUser.LastName}".Trim();
                _ = _emailService.SendAsync(
                    approvedUser.Email ?? "",
                    name,
                    "Your NABD Registration Has Been Approved ✅",
                    DoctorEmailTemplates.Approved(name));
            }

            TempData["AdminSuccess"] = "Doctor has been approved and notified by email.";
            return RedirectToAction(nameof(DoctorVerification));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDoctor(int doctorId, string? rejectionNote)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
            {
                TempData["AdminError"] = "Doctor not found.";
                return RedirectToAction(nameof(DoctorVerification));
            }

            doctor.VerificationStatus = "Rejected";
            doctor.VerificationDate = DateTime.Now;
            doctor.RejectionNote = string.IsNullOrWhiteSpace(rejectionNote) ? null : rejectionNote.Trim();
            await _context.SaveChangesAsync();

            // Send rejection email
            var rejectedUser = await _context.Users.FindAsync(doctor.UserID);
            if (rejectedUser != null)
            {
                var name = $"{rejectedUser.FirstName} {rejectedUser.LastName}".Trim();
                _ = _emailService.SendAsync(
                    rejectedUser.Email ?? "",
                    name,
                    "Update on Your NABD Registration Application",
                    DoctorEmailTemplates.Rejected(name, doctor.RejectionNote));
            }

            TempData["AdminSuccess"] = "Doctor registration has been rejected and the doctor notified by email.";
            return RedirectToAction(nameof(DoctorVerification));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["AdminError"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["AdminError"] = "Admin accounts cannot be deleted.";
                return RedirectToAction(nameof(Users));
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserID == userId);
            if (doctor != null)
                await CascadeDeleteDoctorAsync(doctor.DoctorID);

            // Remove chat messages sent/received by this user
            var chatMsgs = _context.ChatMessages
                .Where(m => m.SenderUserId == userId || m.ReceiverUserId == userId);
            _context.ChatMessages.RemoveRange(chatMsgs);
            await _context.SaveChangesAsync();

            await _userManager.DeleteAsync(user);

            TempData["AdminSuccess"] = "User account and all associated data have been permanently deleted.";
            return RedirectToAction(nameof(Users));
        }

        // ─── Doctor Management ────────────────────────────────────────────────

        public async Task<IActionResult> Doctors(string search = "", string status = "")
        {
            ViewData["Title"] = "Doctor Management";
            ViewData["ActivePage"] = "Doctors";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
                .OrderByDescending(d => d.User.CreatedDate)
                .ToListAsync();

            var patientCounts = await _context.PatientDoctors
                .GroupBy(pd => pd.DoctorID)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            var apptCounts = await _context.Appointments
                .GroupBy(a => a.DoctorID)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            var rows = doctors.Select(d => new AdminDoctorRow
            {
                DoctorId = d.DoctorID,
                UserId = d.UserID ?? "",
                FullName = $"{d.User?.FirstName} {d.User?.LastName}".Trim(),
                Email = string.IsNullOrWhiteSpace(d.User?.OriginalEmail) ? (d.User?.Email ?? "") : d.User.OriginalEmail,
                OriginalEmail = d.User?.OriginalEmail ?? "",
                Specialization = d.Specialization ?? "",
                VerificationStatus = d.VerificationStatus ?? "Pending",
                IsActive = d.User?.IsActive ?? false,
                IsBanned = d.User?.IsBanned ?? false,
                RegisteredDate = d.User?.CreatedDate ?? DateTime.MinValue,
                PatientCount = patientCounts.FirstOrDefault(x => x.Key == d.DoctorID)?.Count ?? 0,
                AppointmentCount = apptCounts.FirstOrDefault(x => x.Key == d.DoctorID)?.Count ?? 0,
                ClinicNames = d.ClinicDoctors?.Select(cd => cd.Clinic?.Name ?? "").Where(n => n != "").ToList() ?? new()
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLowerInvariant();
                rows = rows.Where(r =>
                    r.FullName.ToLowerInvariant().Contains(q) ||
                    r.Email.ToLowerInvariant().Contains(q) ||
                    r.Specialization.ToLowerInvariant().Contains(q)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                rows = status switch
                {
                    "Approved"  => rows.Where(r => r.VerificationStatus == "Approved").ToList(),
                    "Pending"   => rows.Where(r => r.VerificationStatus == "Pending").ToList(),
                    "Rejected"  => rows.Where(r => r.VerificationStatus == "Rejected").ToList(),
                    "Banned"    => rows.Where(r => r.IsBanned).ToList(),
                    "Inactive"  => rows.Where(r => !r.IsActive).ToList(),
                    _           => rows
                };
            }

            var all = await _context.Doctors.Include(d => d.User).ToListAsync();
            var vm = new AdminDoctorListViewModel
            {
                Doctors = rows,
                SearchQuery = search,
                StatusFilter = status,
                TotalCount = all.Count,
                ApprovedCount = all.Count(d => d.VerificationStatus == "Approved"),
                PendingCount = all.Count(d => d.VerificationStatus == "Pending" || string.IsNullOrEmpty(d.VerificationStatus)),
                RejectedCount = all.Count(d => d.VerificationStatus == "Rejected"),
                BannedCount = all.Count(d => d.User != null && d.User.IsBanned)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DoctorDetail(int id)
        {
            ViewData["Title"] = "Doctor Profile";
            ViewData["ActivePage"] = "DoctorDetail";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
                .Include(d => d.AssistantDoctors).ThenInclude(ad => ad.Assistant).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(d => d.DoctorID == id);

            if (doctor == null)
            {
                TempData["AdminError"] = "Doctor not found.";
                return RedirectToAction(nameof(Doctors));
            }

            var patientDoctors = await _context.PatientDoctors
                .Where(pd => pd.DoctorID == id)
                .Include(pd => pd.Patient).ThenInclude(p => p.User)
                .OrderByDescending(pd => pd.RequestDate)
                .ToListAsync();

            var recentAppts = await _context.Appointments
                .Where(a => a.DoctorID == id)
                .Include(a => a.Patient).ThenInclude(p => p != null ? p.User : null)
                .Include(a => a.Clinic)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            var prescCount   = await _context.Prescriptions.CountAsync(p => p.DoctorID == id);
            var notesCount   = await _context.Notes.CountAsync(n => n.DoctorID == id);
            var labTestCount = await _context.LabTests.CountAsync(l => l.DoctorID == id);

            var displayEmail = string.IsNullOrWhiteSpace(doctor.User?.OriginalEmail)
                ? (doctor.User?.Email ?? "")
                : doctor.User.OriginalEmail;

            var vm = new AdminDoctorDetailViewModel
            {
                DoctorId = doctor.DoctorID,
                UserId = doctor.UserID ?? "",
                FullName = $"{doctor.User?.FirstName} {doctor.User?.LastName}".Trim(),
                Email = displayEmail,
                OriginalEmail = doctor.User?.OriginalEmail ?? "",
                PhoneNumber = doctor.User?.PhoneNumber ?? "",
                Specialization = doctor.Specialization ?? "",
                LicenseNumber = doctor.LicenseNumber ?? "",
                LicenseImagePath = doctor.LicenseImagePath ?? "",
                VerificationStatus = doctor.VerificationStatus ?? "Pending",
                VerificationDate = doctor.VerificationDate,
                RejectionNote = doctor.RejectionNote,
                Address = doctor.Address ?? "",
                RegisteredDate = doctor.User?.CreatedDate ?? DateTime.MinValue,
                IsActive = doctor.User?.IsActive ?? false,
                IsBanned = doctor.User?.IsBanned ?? false,
                LockoutEnd = doctor.User?.LockoutEnd,
                TotalPatients = patientDoctors.Count,
                TotalAppointments = await _context.Appointments.CountAsync(a => a.DoctorID == id),
                TotalPrescriptions = prescCount,
                TotalNotes = notesCount,
                TotalLabTests = labTestCount,
                ClinicNames = doctor.ClinicDoctors?.Select(cd => cd.Clinic?.Name ?? "").Where(n => n != "").ToList() ?? new(),
                AssistantNames = doctor.AssistantDoctors?
                    .Select(ad => $"{ad.Assistant?.User?.FirstName} {ad.Assistant?.User?.LastName}".Trim())
                    .Where(n => n != "").ToList() ?? new(),
                Patients = patientDoctors.Select(pd => new AdminDoctorPatientRow
                {
                    PatientId = pd.PatientID,
                    FullName = $"{pd.Patient?.User?.FirstName} {pd.Patient?.User?.LastName}".Trim(),
                    Email = pd.Patient?.User?.Email ?? "",
                    Status = pd.Status ?? "",
                    JoinDate = pd.RequestDate
                }).ToList(),
                RecentAppointments = recentAppts.Select(a => new AdminDoctorAppointmentRow
                {
                    AppointmentId = a.AppointmentID,
                    PatientName = a.Patient != null
                        ? $"{a.Patient.User?.FirstName} {a.Patient.User?.LastName}".Trim()
                        : "Unbooked",
                    Date = a.Date,
                    Time = a.Time,
                    IsBooked = a.isBooked,
                    ClinicName = a.Clinic?.Name ?? ""
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanDoctor(int doctorId, string? banReason)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorID == doctorId);
            if (doctor?.User == null)
            {
                TempData["AdminError"] = "Doctor not found.";
                return RedirectToAction(nameof(Doctors));
            }

            doctor.User.IsBanned = true;
            doctor.User.IsActive = false;
            doctor.User.LockoutEnabled = true;
            doctor.User.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(doctor.User);

            TempData["AdminSuccess"] = $"Dr. {doctor.User.FirstName} {doctor.User.LastName} has been banned.";
            return RedirectToAction(nameof(DoctorDetail), new { id = doctorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanDoctor(int doctorId)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorID == doctorId);
            if (doctor?.User == null)
            {
                TempData["AdminError"] = "Doctor not found.";
                return RedirectToAction(nameof(Doctors));
            }

            doctor.User.IsBanned = false;
            doctor.User.IsActive = true;
            doctor.User.LockoutEnd = null;
            await _userManager.UpdateAsync(doctor.User);

            TempData["AdminSuccess"] = $"Dr. {doctor.User.FirstName} {doctor.User.LastName} has been unbanned.";
            return RedirectToAction(nameof(DoctorDetail), new { id = doctorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDeleteDoctor(int doctorId)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorID == doctorId);
            if (doctor == null)
            {
                TempData["AdminError"] = "Doctor not found.";
                return RedirectToAction(nameof(Doctors));
            }

            var user = doctor.User;
            var userId = doctor.UserID;

            await CascadeDeleteDoctorAsync(doctorId);

            if (user != null)
            {
                var chatMsgs = _context.ChatMessages
                    .Where(m => m.SenderUserId == userId || m.ReceiverUserId == userId);
                _context.ChatMessages.RemoveRange(chatMsgs);
                await _context.SaveChangesAsync();
                await _userManager.DeleteAsync(user);
            }

            TempData["AdminSuccess"] = "Doctor and all associated data have been permanently deleted.";
            return RedirectToAction(nameof(Doctors));
        }

        private async Task CascadeDeleteDoctorAsync(int doctorId)
        {
            // 1. Bookings referencing this doctor's appointments
            var apptIds = await _context.Appointments
                .Where(a => a.DoctorID == doctorId)
                .Select(a => a.AppointmentID)
                .ToListAsync();
            var bookings = _context.Bookings.Where(b => apptIds.Contains(b.AppointmentID) || b.DoctorID == doctorId);
            _context.Bookings.RemoveRange(bookings);
            await _context.SaveChangesAsync();

            // 2. Appointments
            var appointments = _context.Appointments.Where(a => a.DoctorID == doctorId);
            _context.Appointments.RemoveRange(appointments);
            await _context.SaveChangesAsync();

            // 3. Notes
            _context.Notes.RemoveRange(_context.Notes.Where(n => n.DoctorID == doctorId));

            // 4. PrescriptionItems -> Prescriptions
            var prescIds = await _context.Prescriptions
                .Where(p => p.DoctorID == doctorId)
                .Select(p => p.PrescriptionID)
                .ToListAsync();
            _context.PrescriptionItems.RemoveRange(_context.PrescriptionItems.Where(pi => prescIds.Contains(pi.PrescriptionID)));
            _context.Prescriptions.RemoveRange(_context.Prescriptions.Where(p => p.DoctorID == doctorId));

            // 5. PatientDoctor links
            _context.PatientDoctors.RemoveRange(_context.PatientDoctors.Where(pd => pd.DoctorID == doctorId));

            // 6. AssistantDoctor links
            _context.AssistantDoctors.RemoveRange(_context.AssistantDoctors.Where(ad => ad.DoctorID == doctorId));

            // 7. ClinicDoctor links
            _context.ClinicDoctors.RemoveRange(_context.ClinicDoctors.Where(cd => cd.DoctorID == doctorId));

            // 8. MedicalHistories authored by this doctor
            _context.MedicalHistories.RemoveRange(_context.MedicalHistories.Where(mh => mh.DoctorID == doctorId));

            // 9. LabTests ordered by this doctor
            _context.LabTests.RemoveRange(_context.LabTests.Where(lt => lt.DoctorID == doctorId));

            // 10. Null out UltrasoundImage.DoctorID (nullable FK — preserve patient images)
            var usImages = await _context.UltrasoundImages.Where(ui => ui.DoctorID == doctorId).ToListAsync();
            foreach (var img in usImages) img.DoctorID = null;

            await _context.SaveChangesAsync();

            // 11. Doctor record itself
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor != null) _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignClinic(int doctorId, int clinicId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            var clinic = await _context.Clinics.FindAsync(clinicId);

            if (doctor == null || clinic == null)
            {
                TempData["AdminError"] = "Doctor or Clinic not found.";
                return RedirectToAction(nameof(DoctorVerification));
            }

            var exists = await _context.ClinicDoctors
                .AnyAsync(cd => cd.DoctorID == doctorId && cd.ClinicID == clinicId);

            if (!exists)
            {
                _context.ClinicDoctors.Add(new ClinicDoctor
                {
                    DoctorID = doctorId,
                    ClinicID = clinicId
                });
                await _context.SaveChangesAsync();
                TempData["AdminSuccess"] = $"Doctor assigned to {clinic.Name} successfully.";
            }
            else
            {
                TempData["AdminError"] = "Doctor is already assigned to this clinic.";
            }

            return RedirectToAction(nameof(DoctorVerification));
        }

        [HttpGet]
        public async Task<IActionResult> GetClinics()
        {
            var clinics = await _context.Clinics
                .Select(c => new { c.ClinicID, c.Name })
                .ToListAsync();
            return Json(clinics);
        }

        // ─── Clinic Management ────────────────────────────────────────────────

        public async Task<IActionResult> Clinics()
        {
            ViewData["Title"] = "Clinic Management";
            ViewData["ActivePage"] = "Clinics";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var clinics = await _context.Clinics
                .Include(c => c.ClinicDoctors)
                .Include(c => c.Assistants)
                .ToListAsync();

            var appointmentCounts = await _context.Appointments
                .Where(a => a.isBooked)
                .GroupBy(a => a.ClinicID)
                .Select(g => new { ClinicID = g.Key, Count = g.Count() })
                .ToListAsync();

            var rows = clinics.Select(c => new AdminClinicRow
            {
                ClinicID = c.ClinicID,
                Name = c.Name ?? "",
                Location = c.Location ?? "",
                DoctorCount = c.ClinicDoctors?.Count ?? 0,
                AssistantCount = c.Assistants?.Count ?? 0,
                AppointmentCount = appointmentCounts.FirstOrDefault(x => x.ClinicID == c.ClinicID)?.Count ?? 0
            }).OrderBy(r => r.Name).ToList();

            var vm = new AdminClinicsViewModel
            {
                Clinics = rows,
                TotalClinics = rows.Count,
                TotalDoctorsAssigned = rows.Sum(r => r.DoctorCount),
                TotalAssistantsAssigned = rows.Sum(r => r.AssistantCount),
                TotalAppointments = rows.Sum(r => r.AppointmentCount)
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult AddClinic()
        {
            ViewData["Title"] = "Add Clinic";
            ViewData["ActivePage"] = "Clinics";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";
            return View("ClinicForm", new AdminClinicFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClinic(AdminClinicFormViewModel model)
        {
            ViewData["Title"] = "Add Clinic";
            ViewData["ActivePage"] = "Clinics";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            if (!ModelState.IsValid)
                return View("ClinicForm", model);

            var clinic = new Clinic { Name = model.Name, Location = model.Location };
            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] = $"Clinic \"{model.Name}\" has been created successfully.";
            return RedirectToAction(nameof(Clinics));
        }

        [HttpGet]
        public async Task<IActionResult> EditClinic(int id)
        {
            ViewData["Title"] = "Edit Clinic";
            ViewData["ActivePage"] = "Clinics";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null)
            {
                TempData["AdminError"] = "Clinic not found.";
                return RedirectToAction(nameof(Clinics));
            }

            return View("ClinicForm", new AdminClinicFormViewModel
            {
                ClinicID = clinic.ClinicID,
                Name = clinic.Name ?? "",
                Location = clinic.Location ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClinic(AdminClinicFormViewModel model)
        {
            ViewData["Title"] = "Edit Clinic";
            ViewData["ActivePage"] = "Clinics";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            if (!ModelState.IsValid)
                return View("ClinicForm", model);

            var clinic = await _context.Clinics.FindAsync(model.ClinicID);
            if (clinic == null)
            {
                TempData["AdminError"] = "Clinic not found.";
                return RedirectToAction(nameof(Clinics));
            }

            clinic.Name = model.Name;
            clinic.Location = model.Location;
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] = $"Clinic \"{model.Name}\" has been updated successfully.";
            return RedirectToAction(nameof(Clinics));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClinic(int clinicId)
        {
            var clinic = await _context.Clinics.FindAsync(clinicId);
            if (clinic == null)
            {
                TempData["AdminError"] = "Clinic not found.";
                return RedirectToAction(nameof(Clinics));
            }

            _context.Clinics.Remove(clinic);
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] = $"Clinic \"{clinic.Name}\" has been deleted.";
            return RedirectToAction(nameof(Clinics));
        }

        // ─── Analytics ────────────────────────────────────────────────────────

        public async Task<IActionResult> Analytics()
        {
            ViewData["Title"] = "Analytics";
            ViewData["ActivePage"] = "Analytics";
            ViewData["AdminName"] = User.Identity?.Name ?? "Admin";

            var today = DateTime.Today;

            // Patients registered per month (last 6 months)
            var patientsPerMonth = new List<int>();
            var patientMonthLabels = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var ms = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var me = ms.AddMonths(1);
                var count = await _context.Patients
                    .Include(p => p.User)
                    .Where(p => p.User.CreatedDate >= ms && p.User.CreatedDate < me)
                    .CountAsync();
                patientsPerMonth.Add(count);
                patientMonthLabels.Add(ms.ToString("MMM yyyy"));
            }

            // Appointments per clinic
            var clinicApptGroups = await _context.Appointments
                .Where(a => a.isBooked)
                .GroupBy(a => a.ClinicID)
                .Select(g => new { ClinicID = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var clinicIdList = clinicApptGroups.Select(x => x.ClinicID).ToList();
            var clinicLookup = await _context.Clinics
                .Where(c => clinicIdList.Contains(c.ClinicID))
                .ToListAsync();

            var clinicNames = new List<string>();
            var appointmentsPerClinic = new List<int>();
            foreach (var g in clinicApptGroups)
            {
                var c = clinicLookup.FirstOrDefault(x => x.ClinicID == g.ClinicID);
                clinicNames.Add(c?.Name ?? $"Clinic #{g.ClinicID}");
                appointmentsPerClinic.Add(g.Count);
            }

            // Most active doctors
            var doctorApptGroups = await _context.Appointments
                .Where(a => a.isBooked)
                .GroupBy(a => a.DoctorID)
                .Select(g => new { DoctorID = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(8)
                .ToListAsync();

            var doctorIdList = doctorApptGroups.Select(x => x.DoctorID).ToList();
            var doctorLookup = await _context.Doctors
                .Include(d => d.User)
                .Where(d => doctorIdList.Contains(d.DoctorID))
                .ToListAsync();

            var doctorNames = new List<string>();
            var doctorCounts = new List<int>();
            foreach (var g in doctorApptGroups)
            {
                var d = doctorLookup.FirstOrDefault(x => x.DoctorID == g.DoctorID);
                var name = d != null
                    ? $"Dr. {d.User?.FirstName} {d.User?.LastName}".Trim()
                    : $"Doctor #{g.DoctorID}";
                doctorNames.Add(name);
                doctorCounts.Add(g.Count);
            }

            // Alert frequency by type
            var alertByType = await _context.Alerts
                .GroupBy(a => a.AlertType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            var alertTypes = alertByType.Select(x => x.Type ?? "Unknown").ToList();
            var alertCounts = alertByType.Select(x => x.Count).ToList();

            // System growth (last 12 months — cumulative user registrations)
            var systemGrowth = new List<int>();
            var growthMonthLabels = new List<string>();
            for (int i = 11; i >= 0; i--)
            {
                var ms = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var me = ms.AddMonths(1);
                var count = await _context.Users
                    .Where(u => u.CreatedDate >= ms && u.CreatedDate < me)
                    .CountAsync();
                systemGrowth.Add(count);
                growthMonthLabels.Add(ms.ToString("MMM yy"));
            }

            var vm = new AdminAnalyticsViewModel
            {
                PatientsPerMonth = patientsPerMonth,
                PatientMonthLabels = patientMonthLabels,
                ClinicNames = clinicNames,
                AppointmentsPerClinic = appointmentsPerClinic,
                DoctorNames = doctorNames,
                DoctorAppointmentCounts = doctorCounts,
                AlertTypes = alertTypes,
                AlertCounts = alertCounts,
                SystemGrowth = systemGrowth,
                GrowthMonthLabels = growthMonthLabels,
                TotalAlerts = await _context.Alerts.CountAsync(),
                TotalAppointments = await _context.Appointments.Where(a => a.isBooked).CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalClinics = await _context.Clinics.CountAsync()
            };

            return View(vm);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static AdminDoctorVerificationRow MapDoctorRow(Doctor d) => new()
        {
            DoctorId = d.DoctorID,
            UserId = d.UserID ?? "",
            FullName = $"{d.User?.FirstName} {d.User?.LastName}".Trim(),
            Email = d.User?.Email ?? "",
            Specialization = d.Specialization ?? "",
            LicenseNumber = d.LicenseNumber ?? "",
            LicenseImagePath = d.LicenseImagePath ?? "",
            VerificationStatus = d.VerificationStatus ?? "Pending",
            VerificationDate = d.VerificationDate,
            RejectionNote = d.RejectionNote,
            CreatedDate = d.User?.CreatedDate ?? DateTime.MinValue,
            Address = d.Address ?? "",
            ClinicNames = d.ClinicDoctors?.Select(cd => cd.Clinic?.Name ?? "").ToList() ?? new()
        };

        private static List<AdminActivityLog> BuildRecentActivity() =>
        [
            new() { Action = "New patient registered",      UserName = "Sarah Ahmed",      Role = "Patient",   Timestamp = DateTime.Now.AddMinutes(-5),  Icon = "fa-user-plus",     Color = "green"  },
            new() { Action = "Doctor account approved",     UserName = "Dr. Ahmed Hassan", Role = "Admin",     Timestamp = DateTime.Now.AddMinutes(-18), Icon = "fa-user-check",    Color = "teal"   },
            new() { Action = "Appointment booked",          UserName = "Fatima Ali",       Role = "Patient",   Timestamp = DateTime.Now.AddMinutes(-34), Icon = "fa-calendar-plus", Color = "blue"   },
            new() { Action = "Clinic invitation sent",      UserName = "Dr. Mona Ibrahim", Role = "Doctor",    Timestamp = DateTime.Now.AddHours(-1),    Icon = "fa-envelope",      Color = "amber"  },
            new() { Action = "Patient account deactivated", UserName = "Admin",            Role = "Admin",     Timestamp = DateTime.Now.AddHours(-2),    Icon = "fa-user-slash",    Color = "red"    },
            new() { Action = "New doctor registered",       UserName = "Dr. Karim Mostafa",Role = "Doctor",    Timestamp = DateTime.Now.AddHours(-3),    Icon = "fa-stethoscope",   Color = "purple" },
        ];
    }
}

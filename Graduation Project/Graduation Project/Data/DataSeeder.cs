using Graduation_Project.Models;
using Graduation_Project.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Data
{
    /// <summary>
    /// Seeds a complete, demo-ready NABD database covering every role and feature.
    ///
    /// Two kinds of data are seeded:
    ///
    ///  • One-time data (users, clinics, patients, labs, prescriptions, community, …)
    ///    is written only when its table is empty, so restarting the app never duplicates it.
    ///
    ///  • Rolling data (appointments + bookings, medication logs) is regenerated around
    ///    <see cref="DateTime.Today"/> whenever the existing window has gone stale. This is
    ///    what makes the app testable on any day without hand-creating appointments: there
    ///    are always past, today and upcoming appointments, plus free slots to book into.
    ///
    /// All seeded accounts use the password <c>Nabd@123</c>.
    /// </summary>
    public static class DataSeeder
    {
        private const string SeedPassword = "Nabd@123";

        /// <summary>Every slot a doctor offers on a given day. Unbooked ones are bookable by patients.</summary>
        private static readonly TimeSpan[] SlotTimes =
        {
            new(9, 0, 0),  new(9, 30, 0),  new(10, 0, 0), new(10, 30, 0),
            new(11, 0, 0), new(11, 30, 0), new(12, 0, 0),
            new(14, 0, 0), new(14, 30, 0), new(15, 0, 0), new(15, 30, 0), new(16, 0, 0)
        };

        private const int PastWindowDays = 14;
        private const int FutureWindowDays = 14;

        public static async Task SeedAsync(AppDbContext context, IChatMessageCrypto? chatCrypto = null)
        {
            await context.Database.MigrateAsync();

            var cast = new Cast();

            await SeedRolesAsync(context);
            await SeedUsersAsync(context, cast);
            await SeedUserRolesAsync(context, cast);
            await SeedDoctorsAsync(context, cast);
            await SeedClinicsAsync(context, cast);
            await SeedAssistantsAsync(context, cast);
            await SeedPatientsAsync(context, cast);
            await SeedPregnancyRecordsAsync(context, cast);
            await SeedPatientDoctorsAsync(context, cast);
            await SeedAIModelsAsync(context, cast);
            await SeedTestReportsAndLabTestsAsync(context, cast);
            await SeedUltrasoundImagesAsync(context, cast);
            await SeedPrescriptionsAsync(context, cast);
            await SeedMedicationsAsync(context, cast);
            await SeedPatientDrugsAsync(context, cast);
            await SeedVitalsAsync(context, cast);
            await SeedMedicalHistoryAsync(context, cast);
            await SeedNotesAsync(context, cast);
            await SeedAlertsAsync(context, cast);
            await SeedNotificationsAsync(context, cast);
            await SeedPlacesAsync(context, cast);
            await SeedCommunityAsync(context, cast);
            await SeedChatMessagesAsync(context, cast, chatCrypto);
            await SeedChatbotMessagesAsync(context, cast);
            await SeedInvitationsAsync(context, cast);

            // Rolling — always kept around today.
            await SeedAppointmentWindowAsync(context, cast);
            await SeedMedicationLogsAsync(context);
        }

        // ============================================================
        // 1. ROLES
        // ============================================================
        private static async Task SeedRolesAsync(AppDbContext context)
        {
            if (context.Roles.Any())
                return;

            context.Roles.AddRange(
                new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Name = "Doctor", NormalizedName = "DOCTOR" },
                new IdentityRole { Name = "Patient", NormalizedName = "PATIENT" },
                new IdentityRole { Name = "Assistant", NormalizedName = "ASSISTANT" },
                new IdentityRole { Name = "Lab", NormalizedName = "LAB" }
            );
            await context.SaveChangesAsync();
        }

        // ============================================================
        // 2. USERS
        // ============================================================
        // Seeded row-by-row rather than all-or-nothing, so a database that was seeded by an
        // older version of this file still gains any accounts it is missing (and the lookups
        // below can never come up empty).
        private static async Task SeedUsersAsync(AppDbContext context, Cast cast)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var hash = hasher.HashPassword(new ApplicationUser(), SeedPassword);

            foreach (var (_, first, last, email, phone, dob) in UserDefinitions)
            {
                if (context.Users.Any(u => u.Email == email))
                    continue;

                context.Users.Add(new ApplicationUser
                {
                    FirstName = first,
                    LastName = last,
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    PasswordHash = hash,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PhoneNumber = phone,
                    DateOfBirth = dob,
                    IsActive = true,
                    IsBanned = false,
                    CreatedDate = DateTime.Today.AddDays(-240),
                    LockoutEnabled = true
                });
            }

            await context.SaveChangesAsync();

            foreach (var (key, _, _, email, _, _) in UserDefinitions)
            {
                var user = context.Users.FirstOrDefault(u => u.Email == email);
                if (user != null)
                    cast.Users[key] = user;
            }
        }

        /// <summary>key, first, last, email, phone, date of birth.</summary>
        private static readonly (string Key, string First, string Last, string Email, string Phone, DateTime Dob)[] UserDefinitions =
        {
            // Admin + Lab
            ("admin", "System",  "Admin",   "admin@nabd.com",            "01000000000", new DateTime(1990, 1, 1)),
            ("lab",   "Central", "Lab",     "lab@nabd.com",              "01000000001", new DateTime(1989, 5, 9)),
            // Doctors
            ("ahmed", "Ahmed",   "Hassan",  "ahmed.hassan@nabd.com",     "01001234567", new DateTime(1975, 3, 12)),
            ("mona",  "Mona",    "Ibrahim", "mona.ibrahim@nabd.com",     "01009876543", new DateTime(1980, 7, 22)),
            ("karim", "Karim",   "Mostafa", "karim.mostafa@nabd.com",    "01112233445", new DateTime(1978, 11, 5)),
            ("nadia", "Nadia",   "Salem",   "nadia.salem@nabd.com",      "01223344556", new DateTime(1982, 4, 18)),
            ("omar",  "Omar",    "Fathy",   "omar.fathy@nabd.com",       "01334455667", new DateTime(1976, 9, 30)),
            ("sami",  "Sami",    "Gaber",   "sami.gaber@nabd.com",       "01445566778", new DateTime(1984, 2, 14)),
            // Patients
            ("sarah",   "Sarah",   "Ahmed",    "sarah.ahmed@nabd.com",     "01501234567", new DateTime(1995, 6, 14)),
            ("fatima",  "Fatima",  "Ali",      "fatima.ali@nabd.com",      "01509876543", new DateTime(1993, 8, 25)),
            ("yasmine", "Yasmine", "Mahmoud",  "yasmine.mahmoud@nabd.com", "01512233445", new DateTime(1997, 2, 10)),
            ("hana",    "Hana",    "Khaled",   "hana.khaled@nabd.com",     "01523344556", new DateTime(1991, 12, 3)),
            ("reem",    "Reem",    "Nasser",   "reem.nasser@nabd.com",     "01534455667", new DateTime(1996, 5, 20)),
            ("nour",    "Nour",    "Adel",     "nour.adel@nabd.com",       "01545566778", new DateTime(1998, 9, 2)),
            // Assistants
            ("layla", "Layla", "Omar",    "layla.omar@nabd.com",    "01601234567", new DateTime(1990, 1, 8)),
            ("dina",  "Dina",  "Samir",   "dina.samir@nabd.com",    "01609876543", new DateTime(1992, 3, 17)),
            ("noura", "Noura", "Youssef", "noura.youssef@nabd.com", "01612233445", new DateTime(1988, 7, 29)),
            ("amira", "Amira", "Tarek",   "amira.tarek@nabd.com",   "01622334456", new DateTime(1991, 5, 14)),
            ("heba",  "Heba",  "Adel",    "heba.adel@nabd.com",     "01632445567", new DateTime(1994, 10, 2))
        };

        private static readonly string[] DoctorKeys = { "ahmed", "mona", "karim", "nadia", "omar", "sami" };
        private static readonly string[] PatientKeys = { "sarah", "fatima", "yasmine", "hana", "reem", "nour" };
        private static readonly string[] AssistantKeys = { "layla", "dina", "noura", "amira", "heba" };

        private static async Task SeedUserRolesAsync(AppDbContext context, Cast cast)
        {
            var roles = context.Roles.ToDictionary(r => r.Name!, r => r.Id);

            void Assign(string userKey, string role)
            {
                if (!cast.Users.TryGetValue(userKey, out var user) || !roles.TryGetValue(role, out var roleId))
                    return;

                if (context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == roleId))
                    return;

                context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = roleId });
            }

            Assign("admin", "Admin");
            Assign("lab", "Lab");
            foreach (var key in DoctorKeys) Assign(key, "Doctor");
            foreach (var key in PatientKeys) Assign(key, "Patient");
            foreach (var key in AssistantKeys) Assign(key, "Assistant");

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 3. DOCTORS
        // ============================================================
        /// <summary>key, specialization, licence number, verification status, address, rejection note.</summary>
        private static readonly (string Key, string Specialization, string License, string Status, string Address, string? RejectionNote)[] DoctorDefinitions =
        {
            ("ahmed", "Obstetrics & Gynecology", "MD-OBG-001", "Approved", "15 Tahrir St, Cairo", null),
            ("mona",  "Maternal-Fetal Medicine", "MD-MFM-002", "Approved", "22 Nasr City, Cairo", null),
            ("karim", "Obstetrics & Gynecology", "MD-OBG-003", "Approved", "7 Corniche, Alexandria", null),
            ("nadia", "Endocrinology",           "MD-END-004", "Approved", "30 Heliopolis, Cairo", null),
            // Awaiting admin review — exercises the "Under Review" gate and the admin approval queue.
            ("omar",  "Internal Medicine",       "MD-INT-005", "Pending",  "5 Dokki, Giza", null),
            // Rejected — exercises the rejection-note UI.
            ("sami",  "Obstetrics & Gynecology", "MD-OBG-006", "Rejected", "40 Smouha, Alexandria",
                "Licence image is unreadable. Please re-upload a clear scan of a valid medical licence.")
        };

        private static async Task SeedDoctorsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;
            var index = 0;

            foreach (var d in DoctorDefinitions)
            {
                index++;

                if (!cast.Users.TryGetValue(d.Key, out var user))
                    continue;

                if (context.Doctors.Any(x => x.UserID == user.Id))
                    continue;

                context.Doctors.Add(new Doctor
                {
                    UserID = user.Id,
                    Specialization = d.Specialization,
                    LicenseNumber = d.License,
                    LicenseImagePath = $"/uploads/licenses/lic{index}.jpg",
                    VerificationStatus = d.Status,
                    VerificationDate = d.Status == "Pending" ? null : today.AddDays(-200 + index * 2),
                    RejectionNote = d.RejectionNote,
                    Address = d.Address
                });
            }

            await context.SaveChangesAsync();

            foreach (var key in DoctorKeys)
            {
                if (!cast.Users.TryGetValue(key, out var user))
                    continue;

                var doctor = context.Doctors.FirstOrDefault(d => d.UserID == user.Id);
                if (doctor != null)
                    cast.Doctors[key] = doctor;
            }
        }

        // ============================================================
        // 4. CLINICS (+ owner, + clinic/doctor membership)
        // ============================================================
        private static readonly (string Key, string Name, string Location, string OwnerKey)[] ClinicDefinitions =
        {
            ("central",   "MamaCare Central",      "15 Tahrir St, Cairo",    "ahmed"),
            ("helio",     "MamaCare Heliopolis",   "30 Heliopolis, Cairo",   "ahmed"),
            ("fetal",     "Fetal Health Clinic",   "22 Nasr City, Cairo",    "mona"),
            ("alex",      "Alexandria OBG Center", "7 Corniche, Alexandria", "karim"),
            ("endocrine", "Endocrine & Maternal",  "30 Heliopolis, Cairo",   "nadia"),
            ("dokki",     "Dokki General Clinic",  "5 Dokki, Giza",          "omar")
        };

        /// <summary>Which doctors practise at which clinic (the owner is always a member).</summary>
        private static readonly (string ClinicKey, string DoctorKey)[] ClinicMemberships =
        {
            ("central", "ahmed"), ("central", "mona"),
            ("helio", "ahmed"),
            ("fetal", "mona"), ("fetal", "karim"),
            ("alex", "karim"), ("alex", "nadia"),
            ("endocrine", "nadia"),
            ("dokki", "omar")
        };

        private static async Task SeedClinicsAsync(AppDbContext context, Cast cast)
        {
            foreach (var (_, name, location, ownerKey) in ClinicDefinitions)
            {
                if (!cast.Doctors.TryGetValue(ownerKey, out var owner))
                    continue;

                if (context.Clinics.Any(c => c.Name == name))
                    continue;

                context.Clinics.Add(new Clinic
                {
                    Name = name,
                    Location = location,
                    OwnerDoctorID = owner.DoctorID
                });
            }

            await context.SaveChangesAsync();

            foreach (var (key, name, _, _) in ClinicDefinitions)
            {
                var clinic = context.Clinics.FirstOrDefault(c => c.Name == name);
                if (clinic != null)
                    cast.Clinics[key] = clinic;
            }

            foreach (var (clinicKey, doctorKey) in ClinicMemberships)
            {
                if (!cast.Clinics.TryGetValue(clinicKey, out var clinic) || !cast.Doctors.TryGetValue(doctorKey, out var doctor))
                    continue;

                if (context.ClinicDoctors.Any(cd => cd.ClinicID == clinic.ClinicID && cd.DoctorID == doctor.DoctorID))
                    continue;

                context.ClinicDoctors.Add(new ClinicDoctor
                {
                    ClinicID = clinic.ClinicID,
                    DoctorID = doctor.DoctorID
                });
            }

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 5. ASSISTANTS (+ the doctors each one is scoped to)
        // ============================================================
        /// <summary>Which clinic each assistant works at.</summary>
        private static readonly (string AssistantKey, string ClinicKey)[] AssistantPostings =
        {
            ("layla", "central"),
            ("dina", "fetal"),
            ("noura", "alex"),
            ("amira", "central"),
            ("heba", "dokki")
        };

        /// <summary>Which doctors each assistant is scoped to. Amira is absent on purpose.</summary>
        private static readonly (string AssistantKey, string DoctorKey)[] AssistantScopes =
        {
            // Layla covers both doctors at Central.
            ("layla", "ahmed"), ("layla", "mona"),
            // Dina covers both doctors at Fetal Health.
            ("dina", "mona"), ("dina", "karim"),
            // Noura covers only Karim at Alexandria — Nadia's schedule stays hidden from her.
            ("noura", "karim"),
            // Heba covers Omar, who is pending verification and has no approved patients (empty states).
            ("heba", "omar")
            // Amira has NO doctor links — her dashboard falls back to every doctor at Central.
        };

        private static async Task SeedAssistantsAsync(AppDbContext context, Cast cast)
        {
            foreach (var (assistantKey, clinicKey) in AssistantPostings)
            {
                if (!cast.Users.TryGetValue(assistantKey, out var user) || !cast.Clinics.TryGetValue(clinicKey, out var clinic))
                    continue;

                if (context.Assistants.Any(a => a.UserID == user.Id))
                    continue;

                context.Assistants.Add(new Assistant { UserID = user.Id, ClinicID = clinic.ClinicID });
            }

            await context.SaveChangesAsync();

            foreach (var key in AssistantKeys)
            {
                if (!cast.Users.TryGetValue(key, out var user))
                    continue;

                var assistant = context.Assistants.FirstOrDefault(a => a.UserID == user.Id);
                if (assistant != null)
                    cast.Assistants[key] = assistant;
            }

            foreach (var (assistantKey, doctorKey) in AssistantScopes)
            {
                if (!cast.Assistants.TryGetValue(assistantKey, out var assistant) || !cast.Doctors.TryGetValue(doctorKey, out var doctor))
                    continue;

                if (context.AssistantDoctors.Any(ad => ad.AssistantID == assistant.AssistantID && ad.DoctorID == doctor.DoctorID))
                    continue;

                context.AssistantDoctors.Add(new AssistantDoctor
                {
                    AssistantID = assistant.AssistantID,
                    DoctorID = doctor.DoctorID
                });
            }

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 6. PATIENTS
        // ============================================================
        /// <summary>key, gestational week (relative to today), address, first pregnancy, previous, abortions, births, weight, height, BP issue, DgState, RiskState.</summary>
        private static readonly (string Key, int Week, string Address, bool First, int Previous, int Abortions, int Births,
            double Weight, double Height, bool BpIssue, string Dg, string Risk)[] PatientDefinitions =
        {
            ("sarah",   24, "12 Maadi, Cairo",       false, 1, 0, 1, 68.0, 162.0, false, "Stable",   "Low"),
            ("fatima",  32, "5 Zamalek, Cairo",      true,  0, 0, 0, 72.0, 158.0, true,  "Unstable", "High"),
            ("yasmine", 12, "18 New Cairo",          true,  0, 0, 0, 60.0, 165.0, false, "Stable",   "Low"),
            ("hana",    36, "9 Shubra, Cairo",       false, 2, 1, 1, 80.0, 160.0, true,  "Unstable", "High"),
            ("reem",    20, "3 Mohandessin, Giza",   false, 1, 0, 1, 65.0, 170.0, false, "Stable",   "Moderate"),
            // Brand new patient: no doctor approved yet — exercises the "find a doctor" flow and empty dashboards.
            ("nour",     8, "44 Sheikh Zayed, Giza", true,  0, 0, 0, 58.0, 168.0, false, "Stable",   "Low")
        };

        private static async Task SeedPatientsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;

            foreach (var p in PatientDefinitions)
            {
                if (!cast.Users.TryGetValue(p.Key, out var user))
                    continue;

                if (context.Patients.Any(x => x.UserID == user.Id))
                    continue;

                var pregnancyStart = today.AddDays(-7 * p.Week);

                context.Patients.Add(new Patient
                {
                    UserID = user.Id,
                    Address = p.Address,
                    DateOfPregnancy = pregnancyStart,
                    LastPregnancyStartedAt = pregnancyStart,
                    PregnancyCount = p.Previous + 1,
                    GestationalWeeks = p.Week,
                    IsFirstPregnancy = p.First,
                    PreviousPregnancies = p.Previous,
                    Abortions = p.Abortions,
                    Births = p.Births,
                    WeightKg = p.Weight,
                    HeightCm = p.Height,
                    BloodPressureIssue = p.BpIssue,
                    Smoking = false,
                    AlcoholUse = false,
                    DgState = p.Dg,
                    RiskState = p.Risk
                });
            }

            await context.SaveChangesAsync();

            foreach (var key in PatientKeys)
            {
                if (!cast.Users.TryGetValue(key, out var user))
                    continue;

                var patient = context.Patients.FirstOrDefault(p => p.UserID == user.Id);
                if (patient != null)
                    cast.Patients[key] = patient;
            }
        }

        // Pregnancy weeks are derived from PregnancyRecord.StartDate, so anchoring the record
        // to "today minus N weeks" keeps every patient at a sensible gestational age forever.
        private static async Task SeedPregnancyRecordsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;
            var genders = new Dictionary<string, string>
            {
                ["sarah"] = "Girl",
                ["fatima"] = "Boy",
                ["yasmine"] = "Unknown",
                ["hana"] = "Girl",
                ["reem"] = "Boy",
                ["nour"] = "Unknown"
            };

            foreach (var p in PatientDefinitions)
            {
                if (!cast.Patients.TryGetValue(p.Key, out var patient))
                    continue;

                // Only give a patient a pregnancy record if they have none at all.
                if (context.PregnancyRecords.Any(r => r.PatientID == patient.PatientID))
                    continue;

                context.PregnancyRecords.Add(new PregnancyRecord
                {
                    PatientID = patient.PatientID,
                    StartDate = today.AddDays(-7 * p.Week),
                    EndDate = null,
                    BabyGender = genders[p.Key],
                    CreatedAt = today.AddDays(-7 * p.Week)
                });

                // A completed earlier pregnancy, so the pregnancy-history view has something to show.
                if (p.Key == "hana")
                {
                    var previousStart = today.AddYears(-3);
                    context.PregnancyRecords.Add(new PregnancyRecord
                    {
                        PatientID = patient.PatientID,
                        StartDate = previousStart,
                        EndDate = previousStart.AddDays(276),
                        BabyGender = "Boy",
                        CreatedAt = previousStart
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 7. PATIENT <-> DOCTOR
        // ============================================================
        /// <summary>The approved care team. First entry per patient is their primary doctor.</summary>
        private static readonly (string DoctorKey, string PatientKey, bool IsPrimary)[] ApprovedCareTeam =
        {
            ("ahmed", "sarah", true),
            ("ahmed", "fatima", true),
            ("mona", "yasmine", true),
            ("mona", "fatima", false),
            ("karim", "hana", true),
            ("karim", "reem", false),
            ("nadia", "reem", true)
        };

        /// <summary>Requests that are not yet (or never were) approved: doctor, patient, status, days ago.</summary>
        private static readonly (string DoctorKey, string PatientKey, string Status, int DaysAgo)[] PendingCareRequests =
        {
            // These land in each doctor's "patient requests" inbox.
            ("mona", "sarah", "Pending", 2),
            ("ahmed", "nour", "Pending", 1),
            ("nadia", "hana", "Rejected", 120)
        };

        private static async Task SeedPatientDoctorsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;

            // The composite key is {DoctorID, PatientID}, so a pair may only ever appear once.
            void Link(string doctorKey, string patientKey, string status, bool isPrimary, int requestedDaysAgo, int? respondedDaysAgo)
            {
                if (!cast.Doctors.TryGetValue(doctorKey, out var doctor) || !cast.Patients.TryGetValue(patientKey, out var patient))
                    return;

                if (context.PatientDoctors.Any(pd => pd.DoctorID == doctor.DoctorID && pd.PatientID == patient.PatientID))
                    return;

                context.PatientDoctors.Add(new PatientDoctor
                {
                    DoctorID = doctor.DoctorID,
                    PatientID = patient.PatientID,
                    Status = status,
                    RequestDate = today.AddDays(-requestedDaysAgo),
                    ResponseDate = respondedDaysAgo.HasValue ? today.AddDays(-respondedDaysAgo.Value) : null,
                    IsPrimary = isPrimary
                });
            }

            foreach (var (doctorKey, patientKey, isPrimary) in ApprovedCareTeam)
                Link(doctorKey, patientKey, "Approved", isPrimary, 60, 59);

            foreach (var (doctorKey, patientKey, status, daysAgo) in PendingCareRequests)
                Link(doctorKey, patientKey, status, false, daysAgo, status == "Pending" ? null : daysAgo - 1);

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 8. AI MODELS
        // ============================================================
        private static async Task SeedAIModelsAsync(AppDbContext context, Cast cast)
        {
            if (!context.AIModels.Any())
            {
                var today = DateTime.Today;
                context.AIModels.AddRange(
                    new AIModel { ModelName = "CBC Analyzer v2", ModelType = "CBC", ModelVersion = "2.1.0", ModelFilePath = "/models/cbc_v2.pkl", Accuracy = 96.5, DateTrained = today.AddDays(-380) },
                    new AIModel { ModelName = "Ultrasound Detector v3", ModelType = "Ultrasound", ModelVersion = "3.0.1", ModelFilePath = "/models/us_v3.h5", Accuracy = 94.2, DateTrained = today.AddDays(-340) },
                    new AIModel { ModelName = "Blood Sugar Predictor", ModelType = "BloodSugar", ModelVersion = "1.5.0", ModelFilePath = "/models/bsugar_v1.pkl", Accuracy = 91.8, DateTrained = today.AddDays(-300) },
                    new AIModel { ModelName = "TSH Classifier", ModelType = "TSH", ModelVersion = "1.2.3", ModelFilePath = "/models/tsh_v1.pkl", Accuracy = 93.0, DateTrained = today.AddDays(-260) },
                    new AIModel { ModelName = "Ferritin Level AI", ModelType = "Ferritin", ModelVersion = "1.0.0", ModelFilePath = "/models/ferritin_v1.pkl", Accuracy = 89.7, DateTrained = today.AddDays(-220) }
                );
                await context.SaveChangesAsync();
            }

            foreach (var model in context.AIModels.ToList())
                cast.Models[model.ModelType] = model;
        }

        // ============================================================
        // 9. TEST REPORTS + LAB TESTS (all nine test types, one full panel per patient)
        // ============================================================
        /// <summary>patient, doctor, days ago, overall status, confidence, AI summary, doctor interpretation.</summary>
        private static readonly (string PatientKey, string DoctorKey, int DaysAgo, string Status, double Confidence, string Summary, string Interpretation)[] ReportDefinitions =
        {
            ("sarah",   "ahmed", 6,  "Normal",             96.5, "All blood parameters within normal ranges for the second trimester. Haemoglobin is slightly low — monitor iron.", "Continue iron supplementation; repeat CBC in 4 weeks."),
            ("fatima",  "ahmed", 9,  "Requires Attention", 91.2, "Elevated WBC count and mildly raised TSH. Blood-pressure trend is concerning for pre-eclampsia.",                 "Increase monitoring to bi-weekly; continue Labetalol."),
            ("yasmine", "mona",  12, "Normal",             98.1, "First-trimester panel shows every value within the expected range.",                                              "Patient is progressing well. Routine follow-up."),
            ("hana",    "karim", 4,  "Abnormal",           88.4, "Low haemoglobin and critically low ferritin — iron-deficiency anaemia. Urinalysis shows trace protein.",          "Start IV iron therapy and repeat CBC in 2 weeks."),
            ("reem",    "nadia", 7,  "Requires Attention", 93.7, "HbA1c above the gestational-diabetes target and fasting glucose elevated.",                                       "Adjust dietary plan; monitor blood sugar four times daily.")
        };

        private static async Task SeedTestReportsAndLabTestsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;

            if (!context.TestReports.Any())
            {
                foreach (var r in ReportDefinitions)
                {
                    context.TestReports.Add(new TestReport
                    {
                        PatientID = cast.Patients[r.PatientKey].PatientID,
                        DoctorID = cast.Doctors[r.DoctorKey].DoctorID,
                        ReportDate = today.AddDays(-r.DaysAgo),
                        AnalysisStatus = "Completed",
                        OverallStatus = r.Status,
                        ConfidenceScore = r.Confidence,
                        AISummary = r.Summary,
                        DoctorInterpretation = r.Interpretation,
                        PersonalInfoJson = BuildPersonalInfoJson(cast.Patients[r.PatientKey]),
                        AiResultJson = BuildAiResultJson(r.PatientKey),
                        RiskJson = $"{{\"risk_level\":\"{RiskLevelFor(r.Status)}\",\"confidence\":{r.Confidence / 100:0.00}}}",
                        AlertsJson = BuildAlertsJson(r.Status)
                    });
                }
                await context.SaveChangesAsync();
            }

            if (context.LabTests.Any())
                return;

            // One full nine-test panel per patient, attached to that patient's report.
            foreach (var r in ReportDefinitions)
            {
                var patientId = cast.Patients[r.PatientKey].PatientID;
                var doctorId = cast.Doctors[r.DoctorKey].DoctorID;
                var report = context.TestReports.First(t => t.PatientID == patientId);
                var uploadDate = today.AddDays(-r.DaysAgo);

                foreach (var testType in TestTypes)
                {
                    int? modelId = cast.Models.TryGetValue(testType, out var model) ? model.ModelID : null;

                    context.LabTests.Add(new LabTest
                    {
                        PatientID = patientId,
                        DoctorID = doctorId,
                        ModelID = modelId,
                        ReportID = report.ReportID,
                        UploadDate = uploadDate,
                        ImagePath = $"/uploads/tests/{testType.ToLowerInvariant()}_{r.PatientKey}.jpg",
                        TestName = testType,
                        TestType = testType,
                        AnalysisStatus = "Completed",
                        AI_AnalysisJSON = $"{{\"testType\":\"{testType}\",\"status\":\"{r.Status}\"}}"
                    });
                }
            }
            await context.SaveChangesAsync();

            // Child tables: one row per LabTest, keyed by the parent LabTestID.
            foreach (var r in ReportDefinitions)
            {
                var patientId = cast.Patients[r.PatientKey].PatientID;
                var panel = LabPanels[r.PatientKey];

                int LabTestIdFor(string type) => context.LabTests
                    .First(l => l.PatientID == patientId && l.TestType == type).LabTestID;

                context.CBC_Tests.Add(new CBC_Test
                {
                    LabTestID = LabTestIdFor("CBC"),
                    HB = panel.Hb,
                    RBCs_Count = panel.Rbc,
                    MCV = panel.Mcv,
                    MCH = panel.Mch,
                    WBC = panel.Wbc,
                    lymphocytes = panel.Lymphocytes,
                    platelet_count = panel.Platelets
                });

                context.BloodGroup_Tests.Add(new BloodGroup_Test
                {
                    LabTestID = LabTestIdFor("BloodGroup"),
                    ABO_Group = panel.Abo,
                    RH_Factor = panel.Rh
                });

                context.HbA1c_Tests.Add(new HbA1c_Test { LabTestID = LabTestIdFor("HbA1c"), HbA1c = panel.HbA1c });
                context.FBG_Tests.Add(new FBG_Test { LabTestID = LabTestIdFor("FBG"), FBG = panel.Fbg });
                context.TSH_Tests.Add(new TSH_Test { LabTestID = LabTestIdFor("TSH"), TSH = panel.Tsh, TSH_Unit = "mIU/L" });
                context.Ferritin_Tests.Add(new Ferritin_Test { LabTestID = LabTestIdFor("Ferritin"), Ferritin_Value = panel.Ferritin });
                context.HBsAg_Tests.Add(new HBsAg_Test { LabTestID = LabTestIdFor("HBsAg"), HBsAg = "Negative" });
                context.HCV_Tests.Add(new HCV_Test { LabTestID = LabTestIdFor("HCV"), HCV = "Negative" });

                context.Urinalysis_Tests.Add(new Urinalysis_Test
                {
                    LabTestID = LabTestIdFor("Urinalysis"),
                    Color = panel.UrineColor,
                    PH = panel.UrinePh,
                    Specific_Gravity = panel.SpecificGravity,
                    Protein = panel.Protein,
                    Glucose = panel.Glucose,
                    Nitrite = "Negative",
                    Ketones = panel.Ketones,
                    Blood = "Negative",
                    RBCs = panel.UrineRbcs,
                    Leukocytes = panel.Leukocytes
                });
            }

            await context.SaveChangesAsync();

            await SeedInFlightAnalysesAsync(context, cast);
        }

        /// <summary>
        /// Uploads that have not finished the AI pipeline yet. The analysis history groups by
        /// report, so each in-flight state needs its own report to be visible. These cover the
        /// three non-completed states a real upload passes through (or dies in).
        /// </summary>
        private static async Task SeedInFlightAnalysesAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;

            async Task InFlight(string patientKey, string doctorKey, string testType, string status,
                int daysAgo, string? ocrJson, string? confirmedJson, string? errorMessage)
            {
                var patient = cast.Patients[patientKey];
                var report = new TestReport
                {
                    PatientID = patient.PatientID,
                    DoctorID = cast.Doctors[doctorKey].DoctorID,
                    ReportDate = today.AddDays(-daysAgo),
                    AnalysisStatus = status,
                    OverallStatus = null,          // no verdict yet — the pipeline has not produced one
                    AISummary = errorMessage
                };
                context.TestReports.Add(report);
                await context.SaveChangesAsync();

                context.LabTests.Add(new LabTest
                {
                    PatientID = patient.PatientID,
                    DoctorID = cast.Doctors[doctorKey].DoctorID,
                    ReportID = report.ReportID,
                    ModelID = cast.Models.TryGetValue(testType, out var model) ? model.ModelID : null,
                    UploadDate = today.AddDays(-daysAgo),
                    ImagePath = $"/uploads/lab-tests/{patient.PatientID}/{testType.ToLowerInvariant()}_pending.jpg",
                    TestName = testType,
                    TestType = testType,
                    AnalysisStatus = status,
                    OcrRawJson = ocrJson,
                    OcrNormalizedJson = ocrJson,
                    ConfirmedJson = confirmedJson
                });
                await context.SaveChangesAsync();
            }

            // OCR has read the image; the values are waiting for the user to confirm them.
            await InFlight("sarah", "ahmed", "CBC", AnalysisStatus.WaitingForConfirmation, 0,
                "{\"HB\":\"11.1\",\"RBCs_Count\":\"4.0\",\"MCV\":\"80\",\"MCH\":\"26\",\"WBC\":\"8700\",\"lymphocytes\":\"33\",\"platelet_count\":\"240000\"}",
                null, null);

            // Confirmed and handed to the model — still running.
            await InFlight("reem", "nadia", "HbA1c", AnalysisStatus.Processing, 0,
                "{\"HbA1c\":\"6.6\"}", "{\"HbA1c\":\"6.6\"}", null);

            // The pipeline gave up — the failure message is what the UI shows.
            await InFlight("fatima", "ahmed", "Urinalysis", AnalysisStatus.Failed, 1,
                null, null, "The uploaded image could not be read. Please re-upload a clearer photo of the report.");
        }

        private static readonly string[] TestTypes =
        {
            "CBC", "BloodGroup", "HbA1c", "FBG", "Urinalysis", "HBsAg", "HCV", "TSH", "Ferritin"
        };

        private sealed record LabPanel(
            float Hb, float Rbc, float Mcv, float Mch, float Wbc, float Lymphocytes, float Platelets,
            string Abo, string Rh, float HbA1c, float Fbg, float Tsh, float Ferritin,
            string UrineColor, float UrinePh, float SpecificGravity, string Protein, string Glucose,
            string Ketones, string UrineRbcs, string Leukocytes);

        private static readonly Dictionary<string, LabPanel> LabPanels = new()
        {
            ["sarah"] = new LabPanel(10.8f, 3.9f, 78f, 25f, 8500f, 32f, 230000f, "A", "Positive", 5.4f, 88f, 1.8f, 18f,
                "Yellow", 6.0f, 1.015f, "Negative", "Negative", "Negative", "0-2", "Negative"),
            ["fatima"] = new LabPanel(11.5f, 4.1f, 85f, 27f, 12000f, 28f, 210000f, "O", "Positive", 5.9f, 104f, 4.5f, 25f,
                "Light Yellow", 6.5f, 1.018f, "Trace", "Negative", "Negative", "0-2", "Negative"),
            ["yasmine"] = new LabPanel(12.2f, 4.3f, 88f, 29f, 7800f, 35f, 250000f, "B", "Positive", 5.1f, 84f, 2.1f, 30f,
                "Yellow", 5.5f, 1.012f, "Negative", "Negative", "Negative", "0-2", "Negative"),
            ["hana"] = new LabPanel(9.5f, 3.5f, 72f, 22f, 9200f, 30f, 180000f, "AB", "Negative", 6.2f, 112f, 1.5f, 8f,
                "Dark Yellow", 7.0f, 1.022f, "Trace", "Trace", "Trace", "2-5", "Trace"),
            ["reem"] = new LabPanel(11.8f, 4.0f, 86f, 28f, 8000f, 33f, 220000f, "O", "Negative", 6.8f, 126f, 3.2f, 22f,
                "Yellow", 6.0f, 1.016f, "Negative", "Trace", "Negative", "0-2", "Negative")
        };

        private static string RiskLevelFor(string overallStatus) => overallStatus switch
        {
            "Abnormal" => "High",
            "Requires Attention" => "Moderate",
            _ => "Low"
        };

        private static string BuildPersonalInfoJson(Patient patient) =>
            $"{{\"Weight\":{patient.WeightKg},\"Height\":{patient.HeightCm},\"GestationalWeeks\":{patient.GestationalWeeks}," +
            $"\"DgState\":\"{patient.DgState}\",\"RiskState\":\"{patient.RiskState}\"}}";

        private static string BuildAiResultJson(string patientKey)
        {
            var p = LabPanels[patientKey];
            return "[" +
                $"{{\"test_name\":\"Haemoglobin\",\"value\":\"{p.Hb}\",\"unit\":\"g/dL\",\"status\":\"{(p.Hb < 11f ? "Low" : "Normal")}\"}}," +
                $"{{\"test_name\":\"WBC\",\"value\":\"{p.Wbc}\",\"unit\":\"/µL\",\"status\":\"{(p.Wbc > 11000f ? "High" : "Normal")}\"}}," +
                $"{{\"test_name\":\"HbA1c\",\"value\":\"{p.HbA1c}\",\"unit\":\"%\",\"status\":\"{(p.HbA1c >= 6.0f ? "High" : "Normal")}\"}}," +
                $"{{\"test_name\":\"Ferritin\",\"value\":\"{p.Ferritin}\",\"unit\":\"ng/mL\",\"status\":\"{(p.Ferritin < 15f ? "Low" : "Normal")}\"}}," +
                $"{{\"test_name\":\"TSH\",\"value\":\"{p.Tsh}\",\"unit\":\"mIU/L\",\"status\":\"{(p.Tsh > 4.0f ? "High" : "Normal")}\"}}" +
                "]";
        }

        private static string BuildAlertsJson(string overallStatus) => overallStatus switch
        {
            "Abnormal" => "[\"Critically low ferritin — iron-deficiency anaemia\",\"Trace protein in urine\"]",
            "Requires Attention" => "[\"Result above the expected range — clinical review advised\"]",
            _ => "[]"
        };

        // ============================================================
        // 10. ULTRASOUND IMAGES
        // ============================================================
        private static async Task SeedUltrasoundImagesAsync(AppDbContext context, Cast cast)
        {
            if (context.UltrasoundImages.Any())
                return;

            var today = DateTime.Today;
            var modelId = cast.Models["Ultrasound"].ModelID;

            void Scan(string patientKey, string doctorKey, int daysAgo, string anomaly, string prediction,
                double confidence, string comments, bool patientUploaded = false)
            {
                context.UltrasoundImages.Add(new UltrasoundImage
                {
                    PatientID = cast.Patients[patientKey].PatientID,
                    DoctorID = patientUploaded ? null : cast.Doctors[doctorKey].DoctorID,
                    ModelID = patientUploaded ? null : modelId,
                    ImagePath = $"/uploads/ultrasound/us_{patientKey}_{daysAgo}.jpg",
                    OriginalImagePath = $"/uploads/ultrasound/us_{patientKey}_{daysAgo}.jpg",
                    ResultImagePath = patientUploaded ? string.Empty : $"/uploads/ultrasound/us_{patientKey}_{daysAgo}_result.jpg",
                    UploadDate = today.AddDays(-daysAgo),
                    Status = patientUploaded ? UltrasoundStatus.Pending : UltrasoundStatus.Completed,
                    DetectedAnomaly = anomaly,
                    Prediction = prediction,
                    ConfidenceScore = patientUploaded ? null : confidence,
                    DoctorComments = comments,
                    AI_Result_JSON = patientUploaded
                        ? string.Empty
                        : $"{{\"anomaly\":\"{anomaly}\",\"prediction\":\"{prediction}\",\"confidence\":{confidence}}}",
                    IsPatientUploaded = patientUploaded
                });
            }

            Scan("sarah", "ahmed", 6, "None", "Normal", 97.2, "Normal fetal development. Measurements match gestational age.");
            Scan("sarah", "ahmed", 40, "None", "Normal", 96.8, "Anatomy scan completed — no abnormalities.");
            Scan("fatima", "ahmed", 9, "Mild Placenta Previa", "Abnormal", 88.5, "Mild placenta previa. Follow-up scan booked in 4 weeks.");
            Scan("yasmine", "mona", 12, "None", "Normal", 98.1, "First-trimester dating scan looks normal.");
            Scan("hana", "karim", 4, "None", "Normal", 96.0, "Third-trimester scan. Baby is in cephalic position.");
            Scan("reem", "nadia", 7, "None", "Normal", 95.3, "Normal fetal growth. Estimated weight on the 55th centile.");
            // Patient self-upload awaiting a doctor's AI analysis — exercises the pending queue.
            Scan("sarah", "ahmed", 1, string.Empty, string.Empty, 0, string.Empty, patientUploaded: true);

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 11. PRESCRIPTIONS + ITEMS
        // ============================================================
        /// <summary>patient, doctor, days ago, notes, items (name, dosage, frequency, duration days, instructions).</summary>
        private static readonly (string PatientKey, string DoctorKey, int DaysAgo, string Notes,
            (string Name, string Dosage, string Frequency, int Days, string Instructions)[] Items)[] PrescriptionDefinitions =
        {
            ("sarah", "ahmed", 6, "Continue prenatal vitamins; increase iron supplementation.", new[]
            {
                ("Ferrous Sulfate", "325mg", "Twice daily", 90, "Take with vitamin C for better absorption"),
                ("Folic Acid", "5mg", "Once daily", 180, "Take in the morning with food"),
                ("Vitamin D3", "1000 IU", "Once daily", 180, "Take with your main meal")
            }),
            ("fatima", "ahmed", 9, "Blood-pressure management plan. Monitor daily.", new[]
            {
                ("Labetalol", "100mg", "Twice daily", 60, "Do not stop abruptly; monitor BP twice daily"),
                ("Calcium Carbonate", "500mg", "Twice daily", 90, "Take with food"),
                ("Aspirin", "81mg", "Once daily", 90, "Take at bedtime")
            }),
            ("yasmine", "mona", 12, "Standard first-trimester prescription.", new[]
            {
                ("Prenatal Multivitamin", "1 tablet", "Once daily", 270, "Take with food"),
                ("Folic Acid", "0.4mg", "Once daily", 270, "Take in the morning")
            }),
            ("hana", "karim", 4, "Iron-deficiency anaemia treatment plan.", new[]
            {
                ("Methyldopa", "250mg", "Three times daily", 60, "Monitor BP regularly"),
                ("Iron Sucrose IV", "200mg", "Weekly infusion", 42, "Administered in clinic under supervision")
            }),
            ("reem", "nadia", 7, "Gestational diabetes management.", new[]
            {
                ("Metformin", "500mg", "Twice daily", 90, "Take with meals; monitor blood sugar 4x daily"),
                ("Vitamin D3", "1000 IU", "Once daily", 180, "Take with your main meal")
            })
        };

        private static async Task SeedPrescriptionsAsync(AppDbContext context, Cast cast)
        {
            if (context.Prescriptions.Any())
                return;

            var today = DateTime.Today;

            foreach (var p in PrescriptionDefinitions)
            {
                var prescription = new Prescription
                {
                    DoctorID = cast.Doctors[p.DoctorKey].DoctorID,
                    PatientID = cast.Patients[p.PatientKey].PatientID,
                    PrescriptionDate = today.AddDays(-p.DaysAgo),
                    Notes = p.Notes
                };
                context.Prescriptions.Add(prescription);
                await context.SaveChangesAsync();

                foreach (var (name, dosage, frequency, days, instructions) in p.Items)
                {
                    context.PrescriptionItems.Add(new PrescriptionItem
                    {
                        PrescriptionID = prescription.PrescriptionID,
                        MedicineName = name,
                        Dosage = dosage,
                        Frequency = frequency,
                        DurationDays = days,
                        Instructions = instructions
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        // ============================================================
        // 12. MEDICATIONS (+ schedules + reminder settings)
        // ============================================================
        /// <summary>The prescription items that become tracked medications, and the times of day they are taken.</summary>
        private static readonly (string PatientKey, string MedicineName, TimeSpan[] Times, MedicationSource Source)[] MedicationDefinitions =
        {
            ("sarah",   "Ferrous Sulfate",      new[] { new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0) },                     MedicationSource.Prescription),
            ("sarah",   "Folic Acid",           new[] { new TimeSpan(9, 0, 0) },                                            MedicationSource.Prescription),
            ("fatima",  "Labetalol",            new[] { new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0) },                     MedicationSource.Prescription),
            ("fatima",  "Aspirin",              new[] { new TimeSpan(21, 0, 0) },                                           MedicationSource.Prescription),
            ("yasmine", "Prenatal Multivitamin",new[] { new TimeSpan(9, 0, 0) },                                            MedicationSource.Prescription),
            ("hana",    "Methyldopa",           new[] { new TimeSpan(8, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(20, 0, 0) }, MedicationSource.Prescription),
            ("reem",    "Metformin",            new[] { new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0) },                     MedicationSource.Prescription),
            ("reem",    "Vitamin D3",           new[] { new TimeSpan(9, 0, 0) },                                            MedicationSource.Prescription)
        };

        private static async Task SeedMedicationsAsync(AppDbContext context, Cast cast)
        {
            if (!context.Medications.Any())
            {
                var today = DateTime.Today;

                foreach (var m in MedicationDefinitions)
                {
                    var patientId = cast.Patients[m.PatientKey].PatientID;

                    // Link back to the prescription item this medication came from.
                    var item = context.PrescriptionItems
                        .Include(i => i.Prescription)
                        .FirstOrDefault(i => i.Prescription.PatientID == patientId && i.MedicineName == m.MedicineName);

                    var startDate = item?.Prescription.PrescriptionDate.Date ?? today.AddDays(-14);
                    var durationDays = item?.DurationDays ?? 90;

                    var medication = new Medication
                    {
                        PatientID = patientId,
                        Name = m.MedicineName,
                        Dosage = item?.Dosage ?? string.Empty,
                        Frequency = item?.Frequency ?? "Once daily",
                        Instructions = item?.Instructions ?? string.Empty,
                        StartDate = startDate,
                        EndDate = startDate.AddDays(durationDays),
                        IsActive = true,
                        Source = m.Source,
                        PrescriptionItemId = item?.ItemID,
                        ReminderLeadTimeMinutes = 30,
                        TotalPills = durationDays * m.Times.Length,
                        PillsPerDose = 1,
                        CreatedAt = startDate
                    };

                    context.Medications.Add(medication);
                    await context.SaveChangesAsync();

                    foreach (var time in m.Times)
                    {
                        context.MedicationSchedules.Add(new MedicationSchedule
                        {
                            MedicationId = medication.MedicationId,
                            TimeOfDay = time,
                            FrequencyPerDay = m.Times.Length
                        });
                    }
                    await context.SaveChangesAsync();
                }

                // A self-added medication (not from a prescription) for the newest patient.
                var nour = cast.Patients["nour"];
                var selfMed = new Medication
                {
                    PatientID = nour.PatientID,
                    Name = "Folic Acid",
                    Dosage = "0.4mg",
                    Frequency = "Once daily",
                    Instructions = "Take in the morning with water",
                    StartDate = today.AddDays(-20),
                    EndDate = today.AddDays(250),
                    IsActive = true,
                    Source = MedicationSource.Self,
                    ReminderLeadTimeMinutes = 15,
                    TotalPills = 270,
                    PillsPerDose = 1,
                    CreatedAt = today.AddDays(-20)
                };
                context.Medications.Add(selfMed);
                await context.SaveChangesAsync();

                context.MedicationSchedules.Add(new MedicationSchedule
                {
                    MedicationId = selfMed.MedicationId,
                    TimeOfDay = new TimeSpan(9, 0, 0),
                    FrequencyPerDay = 1
                });
                await context.SaveChangesAsync();
            }

            if (!context.MedicationReminderSettings.Any())
            {
                foreach (var patient in cast.Patients.Values)
                {
                    context.MedicationReminderSettings.Add(new MedicationReminderSettings
                    {
                        PatientID = patient.PatientID,
                        LeadTimeMinutes = 30,
                        UpdatedAt = DateTime.Now
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Rolling: rebuilds the last week of dose history (plus today's doses) whenever there is
        /// nothing logged for today, so the adherence charts are never empty.
        /// </summary>
        private static async Task SeedMedicationLogsAsync(AppDbContext context)
        {
            var today = DateTime.Today;

            if (context.MedicationLogs.Any(l => l.ScheduledAt >= today && l.ScheduledAt < today.AddDays(1)))
                return;

            var now = DateTime.Now;
            var medications = context.Medications
                .Include(m => m.Schedules)
                .Where(m => m.IsActive)
                .ToList();

            foreach (var medication in medications)
            {
                foreach (var schedule in medication.Schedules)
                {
                    for (int dayOffset = 6; dayOffset >= 0; dayOffset--)
                    {
                        var scheduledAt = today.AddDays(-dayOffset).Add(schedule.TimeOfDay);

                        if (scheduledAt < medication.StartDate)
                            continue;

                        if (context.MedicationLogs.Any(l => l.MedicationId == medication.MedicationId && l.ScheduledAt == scheduledAt))
                            continue;

                        MedicationLogStatus status;
                        DateTime? takenAt = null;

                        if (scheduledAt > now)
                        {
                            status = MedicationLogStatus.Scheduled;
                        }
                        else if (dayOffset == 2)
                        {
                            // One missed day, so the adherence rate is realistic rather than a perfect 100%.
                            status = MedicationLogStatus.Missed;
                        }
                        else if (dayOffset == 4 && schedule.TimeOfDay.Hours >= 20)
                        {
                            status = MedicationLogStatus.Skipped;
                        }
                        else
                        {
                            status = MedicationLogStatus.Taken;
                            takenAt = scheduledAt.AddMinutes(7);
                        }

                        context.MedicationLogs.Add(new MedicationLog
                        {
                            MedicationId = medication.MedicationId,
                            ScheduledAt = scheduledAt,
                            TakenAt = takenAt,
                            Status = status,
                            Notes = status == MedicationLogStatus.Skipped ? "Felt nauseous — skipped this dose." : null
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 13. PATIENT DRUGS (self-reported medication history)
        // ============================================================
        private static async Task SeedPatientDrugsAsync(AppDbContext context, Cast cast)
        {
            if (context.PatientDrugs.Any())
                return;

            void Drug(string patientKey, string name, int months, string reason, double dose) =>
                context.PatientDrugs.Add(new PatientDrug
                {
                    PatientID = cast.Patients[patientKey].PatientID,
                    DrugName = name,
                    DurationMonths = months,
                    Reason = reason,
                    DoseMgPerDay = dose
                });

            Drug("sarah", "Folic Acid", 9, "Neural tube defect prevention", 0.4);
            Drug("sarah", "Iron Supplement", 6, "Iron-deficiency anaemia", 30.0);
            Drug("fatima", "Labetalol", 5, "Gestational hypertension", 200.0);
            Drug("fatima", "Calcium Carbonate", 7, "Calcium supplementation", 1000.0);
            Drug("yasmine", "Folic Acid", 9, "Neural tube defect prevention", 0.4);
            Drug("yasmine", "Iron Supplement", 4, "Mild anaemia", 20.0);
            Drug("hana", "Methyldopa", 4, "Chronic hypertension", 500.0);
            Drug("hana", "Aspirin Low-Dose", 6, "Pre-eclampsia prevention", 81.0);
            Drug("reem", "Vitamin D3", 9, "Vitamin D deficiency", 1000.0);
            Drug("reem", "Magnesium", 3, "Leg cramps in pregnancy", 350.0);
            Drug("nour", "Folic Acid", 9, "Neural tube defect prevention", 0.4);

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 14. VITALS — blood pressure, blood sugar, weight
        // ============================================================
        private static async Task SeedVitalsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;

            if (!context.PatientBloodPressure.Any())
            {
                // systolic/diastolic baselines per patient; readings drift slightly day to day.
                var baselines = new Dictionary<string, (int Sys, int Dia)>
                {
                    ["sarah"] = (118, 76),
                    ["fatima"] = (145, 92),
                    ["yasmine"] = (110, 70),
                    ["hana"] = (150, 98),
                    ["reem"] = (117, 75),
                    ["nour"] = (112, 72)
                };

                foreach (var (key, baseline) in baselines)
                {
                    for (int dayOffset = 13; dayOffset >= 0; dayOffset--)
                    {
                        var drift = (dayOffset % 5) - 2;               // -2 .. +2
                        var systolic = baseline.Sys + drift * 3;
                        var diastolic = baseline.Dia + drift * 2;

                        context.PatientBloodPressure.Add(new PatientBloodPressure
                        {
                            PatientID = cast.Patients[key].PatientID,
                            BloodPressure = $"{systolic}/{diastolic}",
                            DateTime = today.AddDays(-dayOffset).AddHours(8).AddMinutes(30),
                            MeasurementTime = "Morning"
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            if (!context.PatientBloodSugar.Any())
            {
                // fasting / post-meal baselines — Reem and Hana run high (gestational diabetes).
                var baselines = new Dictionary<string, (double Fasting, double PostMeal)>
                {
                    ["sarah"] = (92, 128),
                    ["fatima"] = (105, 150),
                    ["yasmine"] = (85, 120),
                    ["hana"] = (98, 135),
                    ["reem"] = (118, 168),
                    ["nour"] = (88, 118)
                };

                foreach (var (key, baseline) in baselines)
                {
                    for (int dayOffset = 13; dayOffset >= 0; dayOffset--)
                    {
                        var drift = (dayOffset % 4) - 1.5;

                        context.PatientBloodSugar.Add(new PatientBloodSugar
                        {
                            PatientID = cast.Patients[key].PatientID,
                            BloodSugar = Math.Round(baseline.Fasting + drift * 3, 1),
                            DateTime = today.AddDays(-dayOffset).AddHours(8),
                            MeasurementTime = "Fasting"
                        });

                        context.PatientBloodSugar.Add(new PatientBloodSugar
                        {
                            PatientID = cast.Patients[key].PatientID,
                            BloodSugar = Math.Round(baseline.PostMeal + drift * 4, 1),
                            DateTime = today.AddDays(-dayOffset).AddHours(14),
                            MeasurementTime = "After Meal"
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            if (!context.WeightTrackings.Any())
            {
                foreach (var p in PatientDefinitions)
                {
                    var patientId = cast.Patients[p.Key].PatientID;
                    var pregnancyStart = today.AddDays(-7 * p.Week);
                    // Roughly 0.4 kg/week of gain from a pre-pregnancy baseline up to today's weight.
                    var baseline = p.Weight - 0.4 * p.Week;

                    for (int week = 0; week <= p.Week; week += 4)
                    {
                        var recordedDate = pregnancyStart.AddDays(7 * week);
                        if (recordedDate > today)
                            break;

                        context.WeightTrackings.Add(new WeightTracking
                        {
                            PatientID = patientId,
                            RecordedDate = recordedDate,
                            WeightKg = Math.Round(baseline + 0.4 * week, 1),
                            Notes = week == 0 ? "Pre-pregnancy baseline" : $"Week {week} check-in"
                        });
                    }

                    // Always finish on today's actual weight.
                    context.WeightTrackings.Add(new WeightTracking
                    {
                        PatientID = patientId,
                        RecordedDate = today,
                        WeightKg = p.Weight,
                        Notes = "Latest reading"
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        // ============================================================
        // 15. MEDICAL HISTORY
        // ============================================================
        private static async Task SeedMedicalHistoryAsync(AppDbContext context, Cast cast)
        {
            if (context.MedicalHistories.Any())
                return;

            var today = DateTime.Today;

            void Event(string patientKey, string doctorKey, int daysAgo, string eventType, string summary,
                int? labTestId = null, int? imageId = null)
            {
                var date = today.AddDays(-daysAgo);
                context.MedicalHistories.Add(new MedicalHistory
                {
                    PatientID = cast.Patients[patientKey].PatientID,
                    DoctorID = cast.Doctors[doctorKey].DoctorID,
                    LabTestID = labTestId,
                    ImageID = imageId,
                    EventType = eventType,
                    Summary = summary,
                    DateRecorded = date,
                    Date = date
                });
            }

            int? CbcIdFor(string patientKey)
            {
                var patientId = cast.Patients[patientKey].PatientID;
                return context.LabTests.FirstOrDefault(l => l.PatientID == patientId && l.TestType == "CBC")?.LabTestID;
            }

            int? ScanIdFor(string patientKey)
            {
                var patientId = cast.Patients[patientKey].PatientID;
                return context.UltrasoundImages
                    .Where(u => u.PatientID == patientId && !u.IsPatientUploaded)
                    .OrderByDescending(u => u.UploadDate)
                    .FirstOrDefault()?.ImageID;
            }

            Event("sarah", "ahmed", 6, MedicalHistoryEventTypes.LabTest, "Full blood panel analysed. Mild anaemia detected.", CbcIdFor("sarah"));
            Event("sarah", "ahmed", 6, MedicalHistoryEventTypes.Ultrasound, "Growth scan — no anomalies found.", null, ScanIdFor("sarah"));
            Event("sarah", "ahmed", 20, MedicalHistoryEventTypes.Appointment, "Routine prenatal visit. BP 118/76, weight gain normal.");
            Event("fatima", "ahmed", 9, MedicalHistoryEventTypes.LabTest, "CBC shows elevated WBC. Blood pressure trending high.", CbcIdFor("fatima"));
            Event("fatima", "ahmed", 9, MedicalHistoryEventTypes.Ultrasound, "Mild placenta previa detected. Follow-up scheduled.", null, ScanIdFor("fatima"));
            Event("fatima", "ahmed", 9, MedicalHistoryEventTypes.Alert, "Blood pressure consistently above 140/90 — pre-eclampsia watch.");
            Event("yasmine", "mona", 12, MedicalHistoryEventTypes.LabTest, "First-trimester panel all normal.", CbcIdFor("yasmine"));
            Event("yasmine", "mona", 12, MedicalHistoryEventTypes.Ultrasound, "Dating scan. Crown-rump length within the normal range.", null, ScanIdFor("yasmine"));
            Event("hana", "karim", 4, MedicalHistoryEventTypes.LabTest, "Iron-deficiency anaemia confirmed. IV iron therapy started.", CbcIdFor("hana"));
            Event("hana", "karim", 4, MedicalHistoryEventTypes.DoctorNote, "Third-trimester consultation. Birth plan discussed.");
            Event("reem", "nadia", 7, MedicalHistoryEventTypes.LabTest, "HbA1c above the gestational-diabetes target. Diet plan adjusted.", CbcIdFor("reem"));
            Event("reem", "nadia", 7, MedicalHistoryEventTypes.Medication, "Metformin 500mg twice daily started.");

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 16. DOCTOR NOTES
        // ============================================================
        private static async Task SeedNotesAsync(AppDbContext context, Cast cast)
        {
            if (context.Notes.Any())
                return;

            var today = DateTime.Today;

            void Note(string doctorKey, string patientKey, int daysAgo, string content) =>
                context.Notes.Add(new Note
                {
                    DoctorID = cast.Doctors[doctorKey].DoctorID,
                    PatientID = cast.Patients[patientKey].PatientID,
                    CreatedDate = today.AddDays(-daysAgo),
                    Content = content
                });

            Note("ahmed", "sarah", 6, "Patient reports occasional dizziness, likely from mild anaemia. Iron increased to 60mg/day.");
            Note("ahmed", "sarah", 34, "Anatomy scan normal. Fetal measurements appropriate for gestational age. Patient reassured.");
            Note("ahmed", "fatima", 9, "Blood-pressure monitoring must continue. Patient educated on pre-eclampsia warning signs.");
            Note("mona", "yasmine", 12, "First consultation. Good general health. Prenatal vitamins prescribed; anatomy scan booked for week 20.");
            Note("mona", "yasmine", 3, "Nuchal translucency measurement normal. Low risk of chromosomal abnormalities.");
            Note("karim", "hana", 4, "Iron deficiency confirmed. IV iron infusion scheduled. Advised to increase dietary iron.");
            Note("karim", "hana", 25, "Patient reports back pain. Physiotherapy referral given. Continue aspirin 81mg.");
            Note("nadia", "reem", 7, "Gestational diabetes management. HbA1c trending high. Dietitian referral; sugar monitored 4x daily.");

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 17. ALERTS (clinical only — reminders live in PatientNotifications)
        // ============================================================
        private static async Task SeedAlertsAsync(AppDbContext context, Cast cast)
        {
            if (context.Alerts.Any())
                return;

            var today = DateTime.Today;

            void Alert(string patientKey, string title, string message, string type, int daysAgo, bool isRead = false) =>
                context.Alerts.Add(new Alert
                {
                    PatientID = cast.Patients[patientKey].PatientID,
                    Title = title,
                    Message = message,
                    AlertType = type,
                    Category = "Clinical",
                    DateCreated = today.AddDays(-daysAgo),
                    IsRead = isRead
                });

            Alert("sarah", "Low Haemoglobin Detected", "Your CBC shows haemoglobin of 10.8 g/dL. Consider increasing iron supplementation.", "warning", 6);
            Alert("sarah", "Lab Results Ready", "Your latest blood panel has been reviewed by Dr. Ahmed Hassan.", "info", 5, isRead: true);
            Alert("fatima", "Elevated Blood Pressure", "Your blood pressure has been consistently elevated. Contact your doctor immediately.", "danger", 1);
            Alert("fatima", "Elevated WBC Count", "Your CBC shows a WBC count of 12,000 — this may indicate an infection.", "warning", 9);
            Alert("hana", "Iron Deficiency Anaemia", "Your ferritin level is critically low at 8 ng/mL. IV iron therapy has been prescribed.", "danger", 4);
            Alert("hana", "Protein In Urine", "Trace protein was found in your urinalysis. Your doctor is monitoring for pre-eclampsia.", "warning", 4);
            Alert("reem", "HbA1c Above Target", "Your HbA1c is 6.8%, above the gestational-diabetes target. Dietary adjustments recommended.", "warning", 7);
            Alert("reem", "Blood Sugar Alert", "Your recent readings are above target. Please follow the adjusted dietary plan.", "danger", 0);
            Alert("yasmine", "All Clear", "Your first-trimester screening came back completely normal.", "success", 12, isRead: true);

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 18. NOTIFICATIONS (patient bell, doctor bell, admin bell)
        // ============================================================
        private static async Task SeedNotificationsAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;

            if (!context.PatientNotifications.Any())
            {
                void Notify(string patientKey, string title, string message, string type, string severity, int daysAgo, string? actionUrl, bool isRead = false) =>
                    context.PatientNotifications.Add(new PatientNotification
                    {
                        PatientID = cast.Patients[patientKey].PatientID,
                        Title = title,
                        Message = message,
                        NotificationType = type,
                        Severity = severity,
                        DateCreated = today.AddDays(-daysAgo),
                        IsRead = isRead,
                        ActionUrl = actionUrl
                    });

                var sarahId = cast.Patients["sarah"].PatientID;
                var yasmineId = cast.Patients["yasmine"].PatientID;

                Notify("sarah", "Medication Reminder", "It's time to take your iron supplement (325mg).", PatientNotificationTypes.Medication, "info", 0, $"/Patient/Medications/{sarahId}");
                Notify("sarah", "Appointment Reminder", "You have an appointment with Dr. Ahmed Hassan tomorrow.", PatientNotificationTypes.Appointment, "info", 0, $"/PatientAppointments/Appointments/{sarahId}");
                Notify("sarah", "Lab Results Ready", "Your blood panel results are now available to view.", PatientNotificationTypes.LabResult, "success", 5, $"/PatientMedicalHistory/MedicalHistory/{sarahId}", isRead: true);
                Notify("fatima", "New Prescription", "Dr. Ahmed Hassan added a new prescription to your record.", PatientNotificationTypes.Prescription, "info", 9, null);
                Notify("yasmine", "Ultrasound Analysis Ready", "Dr. Mona Ibrahim analysed your dating scan. Result: normal.", PatientNotificationTypes.Ultrasound, "success", 12, $"/PatientMedicalHistory/MedicalHistory/{yasmineId}", isRead: true);
                Notify("reem", "Medication Reminder", "Remember to take your evening Metformin dose.", PatientNotificationTypes.Medication, "info", 0, null);
                Notify("hana", "New Message", "Dr. Karim Mostafa sent you a message.", PatientNotificationTypes.Message, "info", 0, null);
                Notify("nour", "Welcome to NABD", "Send a request to a doctor to start your pregnancy care plan.", PatientNotificationTypes.Account, "info", 1, null);

                // Operational notifications are surfaced to clinic assistants, not to the patient.
                Notify("sarah", "Patient Checked In", "Sarah Ahmed has checked in for her appointment with Dr. Ahmed Hassan.", PatientNotificationTypes.Operational, "info", 0, "/Assistant/Alerts");
                Notify("fatima", "Appointment Rescheduled", "Fatima Ali's appointment with Dr. Mona Ibrahim was moved to a later slot.", PatientNotificationTypes.Operational, "warning", 0, "/Assistant/Alerts");

                await context.SaveChangesAsync();
            }

            if (!context.DoctorNotifications.Any())
            {
                void Notify(string doctorKey, string title, string message, string type, int daysAgo, string? actionUrl, bool isRead = false) =>
                    context.DoctorNotifications.Add(new DoctorNotification
                    {
                        DoctorID = cast.Doctors[doctorKey].DoctorID,
                        Title = title,
                        Message = message,
                        NotificationType = type,
                        DateCreated = today.AddDays(-daysAgo),
                        IsRead = isRead,
                        ActionUrl = actionUrl
                    });

                Notify("ahmed", "Account Approved", "Your NABD registration has been approved. You now have access to all doctor features.", "admin_approved", 200, null, isRead: true);
                Notify("ahmed", "New Patient Request", "Nour Adel has requested to join your patient list.", "patient_request", 1, "/Doctor/Patients");
                Notify("ahmed", "High-Risk Patient", "Fatima Ali's blood pressure readings are consistently above 140/90.", "patient_risk", 1, "/Doctor/Patients");
                Notify("mona", "New Patient Request", "Sarah Ahmed has requested to join your patient list.", "patient_request", 2, "/Doctor/Patients");
                Notify("mona", "Assistant Joined Your Team", "Dina Samir accepted your clinic invitation and joined your team.", "invitation_accepted", 30, null, isRead: true);
                Notify("karim", "High-Risk Patient", "Hana Khaled's ferritin is critically low (8 ng/mL).", "patient_risk", 4, "/Doctor/Patients");
                Notify("karim", "Clinic Invitation", "Dr. Mona Ibrahim invited you to join Fetal Health Clinic.", "clinic_invitation", 20, "/Doctor/Clinics", isRead: true);
                Notify("nadia", "High-Risk Patient", "Reem Nasser's HbA1c is above the gestational-diabetes target.", "patient_risk", 7, "/Doctor/Patients");

                await context.SaveChangesAsync();
            }

            if (!context.AdminNotifications.Any())
            {
                void Notify(string title, string message, string type, string severity, int daysAgo, string? actionUrl, bool isRead = false) =>
                    context.AdminNotifications.Add(new AdminNotification
                    {
                        Title = title,
                        Message = message,
                        NotificationType = type,
                        Severity = severity,
                        DateCreated = today.AddDays(-daysAgo),
                        IsRead = isRead,
                        ActionUrl = actionUrl
                    });

                Notify("New doctor awaiting review", "Dr. Omar Fathy registered and is waiting for licence verification.", "doctor_registered", "warning", 3, $"/Admin/DoctorDetail/{cast.Doctors["omar"].DoctorID}");
                Notify("New clinic created", $"{cast.Clinics["endocrine"].Name} was created by Dr. Nadia Salem.", "clinic_created", "info", 40, $"/Admin/ClinicDetail/{cast.Clinics["endocrine"].ClinicID}", isRead: true);
                Notify("Doctor registration rejected", "Dr. Sami Gaber's licence image was unreadable and the application was rejected.", "doctor_rejected", "danger", 30, $"/Admin/DoctorDetail/{cast.Doctors["sami"].DoctorID}", isRead: true);
                Notify("New patient registered", "Nour Adel created a patient account.", "patient_registered", "info", 1, null);

                await context.SaveChangesAsync();
            }
        }

        // ============================================================
        // 19. PLACES (patient's saved clinics / pharmacies / labs)
        // ============================================================
        private static async Task SeedPlacesAsync(AppDbContext context, Cast cast)
        {
            if (context.Places.Any())
                return;

            void Place(string patientKey, string name, string type, string address, string phone, string image) =>
                context.Places.Add(new Place
                {
                    PatientID = cast.Patients[patientKey].PatientID,
                    Name = name,
                    Type = type,
                    Address = address,
                    Phone = phone,
                    ImageURL = image
                });

            Place("sarah", "MamaCare Central", "Clinic", "15 Tahrir St, Cairo", "0222345678", "/uploads/places/mamacare_central.jpg");
            Place("sarah", "Cairo Pharmacy", "Pharmacy", "10 Tahrir Square, Cairo", "0223456789", "/uploads/places/cairo_pharmacy.jpg");
            Place("sarah", "Cairo Diagnostic Lab", "Lab", "5 Ramses St, Cairo", "0224567890", "/uploads/places/cairo_lab.jpg");
            Place("fatima", "Fetal Health Clinic", "Clinic", "22 Nasr City, Cairo", "0225678901", "/uploads/places/fetal_clinic.jpg");
            Place("fatima", "Nasr City Hospital", "Hospital", "33 Nasr City, Cairo", "0226789012", "/uploads/places/nasr_hospital.jpg");
            Place("yasmine", "New Cairo OBG Center", "Clinic", "18 New Cairo", "0227890123", "/uploads/places/newcairo_obg.jpg");
            Place("yasmine", "El-Salam Pharmacy", "Pharmacy", "20 New Cairo Blvd", "0228901234", "/uploads/places/elsalam_pharmacy.jpg");
            Place("hana", "Alexandria OBG Center", "Clinic", "7 Corniche, Alexandria", "0345678901", "/uploads/places/alex_obg.jpg");
            Place("hana", "Shubra General Hospital", "Hospital", "12 Shubra St, Cairo", "0229012345", "/uploads/places/shubra_hospital.jpg");
            Place("reem", "Endocrine & Maternal Clinic", "Clinic", "30 Heliopolis, Cairo", "0230123456", "/uploads/places/endocrine_clinic.jpg");
            Place("reem", "Mohandessin Diagnostic Center", "Lab", "15 Mohandessin, Giza", "0231234567", "/uploads/places/mohandessin_lab.jpg");
            Place("nour", "Sheikh Zayed Pharmacy", "Pharmacy", "44 Sheikh Zayed, Giza", "0232345678", "/uploads/places/zayed_pharmacy.jpg");

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 20. COMMUNITY (posts, comments, likes)
        // ============================================================
        private static async Task SeedCommunityAsync(AppDbContext context, Cast cast)
        {
            if (context.CommunityPosts.Any())
                return;

            var now = DateTime.UtcNow;

            var posts = new List<CommunityPost>
            {
                new()
                {
                    PatientID = cast.Patients["sarah"].PatientID,
                    Title = "Morning sickness in the second trimester — normal?",
                    Content = "I'm 24 weeks and still getting nausea in the mornings. Has anyone else had this last past the first trimester? What helped you?",
                    Category = "Pregnancy",
                    CreatedAt = now.AddDays(-5),
                    UpdatedAt = now.AddDays(-5)
                },
                new()
                {
                    DoctorID = cast.Doctors["mona"].DoctorID,
                    Title = "Iron-rich foods every pregnant woman should know",
                    Content = "Iron-deficiency anaemia is one of the most common issues we see in pregnancy. Lentils, spinach, red meat and fortified cereals are excellent sources. Pair them with vitamin C (orange juice, peppers) to nearly double absorption, and avoid drinking tea or coffee with meals — they block it.",
                    Category = "Nutrition",
                    CreatedAt = now.AddDays(-4),
                    UpdatedAt = now.AddDays(-4)
                },
                new()
                {
                    PatientID = cast.Patients["fatima"].PatientID,
                    Title = "Managing anxiety before delivery",
                    Content = "I'm 32 weeks and the closer I get to my due date, the more anxious I feel. How did you all cope with the last few weeks?",
                    Category = "Mental Health",
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-3)
                },
                new()
                {
                    DoctorID = cast.Doctors["ahmed"].DoctorID,
                    Title = "Safe exercise during pregnancy",
                    Content = "Walking, swimming and prenatal yoga are safe for most pregnancies right up to delivery. Aim for about 150 minutes a week. Stop immediately and call your doctor if you have bleeding, contractions, dizziness or fluid leakage.",
                    Category = "Exercise",
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2)
                },
                new()
                {
                    PatientID = cast.Patients["reem"].PatientID,
                    Title = "Gestational diabetes — what actually worked for me",
                    Content = "After my HbA1c came back high, my doctor put me on Metformin plus a diet plan. Splitting meals into six smaller ones and walking for 20 minutes after eating brought my numbers right down.",
                    Category = "Medication",
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1)
                },
                new()
                {
                    PatientID = cast.Patients["yasmine"].PatientID,
                    Title = "First-time mum — what do I actually need for the hospital bag?",
                    Content = "I'm only 12 weeks but I like to plan ahead. What did you actually use from your hospital bag, and what was a waste of space?",
                    Category = "General",
                    CreatedAt = now.AddHours(-8),
                    UpdatedAt = now.AddHours(-8)
                }
            };

            context.CommunityPosts.AddRange(posts);
            await context.SaveChangesAsync();

            var sicknessPost = posts[0];
            var ironPost = posts[1];
            var anxietyPost = posts[2];
            var exercisePost = posts[3];
            var diabetesPost = posts[4];

            context.CommunityComments.AddRange(
                new CommunityComment { CommunityPostId = sicknessPost.CommunityPostId, DoctorID = cast.Doctors["ahmed"].DoctorID, Content = "It can persist beyond the first trimester in a minority of pregnancies. Small frequent meals and ginger help. If you can't keep fluids down, contact me.", CreatedAt = now.AddDays(-5).AddHours(2) },
                new CommunityComment { CommunityPostId = sicknessPost.CommunityPostId, PatientID = cast.Patients["hana"].PatientID, Content = "Mine lasted until week 28 with my first. Dry crackers before getting out of bed were the only thing that worked for me.", CreatedAt = now.AddDays(-4) },
                new CommunityComment { CommunityPostId = ironPost.CommunityPostId, PatientID = cast.Patients["sarah"].PatientID, Content = "This explains so much — I've been drinking tea with every meal and my haemoglobin is low. Stopping that today.", CreatedAt = now.AddDays(-3) },
                new CommunityComment { CommunityPostId = anxietyPost.CommunityPostId, PatientID = cast.Patients["reem"].PatientID, Content = "Totally normal. Breathing exercises and talking to other mums here helped me a lot.", CreatedAt = now.AddDays(-2) },
                new CommunityComment { CommunityPostId = anxietyPost.CommunityPostId, DoctorID = cast.Doctors["mona"].DoctorID, Content = "Antenatal anxiety is common and treatable. Please raise it at your next appointment — you don't have to manage it alone.", CreatedAt = now.AddDays(-2).AddHours(3) },
                new CommunityComment { CommunityPostId = exercisePost.CommunityPostId, PatientID = cast.Patients["yasmine"].PatientID, Content = "Is swimming still fine in the third trimester?", CreatedAt = now.AddDays(-1) },
                new CommunityComment { CommunityPostId = diabetesPost.CommunityPostId, PatientID = cast.Patients["fatima"].PatientID, Content = "The post-meal walk tip is gold. Thank you for sharing.", CreatedAt = now.AddHours(-6) }
            );

            // One like per (post, patient) and per (post, doctor) — the unique indexes forbid duplicates.
            context.CommunityLikes.AddRange(
                new CommunityLike { CommunityPostId = ironPost.CommunityPostId, PatientID = cast.Patients["sarah"].PatientID, CreatedAt = now.AddDays(-3) },
                new CommunityLike { CommunityPostId = ironPost.CommunityPostId, PatientID = cast.Patients["hana"].PatientID, CreatedAt = now.AddDays(-3) },
                new CommunityLike { CommunityPostId = ironPost.CommunityPostId, PatientID = cast.Patients["reem"].PatientID, CreatedAt = now.AddDays(-2) },
                new CommunityLike { CommunityPostId = ironPost.CommunityPostId, DoctorID = cast.Doctors["ahmed"].DoctorID, CreatedAt = now.AddDays(-2) },
                new CommunityLike { CommunityPostId = exercisePost.CommunityPostId, PatientID = cast.Patients["yasmine"].PatientID, CreatedAt = now.AddDays(-1) },
                new CommunityLike { CommunityPostId = exercisePost.CommunityPostId, PatientID = cast.Patients["fatima"].PatientID, CreatedAt = now.AddDays(-1) },
                new CommunityLike { CommunityPostId = diabetesPost.CommunityPostId, PatientID = cast.Patients["sarah"].PatientID, CreatedAt = now.AddHours(-5) },
                new CommunityLike { CommunityPostId = diabetesPost.CommunityPostId, DoctorID = cast.Doctors["nadia"].DoctorID, CreatedAt = now.AddHours(-4) },
                new CommunityLike { CommunityPostId = anxietyPost.CommunityPostId, PatientID = cast.Patients["yasmine"].PatientID, CreatedAt = now.AddDays(-2) }
            );

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 21. DOCTOR <-> PATIENT CHAT (stored encrypted, exactly as ChatHub writes it)
        // ============================================================
        private static async Task SeedChatMessagesAsync(AppDbContext context, Cast cast, IChatMessageCrypto? crypto)
        {
            if (context.ChatMessages.Any())
                return;

            var now = DateTime.UtcNow;

            string Protect(string text) => crypto?.Encrypt(text) ?? text;

            void Message(string fromKey, string toKey, string text, double hoursAgo, bool isRead,
                string? attachmentUrl = null, string? attachmentType = null, string? attachmentName = null)
            {
                var sentAt = now.AddHours(-hoursAgo);
                context.ChatMessages.Add(new ChatMessage
                {
                    SenderUserId = cast.Users[fromKey].Id,
                    ReceiverUserId = cast.Users[toKey].Id,
                    Message = Protect(text),
                    SentAtUtc = sentAt,
                    IsRead = isRead,
                    ReadAtUtc = isRead ? sentAt.AddMinutes(12) : null,
                    AttachmentUrl = attachmentUrl,
                    AttachmentType = attachmentType,
                    AttachmentName = attachmentName
                });
            }

            // Sarah <-> Dr. Ahmed
            Message("sarah", "ahmed", "Good morning doctor, I've been feeling dizzy when I stand up quickly. Should I be worried?", 30, true);
            Message("ahmed", "sarah", "Morning Sarah. That's common with the mild anaemia your last CBC showed. Stand up slowly, stay hydrated, and keep taking the iron.", 29, true);
            Message("sarah", "ahmed", "Understood. I've started taking it with orange juice like you suggested.", 28, true);
            Message("ahmed", "sarah", "Perfect — that boosts absorption. We'll recheck your haemoglobin at your next visit.", 27, true);
            Message("sarah", "ahmed", "Thank you! See you at the appointment.", 2, false);

            // Fatima <-> Dr. Ahmed
            Message("fatima", "ahmed", "My BP this morning was 148/95. Is that too high?", 6, true);
            Message("ahmed", "fatima", "That is above target. Please take the Labetalol on schedule and log every reading. If you get a headache or blurred vision, go to the clinic immediately.", 5, false);

            // Yasmine <-> Dr. Mona
            Message("yasmine", "mona", "Doctor, are the scan results in?", 20, true);
            Message("mona", "yasmine", "Yes — everything is completely normal. I've published the report to your medical history.", 19, true);

            // Hana <-> Dr. Karim (with an attached report — exercises the attachment bubble)
            Message("karim", "hana", "Hana, your ferritin came back at 8 ng/mL. I've prescribed IV iron — please book the infusion this week.", 3, false);
            Message("karim", "hana", "Here is the full blood panel for your records.", 3, false,
                attachmentUrl: "/uploads/tests/cbc_hana.jpg", attachmentType: "image/jpeg", attachmentName: "CBC-Report.jpg");

            // Reem <-> Dr. Nadia
            Message("reem", "nadia", "My fasting sugar was 118 today, still high.", 10, true);
            Message("nadia", "reem", "Keep to the six-small-meals plan and walk for 20 minutes after eating. We'll review at your appointment.", 9, true);

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 22. CHATBOT HISTORY
        // ============================================================
        private static async Task SeedChatbotMessagesAsync(AppDbContext context, Cast cast)
        {
            if (context.ChatbotMessages.Any())
                return;

            var now = DateTime.UtcNow;

            void Ask(string patientKey, string question, string answer, string riskLevel, string recommendation, double hoursAgo)
            {
                var patientId = cast.Patients[patientKey].PatientID;
                context.ChatbotMessages.Add(new ChatbotMessage
                {
                    PatientID = patientId,
                    Role = "User",
                    Message = question,
                    SentAtUtc = now.AddHours(-hoursAgo)
                });
                context.ChatbotMessages.Add(new ChatbotMessage
                {
                    PatientID = patientId,
                    Role = "Bot",
                    Message = answer,
                    RiskLevel = riskLevel,
                    Recommendation = recommendation,
                    SentAtUtc = now.AddHours(-hoursAgo).AddSeconds(4)
                });
            }

            Ask("sarah", "Is it safe to drink coffee while pregnant?",
                "Up to about 200mg of caffeine a day — roughly one cup of coffee — is generally considered safe in pregnancy.",
                "Low", "Keep caffeine under 200mg per day.", 26);
            Ask("sarah", "What foods help with iron deficiency?",
                "Red meat, lentils, spinach and fortified cereals are rich in iron. Pair them with vitamin C to improve absorption.",
                "Low", "Continue your prescribed iron supplement.", 24);
            Ask("fatima", "I have a headache and my vision is blurry. What should I do?",
                "Headache with visual changes in the third trimester can be a sign of pre-eclampsia and needs urgent assessment.",
                "High", "Contact your doctor or go to the nearest clinic immediately.", 4);
            Ask("yasmine", "When will I feel the baby move?",
                "Most first-time mothers feel movement between weeks 18 and 22.",
                "Low", "No action needed — this is a normal question at 12 weeks.", 14);
            Ask("reem", "Can I still eat fruit with gestational diabetes?",
                "Yes, in controlled portions. Whole fruit with a protein source keeps the sugar spike lower than juice.",
                "Moderate", "Keep monitoring your post-meal readings.", 8);

            await context.SaveChangesAsync();
        }

        // ============================================================
        // 23. INVITATIONS + ASSISTANT LEAVE WORKFLOW
        // ============================================================
        private static async Task SeedInvitationsAsync(AppDbContext context, Cast cast)
        {
            var now = DateTime.UtcNow;

            if (!context.ClinicInvitations.Any())
            {
                // Accepted history.
                context.ClinicInvitations.Add(new ClinicInvitation
                {
                    DoctorID = cast.Doctors["mona"].DoctorID,
                    ClinicID = cast.Clinics["fetal"].ClinicID,
                    AssistantID = cast.Assistants["dina"].AssistantID,
                    AssistantEmail = cast.Users["dina"].Email!,
                    Status = "Accepted",
                    SentAtUtc = now.AddDays(-40),
                    RespondedAtUtc = now.AddDays(-39),
                    ResponseMessage = "Happy to join the team."
                });

                // Pending — Heba is invited to Endocrine & Maternal; she must approve/decline,
                // and because she already works at Dokki this also drives the leave-request flow.
                context.ClinicInvitations.Add(new ClinicInvitation
                {
                    DoctorID = cast.Doctors["nadia"].DoctorID,
                    ClinicID = cast.Clinics["endocrine"].ClinicID,
                    AssistantID = cast.Assistants["heba"].AssistantID,
                    AssistantEmail = cast.Users["heba"].Email!,
                    Status = "Pending",
                    SentAtUtc = now.AddDays(-1)
                });

                await context.SaveChangesAsync();
            }

            if (!context.ClinicDoctorInvitations.Any())
            {
                // Pending doctor-to-doctor invitation — lands in Karim's notifications.
                context.ClinicDoctorInvitations.Add(new ClinicDoctorInvitation
                {
                    ClinicID = cast.Clinics["central"].ClinicID,
                    InviterDoctorID = cast.Doctors["ahmed"].DoctorID,
                    InviteeDoctorID = cast.Doctors["karim"].DoctorID,
                    InviteeEmail = cast.Users["karim"].Email!,
                    Status = "Pending",
                    SentAtUtc = now.AddDays(-2)
                });

                // Accepted history — how Mona joined MamaCare Central.
                context.ClinicDoctorInvitations.Add(new ClinicDoctorInvitation
                {
                    ClinicID = cast.Clinics["central"].ClinicID,
                    InviterDoctorID = cast.Doctors["ahmed"].DoctorID,
                    InviteeDoctorID = cast.Doctors["mona"].DoctorID,
                    InviteeEmail = cast.Users["mona"].Email!,
                    Status = "Accepted",
                    SentAtUtc = now.AddDays(-60),
                    RespondedAtUtc = now.AddDays(-59)
                });

                await context.SaveChangesAsync();
            }

            if (!context.AssistantLeaveRequests.Any())
            {
                var pendingInvitation = context.ClinicInvitations
                    .FirstOrDefault(i => i.Status == "Pending" && i.AssistantID == cast.Assistants["heba"].AssistantID);

                if (pendingInvitation != null)
                {
                    var leaveRequest = new AssistantLeaveRequest
                    {
                        AssistantID = cast.Assistants["heba"].AssistantID,
                        OldClinicID = cast.Clinics["dokki"].ClinicID,
                        NewClinicID = cast.Clinics["endocrine"].ClinicID,
                        NewDoctorID = cast.Doctors["nadia"].DoctorID,
                        ClinicInvitationID = pendingInvitation.ClinicInvitationID,
                        Status = "Pending",
                        CreatedAtUtc = now.AddHours(-20)
                    };

                    context.AssistantLeaveRequests.Add(leaveRequest);
                    await context.SaveChangesAsync();

                    // Every doctor at the old clinic must sign off before she can leave.
                    context.AssistantLeaveApprovals.Add(new AssistantLeaveApproval
                    {
                        AssistantLeaveRequestID = leaveRequest.AssistantLeaveRequestID,
                        DoctorID = cast.Doctors["omar"].DoctorID,
                        Status = "Pending"
                    });

                    await context.SaveChangesAsync();
                }
            }
        }

        // ============================================================
        // 24. APPOINTMENTS + BOOKINGS  (rolling window around today)
        // ============================================================
        /// <summary>The assistant who staffs each clinic's front desk (Endocrine &amp; Maternal has none).</summary>
        private static readonly Dictionary<string, string> ClinicDeskAssistant = new()
        {
            ["central"] = "layla",
            ["fetal"] = "dina",
            ["alex"] = "noura",
            ["dokki"] = "heba"
        };

        /// <summary>
        /// Each doctor books at times no other doctor uses, so a shared patient can never be
        /// double-booked, and a doctor is never in two clinics at the same minute.
        /// </summary>
        private static readonly (string DoctorKey, string[] ClinicRotation, TimeSpan[] BookedTimes, string[] PatientRotation)[] ScheduleDefinitions =
        {
            ("ahmed", new[] { "central", "helio" },     new[] { new TimeSpan(9, 0, 0),  new TimeSpan(10, 30, 0) }, new[] { "sarah", "fatima" }),
            ("mona",  new[] { "fetal", "central" },     new[] { new TimeSpan(9, 30, 0), new TimeSpan(11, 0, 0) },  new[] { "yasmine", "fatima" }),
            ("karim", new[] { "alex", "fetal" },        new[] { new TimeSpan(12, 0, 0), new TimeSpan(14, 30, 0) }, new[] { "hana", "reem" }),
            ("nadia", new[] { "endocrine", "alex" },    new[] { new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0) }, new[] { "reem" }),
            // Omar is still pending verification and has no approved patients — his clinic is all free slots.
            ("omar",  new[] { "dokki" },                Array.Empty<TimeSpan>(),                                   Array.Empty<string>())
        };

        /// <summary>
        /// Regenerates the appointment window whenever nothing is scheduled between today and
        /// two weeks out. A fresh database gets the full window; a database seeded weeks ago gets
        /// a new one; restarting the app on the same day changes nothing, so bookings made by hand
        /// while testing survive.
        /// </summary>
        private static async Task SeedAppointmentWindowAsync(AppDbContext context, Cast cast)
        {
            var today = DateTime.Today;
            var windowEnd = today.AddDays(FutureWindowDays - 1);

            if (context.Appointments.Any(a => a.Date >= today && a.Date <= windowEnd))
                return;

            var now = DateTime.Now;
            var appointments = new List<Appointment>();
            var bookings = new List<Booking>();

            for (int dayOffset = -PastWindowDays; dayOffset < FutureWindowDays; dayOffset++)
            {
                var date = today.AddDays(dayOffset);
                var isPast = date < today;
                var isToday = date == today;

                foreach (var schedule in ScheduleDefinitions)
                {
                    var doctorId = cast.Doctors[schedule.DoctorKey].DoctorID;

                    // Rotate the doctor through their clinics so multi-clinic doctors are exercised
                    // without ever landing two appointments on the same minute.
                    var clinicKey = schedule.ClinicRotation[Math.Abs(dayOffset) % schedule.ClinicRotation.Length];
                    var clinicId = cast.Clinics[clinicKey].ClinicID;

                    var bookedTimes = new Dictionary<TimeSpan, (int PatientId, string Status, bool CheckedIn)>();

                    for (int i = 0; i < schedule.BookedTimes.Length && schedule.PatientRotation.Length > 0; i++)
                    {
                        var time = schedule.BookedTimes[i];
                        // Alternate which patient takes which slot day to day.
                        var patientKey = schedule.PatientRotation[(i + Math.Abs(dayOffset)) % schedule.PatientRotation.Length];
                        var patientId = cast.Patients[patientKey].PatientID;

                        var (status, checkedIn) = ResolveBookingState(dayOffset, i, date.Add(time), now);
                        bookedTimes[time] = (patientId, status, checkedIn);
                    }

                    // Roughly half of the bookings at a staffed clinic were made at the desk by
                    // its assistant, rather than by the patient online.
                    int? deskAssistantId = ClinicDeskAssistant.TryGetValue(clinicKey, out var assistantKey)
                                        && cast.Assistants.TryGetValue(assistantKey, out var deskAssistant)
                        ? deskAssistant.AssistantID
                        : null;

                    foreach (var time in SlotTimes)
                    {
                        var isBookedSlot = bookedTimes.TryGetValue(time, out var booking);

                        // A cancelled booking releases the slot but keeps its (cancelled) booking row —
                        // exactly what the assistant's cancel action does.
                        var isCancelled = isBookedSlot && booking.Status == "Cancelled";

                        var appointment = new Appointment
                        {
                            DoctorID = doctorId,
                            ClinicID = clinicId,
                            PatientID = isBookedSlot ? booking.PatientId : null,
                            Date = date,
                            Time = time,
                            isBooked = isBookedSlot && !isCancelled,
                            CreatedByAssistantID = isBookedSlot && dayOffset % 2 == 0 ? deskAssistantId : null
                        };

                        appointments.Add(appointment);

                        if (isBookedSlot)
                        {
                            bookings.Add(new Booking
                            {
                                Appointment = appointment,
                                PatientID = booking.PatientId,
                                DoctorID = doctorId,
                                ClinicID = clinicId,
                                IsActive = true,
                                Status = booking.Status,
                                Reason = ReasonFor(booking.Status, isPast),
                                Notes = NotesFor(booking.Status, isPast),
                                IsCheckedIn = booking.CheckedIn,
                                CheckedInAt = booking.CheckedIn ? date.Add(time).AddMinutes(-10) : null
                            });
                        }
                    }
                }
            }

            context.Appointments.AddRange(appointments);
            context.Bookings.AddRange(bookings);
            await context.SaveChangesAsync();
        }

        /// <summary>Decides what a booking looks like based on where its slot sits relative to now.</summary>
        private static (string Status, bool CheckedIn) ResolveBookingState(int dayOffset, int slotIndex, DateTime slotStart, DateTime now)
        {
            // Past: attended and completed, except one day left un-checked-in so the
            // assistant's virtual "Missed" status has something to show.
            if (dayOffset < 0)
            {
                if (dayOffset == -3)
                    return ("Confirmed", false);   // never checked in → shows as Missed

                return ("Completed", true);
            }

            // Today: confirmed; slots whose time has already passed are checked in.
            if (dayOffset == 0)
            {
                if (slotIndex == 1)
                    return ("Confirmed", false);

                return ("Confirmed", slotStart <= now);
            }

            // One cancellation two days out, and a rescheduled ("Modified") booking every third day.
            if (dayOffset == 2 && slotIndex == 1)
                return ("Cancelled", false);

            if (dayOffset % 3 == 0)
                return ("Modified", false);

            return ("Confirmed", false);
        }

        private static string ReasonFor(string status, bool isPast) => status switch
        {
            "Cancelled" => "Patient requested cancellation",
            "Modified" => "Rescheduled follow-up",
            _ => isPast ? "Routine prenatal check-up" : "Prenatal follow-up"
        };

        private static string NotesFor(string status, bool isPast) => status switch
        {
            "Cancelled" => "Patient will rebook for a later date.",
            "Modified" => "Moved from an earlier slot at the patient's request.",
            _ => isPast ? "Visit completed. Vitals recorded." : "Patient should bring previous test results."
        };

        /// <summary>Resolved entities, keyed by the short names used throughout this file.</summary>
        private sealed class Cast
        {
            public Dictionary<string, ApplicationUser> Users { get; } = new();
            public Dictionary<string, Doctor> Doctors { get; } = new();
            public Dictionary<string, Patient> Patients { get; } = new();
            public Dictionary<string, Assistant> Assistants { get; } = new();
            public Dictionary<string, Clinic> Clinics { get; } = new();
            public Dictionary<string, AIModel> Models { get; } = new();
        }
    }
}

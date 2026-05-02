using Graduation_Project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Apply any pending migrations automatically
            await context.Database.MigrateAsync();

            // ============================================================
            // 1. ROLES (Identity)
            // ============================================================
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new IdentityRole { Name = "Admin",     NormalizedName = "ADMIN" },
                    new IdentityRole { Name = "Doctor",    NormalizedName = "DOCTOR" },
                    new IdentityRole { Name = "Patient",   NormalizedName = "PATIENT" },
                    new IdentityRole { Name = "Assistant", NormalizedName = "ASSISTANT" },
                    new IdentityRole { Name = "Lab",       NormalizedName = "LAB" }
                );
                await context.SaveChangesAsync();
            }

            var roleDoctor = context.Roles.First(r => r.Name == "Doctor");
            var rolePatient = context.Roles.First(r => r.Name == "Patient");
            var roleAssistant = context.Roles.First(r => r.Name == "Assistant");

            // ============================================================
            // 2. USERS (Identity)
            // ============================================================
            if (!context.Users.Any())
            {
                const string seedPassword = "Nabd@123";
                var passwordHasher = new PasswordHasher<ApplicationUser>();
                var passwordHash = passwordHasher.HashPassword(new ApplicationUser(), seedPassword);

                context.Users.AddRange(
                    // Doctors
                    new ApplicationUser { FirstName = "Ahmed",   LastName = "Hassan",  UserName = "ahmed.hassan@nabd.com",  NormalizedUserName = "AHMED.HASSAN@NABD.COM",  Email = "ahmed.hassan@nabd.com",  NormalizedEmail = "AHMED.HASSAN@NABD.COM",  EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01001234567", DateOfBirth = new DateTime(1975, 3, 12),  IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                    new ApplicationUser { FirstName = "Mona",    LastName = "Ibrahim", UserName = "mona.ibrahim@nabd.com",   NormalizedUserName = "MONA.IBRAHIM@NABD.COM",   Email = "mona.ibrahim@nabd.com",   NormalizedEmail = "MONA.IBRAHIM@NABD.COM",   EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01009876543", DateOfBirth = new DateTime(1980, 7, 22),  IsActive = true, CreatedDate = new DateTime(2024, 1, 2) },
                    new ApplicationUser { FirstName = "Karim",   LastName = "Mostafa", UserName = "karim.mostafa@nabd.com",  NormalizedUserName = "KARIM.MOSTAFA@NABD.COM",  Email = "karim.mostafa@nabd.com",  NormalizedEmail = "KARIM.MOSTAFA@NABD.COM",  EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01112233445", DateOfBirth = new DateTime(1978, 11, 5),  IsActive = true, CreatedDate = new DateTime(2024, 1, 3) },
                    new ApplicationUser { FirstName = "Nadia",   LastName = "Salem",   UserName = "nadia.salem@nabd.com",    NormalizedUserName = "NADIA.SALEM@NABD.COM",    Email = "nadia.salem@nabd.com",    NormalizedEmail = "NADIA.SALEM@NABD.COM",    EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01223344556", DateOfBirth = new DateTime(1982, 4, 18),  IsActive = true, CreatedDate = new DateTime(2024, 1, 4) },
                    new ApplicationUser { FirstName = "Omar",    LastName = "Fathy",   UserName = "omar.fathy@nabd.com",     NormalizedUserName = "OMAR.FATHY@NABD.COM",     Email = "omar.fathy@nabd.com",     NormalizedEmail = "OMAR.FATHY@NABD.COM",     EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01334455667", DateOfBirth = new DateTime(1976, 9, 30),  IsActive = true, CreatedDate = new DateTime(2024, 1, 5) },
                    // Patients
                    new ApplicationUser { FirstName = "Sarah",   LastName = "Ahmed",   UserName = "sarah.ahmed@nabd.com",       NormalizedUserName = "SARAH.AHMED@NABD.COM",       Email = "sarah.ahmed@nabd.com",       NormalizedEmail = "SARAH.AHMED@NABD.COM",       EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01501234567", DateOfBirth = new DateTime(1995, 6, 14),  IsActive = true, CreatedDate = new DateTime(2024, 2, 1) },
                    new ApplicationUser { FirstName = "Fatima",  LastName = "Ali",     UserName = "fatima.ali@nabd.com",        NormalizedUserName = "FATIMA.ALI@NABD.COM",        Email = "fatima.ali@nabd.com",        NormalizedEmail = "FATIMA.ALI@NABD.COM",        EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01509876543", DateOfBirth = new DateTime(1993, 8, 25),  IsActive = true, CreatedDate = new DateTime(2024, 2, 5) },
                    new ApplicationUser { FirstName = "Yasmine", LastName = "Mahmoud", UserName = "yasmine.mahmoud@nabd.com",   NormalizedUserName = "YASMINE.MAHMOUD@NABD.COM",   Email = "yasmine.mahmoud@nabd.com",   NormalizedEmail = "YASMINE.MAHMOUD@NABD.COM",   EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01512233445", DateOfBirth = new DateTime(1997, 2, 10),  IsActive = true, CreatedDate = new DateTime(2024, 2, 10) },
                    new ApplicationUser { FirstName = "Hana",    LastName = "Khaled",  UserName = "hana.khaled@nabd.com",       NormalizedUserName = "HANA.KHALED@NABD.COM",       Email = "hana.khaled@nabd.com",       NormalizedEmail = "HANA.KHALED@NABD.COM",       EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01523344556", DateOfBirth = new DateTime(1991, 12, 3),  IsActive = true, CreatedDate = new DateTime(2024, 2, 15) },
                    new ApplicationUser { FirstName = "Reem",    LastName = "Nasser",  UserName = "reem.nasser@nabd.com",       NormalizedUserName = "REEM.NASSER@NABD.COM",       Email = "reem.nasser@nabd.com",       NormalizedEmail = "REEM.NASSER@NABD.COM",       EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01534455667", DateOfBirth = new DateTime(1996, 5, 20),  IsActive = true, CreatedDate = new DateTime(2024, 2, 20) },
                    // Assistants
                    new ApplicationUser { FirstName = "Layla",   LastName = "Omar",    UserName = "layla.omar@nabd.com",     NormalizedUserName = "LAYLA.OMAR@NABD.COM",     Email = "layla.omar@nabd.com",     NormalizedEmail = "LAYLA.OMAR@NABD.COM",     EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01601234567", DateOfBirth = new DateTime(1990, 1, 8),   IsActive = true, CreatedDate = new DateTime(2024, 1, 10) },
                    new ApplicationUser { FirstName = "Dina",    LastName = "Samir",   UserName = "dina.samir@nabd.com",     NormalizedUserName = "DINA.SAMIR@NABD.COM",     Email = "dina.samir@nabd.com",     NormalizedEmail = "DINA.SAMIR@NABD.COM",     EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01609876543", DateOfBirth = new DateTime(1992, 3, 17),  IsActive = true, CreatedDate = new DateTime(2024, 1, 11) },
                    new ApplicationUser { FirstName = "Noura",   LastName = "Youssef", UserName = "noura.youssef@nabd.com",  NormalizedUserName = "NOURA.YOUSSEF@NABD.COM",  Email = "noura.youssef@nabd.com",  NormalizedEmail = "NOURA.YOUSSEF@NABD.COM",  EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01612233445", DateOfBirth = new DateTime(1988, 7, 29),  IsActive = true, CreatedDate = new DateTime(2024, 1, 12) },
                    new ApplicationUser { FirstName = "Amira",   LastName = "Tarek",   UserName = "amira.tarek@nabd.com",    NormalizedUserName = "AMIRA.TAREK@NABD.COM",    Email = "amira.tarek@nabd.com",    NormalizedEmail = "AMIRA.TAREK@NABD.COM",    EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01622334456", DateOfBirth = new DateTime(1991, 5, 14),  IsActive = true, CreatedDate = new DateTime(2024, 1, 13) },
                    new ApplicationUser { FirstName = "Heba",    LastName = "Adel",    UserName = "heba.adel@nabd.com",      NormalizedUserName = "HEBA.ADEL@NABD.COM",      Email = "heba.adel@nabd.com",      NormalizedEmail = "HEBA.ADEL@NABD.COM",      EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01632445567", DateOfBirth = new DateTime(1994, 10, 2),  IsActive = true, CreatedDate = new DateTime(2024, 1, 14) },
                    // Admin
                    new ApplicationUser { FirstName = "System",  LastName = "Admin",   UserName = "admin@nabd.com",           NormalizedUserName = "ADMIN@NABD.COM",           Email = "admin@nabd.com",           NormalizedEmail = "ADMIN@NABD.COM",           EmailConfirmed = true, PasswordHash = passwordHash, PhoneNumber = "01000000000", DateOfBirth = new DateTime(1990, 1, 1),   IsActive = true, CreatedDate = new DateTime(2024, 1, 1)  }
                );
                await context.SaveChangesAsync();
            }

            // Normalize any legacy mixed users (old domains) to @nabd.com and unify password
            {
                const string seedPassword = "Nabd@123";
                var passwordHasher = new PasswordHasher<ApplicationUser>();

                var emailMap = new Dictionary<string, string>
                {
                    ["ahmed.hassan@mamacare.com"] = "ahmed.hassan@nabd.com",
                    ["mona.ibrahim@mamacare.com"] = "mona.ibrahim@nabd.com",
                    ["karim.mostafa@mamacare.com"] = "karim.mostafa@nabd.com",
                    ["nadia.salem@mamacare.com"] = "nadia.salem@nabd.com",
                    ["omar.fathy@mamacare.com"] = "omar.fathy@nabd.com",
                    ["sarah.ahmed@gmail.com"] = "sarah.ahmed@nabd.com",
                    ["fatima.ali@gmail.com"] = "fatima.ali@nabd.com",
                    ["yasmine.mahmoud@gmail.com"] = "yasmine.mahmoud@nabd.com",
                    ["hana.khaled@gmail.com"] = "hana.khaled@nabd.com",
                    ["reem.nasser@gmail.com"] = "reem.nasser@nabd.com",
                    ["layla.omar@mamacare.com"] = "layla.omar@nabd.com",
                    ["dina.samir@mamacare.com"] = "dina.samir@nabd.com",
                    ["noura.youssef@mamacare.com"] = "noura.youssef@nabd.com",
                    ["amira.tarek@mamacare.com"] = "amira.tarek@nabd.com",
                    ["heba.adel@mamacare.com"] = "heba.adel@nabd.com"
                };

                foreach (var pair in emailMap)
                {
                    var oldEmail = pair.Key;
                    var newEmail = pair.Value;

                    var legacyUser = context.Users.FirstOrDefault(u => u.Email == oldEmail);
                    var nabdUser = context.Users.FirstOrDefault(u => u.Email == newEmail);

                    if (legacyUser != null && nabdUser == null)
                    {
                        legacyUser.Email = newEmail;
                        legacyUser.UserName = newEmail;
                        legacyUser.NormalizedEmail = newEmail.ToUpperInvariant();
                        legacyUser.NormalizedUserName = newEmail.ToUpperInvariant();
                        legacyUser.PasswordHash = passwordHasher.HashPassword(legacyUser, seedPassword);
                        continue;
                    }

                    if (legacyUser != null && nabdUser != null && legacyUser.Id != nabdUser.Id)
                    {
                        var doctor = context.Doctors.FirstOrDefault(d => d.UserID == legacyUser.Id);
                        if (doctor != null) doctor.UserID = nabdUser.Id;

                        var patient = context.Patients.FirstOrDefault(p => p.UserID == legacyUser.Id);
                        if (patient != null) patient.UserID = nabdUser.Id;

                        var assistant = context.Assistants.FirstOrDefault(a => a.UserID == legacyUser.Id);
                        if (assistant != null) assistant.UserID = nabdUser.Id;

                        var legacyRoles = context.UserRoles.Where(ur => ur.UserId == legacyUser.Id).ToList();
                        if (legacyRoles.Count > 0)
                            context.UserRoles.RemoveRange(legacyRoles);

                        context.Users.Remove(legacyUser);
                    }

                    if (nabdUser != null)
                    {
                        nabdUser.PasswordHash = passwordHasher.HashPassword(nabdUser, seedPassword);
                    }
                }

                await context.SaveChangesAsync();
            }

            if (!context.UserRoles.Any())
            {
                var appUsers = context.Users.ToList();
                var identityUserRoles =
                    appUsers.Where(u => (u.FirstName == "Ahmed" || u.FirstName == "Mona" || u.FirstName == "Karim" || u.FirstName == "Nadia" || u.FirstName == "Omar"))
                        .Select(u => new IdentityUserRole<string> { UserId = u.Id, RoleId = roleDoctor.Id })
                        .Concat(appUsers.Where(u => (u.FirstName == "Sarah" || u.FirstName == "Fatima" || u.FirstName == "Yasmine" || u.FirstName == "Hana" || u.FirstName == "Reem"))
                            .Select(u => new IdentityUserRole<string> { UserId = u.Id, RoleId = rolePatient.Id }))
                        .Concat(appUsers.Where(u => (u.FirstName == "Layla" || u.FirstName == "Dina" || u.FirstName == "Noura" || u.FirstName == "Amira" || u.FirstName == "Heba"))
                            .Select(u => new IdentityUserRole<string> { UserId = u.Id, RoleId = roleAssistant.Id }))
                        .Concat(appUsers.Where(u => u.Email == "admin@nabd.com")
                            .Select(u => new IdentityUserRole<string> { UserId = u.Id, RoleId = context.Roles.First(r => r.Name == "Admin").Id }))
                        .ToList();

                context.UserRoles.AddRange(identityUserRoles);
                await context.SaveChangesAsync();
            }

            // Fetch users by email so we can wire up foreign keys
            var uAhmed   = context.Users.First(u => u.Email == "ahmed.hassan@nabd.com");
            var uMona    = context.Users.First(u => u.Email == "mona.ibrahim@nabd.com");
            var uKarim   = context.Users.First(u => u.Email == "karim.mostafa@nabd.com");
            var uNadia   = context.Users.First(u => u.Email == "nadia.salem@nabd.com");
            var uOmar    = context.Users.First(u => u.Email == "omar.fathy@nabd.com");
            var uSarah   = context.Users.First(u => u.Email == "sarah.ahmed@nabd.com");
            var uFatima  = context.Users.First(u => u.Email == "fatima.ali@nabd.com");
            var uYasmine = context.Users.First(u => u.Email == "yasmine.mahmoud@nabd.com");
            var uHana    = context.Users.First(u => u.Email == "hana.khaled@nabd.com");
            var uReem    = context.Users.First(u => u.Email == "reem.nasser@nabd.com");
            var uLayla   = context.Users.First(u => u.Email == "layla.omar@nabd.com");
            var uDina    = context.Users.First(u => u.Email == "dina.samir@nabd.com");
            var uNoura   = context.Users.First(u => u.Email == "noura.youssef@nabd.com");
            var uAmira   = context.Users.First(u => u.Email == "amira.tarek@nabd.com");
            var uHeba    = context.Users.First(u => u.Email == "heba.adel@nabd.com");

            // ============================================================
            // 3. DOCTORS
            // ============================================================
            if (!context.Doctors.Any())
            {
                context.Doctors.AddRange(
                    new Doctor { UserID = uAhmed.UserID,  Specialization = "Obstetrics & Gynecology", LicenseNumber = "MD-OBG-001", LicenseImagePath = "/uploads/licenses/lic1.jpg", VerificationStatus = "Verified", VerificationDate = new DateTime(2024, 1, 15), Address = "15 Tahrir St, Cairo"     },
                    new Doctor { UserID = uMona.UserID,   Specialization = "Maternal-Fetal Medicine",  LicenseNumber = "MD-MFM-002", LicenseImagePath = "/uploads/licenses/lic2.jpg", VerificationStatus = "Verified", VerificationDate = new DateTime(2024, 1, 16), Address = "22 Nasr City, Cairo"     },
                    new Doctor { UserID = uKarim.UserID,  Specialization = "Obstetrics & Gynecology", LicenseNumber = "MD-OBG-003", LicenseImagePath = "/uploads/licenses/lic3.jpg", VerificationStatus = "Verified", VerificationDate = new DateTime(2024, 1, 17), Address = "7 Corniche, Alexandria"  },
                    new Doctor { UserID = uNadia.UserID,  Specialization = "Endocrinology",            LicenseNumber = "MD-END-004", LicenseImagePath = "/uploads/licenses/lic4.jpg", VerificationStatus = "Verified", VerificationDate = new DateTime(2024, 1, 18), Address = "30 Heliopolis, Cairo"    },
                    new Doctor { UserID = uOmar.UserID,   Specialization = "Internal Medicine",        LicenseNumber = "MD-INT-005", LicenseImagePath = "/uploads/licenses/lic5.jpg", VerificationStatus = "Pending",  VerificationDate = null,                       Address = "5 Dokki, Giza"           }
                );
                await context.SaveChangesAsync();
            }

            var dAhmed  = context.Doctors.First(d => d.UserID == uAhmed.UserID);
            var dMona   = context.Doctors.First(d => d.UserID == uMona.UserID);
            var dKarim  = context.Doctors.First(d => d.UserID == uKarim.UserID);
            var dNadia  = context.Doctors.First(d => d.UserID == uNadia.UserID);
            var dOmar   = context.Doctors.First(d => d.UserID == uOmar.UserID);

            // ============================================================
            // 4. CLINICS
            // ============================================================
            if (!context.Clinics.Any())
            {
                context.Clinics.AddRange(
                    new Clinic { Name = "MamaCare Central",      Location = "15 Tahrir St, Cairo"     },
                    new Clinic { Name = "MamaCare Heliopolis",   Location = "30 Heliopolis, Cairo"    },
                    new Clinic { Name = "Fetal Health Clinic",   Location = "22 Nasr City, Cairo"    },
                    new Clinic { Name = "Alexandria OBG Center", Location = "7 Corniche, Alexandria"  },
                    new Clinic { Name = "Endocrine & Maternal",  Location = "30 Heliopolis, Cairo"    },
                    new Clinic { Name = "Dokki General Clinic",  Location = "5 Dokki, Giza"           }
                );
                await context.SaveChangesAsync();
            }

            var cCentral   = context.Clinics.First(c => c.Name == "MamaCare Central");
            var cHelio     = context.Clinics.First(c => c.Name == "MamaCare Heliopolis");
            var cFetal     = context.Clinics.First(c => c.Name == "Fetal Health Clinic");
            var cAlex      = context.Clinics.First(c => c.Name == "Alexandria OBG Center");
            var cEndocrine = context.Clinics.First(c => c.Name == "Endocrine & Maternal");
            var cDokki     = context.Clinics.First(c => c.Name == "Dokki General Clinic");

            // ============================================================
            // 4b. CLINIC-DOCTOR RELATIONSHIPS
            // ============================================================
            if (!context.ClinicDoctors.Any())
            {
                context.ClinicDoctors.AddRange(
                    new ClinicDoctor { ClinicID = cCentral.ClinicID,   DoctorID = dAhmed.DoctorID },
                    new ClinicDoctor { ClinicID = cHelio.ClinicID,     DoctorID = dAhmed.DoctorID },
                    new ClinicDoctor { ClinicID = cFetal.ClinicID,     DoctorID = dMona.DoctorID  },
                    new ClinicDoctor { ClinicID = cAlex.ClinicID,      DoctorID = dKarim.DoctorID },
                    new ClinicDoctor { ClinicID = cEndocrine.ClinicID, DoctorID = dNadia.DoctorID },
                    new ClinicDoctor { ClinicID = cDokki.ClinicID,     DoctorID = dOmar.DoctorID  },
                    // Multi-doctor clinics
                    new ClinicDoctor { ClinicID = cCentral.ClinicID,   DoctorID = dMona.DoctorID  },
                    new ClinicDoctor { ClinicID = cFetal.ClinicID,     DoctorID = dKarim.DoctorID },
                    new ClinicDoctor { ClinicID = cAlex.ClinicID,      DoctorID = dNadia.DoctorID }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 5. ASSISTANTS
            // ============================================================
            if (!context.Assistants.Any())
            {
                context.Assistants.AddRange(
                    new Assistant { UserID = uLayla.UserID, ClinicID = cCentral.ClinicID },
                    new Assistant { UserID = uDina.UserID,  ClinicID = cFetal.ClinicID   },
                    new Assistant { UserID = uNoura.UserID, ClinicID = cAlex.ClinicID    },
                    new Assistant { UserID = uAmira.UserID, ClinicID = cCentral.ClinicID },  // second assistant at Central — no AssistantDoctor records (fallback)
                    new Assistant { UserID = uHeba.UserID,  ClinicID = cDokki.ClinicID   }   // clinic with pending-verification doctor, no approved patients
                );
                await context.SaveChangesAsync();
            }

            var aLayla = context.Assistants.First(a => a.UserID == uLayla.UserID);
            var aDina  = context.Assistants.First(a => a.UserID == uDina.UserID);
            var aNoura = context.Assistants.First(a => a.UserID == uNoura.UserID);
            var aAmira = context.Assistants.First(a => a.UserID == uAmira.UserID);
            var aHeba  = context.Assistants.First(a => a.UserID == uHeba.UserID);

            // ============================================================
            // 5b. ASSISTANT-DOCTOR RELATIONSHIPS
            // ============================================================
            if (!context.AssistantDoctors.Any())
            {
                context.AssistantDoctors.AddRange(
                    // Layla: handles Ahmed + Mona (full overlap at Central which has Ahmed + Mona)
                    new AssistantDoctor { AssistantID = aLayla.AssistantID, DoctorID = dAhmed.DoctorID },
                    new AssistantDoctor { AssistantID = aLayla.AssistantID, DoctorID = dMona.DoctorID  },
                    // Dina: handles Mona + Karim (full overlap at Fetal which has Mona + Karim)
                    new AssistantDoctor { AssistantID = aDina.AssistantID,  DoctorID = dMona.DoctorID  },
                    new AssistantDoctor { AssistantID = aDina.AssistantID,  DoctorID = dKarim.DoctorID },
                    // Noura: handles only Karim (partial overlap — Alex has Karim + Nadia, Noura sees only Karim)
                    new AssistantDoctor { AssistantID = aNoura.AssistantID, DoctorID = dKarim.DoctorID },
                    // Heba: handles Omar (single doctor at Dokki, pending verification, no approved patients)
                    new AssistantDoctor { AssistantID = aHeba.AssistantID,  DoctorID = dOmar.DoctorID  }
                    // NOTE: Amira has NO AssistantDoctor entries ? dashboard falls back to all clinic doctors
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 6. PATIENTS
            // ============================================================
            if (!context.Patients.Any())
            {
                context.Patients.AddRange(
                    new Patient { UserID = uSarah.UserID,   Address = "12 Maadi, Cairo",       DateOfPregnancy = new DateTime(2024, 11, 1),  GestationalWeeks = 24, IsFirstPregnancy = false, PreviousPregnancies = 1, Abortions = 0, Births = 1, WeightKg = 68.0, HeightCm = 162.0, BloodPressureIssue = false, Smoking = false, AlcoholUse = false },
                    new Patient { UserID = uFatima.UserID,  Address = "5 Zamalek, Cairo",       DateOfPregnancy = new DateTime(2024, 9, 15),  GestationalWeeks = 32, IsFirstPregnancy = true,  PreviousPregnancies = 0, Abortions = 0, Births = 0, WeightKg = 72.0, HeightCm = 158.0, BloodPressureIssue = true,  Smoking = false, AlcoholUse = false },
                    new Patient { UserID = uYasmine.UserID, Address = "18 New Cairo",            DateOfPregnancy = new DateTime(2025, 1, 10),  GestationalWeeks = 12, IsFirstPregnancy = true,  PreviousPregnancies = 0, Abortions = 0, Births = 0, WeightKg = 60.0, HeightCm = 165.0, BloodPressureIssue = false, Smoking = false, AlcoholUse = false },
                    new Patient { UserID = uHana.UserID,    Address = "9 Shubra, Cairo",         DateOfPregnancy = new DateTime(2024, 8, 20),  GestationalWeeks = 36, IsFirstPregnancy = false, PreviousPregnancies = 2, Abortions = 1, Births = 1, WeightKg = 80.0, HeightCm = 160.0, BloodPressureIssue = true,  Smoking = false, AlcoholUse = false },
                    new Patient { UserID = uReem.UserID,    Address = "3 Mohandessin, Giza",     DateOfPregnancy = new DateTime(2024, 12, 5),  GestationalWeeks = 20, IsFirstPregnancy = false, PreviousPregnancies = 1, Abortions = 0, Births = 1, WeightKg = 65.0, HeightCm = 170.0, BloodPressureIssue = false, Smoking = false, AlcoholUse = false }
                );
                await context.SaveChangesAsync();
            }

            var pSarah   = context.Patients.First(p => p.UserID == uSarah.UserID);
            var pFatima  = context.Patients.First(p => p.UserID == uFatima.UserID);
            var pYasmine = context.Patients.First(p => p.UserID == uYasmine.UserID);
            var pHana    = context.Patients.First(p => p.UserID == uHana.UserID);
            var pReem    = context.Patients.First(p => p.UserID == uReem.UserID);

            // ============================================================
            // 7. AI MODELS
            // ============================================================
            if (!context.AIModels.Any())
            {
                context.AIModels.AddRange(
                    new AIModel { ModelName = "CBC Analyzer v2",        ModelType = "CBC",        ModelVersion = "2.1.0", ModelFilePath = "/models/cbc_v2.pkl",      Accuracy = 96.5, DateTrained = new DateTime(2024, 6, 1)  },
                    new AIModel { ModelName = "Ultrasound Detector v3", ModelType = "Ultrasound", ModelVersion = "3.0.1", ModelFilePath = "/models/us_v3.h5",        Accuracy = 94.2, DateTrained = new DateTime(2024, 7, 15) },
                    new AIModel { ModelName = "Blood Sugar Predictor",  ModelType = "BloodSugar", ModelVersion = "1.5.0", ModelFilePath = "/models/bsugar_v1.pkl",   Accuracy = 91.8, DateTrained = new DateTime(2024, 8, 10) },
                    new AIModel { ModelName = "TSH Classifier",         ModelType = "TSH",        ModelVersion = "1.2.3", ModelFilePath = "/models/tsh_v1.pkl",      Accuracy = 93.0, DateTrained = new DateTime(2024, 9, 5)  },
                    new AIModel { ModelName = "Ferritin Level AI",      ModelType = "Ferritin",   ModelVersion = "1.0.0", ModelFilePath = "/models/ferritin_v1.pkl", Accuracy = 89.7, DateTrained = new DateTime(2024, 10, 1) }
                );
                await context.SaveChangesAsync();
            }

            var aiCbc       = context.AIModels.First(a => a.ModelType == "CBC");
            var aiUs        = context.AIModels.First(a => a.ModelType == "Ultrasound");
            var aiTsh       = context.AIModels.First(a => a.ModelType == "TSH");
            var aiFerritin  = context.AIModels.First(a => a.ModelType == "Ferritin");

            // ============================================================
            // 8. PATIENT <-> DOCTOR
            // ============================================================
            if (!context.PatientDoctors.Any())
            {
                context.PatientDoctors.AddRange(
                    new PatientDoctor { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   Status = "Approved", RequestDate = new DateTime(2024, 11, 5), ResponseDate = new DateTime(2024, 11, 6), IsPrimary = true  },
                    new PatientDoctor { DoctorID = dAhmed.DoctorID, PatientID = pFatima.PatientID,  Status = "Approved", RequestDate = new DateTime(2024, 9, 20),  ResponseDate = new DateTime(2024, 9, 21),  IsPrimary = true  },
                    new PatientDoctor { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, Status = "Approved", RequestDate = new DateTime(2025, 1, 12),  ResponseDate = new DateTime(2025, 1, 13),  IsPrimary = true  },
                    new PatientDoctor { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    Status = "Approved", RequestDate = new DateTime(2024, 8, 25),  ResponseDate = new DateTime(2024, 8, 26),  IsPrimary = true  },
                    new PatientDoctor { DoctorID = dNadia.DoctorID, PatientID = pReem.PatientID,    Status = "Approved", RequestDate = new DateTime(2024, 12, 8),  ResponseDate = new DateTime(2024, 12, 9),  IsPrimary = true  },
                    new PatientDoctor { DoctorID = dMona.DoctorID,  PatientID = pSarah.PatientID,   Status = "Pending",  RequestDate = new DateTime(2025, 1, 20),  ResponseDate = null,                        IsPrimary = false },
                    // Shared patients across doctors at the same clinic
                    new PatientDoctor { DoctorID = dMona.DoctorID,  PatientID = pFatima.PatientID,  Status = "Approved", RequestDate = new DateTime(2025, 2, 1),   ResponseDate = new DateTime(2025, 2, 2),    IsPrimary = false },
                    new PatientDoctor { DoctorID = dKarim.DoctorID, PatientID = pReem.PatientID,    Status = "Approved", RequestDate = new DateTime(2025, 2, 10),  ResponseDate = new DateTime(2025, 2, 11),   IsPrimary = false },
                    // Pending requests (doctor has no approved patients / cross-clinic referrals)
                    new PatientDoctor { DoctorID = dOmar.DoctorID,  PatientID = pYasmine.PatientID, Status = "Pending",  RequestDate = new DateTime(2025, 3, 1),   ResponseDate = null,                        IsPrimary = false },
                    new PatientDoctor { DoctorID = dAhmed.DoctorID, PatientID = pHana.PatientID,    Status = "Pending",  RequestDate = new DateTime(2025, 3, 5),   ResponseDate = null,                        IsPrimary = false }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 9. PATIENT DRUGS
            // ============================================================
            if (!context.PatientDrugs.Any())
            {
                context.PatientDrugs.AddRange(
                    new PatientDrug { PatientID = pSarah.PatientID,   DrugName = "Folic Acid",       DurationMonths = 9, Reason = "Neural tube prevention",   DoseMgPerDay = 0.4    },
                    new PatientDrug { PatientID = pSarah.PatientID,   DrugName = "Iron Supplement",  DurationMonths = 6, Reason = "Iron deficiency anemia",    DoseMgPerDay = 30.0   },
                    new PatientDrug { PatientID = pFatima.PatientID,  DrugName = "Labetalol",        DurationMonths = 5, Reason = "Gestational hypertension",  DoseMgPerDay = 200.0  },
                    new PatientDrug { PatientID = pFatima.PatientID,  DrugName = "Calcium Carbonate",DurationMonths = 7, Reason = "Calcium supplementation",   DoseMgPerDay = 1000.0 },
                    new PatientDrug { PatientID = pYasmine.PatientID, DrugName = "Folic Acid",       DurationMonths = 9, Reason = "Neural tube prevention",    DoseMgPerDay = 0.4    },
                    new PatientDrug { PatientID = pHana.PatientID,    DrugName = "Methyldopa",       DurationMonths = 4, Reason = "Chronic hypertension",      DoseMgPerDay = 500.0  },
                    new PatientDrug { PatientID = pHana.PatientID,    DrugName = "Aspirin Low-Dose", DurationMonths = 6, Reason = "Preeclampsia prevention",   DoseMgPerDay = 81.0   },
                    new PatientDrug { PatientID = pReem.PatientID,    DrugName = "Vitamin D3",       DurationMonths = 9, Reason = "Vitamin D deficiency",      DoseMgPerDay = 1000.0 },
                    new PatientDrug { PatientID = pReem.PatientID,    DrugName = "Magnesium",        DurationMonths = 3, Reason = "Leg cramps in pregnancy",   DoseMgPerDay = 350.0  },
                    new PatientDrug { PatientID = pYasmine.PatientID, DrugName = "Iron Supplement",  DurationMonths = 4, Reason = "Mild anemia",               DoseMgPerDay = 20.0   }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 10. APPOINTMENTS
            // ============================================================
            if (!context.Appointments.Any())
            {
                context.Appointments.AddRange(
                    new Appointment { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   ClinicID = cCentral.ClinicID,   Date = new DateTime(2025, 3, 20), Time = new TimeSpan(10, 0, 0),  isBooked = true  },
                    new Appointment { DoctorID = dAhmed.DoctorID, PatientID = pFatima.PatientID,  ClinicID = cCentral.ClinicID,   Date = new DateTime(2025, 3, 21), Time = new TimeSpan(11, 0, 0),  isBooked = true  },
                    new Appointment { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, ClinicID = cFetal.ClinicID,     Date = new DateTime(2025, 3, 22), Time = new TimeSpan(9, 30, 0),  isBooked = true  },
                    new Appointment { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    ClinicID = cAlex.ClinicID,      Date = new DateTime(2025, 3, 25), Time = new TimeSpan(14, 0, 0),  isBooked = true  },
                    new Appointment { DoctorID = dNadia.DoctorID, PatientID = pReem.PatientID,    ClinicID = cEndocrine.ClinicID, Date = new DateTime(2025, 3, 26), Time = new TimeSpan(10, 30, 0), isBooked = true  },
                    new Appointment { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   ClinicID = cHelio.ClinicID,     Date = new DateTime(2025, 4, 5),  Time = new TimeSpan(9, 0, 0),   isBooked = false },
                    new Appointment { DoctorID = dMona.DoctorID,  PatientID = pFatima.PatientID,  ClinicID = cFetal.ClinicID,     Date = new DateTime(2025, 4, 8),  Time = new TimeSpan(11, 30, 0), isBooked = false },
                    new Appointment { DoctorID = dOmar.DoctorID,  PatientID = pYasmine.PatientID, ClinicID = cDokki.ClinicID,     Date = new DateTime(2025, 4, 10), Time = new TimeSpan(12, 0, 0),  isBooked = false },
                    new Appointment { DoctorID = dKarim.DoctorID, PatientID = pReem.PatientID,    ClinicID = cAlex.ClinicID,      Date = new DateTime(2025, 4, 12), Time = new TimeSpan(15, 0, 0),  isBooked = false },
                    new Appointment { DoctorID = dNadia.DoctorID, PatientID = pHana.PatientID,    ClinicID = cEndocrine.ClinicID, Date = new DateTime(2025, 4, 15), Time = new TimeSpan(10, 0, 0),  isBooked = false },
                    // ?? Today's appointments: Central clinic (Layla handles Ahmed+Mona, Amira sees all via fallback) ??
                    new Appointment { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   ClinicID = cCentral.ClinicID,   Date = DateTime.Today, Time = new TimeSpan(9,  0,  0), isBooked = true  },
                    new Appointment { DoctorID = dAhmed.DoctorID, PatientID = pFatima.PatientID,  ClinicID = cCentral.ClinicID,   Date = DateTime.Today, Time = new TimeSpan(10, 30, 0), isBooked = true  },
                    new Appointment { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, ClinicID = cCentral.ClinicID,   Date = DateTime.Today, Time = new TimeSpan(11, 0,  0), isBooked = true  },
                    new Appointment { DoctorID = dMona.DoctorID,  PatientID = pFatima.PatientID,  ClinicID = cCentral.ClinicID,   Date = DateTime.Today, Time = new TimeSpan(14, 0,  0), isBooked = true  },
                    new Appointment { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   ClinicID = cCentral.ClinicID,   Date = DateTime.Today, Time = new TimeSpan(15, 30, 0), isBooked = false },
                    // ?? Today's appointments: Fetal Health clinic (Dina handles Mona+Karim) ??
                    new Appointment { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, ClinicID = cFetal.ClinicID,     Date = DateTime.Today, Time = new TimeSpan(9,  30, 0), isBooked = true  },
                    new Appointment { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    ClinicID = cFetal.ClinicID,     Date = DateTime.Today, Time = new TimeSpan(11, 0,  0), isBooked = true  },
                    // ?? Today's appointments: Alexandria clinic (Noura handles only Karim — Nadia's appointment hidden) ??
                    new Appointment { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    ClinicID = cAlex.ClinicID,      Date = DateTime.Today, Time = new TimeSpan(14, 0,  0), isBooked = true  },
                    new Appointment { DoctorID = dNadia.DoctorID, PatientID = pReem.PatientID,    ClinicID = cAlex.ClinicID,      Date = DateTime.Today, Time = new TimeSpan(15, 30, 0), isBooked = true  },
                    // ?? Today's appointments: Endocrine clinic (no assistant assigned) ??
                    new Appointment { DoctorID = dNadia.DoctorID, PatientID = pReem.PatientID,    ClinicID = cEndocrine.ClinicID, Date = DateTime.Today, Time = new TimeSpan(10, 0,  0), isBooked = true  },
                    // ?? Today's appointments: Dokki clinic (Heba — Omar has no approved patients, open slots) ??
                    new Appointment { DoctorID = dOmar.DoctorID,  PatientID = pYasmine.PatientID, ClinicID = cDokki.ClinicID,     Date = DateTime.Today, Time = new TimeSpan(12, 0,  0), isBooked = false },
                    new Appointment { DoctorID = dOmar.DoctorID,  PatientID = pHana.PatientID,    ClinicID = cDokki.ClinicID,     Date = DateTime.Today, Time = new TimeSpan(14, 0,  0), isBooked = false }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 11. BOOKINGS  (one per booked appointment)
            // ============================================================
            if (!context.Bookings.Any())
            {
                var bookedAppts = context.Appointments.Where(a => a.isBooked).OrderBy(a => a.Date).ThenBy(a => a.Time).ToList();
                var reasons = new[] { "Routine prenatal check-up", "Blood pressure follow-up", "First trimester consultation", "Third trimester check-up", "Diabetes screening follow-up", "Pregnancy monitoring", "Ultrasound follow-up", "Lab results review", "General check-up", "Gestational diabetes follow-up", "Fetal monitoring", "Growth scan review", "Prenatal screening", "Pre-delivery assessment", "Weekly follow-up", "Medication review" };
                var notesArr = new[] { "Patient should bring previous test results", "Monitor BP readings from last week", "Discuss first trimester screening results", "Review birth plan options", "Review GTT results", "Follow-up on previous visit", "Check ultrasound measurements", "Review lab test results", "Standard prenatal visit", "Monitor blood sugar levels", "Check fetal heart rate", "Compare growth measurements", "Discuss screening options", "Pre-delivery checklist", "Weekly progress check", "Review current medications" };
                var statuses = new[] { "Confirmed", "Confirmed", "Confirmed", "Confirmed", "Confirmed", "Modified", "Confirmed", "Confirmed", "Confirmed", "Confirmed", "Confirmed", "Confirmed", "Cancelled", "Confirmed", "Confirmed", "Confirmed" };

                for (int i = 0; i < bookedAppts.Count; i++)
                {
                    context.Bookings.Add(new Booking
                    {
                        AppointmentID = bookedAppts[i].AppointmentID,
                        PatientID     = bookedAppts[i].PatientID!.Value,
                        DoctorID      = bookedAppts[i].DoctorID,
                        ClinicID      = bookedAppts[i].ClinicID,
                        Status        = statuses[i % statuses.Length],
                        Reason        = reasons[i % reasons.Length],
                        Notes         = notesArr[i % notesArr.Length]
                    });
                }
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 12. TEST REPORTS  (needed before LabTests)
            // ============================================================
            if (!context.TestReports.Any())
            {
                context.TestReports.AddRange(
                    new TestReport { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ReportDate = new DateTime(2025, 3, 16), OverallStatus = "Normal",             ConfidenceScore = 96.5, AISummary = "All blood parameters within normal ranges for week 24. Hemoglobin slightly low; monitor iron.",          DoctorInterpretation = "Recommend continuing iron supplementation."    },
                    new TestReport { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ReportDate = new DateTime(2025, 3, 10), OverallStatus = "Requires Attention", ConfidenceScore = 91.2, AISummary = "Elevated WBC count. TSH mildly elevated. Blood pressure trend concerning.",                             DoctorInterpretation = "Increase monitoring frequency to bi-weekly."  },
                    new TestReport { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ReportDate = new DateTime(2025, 2, 28), OverallStatus = "Normal",             ConfidenceScore = 98.1, AISummary = "First trimester panel shows all values within expected ranges.",                                         DoctorInterpretation = "Patient is progressing well."                  },
                    new TestReport { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ReportDate = new DateTime(2025, 3, 1),  OverallStatus = "Abnormal",           ConfidenceScore = 88.4, AISummary = "Low hemoglobin and ferritin. Iron deficiency anemia. Urinalysis shows trace protein.",                  DoctorInterpretation = "Start IV iron therapy and repeat CBC in 2 weeks." },
                    new TestReport { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ReportDate = new DateTime(2025, 3, 5),  OverallStatus = "Requires Attention", ConfidenceScore = 93.7, AISummary = "HbA1c slightly above target for gestational diabetes. Fasting blood glucose elevated.",                 DoctorInterpretation = "Adjust dietary plan and increase monitoring."  }
                );
                await context.SaveChangesAsync();
            }

            var rSarah   = context.TestReports.First(r => r.PatientID == pSarah.PatientID);
            var rFatima  = context.TestReports.First(r => r.PatientID == pFatima.PatientID);
            var rYasmine = context.TestReports.First(r => r.PatientID == pYasmine.PatientID);
            var rHana    = context.TestReports.First(r => r.PatientID == pHana.PatientID);
            var rReem    = context.TestReports.First(r => r.PatientID == pReem.PatientID);

            // ============================================================
            // 13. LAB TESTS
            // ============================================================
            if (!context.LabTests.Any())
            {
                context.LabTests.AddRange(
                    // CBC
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = aiCbc.ModelID,      ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/cbc_p1.jpg",      AI_AnalysisJSON = "{\"status\":\"normal\"}",   TestType = "CBC"        },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = aiCbc.ModelID,      ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/cbc_p2.jpg",      AI_AnalysisJSON = "{\"status\":\"abnormal\"}", TestType = "CBC"        },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = aiCbc.ModelID,      ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/cbc_p3.jpg",      AI_AnalysisJSON = "{\"status\":\"normal\"}",   TestType = "CBC"        },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = aiCbc.ModelID,      ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/cbc_p4.jpg",      AI_AnalysisJSON = "{\"status\":\"abnormal\"}", TestType = "CBC"        },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = aiCbc.ModelID,      ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/cbc_p5.jpg",      AI_AnalysisJSON = "{\"status\":\"normal\"}",   TestType = "CBC"        },
                    // BloodGroup
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/bg_p1.jpg",       AI_AnalysisJSON = "{\"group\":\"A+\"}",        TestType = "BloodGroup" },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/bg_p2.jpg",       AI_AnalysisJSON = "{\"group\":\"O+\"}",        TestType = "BloodGroup" },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = null,               ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/bg_p3.jpg",       AI_AnalysisJSON = "{\"group\":\"B+\"}",        TestType = "BloodGroup" },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = null,               ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/bg_p4.jpg",       AI_AnalysisJSON = "{\"group\":\"AB-\"}",       TestType = "BloodGroup" },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = null,               ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/bg_p5.jpg",       AI_AnalysisJSON = "{\"group\":\"O-\"}",        TestType = "BloodGroup" },
                    // HbA1c
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/hba1c_p1.jpg",    AI_AnalysisJSON = "{\"hba1c\":5.4}",           TestType = "HbA1c"      },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/hba1c_p2.jpg",    AI_AnalysisJSON = "{\"hba1c\":5.9}",           TestType = "HbA1c"      },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = null,               ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/hba1c_p3.jpg",    AI_AnalysisJSON = "{\"hba1c\":5.1}",           TestType = "HbA1c"      },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = null,               ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/hba1c_p4.jpg",    AI_AnalysisJSON = "{\"hba1c\":6.2}",           TestType = "HbA1c"      },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = null,               ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/hba1c_p5.jpg",    AI_AnalysisJSON = "{\"hba1c\":6.8}",           TestType = "HbA1c"      },
                    // Urinalysis
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/uri_p1.jpg",      AI_AnalysisJSON = "{\"protein\":\"negative\"}",TestType = "Urinalysis" },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/uri_p2.jpg",      AI_AnalysisJSON = "{\"protein\":\"trace\"}",   TestType = "Urinalysis" },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = null,               ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/uri_p3.jpg",      AI_AnalysisJSON = "{\"protein\":\"negative\"}",TestType = "Urinalysis" },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = null,               ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/uri_p4.jpg",      AI_AnalysisJSON = "{\"protein\":\"trace\"}",   TestType = "Urinalysis" },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = null,               ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/uri_p5.jpg",      AI_AnalysisJSON = "{\"protein\":\"negative\"}",TestType = "Urinalysis" },
                    // HBsAg
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/hbsag_p1.jpg",    AI_AnalysisJSON = "{\"hbsag\":\"negative\"}",  TestType = "HBsAg"      },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/hbsag_p2.jpg",    AI_AnalysisJSON = "{\"hbsag\":\"negative\"}",  TestType = "HBsAg"      },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = null,               ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/hbsag_p3.jpg",    AI_AnalysisJSON = "{\"hbsag\":\"negative\"}",  TestType = "HBsAg"      },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = null,               ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/hbsag_p4.jpg",    AI_AnalysisJSON = "{\"hbsag\":\"negative\"}",  TestType = "HBsAg"      },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = null,               ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/hbsag_p5.jpg",    AI_AnalysisJSON = "{\"hbsag\":\"negative\"}",  TestType = "HBsAg"      },
                    // HCV
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/hcv_p1.jpg",      AI_AnalysisJSON = "{\"hcv\":\"negative\"}",    TestType = "HCV"        },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = null,               ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/hcv_p2.jpg",      AI_AnalysisJSON = "{\"hcv\":\"negative\"}",    TestType = "HCV"        },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = null,               ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/hcv_p3.jpg",      AI_AnalysisJSON = "{\"hcv\":\"negative\"}",    TestType = "HCV"        },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = null,               ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/hcv_p4.jpg",      AI_AnalysisJSON = "{\"hcv\":\"negative\"}",    TestType = "HCV"        },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = null,               ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/hcv_p5.jpg",      AI_AnalysisJSON = "{\"hcv\":\"negative\"}",    TestType = "HCV"        },
                    // TSH
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = aiTsh.ModelID,      ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/tsh_p1.jpg",      AI_AnalysisJSON = "{\"tsh\":1.8}",             TestType = "TSH"        },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = aiTsh.ModelID,      ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/tsh_p2.jpg",      AI_AnalysisJSON = "{\"tsh\":4.5}",             TestType = "TSH"        },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = aiTsh.ModelID,      ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/tsh_p3.jpg",      AI_AnalysisJSON = "{\"tsh\":2.1}",             TestType = "TSH"        },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = aiTsh.ModelID,      ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/tsh_p4.jpg",      AI_AnalysisJSON = "{\"tsh\":1.5}",             TestType = "TSH"        },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = aiTsh.ModelID,      ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/tsh_p5.jpg",      AI_AnalysisJSON = "{\"tsh\":3.2}",             TestType = "TSH"        },
                    // Ferritin
                    new LabTest { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = aiFerritin.ModelID, ReportID = rSarah.ReportID,   UploadDate = new DateTime(2025, 3, 16), ImagePath = "/uploads/tests/ferritin_p1.jpg", AI_AnalysisJSON = "{\"ferritin\":18}",          TestType = "Ferritin"   },
                    new LabTest { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = aiFerritin.ModelID, ReportID = rFatima.ReportID,  UploadDate = new DateTime(2025, 3, 10), ImagePath = "/uploads/tests/ferritin_p2.jpg", AI_AnalysisJSON = "{\"ferritin\":25}",          TestType = "Ferritin"   },
                    new LabTest { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = aiFerritin.ModelID, ReportID = rYasmine.ReportID, UploadDate = new DateTime(2025, 2, 28), ImagePath = "/uploads/tests/ferritin_p3.jpg", AI_AnalysisJSON = "{\"ferritin\":30}",          TestType = "Ferritin"   },
                    new LabTest { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = aiFerritin.ModelID, ReportID = rHana.ReportID,    UploadDate = new DateTime(2025, 3, 1),  ImagePath = "/uploads/tests/ferritin_p4.jpg", AI_AnalysisJSON = "{\"ferritin\":8}",           TestType = "Ferritin"   },
                    new LabTest { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = aiFerritin.ModelID, ReportID = rReem.ReportID,    UploadDate = new DateTime(2025, 3, 5),  ImagePath = "/uploads/tests/ferritin_p5.jpg", AI_AnalysisJSON = "{\"ferritin\":22}",          TestType = "Ferritin"   }
                );
                await context.SaveChangesAsync();
            }

            // Fetch lab tests grouped by type for the child test tables
            var cbcTests       = context.LabTests.Where(l => l.TestType == "CBC").OrderBy(l => l.LabTestID).ToList();
            var bgTests        = context.LabTests.Where(l => l.TestType == "BloodGroup").OrderBy(l => l.LabTestID).ToList();
            var hba1cTests     = context.LabTests.Where(l => l.TestType == "HbA1c").OrderBy(l => l.LabTestID).ToList();
            var uriTests       = context.LabTests.Where(l => l.TestType == "Urinalysis").OrderBy(l => l.LabTestID).ToList();
            var hbsagTests     = context.LabTests.Where(l => l.TestType == "HBsAg").OrderBy(l => l.LabTestID).ToList();
            var hcvTests       = context.LabTests.Where(l => l.TestType == "HCV").OrderBy(l => l.LabTestID).ToList();
            var tshTests       = context.LabTests.Where(l => l.TestType == "TSH").OrderBy(l => l.LabTestID).ToList();
            var ferritinTests  = context.LabTests.Where(l => l.TestType == "Ferritin").OrderBy(l => l.LabTestID).ToList();

            // ============================================================
            // 14. CBC TESTS
            // ============================================================
            if (!context.CBC_Tests.Any())
            {
                context.CBC_Tests.AddRange(
                    new CBC_Test { LabTestID = cbcTests[0].LabTestID, HB = 10.8, MCV = 78.0, MCHC = 31.5, MCH = 25.0, RBC_Count = 3.9, WBC_Count = 8500,  Platelet_Count = 230000, Lymphocytes = 32.0 },
                    new CBC_Test { LabTestID = cbcTests[1].LabTestID, HB = 11.5, MCV = 85.0, MCHC = 33.0, MCH = 27.0, RBC_Count = 4.1, WBC_Count = 12000, Platelet_Count = 210000, Lymphocytes = 28.0 },
                    new CBC_Test { LabTestID = cbcTests[2].LabTestID, HB = 12.2, MCV = 88.0, MCHC = 34.0, MCH = 29.0, RBC_Count = 4.3, WBC_Count = 7800,  Platelet_Count = 250000, Lymphocytes = 35.0 },
                    new CBC_Test { LabTestID = cbcTests[3].LabTestID, HB = 9.5,  MCV = 72.0, MCHC = 29.0, MCH = 22.0, RBC_Count = 3.5, WBC_Count = 9200,  Platelet_Count = 180000, Lymphocytes = 30.0 },
                    new CBC_Test { LabTestID = cbcTests[4].LabTestID, HB = 11.8, MCV = 86.0, MCHC = 33.5, MCH = 28.0, RBC_Count = 4.0, WBC_Count = 8000,  Platelet_Count = 220000, Lymphocytes = 33.0 }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 15. BLOOD GROUP TESTS
            // ============================================================
            if (!context.BloodGroup_Tests.Any())
            {
                context.BloodGroup_Tests.AddRange(
                    new BloodGroup_Test { LabTestID = bgTests[0].LabTestID, ABO_Group = "A",  RH_Factor = "Positive" },
                    new BloodGroup_Test { LabTestID = bgTests[1].LabTestID, ABO_Group = "O",  RH_Factor = "Positive" },
                    new BloodGroup_Test { LabTestID = bgTests[2].LabTestID, ABO_Group = "B",  RH_Factor = "Positive" },
                    new BloodGroup_Test { LabTestID = bgTests[3].LabTestID, ABO_Group = "AB", RH_Factor = "Negative" },
                    new BloodGroup_Test { LabTestID = bgTests[4].LabTestID, ABO_Group = "O",  RH_Factor = "Negative" }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 16. HbA1c TESTS
            // ============================================================
            if (!context.HbA1c_Tests.Any())
            {
                context.HbA1c_Tests.AddRange(
                    new HbA1c_Test { LabTestID = hba1cTests[0].LabTestID, HbA1c = 5.4f },
                    new HbA1c_Test { LabTestID = hba1cTests[1].LabTestID, HbA1c = 5.9f },
                    new HbA1c_Test { LabTestID = hba1cTests[2].LabTestID, HbA1c = 5.1f },
                    new HbA1c_Test { LabTestID = hba1cTests[3].LabTestID, HbA1c = 6.2f },
                    new HbA1c_Test { LabTestID = hba1cTests[4].LabTestID, HbA1c = 6.8f }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 17. URINALYSIS TESTS
            // ============================================================
            if (!context.Urinalysis_Tests.Any())
            {
                context.Urinalysis_Tests.AddRange(
                    new Urinalysis_Test { LabTestID = uriTests[0].LabTestID, Color = "Yellow",       PH = 6.0f, Specific_Gravity = 1.015f, Protein = "Negative", Glucose = "Negative", Nitrite = "Negative", Ketones = "Negative", Blood = "Negative", RBCs = "0-2", Leukocytes = "Negative" },
                    new Urinalysis_Test { LabTestID = uriTests[1].LabTestID, Color = "Light Yellow", PH = 6.5f, Specific_Gravity = 1.018f, Protein = "Trace",    Glucose = "Negative", Nitrite = "Negative", Ketones = "Negative", Blood = "Negative", RBCs = "0-2", Leukocytes = "Negative" },
                    new Urinalysis_Test { LabTestID = uriTests[2].LabTestID, Color = "Yellow",       PH = 5.5f, Specific_Gravity = 1.012f, Protein = "Negative", Glucose = "Negative", Nitrite = "Negative", Ketones = "Negative", Blood = "Negative", RBCs = "0-2", Leukocytes = "Negative" },
                    new Urinalysis_Test { LabTestID = uriTests[3].LabTestID, Color = "Dark Yellow",  PH = 7.0f, Specific_Gravity = 1.022f, Protein = "Trace",    Glucose = "Negative", Nitrite = "Negative", Ketones = "Trace",    Blood = "Negative", RBCs = "2-5", Leukocytes = "Trace"     },
                    new Urinalysis_Test { LabTestID = uriTests[4].LabTestID, Color = "Yellow",       PH = 6.0f, Specific_Gravity = 1.016f, Protein = "Negative", Glucose = "Negative", Nitrite = "Negative", Ketones = "Negative", Blood = "Negative", RBCs = "0-2", Leukocytes = "Negative" }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 18. HBsAg TESTS
            // ============================================================
            if (!context.HBsAg_Tests.Any())
            {
                context.HBsAg_Tests.AddRange(
                    new HBsAg_Test { LabTestID = hbsagTests[0].LabTestID, HBsAg = "Negative" },
                    new HBsAg_Test { LabTestID = hbsagTests[1].LabTestID, HBsAg = "Negative" },
                    new HBsAg_Test { LabTestID = hbsagTests[2].LabTestID, HBsAg = "Negative" },
                    new HBsAg_Test { LabTestID = hbsagTests[3].LabTestID, HBsAg = "Negative" },
                    new HBsAg_Test { LabTestID = hbsagTests[4].LabTestID, HBsAg = "Negative" }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 19. HCV TESTS
            // ============================================================
            if (!context.HCV_Tests.Any())
            {
                context.HCV_Tests.AddRange(
                    new HCV_Test { LabTestID = hcvTests[0].LabTestID, HCV = "Negative" },
                    new HCV_Test { LabTestID = hcvTests[1].LabTestID, HCV = "Negative" },
                    new HCV_Test { LabTestID = hcvTests[2].LabTestID, HCV = "Negative" },
                    new HCV_Test { LabTestID = hcvTests[3].LabTestID, HCV = "Negative" },
                    new HCV_Test { LabTestID = hcvTests[4].LabTestID, HCV = "Negative" }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 20. TSH TESTS
            // ============================================================
            if (!context.TSH_Tests.Any())
            {
                context.TSH_Tests.AddRange(
                    new TSH_Test { LabTestID = tshTests[0].LabTestID, TSH = 1.8f, TSH_Unit = "mIU/L" },
                    new TSH_Test { LabTestID = tshTests[1].LabTestID, TSH = 4.5f, TSH_Unit = "mIU/L" },
                    new TSH_Test { LabTestID = tshTests[2].LabTestID, TSH = 2.1f, TSH_Unit = "mIU/L" },
                    new TSH_Test { LabTestID = tshTests[3].LabTestID, TSH = 1.5f, TSH_Unit = "mIU/L" },
                    new TSH_Test { LabTestID = tshTests[4].LabTestID, TSH = 3.2f, TSH_Unit = "mIU/L" }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 21. FERRITIN TESTS
            // ============================================================
            if (!context.Ferritin_Tests.Any())
            {
                context.Ferritin_Tests.AddRange(
                    new Ferritin_Test { LabTestID = ferritinTests[0].LabTestID, Ferritin_Value = 18.0f },
                    new Ferritin_Test { LabTestID = ferritinTests[1].LabTestID, Ferritin_Value = 25.0f },
                    new Ferritin_Test { LabTestID = ferritinTests[2].LabTestID, Ferritin_Value = 30.0f },
                    new Ferritin_Test { LabTestID = ferritinTests[3].LabTestID, Ferritin_Value = 8.0f  },
                    new Ferritin_Test { LabTestID = ferritinTests[4].LabTestID, Ferritin_Value = 22.0f }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 22. ULTRASOUND IMAGES
            // ===========================================================
            if (!context.UltrasoundImages.Any())
            {
                context.UltrasoundImages.AddRange(
                    new UltrasoundImage { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = aiUs.ModelID, ImagePath = "/uploads/ultrasound/us_p1_1.jpg", UploadDate = new DateTime(2025, 3, 15), DetectedAnomaly = "None",                  DoctorComments = "Normal fetal development at week 24.",            AI_Result_JSON = "{\"anomaly\":\"none\",\"confidence\":97.2}"              },
                    new UltrasoundImage { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, ModelID = aiUs.ModelID, ImagePath = "/uploads/ultrasound/us_p2_1.jpg", UploadDate = new DateTime(2025, 3, 9),  DetectedAnomaly = "Mild Placenta Previa", DoctorComments = "Scheduled follow-up scan in 4 weeks.",             AI_Result_JSON = "{\"anomaly\":\"placenta_previa\",\"confidence\":88.5}" },
                    new UltrasoundImage { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  ModelID = aiUs.ModelID, ImagePath = "/uploads/ultrasound/us_p3_1.jpg", UploadDate = new DateTime(2025, 2, 27), DetectedAnomaly = "None",                  DoctorComments = "First trimester scan looks normal.",               AI_Result_JSON = "{\"anomaly\":\"none\",\"confidence\":98.1}"              },
                    new UltrasoundImage { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, ModelID = aiUs.ModelID, ImagePath = "/uploads/ultrasound/us_p4_1.jpg", UploadDate = new DateTime(2025, 2, 28), DetectedAnomaly = "None",                  DoctorComments = "Third trimester scan. Baby in cephalic position.", AI_Result_JSON = "{\"anomaly\":\"none\",\"confidence\":96.0}"              },
                    new UltrasoundImage { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, ModelID = aiUs.ModelID, ImagePath = "/uploads/ultrasound/us_p5_1.jpg", UploadDate = new DateTime(2025, 3, 4),  DetectedAnomaly = "None",                  DoctorComments = "Normal fetal growth at week 20.",                 AI_Result_JSON = "{\"anomaly\":\"none\",\"confidence\":95.3}"              },
                    new UltrasoundImage { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, ModelID = aiUs.ModelID, ImagePath = "/uploads/ultrasound/us_p1_2.jpg", UploadDate = new DateTime(2025, 2, 10), DetectedAnomaly = "None",                  DoctorComments = "Week 20 anatomy scan completed.",                  AI_Result_JSON = "{\"anomaly\":\"none\",\"confidence\":96.8}"              }
                );
                await context.SaveChangesAsync();
            }

            var usImages = context.UltrasoundImages.OrderBy(u => u.ImageID).ToList();

            // ============================================================
            // 23. MEDICAL HISTORY
            // ============================================================
            if (!context.MedicalHistories.Any())
            {
                context.MedicalHistories.AddRange(
                    new MedicalHistory { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, LabTestID = cbcTests[0].LabTestID,  ImageID = usImages[0].ImageID, EventType = "LabTest",     Summary = "CBC test analyzed. Mild anemia detected.",                                  DateRecorded = new DateTime(2025, 3, 16), Date = new DateTime(2025, 3, 16) },
                    new MedicalHistory { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, LabTestID = null,                   ImageID = usImages[0].ImageID, EventType = "Ultrasound",  Summary = "Week 24 anatomy scan. No anomalies found.",                                 DateRecorded = new DateTime(2025, 3, 15), Date = new DateTime(2025, 3, 15) },
                    new MedicalHistory { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, LabTestID = cbcTests[1].LabTestID,  ImageID = usImages[1].ImageID, EventType = "LabTest",     Summary = "CBC shows elevated WBC. Blood pressure trending high.",                     DateRecorded = new DateTime(2025, 3, 10), Date = new DateTime(2025, 3, 10) },
                    new MedicalHistory { PatientID = pFatima.PatientID,  DoctorID = dAhmed.DoctorID, LabTestID = null,                   ImageID = usImages[1].ImageID, EventType = "Ultrasound",  Summary = "Mild placenta previa detected. Follow-up scheduled.",                       DateRecorded = new DateTime(2025, 3, 9),  Date = new DateTime(2025, 3, 9)  },
                    new MedicalHistory { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  LabTestID = cbcTests[2].LabTestID,  ImageID = usImages[2].ImageID, EventType = "LabTest",     Summary = "First trimester panel all normal.",                                          DateRecorded = new DateTime(2025, 2, 28), Date = new DateTime(2025, 2, 28) },
                    new MedicalHistory { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, LabTestID = cbcTests[3].LabTestID,  ImageID = usImages[3].ImageID, EventType = "LabTest",     Summary = "Iron deficiency anemia confirmed. IV iron therapy initiated.",               DateRecorded = new DateTime(2025, 3, 1),  Date = new DateTime(2025, 3, 1)  },
                    new MedicalHistory { PatientID = pReem.PatientID,    DoctorID = dNadia.DoctorID, LabTestID = cbcTests[4].LabTestID,  ImageID = usImages[4].ImageID, EventType = "LabTest",     Summary = "HbA1c above target for gestational diabetes. Diet plan adjusted.",           DateRecorded = new DateTime(2025, 3, 5),  Date = new DateTime(2025, 3, 5)  },
                    new MedicalHistory { PatientID = pSarah.PatientID,   DoctorID = dAhmed.DoctorID, LabTestID = null,                   ImageID = null,                EventType = "Appointment", Summary = "Routine prenatal visit. Blood pressure 118/76. Weight gain normal.",         DateRecorded = new DateTime(2025, 3, 5),  Date = new DateTime(2025, 3, 5)  },
                    new MedicalHistory { PatientID = pYasmine.PatientID, DoctorID = dMona.DoctorID,  LabTestID = null,                   ImageID = usImages[2].ImageID, EventType = "Ultrasound",  Summary = "Week 12 dating scan. Crown-rump length within normal range.",                DateRecorded = new DateTime(2025, 2, 10), Date = new DateTime(2025, 2, 10) },
                    new MedicalHistory { PatientID = pHana.PatientID,    DoctorID = dKarim.DoctorID, LabTestID = null,                   ImageID = usImages[3].ImageID, EventType = "Appointment", Summary = "Third trimester consultation. Birth plan discussed.",                        DateRecorded = new DateTime(2025, 3, 1),  Date = new DateTime(2025, 3, 1)  }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 24. ALERTS
            // ============================================================
            if (!context.Alerts.Any())
            {
                context.Alerts.AddRange(
                    new Alert { PatientID = pSarah.PatientID,   Title = "Low Hemoglobin Detected",  Message = "Your CBC shows hemoglobin of 10.8 g/dL. Consider increasing iron supplementation.",             AlertType = "Warning",  DateCreated = new DateTime(2025, 3, 16), IsRead = false },
                    new Alert { PatientID = pSarah.PatientID,   Title = "Upcoming Appointment",      Message = "You have an appointment with Dr. Ahmed Hassan tomorrow at 10:00 AM.",                            AlertType = "Info",     DateCreated = new DateTime(2025, 3, 19), IsRead = false },
                    new Alert { PatientID = pSarah.PatientID,   Title = "Blood Pressure Normal",     Message = "Your blood pressure reading of 118/76 mmHg is within the normal range.",                        AlertType = "Success",  DateCreated = new DateTime(2025, 3, 17), IsRead = true  },
                    new Alert { PatientID = pFatima.PatientID,  Title = "Elevated Blood Pressure",   Message = "Your blood pressure has been consistently elevated. Follow up with your doctor immediately.",    AlertType = "Critical", DateCreated = new DateTime(2025, 3, 11), IsRead = false },
                    new Alert { PatientID = pFatima.PatientID,  Title = "Elevated WBC Count",        Message = "Your CBC shows an elevated WBC count of 12,000. This may indicate an infection.",               AlertType = "Warning",  DateCreated = new DateTime(2025, 3, 10), IsRead = false },
                    new Alert { PatientID = pYasmine.PatientID, Title = "Welcome to MamaCare",       Message = "Welcome! Your first trimester panel results are all normal. Keep following your care plan.",     AlertType = "Success",  DateCreated = new DateTime(2025, 2, 28), IsRead = true  },
                    new Alert { PatientID = pHana.PatientID,    Title = "Iron Deficiency Anemia",    Message = "Your ferritin level is critically low at 8 ng/mL. IV iron therapy has been prescribed.",         AlertType = "Critical", DateCreated = new DateTime(2025, 3, 1),  IsRead = false },
                    new Alert { PatientID = pHana.PatientID,    Title = "Upcoming Appointment",      Message = "Your appointment with Dr. Karim Mostafa is on March 25 at 2:00 PM.",                             AlertType = "Info",     DateCreated = new DateTime(2025, 3, 20), IsRead = false },
                    new Alert { PatientID = pReem.PatientID,    Title = "HbA1c Above Target",        Message = "Your HbA1c is 6.8%, above the gestational diabetes target. Dietary adjustments recommended.",    AlertType = "Warning",  DateCreated = new DateTime(2025, 3, 5),  IsRead = false },
                    new Alert { PatientID = pReem.PatientID,    Title = "Prenatal Vitamin Reminder", Message = "Remember to take your daily prenatal vitamin and folic acid supplement.",                         AlertType = "Info",     DateCreated = new DateTime(2025, 3, 18), IsRead = false },
                    // Today's alerts
                    new Alert { PatientID = pSarah.PatientID,   Title = "Appointment Today",         Message = "You have appointments at MamaCare Central today. Please arrive 15 minutes early.",                AlertType = "Info",     DateCreated = DateTime.Today,             IsRead = false },
                    new Alert { PatientID = pFatima.PatientID,  Title = "Appointment Today",         Message = "You have appointments today with Dr. Ahmed and Dr. Mona. Bring your recent test results.",       AlertType = "Info",     DateCreated = DateTime.Today,             IsRead = false },
                    new Alert { PatientID = pHana.PatientID,    Title = "Iron Therapy Reminder",     Message = "Your weekly IV iron infusion is due. Contact the clinic to schedule your appointment.",            AlertType = "Warning",  DateCreated = DateTime.Today,             IsRead = false },
                    new Alert { PatientID = pYasmine.PatientID, Title = "First Trimester Update",    Message = "Your first trimester is progressing well. Next screening scheduled soon.",                         AlertType = "Success",  DateCreated = DateTime.Today,             IsRead = false },
                    new Alert { PatientID = pReem.PatientID,    Title = "Blood Sugar Alert",         Message = "Your recent blood sugar readings are above target. Please follow the adjusted dietary plan.",      AlertType = "Critical", DateCreated = DateTime.Today,             IsRead = false }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 25. NOTES
            // ============================================================
            if (!context.Notes.Any())
            {
                context.Notes.AddRange(
                    new Note { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   CreatedDate = new DateTime(2025, 3, 16), Content = "Patient reports occasional dizziness. Likely related to mild anemia. Iron supplement dosage increased to 60mg/day." },
                    new Note { DoctorID = dAhmed.DoctorID, PatientID = pFatima.PatientID,  CreatedDate = new DateTime(2025, 3, 10), Content = "Blood pressure monitoring needs to continue. Patient educated on warning signs of preeclampsia." },
                    new Note { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, CreatedDate = new DateTime(2025, 2, 28), Content = "First consultation. Patient is in good health. Standard prenatal vitamins prescribed. Anatomy scan booked for week 20." },
                    new Note { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    CreatedDate = new DateTime(2025, 3, 1),  Content = "Iron deficiency confirmed. IV iron infusion scheduled. Patient advised to increase dietary iron intake." },
                    new Note { DoctorID = dNadia.DoctorID, PatientID = pReem.PatientID,    CreatedDate = new DateTime(2025, 3, 5),  Content = "Gestational diabetes management. HbA1c trending high. Dietitian referral given. Blood sugar monitored 4x daily." },
                    new Note { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   CreatedDate = new DateTime(2025, 2, 15), Content = "Week 20 anatomy scan normal. Fetal measurements appropriate for gestational age. Patient reassured." },
                    new Note { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, CreatedDate = new DateTime(2025, 3, 5),  Content = "Week 12 nuchal translucency measurement normal. Low risk for chromosomal abnormalities." },
                    new Note { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    CreatedDate = new DateTime(2025, 2, 20), Content = "Patient experiencing back pain. Physiotherapy referral provided. Continue aspirin 81mg for preeclampsia prevention." }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 26. PRESCRIPTIONS
            // ============================================================
            if (!context.Prescriptions.Any())
            {
                context.Prescriptions.AddRange(
                    new Prescription { DoctorID = dAhmed.DoctorID, PatientID = pSarah.PatientID,   PrescriptionDate = new DateTime(2025, 3, 16), Notes = "Continue prenatal vitamins. Increase iron supplementation." },
                    new Prescription { DoctorID = dAhmed.DoctorID, PatientID = pFatima.PatientID,  PrescriptionDate = new DateTime(2025, 3, 10), Notes = "Blood pressure management plan. Monitor daily."             },
                    new Prescription { DoctorID = dMona.DoctorID,  PatientID = pYasmine.PatientID, PrescriptionDate = new DateTime(2025, 2, 28), Notes = "Standard first trimester prescription."                      },
                    new Prescription { DoctorID = dKarim.DoctorID, PatientID = pHana.PatientID,    PrescriptionDate = new DateTime(2025, 3, 1),  Notes = "Iron deficiency anemia treatment plan."                      },
                    new Prescription { DoctorID = dNadia.DoctorID, PatientID = pReem.PatientID,    PrescriptionDate = new DateTime(2025, 3, 5),  Notes = "Gestational diabetes management."                            }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 27. PRESCRIPTION ITEMS
            // ============================================================
            if (!context.PrescriptionItems.Any())
            {
                var prescriptions = context.Prescriptions.OrderBy(p => p.PrescriptionID).ToList();
                var pr1 = prescriptions[0]; var pr2 = prescriptions[1];
                var pr3 = prescriptions[2]; var pr4 = prescriptions[3]; var pr5 = prescriptions[4];

                context.PrescriptionItems.AddRange(
                    new PrescriptionItem { PrescriptionID = pr1.PrescriptionID, MedicineName = "Ferrous Sulfate",      Dosage = "325mg",    Frequency = "Twice daily",       DurationDays = 90,  Instructions = "Take with vitamin C for better absorption"   },
                    new PrescriptionItem { PrescriptionID = pr1.PrescriptionID, MedicineName = "Folic Acid",            Dosage = "5mg",      Frequency = "Once daily",         DurationDays = 180, Instructions = "Take in the morning with food"                },
                    new PrescriptionItem { PrescriptionID = pr1.PrescriptionID, MedicineName = "Vitamin D3",            Dosage = "1000 IU",  Frequency = "Once daily",         DurationDays = 180, Instructions = "Take with main meal"                           },
                    new PrescriptionItem { PrescriptionID = pr2.PrescriptionID, MedicineName = "Labetalol",             Dosage = "100mg",    Frequency = "Twice daily",        DurationDays = 60,  Instructions = "Do not stop abruptly; monitor BP twice daily" },
                    new PrescriptionItem { PrescriptionID = pr2.PrescriptionID, MedicineName = "Calcium Carbonate",     Dosage = "500mg",    Frequency = "Twice daily",        DurationDays = 90,  Instructions = "Take with food"                                },
                    new PrescriptionItem { PrescriptionID = pr2.PrescriptionID, MedicineName = "Aspirin",               Dosage = "81mg",     Frequency = "Once daily",         DurationDays = 90,  Instructions = "Take at bedtime"                               },
                    new PrescriptionItem { PrescriptionID = pr3.PrescriptionID, MedicineName = "Prenatal Multivitamin", Dosage = "1 tablet", Frequency = "Once daily",         DurationDays = 270, Instructions = "Take with food"                                },
                    new PrescriptionItem { PrescriptionID = pr3.PrescriptionID, MedicineName = "Folic Acid",            Dosage = "0.4mg",    Frequency = "Once daily",         DurationDays = 270, Instructions = "Take in the morning"                           },
                    new PrescriptionItem { PrescriptionID = pr4.PrescriptionID, MedicineName = "Iron Sucrose IV",       Dosage = "200mg",    Frequency = "Weekly infusion",    DurationDays = 42,  Instructions = "Administered in clinic under supervision"      },
                    new PrescriptionItem { PrescriptionID = pr4.PrescriptionID, MedicineName = "Methyldopa",            Dosage = "250mg",    Frequency = "Three times daily",  DurationDays = 60,  Instructions = "Monitor BP regularly"                          },
                    new PrescriptionItem { PrescriptionID = pr5.PrescriptionID, MedicineName = "Metformin",             Dosage = "500mg",    Frequency = "Twice daily",        DurationDays = 90,  Instructions = "Take with meals. Monitor blood sugar 4x daily."},
                    new PrescriptionItem { PrescriptionID = pr5.PrescriptionID, MedicineName = "Folic Acid",            Dosage = "5mg",      Frequency = "Once daily",         DurationDays = 180, Instructions = "Take in the morning"                           }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 28. PATIENT BLOOD PRESSURE
            // ============================================================
            if (!context.PatientBloodPressure.Any())
            {
                context.PatientBloodPressure.AddRange(
                    new PatientBloodPressure { PatientID = pSarah.PatientID,   BloodPressure = "118/76",  DateTime = new DateTime(2025, 3, 17, 9,  30, 0) },
                    new PatientBloodPressure { PatientID = pSarah.PatientID,   BloodPressure = "122/82",  DateTime = new DateTime(2025, 3, 16, 10, 15, 0) },
                    new PatientBloodPressure { PatientID = pSarah.PatientID,   BloodPressure = "135/88",  DateTime = new DateTime(2025, 3, 15, 8,  45, 0) },
                    new PatientBloodPressure { PatientID = pSarah.PatientID,   BloodPressure = "115/75",  DateTime = new DateTime(2025, 3, 14, 9,  0,  0) },
                    new PatientBloodPressure { PatientID = pSarah.PatientID,   BloodPressure = "120/80",  DateTime = new DateTime(2025, 3, 18, 8,  0,  0) },
                    new PatientBloodPressure { PatientID = pFatima.PatientID,  BloodPressure = "145/92",  DateTime = new DateTime(2025, 3, 17, 8,  0,  0) },
                    new PatientBloodPressure { PatientID = pFatima.PatientID,  BloodPressure = "148/95",  DateTime = new DateTime(2025, 3, 16, 9,  0,  0) },
                    new PatientBloodPressure { PatientID = pFatima.PatientID,  BloodPressure = "142/90",  DateTime = new DateTime(2025, 3, 15, 8,  30, 0) },
                    new PatientBloodPressure { PatientID = pYasmine.PatientID, BloodPressure = "110/70",  DateTime = new DateTime(2025, 3, 15, 9,  0,  0) },
                    new PatientBloodPressure { PatientID = pYasmine.PatientID, BloodPressure = "112/72",  DateTime = new DateTime(2025, 3, 14, 9,  30, 0) },
                    new PatientBloodPressure { PatientID = pHana.PatientID,    BloodPressure = "150/98",  DateTime = new DateTime(2025, 3, 17, 7,  45, 0) },
                    new PatientBloodPressure { PatientID = pHana.PatientID,    BloodPressure = "155/100", DateTime = new DateTime(2025, 3, 16, 8,  0,  0) },
                    new PatientBloodPressure { PatientID = pReem.PatientID,    BloodPressure = "116/74",  DateTime = new DateTime(2025, 3, 17, 10, 0,  0) },
                    new PatientBloodPressure { PatientID = pReem.PatientID,    BloodPressure = "118/76",  DateTime = new DateTime(2025, 3, 16, 10, 30, 0) },
                    new PatientBloodPressure { PatientID = pReem.PatientID,    BloodPressure = "120/78",  DateTime = new DateTime(2025, 3, 15, 9,  0,  0) }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 29. PATIENT BLOOD SUGAR
            // ============================================================
            if (!context.PatientBloodSugar.Any())
            {
                context.PatientBloodSugar.AddRange(
                    new PatientBloodSugar { PatientID = pSarah.PatientID,   BloodSugar = 92.0,  DateTime = new DateTime(2025, 3, 17, 8,  0,  0) },
                    new PatientBloodSugar { PatientID = pSarah.PatientID,   BloodSugar = 128.0, DateTime = new DateTime(2025, 3, 17, 14, 15, 0) },
                    new PatientBloodSugar { PatientID = pSarah.PatientID,   BloodSugar = 88.0,  DateTime = new DateTime(2025, 3, 16, 7,  45, 0) },
                    new PatientBloodSugar { PatientID = pSarah.PatientID,   BloodSugar = 145.0, DateTime = new DateTime(2025, 3, 15, 20, 30, 0) },
                    new PatientBloodSugar { PatientID = pSarah.PatientID,   BloodSugar = 95.0,  DateTime = new DateTime(2025, 3, 18, 8,  0,  0) },
                    new PatientBloodSugar { PatientID = pFatima.PatientID,  BloodSugar = 105.0, DateTime = new DateTime(2025, 3, 17, 7,  30, 0) },
                    new PatientBloodSugar { PatientID = pFatima.PatientID,  BloodSugar = 155.0, DateTime = new DateTime(2025, 3, 17, 14, 0,  0) },
                    new PatientBloodSugar { PatientID = pYasmine.PatientID, BloodSugar = 85.0,  DateTime = new DateTime(2025, 3, 16, 8,  0,  0) },
                    new PatientBloodSugar { PatientID = pYasmine.PatientID, BloodSugar = 120.0, DateTime = new DateTime(2025, 3, 16, 14, 0,  0) },
                    new PatientBloodSugar { PatientID = pHana.PatientID,    BloodSugar = 98.0,  DateTime = new DateTime(2025, 3, 17, 7,  0,  0) },
                    new PatientBloodSugar { PatientID = pHana.PatientID,    BloodSugar = 135.0, DateTime = new DateTime(2025, 3, 17, 14, 30, 0) },
                    new PatientBloodSugar { PatientID = pReem.PatientID,    BloodSugar = 115.0, DateTime = new DateTime(2025, 3, 17, 8,  0,  0) },
                    new PatientBloodSugar { PatientID = pReem.PatientID,    BloodSugar = 165.0, DateTime = new DateTime(2025, 3, 17, 14, 0,  0) },
                    new PatientBloodSugar { PatientID = pReem.PatientID,    BloodSugar = 118.0, DateTime = new DateTime(2025, 3, 16, 8,  30, 0) },
                    new PatientBloodSugar { PatientID = pReem.PatientID,    BloodSugar = 170.0, DateTime = new DateTime(2025, 3, 16, 20, 0,  0) }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 30. WEIGHT TRACKING
            // ============================================================
            if (!context.WeightTrackings.Any())
            {
                context.WeightTrackings.AddRange(
                    new WeightTracking { PatientID = pSarah.PatientID,   RecordedDate = new DateTime(2025, 1, 1),  WeightKg = 63.0, Notes = "Pre-pregnancy baseline weight"          },
                    new WeightTracking { PatientID = pSarah.PatientID,   RecordedDate = new DateTime(2025, 2, 1),  WeightKg = 65.5, Notes = "Week 16 - weight within expected range"  },
                    new WeightTracking { PatientID = pSarah.PatientID,   RecordedDate = new DateTime(2025, 3, 1),  WeightKg = 68.0, Notes = "Week 24 - gaining ~1 kg/month"            },
                    new WeightTracking { PatientID = pSarah.PatientID,   RecordedDate = new DateTime(2025, 3, 18), WeightKg = 68.5, Notes = "Latest reading"                           },
                    new WeightTracking { PatientID = pFatima.PatientID,  RecordedDate = new DateTime(2025, 1, 1),  WeightKg = 67.0, Notes = "Pre-pregnancy weight"                     },
                    new WeightTracking { PatientID = pFatima.PatientID,  RecordedDate = new DateTime(2025, 2, 1),  WeightKg = 70.0, Notes = "Week 24 weight"                           },
                    new WeightTracking { PatientID = pFatima.PatientID,  RecordedDate = new DateTime(2025, 3, 1),  WeightKg = 72.0, Notes = "Week 32 - slightly above expected gain"   },
                    new WeightTracking { PatientID = pYasmine.PatientID, RecordedDate = new DateTime(2025, 1, 10), WeightKg = 58.0, Notes = "Week 4 - early pregnancy baseline"        },
                    new WeightTracking { PatientID = pYasmine.PatientID, RecordedDate = new DateTime(2025, 2, 15), WeightKg = 59.5, Notes = "Week 10"                                  },
                    new WeightTracking { PatientID = pYasmine.PatientID, RecordedDate = new DateTime(2025, 3, 10), WeightKg = 60.0, Notes = "Week 12 - normal gain"                    },
                    new WeightTracking { PatientID = pHana.PatientID,    RecordedDate = new DateTime(2024, 9, 1),  WeightKg = 74.0, Notes = "Pre-pregnancy baseline"                   },
                    new WeightTracking { PatientID = pHana.PatientID,    RecordedDate = new DateTime(2025, 1, 1),  WeightKg = 78.0, Notes = "Week 20"                                  },
                    new WeightTracking { PatientID = pHana.PatientID,    RecordedDate = new DateTime(2025, 3, 1),  WeightKg = 80.0, Notes = "Week 36 - normal late pregnancy weight"   },
                    new WeightTracking { PatientID = pReem.PatientID,    RecordedDate = new DateTime(2024, 12, 5), WeightKg = 62.0, Notes = "Early pregnancy baseline"                 },
                    new WeightTracking { PatientID = pReem.PatientID,    RecordedDate = new DateTime(2025, 2, 1),  WeightKg = 64.0, Notes = "Week 16"                                  },
                    new WeightTracking { PatientID = pReem.PatientID,    RecordedDate = new DateTime(2025, 3, 18), WeightKg = 65.0, Notes = "Week 20"                                  }
                );
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 31. PLACES
            // ============================================================
            if (!context.Places.Any())
            {
                context.Places.AddRange(
                    new Place { PatientID = pSarah.PatientID,   Name = "MamaCare Central Clinic",      Type = "Clinic",   Address = "15 Tahrir St, Cairo",      Phone = "0222345678", ImageURL = "/uploads/places/mamacare_central.jpg" },
                    new Place { PatientID = pSarah.PatientID,   Name = "Cairo Pharmacy",                Type = "Pharmacy", Address = "10 Tahrir Square, Cairo",   Phone = "0223456789", ImageURL = "/uploads/places/cairo_pharmacy.jpg"   },
                    new Place { PatientID = pSarah.PatientID,   Name = "Cairo Diagnostic Lab",          Type = "Lab",      Address = "5 Ramses St, Cairo",        Phone = "0224567890", ImageURL = "/uploads/places/cairo_lab.jpg"        },
                    new Place { PatientID = pFatima.PatientID,  Name = "Fetal Health Clinic",           Type = "Clinic",   Address = "22 Nasr City, Cairo",       Phone = "0225678901", ImageURL = "/uploads/places/fetal_clinic.jpg"     },
                    new Place { PatientID = pFatima.PatientID,  Name = "Nasr City Hospital",            Type = "Hospital", Address = "33 Nasr City, Cairo",       Phone = "0226789012", ImageURL = "/uploads/places/nasr_hospital.jpg"    },
                    new Place { PatientID = pYasmine.PatientID, Name = "New Cairo OBG Center",          Type = "Clinic",   Address = "18 New Cairo",              Phone = "0227890123", ImageURL = "/uploads/places/newcairo_obg.jpg"     },
                    new Place { PatientID = pYasmine.PatientID, Name = "El-Salam Pharmacy",             Type = "Pharmacy", Address = "20 New Cairo Blvd",          Phone = "0228901234", ImageURL = "/uploads/places/elsalam_pharmacy.jpg" },
                    new Place { PatientID = pHana.PatientID,    Name = "Alexandria OBG Center",         Type = "Clinic",   Address = "7 Corniche, Alexandria",    Phone = "0345678901", ImageURL = "/uploads/places/alex_obg.jpg"         },
                    new Place { PatientID = pHana.PatientID,    Name = "Shubra General Hospital",       Type = "Hospital", Address = "12 Shubra St, Cairo",       Phone = "0229012345", ImageURL = "/uploads/places/shubra_hospital.jpg"  },
                    new Place { PatientID = pReem.PatientID,    Name = "Endocrine & Maternal Clinic",   Type = "Clinic",   Address = "30 Heliopolis, Cairo",      Phone = "0230123456", ImageURL = "/uploads/places/endocrine_clinic.jpg" },
                    new Place { PatientID = pReem.PatientID,    Name = "Mohandessin Diagnostic Center", Type = "Lab",      Address = "15 Mohandessin, Giza",      Phone = "0231234567", ImageURL = "/uploads/places/mohandessin_lab.jpg"  }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}

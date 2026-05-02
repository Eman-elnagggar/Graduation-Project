using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Graduation_Project.Models;

namespace Graduation_Project.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet properties
        public DbSet<AIModel> AIModels { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Assistant> Assistants { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CBC_Test> CBC_Tests { get; set; }
        public DbSet<BloodGroup_Test> BloodGroup_Tests { get; set; }
        public DbSet<HbA1c_Test> HbA1c_Tests { get; set; }
        public DbSet<Urinalysis_Test> Urinalysis_Tests { get; set; }
        public DbSet<HBsAg_Test> HBsAg_Tests { get; set; }
        public DbSet<HCV_Test> HCV_Tests { get; set; }
        public DbSet<TSH_Test> TSH_Tests { get; set; }
        public DbSet<Ferritin_Test> Ferritin_Tests { get; set; }
        public DbSet<FBG_Test> FBG_Tests { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<ClinicDoctor> ClinicDoctors { get; set; }
        public DbSet<AssistantDoctor> AssistantDoctors { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<LabTest> LabTests { get; set; }
        public DbSet<MedicalHistory> MedicalHistories { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientBloodPressure> PatientBloodPressure { get; set; }
        public DbSet<PatientBloodSugar> PatientBloodSugar { get; set; }
        public DbSet<PatientDoctor> PatientDoctors { get; set; }
        public DbSet<PatientDrug> PatientDrugs { get; set; }
        public DbSet<PregnancyRecord> PregnancyRecords { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<TestReport> TestReports { get; set; }
        public DbSet<UltrasoundImage> UltrasoundImages { get; set; }
        public DbSet<WeightTracking> WeightTrackings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ClinicInvitation> ClinicInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // APPOINTMENT -> PATIENT (optional — availability slots have no patient)
            // ============================================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasOne(a => a.Patient)
                    .WithMany()
                    .HasForeignKey(a => a.PatientID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 2. PATIENT -> USER (One Patient is One User)
            // ============================================================
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.PatientID);

                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<Patient>(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 3. DOCTOR -> USER (One Doctor is One User)
            // ============================================================
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(e => e.DoctorID);

                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<Doctor>(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 4. ASSISTANT -> USER (One Assistant is One User)
            // ============================================================
            modelBuilder.Entity<Assistant>(entity =>
            {
                entity.HasKey(e => e.AssistantID);

                entity.HasOne(d => d.User)
                    .WithOne()
                    .HasForeignKey<Assistant>(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 5. ASSISTANT -> CLINIC (Many Assistants belong to One Clinic; optional until invitation acceptance)
            // ============================================================
            modelBuilder.Entity<Assistant>(entity =>
            {
                entity.HasOne(d => d.Clinic)
                    .WithMany(c => c.Assistants)
                    .HasForeignKey(d => d.ClinicID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 6. CLINIC -> DOCTOR (Many-to-Many through ClinicDoctor)
            // ============================================================
            modelBuilder.Entity<Clinic>(entity =>
            {
                entity.HasKey(e => e.ClinicID);
            });

            modelBuilder.Entity<ClinicDoctor>(entity =>
            {
                entity.HasKey(e => new { e.ClinicID, e.DoctorID });

                entity.HasOne(d => d.Clinic)
                    .WithMany(c => c.ClinicDoctors)
                    .HasForeignKey(d => d.ClinicID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Doctor)
                    .WithMany(d => d.ClinicDoctors)
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // ASSISTANT <-> DOCTOR (Many-to-Many through AssistantDoctor)
            // ============================================================
            modelBuilder.Entity<AssistantDoctor>(entity =>
            {
                entity.HasKey(e => new { e.AssistantID, e.DoctorID });

                entity.HasOne(d => d.Assistant)
                    .WithMany(a => a.AssistantDoctors)
                    .HasForeignKey(d => d.AssistantID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Doctor)
                    .WithMany(d => d.AssistantDoctors)
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 7. PATIENT -> PLACES (One Patient has Many Places)
            // ============================================================
            modelBuilder.Entity<Place>(entity =>
            {
                entity.HasKey(e => e.PlaceID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // PATIENT -> PREGNANCYRECORDS (One Patient has Many PregnancyRecords)
            // ============================================================
            modelBuilder.Entity<PregnancyRecord>(entity =>
            {
                entity.HasKey(e => e.PregnancyRecordID);

                entity.HasOne(d => d.Patient)
                    .WithMany(p => p.PregnancyRecords)
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.PatientID, e.StartDate });
            });

            // ============================================================
            // 8. PATIENT -> LABTESTS (One Patient has Many LabTests)
            // ============================================================
            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 9. DOCTOR -> LABTESTS (One Doctor has Many LabTests)
            // ============================================================
            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TestReport>(entity =>
            {
                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 10. AIMODEL -> LABTESTS (One AIModel has Many LabTests)
            // ============================================================
            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.HasOne(d => d.AIModel)
                    .WithMany()
                    .HasForeignKey(d => d.ModelID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // 11. CBC_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<CBC_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<CBC_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 40. BLOODGROUP_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<BloodGroup_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<BloodGroup_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 41. HBA1C_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<HbA1c_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<HbA1c_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 42. URINALYSIS_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<Urinalysis_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<Urinalysis_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 43. HBSAG_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<HBsAg_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<HBsAg_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 44. HCV_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<HCV_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<HCV_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 45. TSH_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<TSH_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<TSH_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 46. FERRITIN_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<Ferritin_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<Ferritin_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 47. FBG_TEST -> LABTEST (One-to-One)
            // ============================================================
            modelBuilder.Entity<FBG_Test>(entity =>
            {
                entity.HasKey(e => e.LabTestID);

                entity.HasOne(d => d.LabTest)
                    .WithOne()
                    .HasForeignKey<FBG_Test>(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // CHATMESSAGES
            // ============================================================
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.ChatMessageId);

                entity.Property(e => e.SenderUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(e => e.ReceiverUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(e => e.Message)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.HasOne(e => e.SenderUser)
                    .WithMany()
                    .HasForeignKey(e => e.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReceiverUser)
                    .WithMany()
                    .HasForeignKey(e => e.ReceiverUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.SenderUserId, e.ReceiverUserId, e.SentAtUtc });
                entity.HasIndex(e => new { e.ReceiverUserId, e.IsRead });
            });

            // ============================================================
            // 12. PATIENT -> ULTRASOUNDIMAGES (One Patient has Many UltrasoundImages)
            // ============================================================
            modelBuilder.Entity<UltrasoundImage>(entity =>
            {
                entity.HasKey(e => e.ImageID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 13. DOCTOR -> ULTRASOUNDIMAGES (One Doctor adds Many UltrasoundImages)
            // ============================================================
            modelBuilder.Entity<UltrasoundImage>(entity =>
            {
                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 14. AIMODEL -> ULTRASOUNDIMAGES (One AIModel has Many UltrasoundImages)
            // ============================================================
            modelBuilder.Entity<UltrasoundImage>(entity =>
            {
                entity.HasOne(d => d.AIModel)
                    .WithMany()
                    .HasForeignKey(d => d.ModelID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // 15. PATIENT -> MEDICALHISTORY (One Patient has Many MedicalHistory)
            // ============================================================
            modelBuilder.Entity<MedicalHistory>(entity =>
            {
                entity.HasKey(e => e.HistoryID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 16. DOCTOR -> MEDICALHISTORY (One Doctor has Many MedicalHistory)
            // ============================================================
            modelBuilder.Entity<MedicalHistory>(entity =>
            {
                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 17. MEDICALHISTORY -> LABTEST (One MedicalHistory has One LabTest)
            // ============================================================
            modelBuilder.Entity<MedicalHistory>(entity =>
            {
                entity.HasOne<LabTest>()
                    .WithMany()
                    .HasForeignKey(d => d.LabTestID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // 18. MEDICALHISTORY -> ULTRASOUNDIMAGE (One MedicalHistory has One UltrasoundImage)
            // ============================================================
            modelBuilder.Entity<MedicalHistory>(entity =>
            {
                entity.HasOne<UltrasoundImage>()
                    .WithMany()
                    .HasForeignKey(d => d.ImageID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // 19. PATIENT -> ALERTS (One Patient receives Many Alerts)
            // ============================================================
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(e => e.AlertID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 20. PATIENT -> APPOINTMENTS (One Patient has Many Appointments)
            // ============================================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(e => e.AppointmentID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 21. DOCTOR -> APPOINTMENTS (One Doctor has Many Appointments)
            // ============================================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasOne(d => d.Doctor)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 22. CLINIC -> APPOINTMENTS (One Clinic has Many Appointments)
            // ============================================================
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasOne(d => d.Clinic)
                    .WithMany()
                    .HasForeignKey(d => d.ClinicID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.CreatedByAssistant)
                    .WithMany()
                    .HasForeignKey(d => d.CreatedByAssistantID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // 23. PATIENT -> BOOKINGS (One Patient has Many Bookings)
            // ============================================================
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingID);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne<Patient>()
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 24. DOCTOR -> BOOKINGS (One Doctor has Many Bookings)
            // ============================================================
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne<Doctor>()
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 25. APPOINTMENT -> BOOKING (One Appointment has One Booking - One-to-One)
            // ============================================================
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(d => d.Appointment)
                    .WithMany(a => a.Bookings)
                    .HasForeignKey(d => d.AppointmentID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Enforce only one active booking per appointment at database level
                entity.HasIndex(e => e.AppointmentID)
                    .IsUnique()
                    .HasFilter("[IsActive] = 1");
            });

            // ============================================================
            // 26. CLINIC -> BOOKINGS (One Clinic has Many Bookings)
            // ============================================================
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne<Clinic>()
                    .WithMany()
                    .HasForeignKey(d => d.ClinicID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 27. PATIENT <-> DOCTORS (Many-to-Many through PatientDoctor)
            // ============================================================
            modelBuilder.Entity<PatientDoctor>(entity =>
            {
                entity.HasKey(e => new { e.DoctorID, e.PatientID });

                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 28. PATIENT -> PATIENTDRUGS (One Patient has Many PatientDrugs)
            // ============================================================
            modelBuilder.Entity<PatientDrug>(entity =>
            {
                entity.HasKey(e => e.DrugID);

                entity.HasOne(d => d.Patient)
                    .WithMany(p => p.PatientDrugs)
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 29. PATIENT -> PATIENTBLOODPRESSURE (One Patient has Many BloodPressure readings)
            // ============================================================
            modelBuilder.Entity<PatientBloodPressure>(entity =>
            {
                entity.HasKey(e => e.ID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 30. PATIENT -> PATIENTBLOODSUGAR (One Patient has Many BloodSugar readings)
            // ============================================================
            modelBuilder.Entity<PatientBloodSugar>(entity =>
            {
                entity.HasKey(e => e.ID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 31. NOTE -> DOCTOR (Many Notes belong to One Doctor)
            // ============================================================
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.NoteID);

                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 32. NOTE -> PATIENT (Many Notes belong to One Patient)
            // ============================================================
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 33. PRESCRIPTION -> DOCTOR (Many Prescriptions belong to One Doctor)
            // ============================================================
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.HasKey(e => e.PrescriptionID);

                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 34. PRESCRIPTION -> PATIENT (Many Prescriptions belong to One Patient)
            // ============================================================
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 35. PRESCRIPTIONITEM -> PRESCRIPTION (Many Items belong to One Prescription)
            // ============================================================
            modelBuilder.Entity<PrescriptionItem>(entity =>
            {
                entity.HasKey(e => e.ItemID);

                entity.HasOne(d => d.Prescription)
                    .WithMany(p => p.Items)
                    .HasForeignKey(d => d.PrescriptionID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // 36. TESTREPORT -> PATIENT (Many Reports belong to One Patient)
            // ============================================================
            modelBuilder.Entity<TestReport>(entity =>
            {
                entity.HasKey(e => e.ReportID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 37. TESTREPORT -> DOCTOR (Many Reports belong to One Doctor)
            // ============================================================
            modelBuilder.Entity<TestReport>(entity =>
            {
                entity.HasOne(d => d.Doctor)
                    .WithMany()
                    .HasForeignKey(d => d.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================================
            // 38. TESTREPORT -> LABTESTS (One Report covers Many LabTests)
            // ============================================================
            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.HasOne(d => d.TestReport)
                    .WithMany(r => r.LabTests)
                    .HasForeignKey(d => d.ReportID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============================================================
            // 39. WEIGHTTRACKING -> PATIENT (One Patient has Many WeightTracking records)
            // ============================================================
            modelBuilder.Entity<WeightTracking>(entity =>
            {
                entity.HasKey(e => e.WeightTrackingID);

                entity.HasOne(d => d.Patient)
                    .WithMany()
                    .HasForeignKey(d => d.PatientID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // CLINIC INVITATIONS
            // ============================================================
            modelBuilder.Entity<ClinicInvitation>(entity =>
            {
                entity.HasKey(e => e.ClinicInvitationID);

                entity.Property(e => e.AssistantEmail)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasMaxLength(32)
                    .IsRequired();

                entity.HasOne(e => e.Doctor)
                    .WithMany()
                    .HasForeignKey(e => e.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Clinic)
                    .WithMany()
                    .HasForeignKey(e => e.ClinicID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Assistant)
                    .WithMany()
                    .HasForeignKey(e => e.AssistantID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.DoctorID, e.ClinicID, e.AssistantID, e.Status });
            });
        }
    }
}

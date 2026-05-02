using Graduation_Project.Data;
using Graduation_Project.Hubs;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Repository;
using Graduation_Project.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace Graduation_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();

            // Database Connection (single registration)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            });

            // Register Repositories
            builder.Services.AddScoped<IAIModel, AIModelRepository>();
            builder.Services.AddScoped<IAlert, AlertRepository>();
            builder.Services.AddScoped<IAppointment, AppointmentRepository>();
            builder.Services.AddScoped<IAssistant, AssistantRepository>();
            builder.Services.AddScoped<IBloodGroup_Test, BloodGroup_TestRepository>();
            builder.Services.AddScoped<IBooking, BookingRepository>();
            builder.Services.AddScoped<ICBC_Test, CBC_TestRepository>();
            builder.Services.AddScoped<IClinic, ClinicRepository>();
            builder.Services.AddScoped<IDoctor, DoctorRepository>();
            builder.Services.AddScoped<IFerritin_Test, Ferritin_TestRepository>();
            builder.Services.AddScoped<IHbA1c_Test, HbA1c_TestRepository>();
            builder.Services.AddScoped<IHBsAg_Test, HBsAg_TestRepository>();
            builder.Services.AddScoped<IHCV_Test, HCV_TestRepository>();
            builder.Services.AddScoped<ILabTest, LabTestRepository>();
            builder.Services.AddScoped<IMedicalHistory, MedicalHistoryRepository>();
            builder.Services.AddScoped<IMedication, MedicationRepository>();
            builder.Services.AddScoped<IMedicationLog, MedicationLogRepository>();
            builder.Services.AddScoped<IMedicationSchedule, MedicationScheduleRepository>();
            builder.Services.AddScoped<INote, NoteRepository>();
            builder.Services.AddScoped<IPatient, PatientRepository>();
            builder.Services.AddScoped<IPatientBloodPressure, PatientBloodPressureRepository>();
            builder.Services.AddScoped<IPatientBloodSugar, PatientBloodSugarRepository>();
            builder.Services.AddScoped<IPatientDoctor, PatientDoctorRepository>();
            builder.Services.AddScoped<IPatientDrug, PatientDrugRepository>();
            builder.Services.AddScoped<IPlace, PlaceRepository>();
            builder.Services.AddScoped<IPrescription, PrescriptionRepository>();
            builder.Services.AddScoped<IPrescriptionItem, PrescriptionItemRepository>();
            builder.Services.AddScoped<ITestReport, TestReportRepository>();
            builder.Services.AddScoped<ITSH_Test, TSH_TestRepository>();
            builder.Services.AddScoped<IUltrasoundImage, UltrasoundImageRepository>();
            builder.Services.AddScoped<IUrinalysis_Test, Urinalysis_TestRepository>();
            builder.Services.AddScoped<IWeightTracking, WeightTrackingRepository>();

            // Register Services
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<AlertService>();
            builder.Services.AddScoped<AssistantScheduleService>();
            builder.Services.AddScoped<MedicationService>();
            builder.Services.AddScoped<MedicationAdherenceService>();
            builder.Services.AddScoped<MedicationReminderService>();
            builder.Services.AddSingleton<IChatMessageCrypto, ChatMessageCrypto>();
            builder.Services.AddHostedService<MedicationReminderHostedService>();

            // ?? Product OCR ????????????????????????????????????????????????
            builder.Services.AddHttpClient("ProductOcr", client =>
            {
                client.BaseAddress = new Uri("https://eman123yasser-product-ocr.hf.space/");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            builder.Services.AddScoped<ProductOcrClient>();
            // ??????????????????????????????????????????????????????????????

            var app = builder.Build();

            // ?? Seed the database ????????????????????????????????????????
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    if (!await db.Database.CanConnectAsync())
                    {
                        app.Logger.LogWarning("Database is not reachable during startup. Skipping startup SQL/seeding to prevent app crash.");
                    }
                    else
                    {
                        // Ensure chat persistence table exists for real-time messaging.
                        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages](
        [ChatMessageId] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SenderUserId] NVARCHAR(450) NOT NULL,
        [ReceiverUserId] NVARCHAR(450) NOT NULL,
        [Message] NVARCHAR(2000) NOT NULL,
        [SentAtUtc] DATETIME2 NOT NULL,
        [IsRead] BIT NOT NULL,
        [ReadAtUtc] DATETIME2 NULL,
        CONSTRAINT [FK_ChatMessages_AspNetUsers_SenderUserId]
            FOREIGN KEY ([SenderUserId]) REFERENCES [dbo].[AspNetUsers]([Id]),
        CONSTRAINT [FK_ChatMessages_AspNetUsers_ReceiverUserId]
            FOREIGN KEY ([ReceiverUserId]) REFERENCES [dbo].[AspNetUsers]([Id])
    );

    CREATE INDEX [IX_ChatMessages_SenderUserId_ReceiverUserId_SentAtUtc]
        ON [dbo].[ChatMessages]([SenderUserId], [ReceiverUserId], [SentAtUtc]);

    CREATE INDEX [IX_ChatMessages_ReceiverUserId_IsRead]
        ON [dbo].[ChatMessages]([ReceiverUserId], [IsRead]);
 END");

                        await DataSeeder.SeedAsync(db);
                    }
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Startup database initialization failed. Continuing app startup.");
                }
            }
            // ????????????????????????????????????????????????????????????

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
           //     app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}

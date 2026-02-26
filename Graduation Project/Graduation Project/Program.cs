using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Repository;
using Graduation_Project.Services;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Database Connection (single registration)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
            builder.Services.AddScoped<INote, NoteRepository>();
            builder.Services.AddScoped<IPatient, PatientRepository>();
            builder.Services.AddScoped<IPatientBloodPressure, PatientBloodPressureRepository>();
            builder.Services.AddScoped<IPatientBloodSugar, PatientBloodSugarRepository>();
            builder.Services.AddScoped<IPatientDoctor, PatientDoctorRepository>();
            builder.Services.AddScoped<IPatientDrug, PatientDrugRepository>();
            builder.Services.AddScoped<IPlace, PlaceRepository>();
            builder.Services.AddScoped<IPrescription, PrescriptionRepository>();
            builder.Services.AddScoped<IPrescriptionItem, PrescriptionItemRepository>();
            builder.Services.AddScoped<IRole, RoleRepository>();
            builder.Services.AddScoped<ITestReport, TestReportRepository>();
            builder.Services.AddScoped<ITSH_Test, TSH_TestRepository>();
            builder.Services.AddScoped<IUltrasoundImage, UltrasoundImageRepository>();
            builder.Services.AddScoped<IUrinalysis_Test, Urinalysis_TestRepository>();
            builder.Services.AddScoped<IUser, UserRepository>();
            builder.Services.AddScoped<IWeightTracking, WeightTrackingRepository>();

            // Register Services
            builder.Services.AddScoped<AlertService>();

            var app = builder.Build();

            // ?? Seed the database ????????????????????????????????????????
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await DataSeeder.SeedAsync(db);
            }
            // ????????????????????????????????????????????????????????????

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

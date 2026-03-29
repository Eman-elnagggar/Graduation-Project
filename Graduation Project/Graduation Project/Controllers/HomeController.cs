using System.Diagnostics;
using System.Security.Claims;
using Graduation_Project.Data;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    if (User.IsInRole("Patient"))
                    {
                        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == userId);
                        if (patient != null)
                            return RedirectToAction("Index", "Patient", new { id = patient.PatientID });
                    }

                    if (User.IsInRole("Assistant"))
                    {
                        var assistant = await _context.Assistants.FirstOrDefaultAsync(a => a.UserID == userId);
                        if (assistant != null)
                            return RedirectToAction("Index", "Assistant", new { id = assistant.AssistantID });
                    }

                    if (User.IsInRole("Doctor"))
                    {
                        return RedirectToAction("Index", "Doctor");
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

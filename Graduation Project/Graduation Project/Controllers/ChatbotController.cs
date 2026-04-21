using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class ChatbotController : Controller
    {
        // GET /Chatbot/Index/5
        [HttpGet]
        public IActionResult Index(int id = 0)
        {
            ViewData["Title"] = "MamaCare Assistant";
            ViewData["ActivePage"] = "Chatbot";
            ViewData["PatientId"] = id;
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    public class CommunityController : Controller
    {
        // GET /Community/Index/5
        [HttpGet]
        public IActionResult Index(int id = 0)
        {
            ViewData["Title"]      = "Community";
            ViewData["ActivePage"] = "Community";
            ViewData["PatientId"]  = id;
            return View();
        }
    }
}

using Graduation_Project.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult UnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { count = 0 });

            var count = _context.ChatMessages
                .Count(m => m.ReceiverUserId == userId && !m.IsRead);

            return Json(new { count });
        }
    }
}

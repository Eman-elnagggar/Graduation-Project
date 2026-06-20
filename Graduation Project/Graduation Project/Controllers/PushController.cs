using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [IgnoreAntiforgeryToken]
    public class PushController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPushNotificationService _push;

        public PushController(AppDbContext db, IPushNotificationService push)
        {
            _db = db;
            _push = push;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VapidPublicKey()
            => Content(_push.GetVapidPublicKey(), "text/plain");

        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request)
        {
            if (request?.Endpoint == null || request.Keys == null)
                return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var existing = _db.UserPushSubscriptions
                .FirstOrDefault(s => s.Endpoint == request.Endpoint);

            if (existing != null)
            {
                existing.UserId = userId;
                existing.P256DH = request.Keys.P256dh;
                existing.Auth = request.Keys.Auth;
            }
            else
            {
                _db.UserPushSubscriptions.Add(new UserPushSubscription
                {
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    P256DH = request.Keys.P256dh,
                    Auth = request.Keys.Auth,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public IActionResult Status()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = _db.UserPushSubscriptions.Count(s => s.UserId == userId);
            return Json(new { subscriptions = count });
        }

        [HttpPost]
        public async Task<IActionResult> Test()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = _db.UserPushSubscriptions.Count(s => s.UserId == userId);

            if (count > 0)
                await _push.SendToUserAsync(userId, "NABD نبض", "Test notification ✓", "/");

            return Json(new { subscriptions = count });
        }

        [HttpPost]
        public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Endpoint))
                return BadRequest();

            var sub = _db.UserPushSubscriptions
                .FirstOrDefault(s => s.Endpoint == request.Endpoint);

            if (sub != null)
            {
                _db.UserPushSubscriptions.Remove(sub);
                await _db.SaveChangesAsync();
            }

            return Ok();
        }
    }

    public class PushSubscribeRequest
    {
        public string Endpoint { get; set; } = "";
        public string? ExpirationTime { get; set; }
        public PushSubscribeKeys Keys { get; set; } = null!;
    }

    public class PushSubscribeKeys
    {
        public string P256dh { get; set; } = "";
        public string Auth { get; set; } = "";
    }

    public class PushUnsubscribeRequest
    {
        public string Endpoint { get; set; } = "";
    }
}

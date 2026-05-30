using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    public class ChatbotController : Controller
    {
        private const string RoleUser = "User";
        private const string RoleBot = "Bot";
        private readonly IChatbotService _chatbotService;
        private readonly IChatbotHistoryService _chatbotHistoryService;
        private readonly AppDbContext _context;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IChatbotService chatbotService,
            IChatbotHistoryService chatbotHistoryService,
            AppDbContext context,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _chatbotHistoryService = chatbotHistoryService;
            _context = context;
            _logger = logger;
        }

        // GET /Chatbot/Index/5
        [HttpGet]
        public IActionResult Index(int id = 0)
        {
            ViewData["Title"] = "MamaCare Assistant";
            ViewData["ActivePage"] = "Chatbot";
            ViewData["PatientId"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromBody] ChatbotAskRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message is required." });
            }

            var (patientId, forbidden) = await ResolvePatientIdAsync(request.PatientId, cancellationToken);
            if (forbidden)
            {
                return Forbid();
            }

            if (patientId <= 0)
            {
                return BadRequest(new { message = "Patient id is required." });
            }

            ChatbotMessage? userMessage = null;
            try
            {
                userMessage = await _chatbotHistoryService.SaveMessageAsync(new ChatbotMessage
                {
                    PatientID = patientId,
                    Role = RoleUser,
                    Message = request.Message.Trim(),
                    SentAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist user chatbot message.");
                return StatusCode(503, new { message = "Chat history is unavailable. Please try again later." });
            }

            try
            {
                var reply = await _chatbotService.GetReplyAsync(request.Message, cancellationToken);
                ChatbotMessage? botMessage = null;
                try
                {
                    botMessage = await _chatbotHistoryService.SaveMessageAsync(new ChatbotMessage
                    {
                        PatientID = patientId,
                        Role = RoleBot,
                        Message = reply.Response,
                        RiskLevel = reply.RiskLevel,
                        Recommendation = reply.Recommendation,
                        SentAtUtc = DateTime.UtcNow
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist bot chatbot message.");
                    return StatusCode(503, new { message = "Chat history is unavailable. Please try again later." });
                }

                return Json(new
                {
                    response = reply.Response,
                    risk_level = reply.RiskLevel,
                    recommendation = reply.Recommendation,
                    userMessage = new
                    {
                        id = userMessage?.ChatbotMessageId ?? 0,
                        sentAtUtc = userMessage == null
                            ? (DateTimeOffset?)null
                            : ToUtcOffset(userMessage.SentAtUtc)
                    },
                    botMessage = new
                    {
                        id = botMessage?.ChatbotMessageId ?? 0,
                        sentAtUtc = botMessage == null
                            ? (DateTimeOffset?)null
                            : ToUtcOffset(botMessage.SentAtUtc)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot API call failed.");
                return StatusCode(503, new { message = "Chatbot service is unavailable. Please try again later." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> History(int? patientId, CancellationToken cancellationToken)
        {
            var (resolvedPatientId, forbidden) = await ResolvePatientIdAsync(patientId, cancellationToken);
            if (forbidden)
            {
                return Forbid();
            }

            if (resolvedPatientId <= 0)
            {
                return BadRequest(new { message = "Patient id is required." });
            }

            try
            {
                var messages = await _chatbotHistoryService.GetHistoryAsync(resolvedPatientId, cancellationToken);
                var payload = messages.Select(m => new
                {
                    id = m.ChatbotMessageId,
                    role = m.Role,
                    message = m.Message,
                    risk_level = m.RiskLevel,
                    recommendation = m.Recommendation,
                    sentAtUtc = ToUtcOffset(m.SentAtUtc)
                });

                return Json(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load chatbot history.");
                return StatusCode(503, new { message = "Chat history is unavailable. Please try again later." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear([FromBody] ChatbotClearRequest request, CancellationToken cancellationToken)
        {
            var (resolvedPatientId, forbidden) = await ResolvePatientIdAsync(request?.PatientId, cancellationToken);
            if (forbidden)
            {
                return Forbid();
            }

            if (resolvedPatientId <= 0)
            {
                return BadRequest(new { message = "Patient id is required." });
            }

            try
            {
                await _chatbotHistoryService.ClearHistoryAsync(resolvedPatientId, cancellationToken);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear chatbot history.");
                return StatusCode(503, new { message = "Unable to clear chat history right now." });
            }
        }

        private async Task<(int patientId, bool forbidden)> ResolvePatientIdAsync(int? patientId, CancellationToken cancellationToken)
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var dbPatientId = await _context.Patients
                    .Where(p => p.UserID == userId)
                    .Select(p => (int?)p.PatientID)
                    .FirstOrDefaultAsync(cancellationToken);

                if (dbPatientId.HasValue && dbPatientId.Value > 0)
                {
                    if (patientId.HasValue && patientId.Value > 0 && patientId.Value != dbPatientId.Value)
                    {
                        return (0, true);
                    }

                    return (dbPatientId.Value, false);
                }
            }

            if (patientId.HasValue && patientId.Value > 0)
            {
                return (patientId.Value, false);
            }

            return (0, false);
        }

        private static DateTimeOffset ToUtcOffset(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return new DateTimeOffset(utcValue);
        }
    }
}

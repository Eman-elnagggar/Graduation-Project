using Graduation_Project.Data;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class CommunityController : Controller
    {
        private readonly AppDbContext _db;

        public CommunityController(AppDbContext db)
        {
            _db = db;
        }

        private int GetCurrentPatientId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return 0;
            return _db.Patients
                .Where(p => p.UserID == userId)
                .Select(p => p.PatientID)
                .FirstOrDefault();
        }

        [HttpGet]
        public IActionResult Index(int id = 0)
        {
            if (id <= 0)
                id = GetCurrentPatientId();

            ViewData["Title"] = "Community";
            ViewData["ActivePage"] = "Community";
            ViewData["PatientId"] = id;
            return View();
        }

        // ────────────────────────────────────────────────
        // GET /Community/GetPosts?category=&search=&page=1
        // ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetPosts(string? category, string? search, int page = 1)
        {
            var currentPatientId = GetCurrentPatientId();
            const int pageSize = 10;

            var query = _db.CommunityPosts
                .Include(p => p.Patient).ThenInclude(p => p!.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Title.Contains(search) || p.Content.Contains(search));

            var total = query.Count();
            var posts = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.CommunityPostId,
                    p.Title,
                    p.Content,
                    p.Category,
                    p.CreatedAt,
                    AuthorName = (p.Patient != null && p.Patient.User != null)
                        ? (p.Patient.User.FirstName + " " + p.Patient.User.LastName).Trim()
                        : "Anonymous",
                    AuthorId = p.PatientID,
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count,
                    IsLikedByMe = p.Likes.Any(l => l.PatientID == currentPatientId),
                    IsMyPost = p.PatientID == currentPatientId
                })
                .ToList();

            return Json(new
            {
                success = true,
                posts,
                totalCount = total,
                pageSize,
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        // ────────────────────────────────────────────────
        // POST /Community/CreatePost
        // ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePost([FromForm] string title, [FromForm] string content, [FromForm] string category = "General")
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Title and content are required." });

            var patientId = GetCurrentPatientId();
            if (patientId <= 0)
                return Json(new { success = false, message = "Patient not found." });

            var post = new CommunityPost
            {
                Title = title.Trim(),
                Content = content.Trim(),
                Category = category.Trim(),
                PatientID = patientId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.CommunityPosts.Add(post);
            _db.SaveChanges();

            var patient = _db.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientID == patientId);

            var authorName = patient?.User != null
                ? (patient.User.FirstName + " " + patient.User.LastName).Trim()
                : "Anonymous";

            return Json(new
            {
                success = true,
                post = new
                {
                    post.CommunityPostId,
                    post.Title,
                    post.Content,
                    post.Category,
                    post.CreatedAt,
                    AuthorName = authorName,
                    AuthorId = patientId,
                    LikeCount = 0,
                    CommentCount = 0,
                    IsLikedByMe = false,
                    IsMyPost = true
                }
            });
        }

        // ────────────────────────────────────────────────
        // POST /Community/DeletePost
        // ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost([FromForm] int postId)
        {
            var patientId = GetCurrentPatientId();
            var post = _db.CommunityPosts.FirstOrDefault(p => p.CommunityPostId == postId);

            if (post == null)
                return Json(new { success = false, message = "Post not found." });

            if (post.PatientID != patientId)
                return Json(new { success = false, message = "You can only delete your own posts." });

            _db.CommunityPosts.Remove(post);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        // ────────────────────────────────────────────────
        // POST /Community/ToggleLike
        // ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleLike([FromForm] int postId)
        {
            var patientId = GetCurrentPatientId();
            if (patientId <= 0)
                return Json(new { success = false });

            var existing = _db.CommunityLikes
                .FirstOrDefault(l => l.CommunityPostId == postId && l.PatientID == patientId);

            if (existing != null)
            {
                _db.CommunityLikes.Remove(existing);
            }
            else
            {
                _db.CommunityLikes.Add(new CommunityLike
                {
                    CommunityPostId = postId,
                    PatientID = patientId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.SaveChanges();

            var likeCount = _db.CommunityLikes.Count(l => l.CommunityPostId == postId);
            var liked = existing == null;

            return Json(new { success = true, liked, likeCount });
        }

        // ────────────────────────────────────────────────
        // GET /Community/GetComments?postId=
        // ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetComments(int postId)
        {
            var currentPatientId = GetCurrentPatientId();

            var comments = _db.CommunityComments
                .Include(c => c.Patient).ThenInclude(p => p!.User)
                .Where(c => c.CommunityPostId == postId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CommunityCommentId,
                    c.Content,
                    c.CreatedAt,
                    AuthorName = (c.Patient != null && c.Patient.User != null)
                        ? (c.Patient.User.FirstName + " " + c.Patient.User.LastName).Trim()
                        : "Anonymous",
                    IsMyComment = c.PatientID == currentPatientId
                })
                .ToList();

            return Json(new { success = true, comments });
        }

        // ────────────────────────────────────────────────
        // POST /Community/AddComment
        // ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment([FromForm] int postId, [FromForm] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Comment cannot be empty." });

            var patientId = GetCurrentPatientId();
            if (patientId <= 0)
                return Json(new { success = false, message = "Patient not found." });

            var post = _db.CommunityPosts.Find(postId);
            if (post == null)
                return Json(new { success = false, message = "Post not found." });

            var comment = new CommunityComment
            {
                CommunityPostId = postId,
                PatientID = patientId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.CommunityComments.Add(comment);
            _db.SaveChanges();

            var patient = _db.Patients.Include(p => p.User).FirstOrDefault(p => p.PatientID == patientId);
            var authorName = patient?.User != null
                ? (patient.User.FirstName + " " + patient.User.LastName).Trim()
                : "Anonymous";

            return Json(new
            {
                success = true,
                comment = new
                {
                    comment.CommunityCommentId,
                    comment.Content,
                    comment.CreatedAt,
                    AuthorName = authorName,
                    IsMyComment = true
                }
            });
        }

        // ────────────────────────────────────────────────
        // POST /Community/DeleteComment
        // ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment([FromForm] int commentId)
        {
            var patientId = GetCurrentPatientId();
            var comment = _db.CommunityComments.FirstOrDefault(c => c.CommunityCommentId == commentId);

            if (comment == null)
                return Json(new { success = false, message = "Comment not found." });

            if (comment.PatientID != patientId)
                return Json(new { success = false, message = "You can only delete your own comments." });

            _db.CommunityComments.Remove(comment);
            _db.SaveChanges();

            return Json(new { success = true });
        }
    }
}

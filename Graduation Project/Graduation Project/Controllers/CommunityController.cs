using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient,Doctor")]
    public class CommunityController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IPatientNotificationService _patientNotifications;

        public CommunityController(AppDbContext db, IWebHostEnvironment env,
            IPatientNotificationService patientNotifications)
        {
            _db = db;
            _env = env;
            _patientNotifications = patientNotifications;
        }

        private bool IsDoctor() => User.IsInRole("Doctor");

        private int GetCurrentPatientId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return 0;
            return _db.Patients
                .Where(p => p.UserID == userId)
                .Select(p => p.PatientID)
                .FirstOrDefault();
        }

        private int GetCurrentDoctorId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return 0;
            return _db.Doctors
                .Where(d => d.UserID == userId)
                .Select(d => d.DoctorID)
                .FirstOrDefault();
        }

        private string GetActorDisplayName(bool isDoctor, int doctorId, int patientId)
        {
            if (isDoctor)
            {
                var doctor = _db.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorID == doctorId);
                return doctor?.User != null
                    ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "A doctor";
            }

            var patient = _db.Patients.Include(p => p.User).FirstOrDefault(p => p.PatientID == patientId);
            return patient?.User != null
                ? $"{patient.User.FirstName} {patient.User.LastName}".Trim()
                : "Someone";
        }

        [HttpGet]
        public IActionResult Index(int id = 0)
        {
            ViewData["Title"] = "Community";
            ViewData["ActivePage"] = "Community";

            if (IsDoctor())
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = _db.Doctors
                    .Include(d => d.User)
                    .FirstOrDefault(d => d.UserID == userId);

                ViewData["IsDoctor"] = true;
                ViewData["DoctorId"] = doctor?.DoctorID ?? 0;
                ViewData["DoctorName"] = doctor?.User != null
                    ? $"{doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "Doctor";
            }
            else
            {
                if (id <= 0)
                    id = GetCurrentPatientId();

                ViewData["IsDoctor"] = false;
                ViewData["PatientId"] = id;
            }

            return View();
        }

        // ────────────────────────────────────────────────
        // GET /Community/GetPosts?category=&search=&page=1
        // ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetPosts(string? category, string? search, int page = 1)
        {
            var isDoctor = IsDoctor();
            var currentPatientId = isDoctor ? 0 : GetCurrentPatientId();
            var currentDoctorId = isDoctor ? GetCurrentDoctorId() : 0;
            const int pageSize = 10;

            var query = _db.CommunityPosts
                .Include(p => p.Patient).ThenInclude(p => p!.User)
                .Include(p => p.Doctor).ThenInclude(d => d!.User)
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
                    p.ImageUrl,
                    p.CreatedAt,
                    AuthorName = p.DoctorID != null
                        ? (p.Doctor != null && p.Doctor.User != null
                            ? ("Dr. " + p.Doctor.User.FirstName + " " + p.Doctor.User.LastName).Trim()
                            : "Doctor")
                        : (p.Patient != null && p.Patient.User != null
                            ? (p.Patient.User.FirstName + " " + p.Patient.User.LastName).Trim()
                            : "Anonymous"),
                    AuthorIsDoctor = p.DoctorID != null,
                    AuthorUserId = p.DoctorID != null
                        ? (p.Doctor != null ? p.Doctor.UserID : null)
                        : (p.Patient != null && p.Patient.User != null ? p.Patient.User.Id : null),
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count,
                    IsLikedByMe = isDoctor
                        ? p.Likes.Any(l => l.DoctorID == currentDoctorId)
                        : p.Likes.Any(l => l.PatientID == currentPatientId),
                    IsMyPost = isDoctor
                        ? p.DoctorID == currentDoctorId
                        : p.PatientID == currentPatientId
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
        public async Task<IActionResult> CreatePost([FromForm] string title, [FromForm] string content, [FromForm] string category = "General", [FromForm] IFormFile? image = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Title and content are required." });

            var isDoctor = IsDoctor();
            var patientId = isDoctor ? 0 : GetCurrentPatientId();
            var doctorId = isDoctor ? GetCurrentDoctorId() : 0;

            if (isDoctor && doctorId <= 0)
                return Json(new { success = false, message = "Doctor not found." });
            if (!isDoctor && patientId <= 0)
                return Json(new { success = false, message = "Patient not found." });

            var folderKey = isDoctor ? $"doctor-{doctorId}" : patientId.ToString();

            string? imageUrl = null;
            if (image != null && image.Length > 0)
            {
                var (savedUrl, error) = await SaveImageAsync(image, folderKey);
                if (error != null)
                    return Json(new { success = false, message = error });
                imageUrl = savedUrl;
            }

            var post = new CommunityPost
            {
                Title = title.Trim(),
                Content = content.Trim(),
                Category = category.Trim(),
                ImageUrl = imageUrl,
                PatientID = isDoctor ? (int?)null : patientId,
                DoctorID = isDoctor ? doctorId : (int?)null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.CommunityPosts.Add(post);
            _db.SaveChanges();

            string authorName;
            string? authorUserId;
            if (isDoctor)
            {
                var doctor = _db.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorID == doctorId);
                authorName = doctor?.User != null
                    ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "Doctor";
                authorUserId = doctor?.UserID;
            }
            else
            {
                var patient = _db.Patients.Include(p => p.User).FirstOrDefault(p => p.PatientID == patientId);
                authorName = patient?.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}".Trim()
                    : "Anonymous";
                authorUserId = patient?.User?.Id;
            }

            return Json(new
            {
                success = true,
                post = new
                {
                    post.CommunityPostId,
                    post.Title,
                    post.Content,
                    post.Category,
                    post.ImageUrl,
                    post.CreatedAt,
                    AuthorName = authorName,
                    AuthorIsDoctor = isDoctor,
                    AuthorUserId = authorUserId,
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
            var post = _db.CommunityPosts.FirstOrDefault(p => p.CommunityPostId == postId);

            if (post == null)
                return Json(new { success = false, message = "Post not found." });

            if (!OwnsAuthored(post.PatientID, post.DoctorID))
                return Json(new { success = false, message = "You can only delete your own posts." });

            DeleteImageFile(post.ImageUrl);

            _db.CommunityPosts.Remove(post);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        // ────────────────────────────────────────────────
        // Returns true when the authored content (patient/doctor ids)
        // belongs to the current user.
        // ────────────────────────────────────────────────
        private bool OwnsAuthored(int? patientId, int? doctorId)
        {
            if (IsDoctor())
            {
                var myDoctorId = GetCurrentDoctorId();
                return myDoctorId != 0 && doctorId == myDoctorId;
            }

            var myPatientId = GetCurrentPatientId();
            return myPatientId != 0 && patientId == myPatientId;
        }

        // ────────────────────────────────────────────────
        // Image helpers
        // ────────────────────────────────────────────────
        private async Task<(string? url, string? error)> SaveImageAsync(IFormFile image, string folderKey)
        {
            if (image.Length > 5 * 1024 * 1024)
                return (null, "Image exceeds the 5 MB limit.");

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "image/gif", "image/webp"
            };

            if (!allowedTypes.Contains(image.ContentType))
                return (null, "Only JPEG, PNG, GIF, or WebP images are allowed.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "community", folderKey);
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(dir, fileName);

            using (var stream = System.IO.File.Create(filePath))
                await image.CopyToAsync(stream);

            return ($"/uploads/community/{folderKey}/{fileName}", null);
        }

        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;
            try
            {
                var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_env.WebRootPath, relativePath);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                // Best-effort cleanup; ignore failures.
            }
        }

        // ────────────────────────────────────────────────
        // POST /Community/ToggleLike
        // ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleLike([FromForm] int postId)
        {
            var isDoctor = IsDoctor();
            var patientId = isDoctor ? 0 : GetCurrentPatientId();
            var doctorId = isDoctor ? GetCurrentDoctorId() : 0;

            if (isDoctor && doctorId <= 0) return Json(new { success = false });
            if (!isDoctor && patientId <= 0) return Json(new { success = false });

            var existing = isDoctor
                ? _db.CommunityLikes.FirstOrDefault(l => l.CommunityPostId == postId && l.DoctorID == doctorId)
                : _db.CommunityLikes.FirstOrDefault(l => l.CommunityPostId == postId && l.PatientID == patientId);

            if (existing != null)
            {
                _db.CommunityLikes.Remove(existing);
            }
            else
            {
                _db.CommunityLikes.Add(new CommunityLike
                {
                    CommunityPostId = postId,
                    PatientID = isDoctor ? (int?)null : patientId,
                    DoctorID = isDoctor ? doctorId : (int?)null,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.SaveChanges();

            var likeCount = _db.CommunityLikes.Count(l => l.CommunityPostId == postId);
            var liked = existing == null;

            // Notify the post author (if a patient) when their post gets a new like —
            // not on unlike, and not when liking their own post.
            if (liked)
            {
                var post = _db.CommunityPosts.Find(postId);
                if (post?.PatientID is int likePostOwner
                    && !(isDoctor == false && likePostOwner == patientId))
                {
                    var likerName = GetActorDisplayName(isDoctor, doctorId, patientId);
                    _patientNotifications.Notify(likePostOwner,
                        "New like on your post",
                        $"{likerName} liked your post \"{post.Title}\".",
                        PatientNotificationTypes.Community,
                        "/Community");
                }
            }

            return Json(new { success = true, liked, likeCount });
        }

        // ────────────────────────────────────────────────
        // GET /Community/GetComments?postId=
        // ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult GetComments(int postId)
        {
            var isDoctor = IsDoctor();
            var currentPatientId = isDoctor ? 0 : GetCurrentPatientId();
            var currentDoctorId = isDoctor ? GetCurrentDoctorId() : 0;

            var comments = _db.CommunityComments
                .Include(c => c.Patient).ThenInclude(p => p!.User)
                .Include(c => c.Doctor).ThenInclude(d => d!.User)
                .Where(c => c.CommunityPostId == postId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CommunityCommentId,
                    c.Content,
                    c.CreatedAt,
                    AuthorName = c.DoctorID != null
                        ? (c.Doctor != null && c.Doctor.User != null
                            ? ("Dr. " + c.Doctor.User.FirstName + " " + c.Doctor.User.LastName).Trim()
                            : "Doctor")
                        : (c.Patient != null && c.Patient.User != null
                            ? (c.Patient.User.FirstName + " " + c.Patient.User.LastName).Trim()
                            : "Anonymous"),
                    AuthorIsDoctor = c.DoctorID != null,
                    AuthorUserId = c.DoctorID != null
                        ? (c.Doctor != null ? c.Doctor.UserID : null)
                        : (c.Patient != null && c.Patient.User != null ? c.Patient.User.Id : null),
                    IsMyComment = isDoctor
                        ? c.DoctorID == currentDoctorId
                        : c.PatientID == currentPatientId
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

            var isDoctor = IsDoctor();
            var patientId = isDoctor ? 0 : GetCurrentPatientId();
            var doctorId = isDoctor ? GetCurrentDoctorId() : 0;

            if (isDoctor && doctorId <= 0)
                return Json(new { success = false, message = "Doctor not found." });
            if (!isDoctor && patientId <= 0)
                return Json(new { success = false, message = "Patient not found." });

            var post = _db.CommunityPosts.Find(postId);
            if (post == null)
                return Json(new { success = false, message = "Post not found." });

            var comment = new CommunityComment
            {
                CommunityPostId = postId,
                PatientID = isDoctor ? (int?)null : patientId,
                DoctorID = isDoctor ? doctorId : (int?)null,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.CommunityComments.Add(comment);
            _db.SaveChanges();

            string authorName;
            if (isDoctor)
            {
                var doctor = _db.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorID == doctorId);
                authorName = doctor?.User != null
                    ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "Doctor";
            }
            else
            {
                var patient = _db.Patients.Include(p => p.User).FirstOrDefault(p => p.PatientID == patientId);
                authorName = patient?.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}".Trim()
                    : "Anonymous";
            }

            // Notify the post author (if a patient) about the new comment — unless they
            // are commenting on their own post.
            if (post.PatientID is int commentPostOwner
                && !(isDoctor == false && commentPostOwner == patientId))
            {
                var snippet = comment.Content.Length > 80 ? comment.Content[..80] + "…" : comment.Content;
                _patientNotifications.Notify(commentPostOwner,
                    "New comment on your post",
                    $"{authorName} commented on \"{post.Title}\": {snippet}",
                    PatientNotificationTypes.Community,
                    "/Community");
            }

            return Json(new
            {
                success = true,
                comment = new
                {
                    comment.CommunityCommentId,
                    comment.Content,
                    comment.CreatedAt,
                    AuthorName = authorName,
                    AuthorIsDoctor = isDoctor,
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
            var comment = _db.CommunityComments.FirstOrDefault(c => c.CommunityCommentId == commentId);

            if (comment == null)
                return Json(new { success = false, message = "Comment not found." });

            if (!OwnsAuthored(comment.PatientID, comment.DoctorID))
                return Json(new { success = false, message = "You can only delete your own comments." });

            _db.CommunityComments.Remove(comment);
            _db.SaveChanges();

            return Json(new { success = true });
        }
    }
}

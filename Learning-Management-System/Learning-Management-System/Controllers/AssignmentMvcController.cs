using Learning_Management_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class AssignmentMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private const string BaseUrl = "http://localhost:5171";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AssignmentMvcController(IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        private string? GetToken() => HttpContext.Session.GetString("JwtToken");
        private string GetRole() => HttpContext.Session.GetString("UserRole") ?? "Student";
        private string GetUserId() => HttpContext.Session.GetString("UserId") ?? "";
        private bool IsAuthenticated() => !string.IsNullOrEmpty(GetToken());

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task LoadCoursesAsync()
        {
            try
            {
                var client = CreateClient();
                var c = await client.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }
        }

        // Teacher/Admin: list assignments by subject
        public async Task<IActionResult> Index(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Teacher" && role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.UserRole = role;
            ViewBag.SubjectId = subjectId;
            ViewBag.AssignmentsJson = "[]";

            if (subjectId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/assignment/subject/{subjectId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.AssignmentsJson = await res.Content.ReadAsStringAsync();
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            await LoadCoursesAsync();
            return View();
        }

        // Teacher: create assignment
        public async Task<IActionResult> Create()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Teacher" && role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.UserRole = role;
            await LoadCoursesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(long subjectId, string title, string description, int maxScore, DateTime dueDate)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { subjectId, title, description, maxScore, dueDate };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/assignment", content);

                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Assignment created!";
                    return RedirectToAction("Index", new { subjectId });
                }
                TempData["Error"] = $"Failed: {await res.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Create");
        }

        // Teacher: view submissions for an assignment (includes grade info)
        public async Task<IActionResult> Submissions(long? assignmentId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Teacher" && role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.UserRole = role;
            ViewBag.AssignmentId = assignmentId;
            ViewBag.SubmissionsJson = "[]";
            ViewBag.AssignmentJson = "{}";

            if (!assignmentId.HasValue) return View();

            try
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Subject)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId.Value && !a.IsDeleted);

                if (assignment != null)
                {
                    ViewBag.AssignmentJson = JsonSerializer.Serialize(new
                    {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        maxScore = assignment.MaxScore,
                        dueDate = assignment.DueDate,
                        subjectName = assignment.Subject?.Name
                    }, _jsonOptions);
                }

                var submissions = await _context.Submissions
                    .Include(s => s.Student)
                    .Include(s => s.Grade).ThenInclude(g => g!.GradedBy)
                    .Where(s => s.AssignmentId == assignmentId.Value && !s.IsDeleted)
                    .OrderBy(s => s.SubmittedAt)
                    .ToListAsync();

                var data = submissions.Select(s => new
                {
                    id = s.Id,
                    studentId = s.StudentId,
                    studentName = s.Student?.FullName ?? s.Student?.UserName,
                    studentEmail = s.Student?.Email,
                    fileUrl = s.FileUrl,
                    status = s.Status,
                    submittedAt = s.SubmittedAt,
                    graded = s.Grade != null && !s.Grade.IsDeleted,
                    gradeScore = s.Grade?.Score,
                    gradeFeedback = s.Grade?.Feedback,
                    gradedByName = s.Grade?.GradedBy?.FullName ?? s.Grade?.GradedBy?.UserName,
                    gradedAt = s.Grade?.GradedAt
                });

                ViewBag.SubmissionsJson = JsonSerializer.Serialize(data, _jsonOptions);
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Grade(long submissionId, int score, string? feedback, long? assignmentId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var graderId = GetUserId();
            try
            {
                var submission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.Id == submissionId && !s.IsDeleted);
                if (submission == null) { TempData["Error"] = "Submission not found."; return RedirectToAction("Submissions", new { assignmentId }); }

                var existing = await _context.AssignmentGrades
                    .FirstOrDefaultAsync(g => g.SubmissionId == submissionId && !g.IsDeleted);

                if (existing != null)
                {
                    existing.Score = score;
                    existing.Feedback = feedback;
                    existing.GradedById = graderId;
                    existing.GradedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    TempData["Success"] = "Grade updated successfully!";
                }
                else
                {
                    _context.AssignmentGrades.Add(new Learning_Management_System.Models.AssignmentGrade
                    {
                        SubmissionId = submissionId,
                        Score = score,
                        Feedback = feedback,
                        GradedById = graderId,
                        GradedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                    TempData["Success"] = "Grade saved successfully!";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Submissions", new { assignmentId });
        }

        // Student: view my assignments with submission status
        public async Task<IActionResult> MyAssignments(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.SubjectId = subjectId;
            ViewBag.AssignmentsJson = "[]";

            if (subjectId.HasValue)
            {
                try
                {
                    var assignments = await _context.Assignments
                        .Where(a => a.SubjectId == subjectId.Value && !a.IsDeleted)
                        .OrderBy(a => a.DueDate)
                        .ToListAsync();

                    var assignmentIds = assignments.Select(a => a.Id).ToList();
                    var submissions = assignmentIds.Any()
                        ? await _context.Submissions
                            .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId) && !s.IsDeleted)
                            .ToListAsync()
                        : new List<Learning_Management_System.Models.Submission>();

                    var submissionMap = submissions.ToDictionary(s => s.AssignmentId);

                    var data = assignments.Select(a =>
                    {
                        var sub = submissionMap.TryGetValue(a.Id, out var s) ? s : null;
                        return new
                        {
                            id = a.Id,
                            title = a.Title,
                            description = a.Description,
                            maxScore = a.MaxScore,
                            dueDate = a.DueDate,
                            submitted = sub != null,
                            submittedAt = sub?.SubmittedAt,
                            submissionStatus = sub?.Status,
                            fileUrl = sub?.FileUrl
                        };
                    });

                    ViewBag.AssignmentsJson = JsonSerializer.Serialize(data, _jsonOptions);
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            await LoadCoursesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(long assignmentId, string? fileUrl, long? subjectId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var studentId = GetUserId();
            try
            {
                var assignment = await _context.Assignments.FindAsync(assignmentId);
                if (assignment == null || assignment.IsDeleted)
                {
                    TempData["Error"] = "Assignment not found.";
                    return RedirectToAction("MyAssignments", new { subjectId });
                }

                var existing = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId && !s.IsDeleted);

                var isLate = DateTime.UtcNow > assignment.DueDate;

                if (existing != null)
                {
                    existing.FileUrl = fileUrl;
                    existing.SubmittedAt = DateTime.UtcNow;
                    existing.Status = isLate ? "LATE" : "SUBMITTED";
                    existing.UpdatedAt = DateTime.UtcNow;
                    TempData["Success"] = "Assignment re-submitted successfully!";
                }
                else
                {
                    _context.Submissions.Add(new Learning_Management_System.Models.Submission
                    {
                        AssignmentId = assignmentId,
                        StudentId = studentId,
                        FileUrl = fileUrl,
                        Status = isLate ? "LATE" : "SUBMITTED",
                        SubmittedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                    TempData["Success"] = "Assignment submitted successfully!";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MyAssignments", new { subjectId });
        }

        // Student: view my grades with score, feedback, and graded status
        public async Task<IActionResult> MyGrades(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.SubjectId = subjectId;
            ViewBag.GradesJson = "[]";

            if (subjectId.HasValue)
            {
                try
                {
                    var assignments = await _context.Assignments
                        .Include(a => a.Subject)
                        .Where(a => a.SubjectId == subjectId.Value && !a.IsDeleted)
                        .OrderBy(a => a.DueDate)
                        .ToListAsync();

                    var assignmentIds = assignments.Select(a => a.Id).ToList();
                    var submissions = assignmentIds.Any()
                        ? await _context.Submissions
                            .Include(s => s.Grade).ThenInclude(g => g!.GradedBy)
                            .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId) && !s.IsDeleted)
                            .ToListAsync()
                        : new List<Learning_Management_System.Models.Submission>();

                    var submissionMap = submissions.ToDictionary(s => s.AssignmentId);

                    var gradesData = assignments.Select(a =>
                    {
                        var sub = submissionMap.TryGetValue(a.Id, out var s) ? s : null;
                        var grade = sub?.Grade != null && !sub.Grade.IsDeleted ? sub.Grade : null;
                        return new
                        {
                            id = a.Id,
                            title = a.Title,
                            description = a.Description,
                            maxScore = a.MaxScore,
                            dueDate = a.DueDate,
                            subjectName = a.Subject?.Name,
                            submitted = sub != null,
                            submittedAt = sub?.SubmittedAt,
                            submissionStatus = sub?.Status,
                            fileUrl = sub?.FileUrl,
                            graded = grade != null,
                            score = grade?.Score,
                            feedback = grade?.Feedback,
                            gradedByName = grade?.GradedBy?.FullName ?? grade?.GradedBy?.UserName,
                            gradedAt = grade?.GradedAt
                        };
                    });

                    ViewBag.GradesJson = JsonSerializer.Serialize(gradesData, _jsonOptions);
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            await LoadCoursesAsync();
            return View();
        }
    }
}

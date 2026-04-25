using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class AssignmentMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://localhost:5171";

        public AssignmentMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

            try
            {
                var client2 = CreateClient();
                var c = await client2.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

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
            try
            {
                var client = CreateClient();
                var c = await client.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

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
                var err = await res.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed: {err}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Create");
        }

        // Teacher: view submissions for an assignment
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

            if (assignmentId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var assignTask = client.GetAsync($"{BaseUrl}/api/assignment/{assignmentId}");
                    var subsTask = client.GetAsync($"{BaseUrl}/api/assignment/{assignmentId}/submissions");
                    await Task.WhenAll(assignTask, subsTask);

                    ViewBag.AssignmentJson = assignTask.Result.IsSuccessStatusCode
                        ? await assignTask.Result.Content.ReadAsStringAsync() : "{}";
                    ViewBag.SubmissionsJson = subsTask.Result.IsSuccessStatusCode
                        ? await subsTask.Result.Content.ReadAsStringAsync() : "[]";
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Grade(long submissionId, decimal score, string feedback, long? assignmentId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { submissionId, score, feedback };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/assignment/grade", content);
                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Grade saved!" : "Failed to save grade.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Submissions", new { assignmentId });
        }

        // Student: view my assignments
        public async Task<IActionResult> MyAssignments(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            ViewBag.UserRole = GetRole();
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

            try
            {
                var client2 = CreateClient();
                var c = await client2.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(long assignmentId, string? fileUrl)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var studentId = GetUserId();
            try
            {
                var client = CreateClient();
                var payload = new { assignmentId, studentId, fileUrl };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/assignment/submission", content);
                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Assignment submitted!" : "Submission failed.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MyAssignments");
        }

        // Student: view my grades
        public async Task<IActionResult> MyGrades(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.GradesJson = "[]";

            // For now, return assignments list with grades available
            if (subjectId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/assignment/subject/{subjectId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.AssignmentsJson = await res.Content.ReadAsStringAsync();
                }
                catch { }
            }

            try
            {
                var client2 = CreateClient();
                var c = await client2.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

            ViewBag.SubjectId = subjectId;
            return View();
        }
    }
}

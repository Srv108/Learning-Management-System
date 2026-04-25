using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class AttendanceMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://localhost:5171";

        public AttendanceMvcController(IHttpClientFactory httpClientFactory)
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

        // Teacher/Coordinator/Admin: list sessions
        public async Task<IActionResult> Index(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var role = GetRole();
            var canManage = role == "Teacher" || role == "CourseCoordinator" || role == "Admin";
            if (!canManage)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserRole = role;
            ViewBag.SubjectId = subjectId;
            ViewBag.SessionsJson = "[]";

            if (subjectId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/attendance/subject/{subjectId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.SessionsJson = await res.Content.ReadAsStringAsync();
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            // Load courses for dropdown
            try
            {
                var client2 = CreateClient();
                var c = await client2.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

            return View();
        }

        // Create attendance session
        public async Task<IActionResult> CreateSession()
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
        public async Task<IActionResult> CreateSession(long subjectId, DateTime sessionDate)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { subjectId, sessionDate };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/attendance/session", content);

                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Attendance session created!";
                    return RedirectToAction("Index", new { subjectId });
                }
                var err = await res.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed: {err}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("CreateSession");
        }

        // Mark attendance for a session
        public async Task<IActionResult> MarkAttendance(long sessionId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Teacher" && role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.UserRole = role;
            ViewBag.SessionId = sessionId;

            try
            {
                var client = CreateClient();
                var sessionTask = client.GetAsync($"{BaseUrl}/api/attendance/session/{sessionId}");
                var recordsTask = client.GetAsync($"{BaseUrl}/api/attendance/session/{sessionId}/records");
                await Task.WhenAll(sessionTask, recordsTask);

                ViewBag.SessionJson = sessionTask.Result.IsSuccessStatusCode
                    ? await sessionTask.Result.Content.ReadAsStringAsync() : "{}";
                ViewBag.RecordsJson = recordsTask.Result.IsSuccessStatusCode
                    ? await recordsTask.Result.Content.ReadAsStringAsync() : "[]";
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BulkMark(long sessionId, string recordsJson, long? subjectId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var records = JsonSerializer.Deserialize<List<object>>(recordsJson);
                var client = CreateClient();
                var payload = new { sessionId, records };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/attendance/bulk-mark", content);

                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Attendance marked successfully!" : "Failed to mark attendance.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MarkAttendance", new { sessionId });
        }

        // Student: my attendance summary
        public async Task<IActionResult> MyAttendance(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.SubjectId = subjectId;
            ViewBag.SummaryJson = "null";

            if (!string.IsNullOrEmpty(userId) && subjectId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/attendance/student/{userId}/subject/{subjectId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.SummaryJson = await res.Content.ReadAsStringAsync();
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            // Load subjects from enrolled courses
            try
            {
                var client2 = CreateClient();
                var enrollments = await client2.GetAsync($"{BaseUrl}/api/enrollment/student/{userId}");
                ViewBag.EnrollmentsJson = enrollments.IsSuccessStatusCode
                    ? await enrollments.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.EnrollmentsJson = "[]"; }

            return View();
        }
    }
}

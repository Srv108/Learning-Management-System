using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class ProgressMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://localhost:5171";

        public ProgressMvcController(IHttpClientFactory httpClientFactory)
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

        // Student: my progress across all courses
        public async Task<IActionResult> MyProgress()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.ProgressJson = "null";

            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/studentprogress/student/{userId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.ProgressJson = await res.Content.ReadAsStringAsync();
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            return View();
        }

        // Teacher/Coordinator/Admin: course analytics
        public async Task<IActionResult> Analytics(long? courseId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role == "Student") return RedirectToAction("MyProgress");

            ViewBag.UserRole = role;
            ViewBag.CourseId = courseId;
            ViewBag.AnalyticsJson = "null";

            if (courseId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/studentprogress/report/course/{courseId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.AnalyticsJson = await res.Content.ReadAsStringAsync();
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

        // Recalculate a student's progress for a course
        [HttpPost]
        public async Task<IActionResult> Recalculate(string studentId, long courseId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var res = await client.PostAsync($"{BaseUrl}/api/studentprogress/recalculate/{studentId}/{courseId}", null);
                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Progress recalculated!" : "Failed to recalculate progress.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MyProgress");
        }

        // System-wide analytics (Admin/Coordinator)
        public async Task<IActionResult> SystemAnalytics()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Admin" && role != "CourseCoordinator")
            { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "Home"); }

            ViewBag.UserRole = role;
            try
            {
                var client = CreateClient();
                var res = await client.GetAsync($"{BaseUrl}/api/studentprogress/analytics");
                ViewBag.AnalyticsJson = res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : "null";
            }
            catch { ViewBag.AnalyticsJson = "null"; }

            return View();
        }

        // Teacher/Coordinator/Admin: at-risk students
        public async Task<IActionResult> AtRisk(long? courseId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role == "Student") return RedirectToAction("MyProgress");

            ViewBag.UserRole = role;
            ViewBag.CourseId = courseId;
            ViewBag.AtRiskJson = "null";

            try
            {
                var client = CreateClient();
                var url = courseId.HasValue
                    ? $"{BaseUrl}/api/studentprogress/at-risk?courseId={courseId}"
                    : $"{BaseUrl}/api/studentprogress/at-risk";
                var res = await client.GetAsync(url);
                if (res.IsSuccessStatusCode)
                    ViewBag.AtRiskJson = await res.Content.ReadAsStringAsync();
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }

            try
            {
                var client2 = CreateClient();
                var c = await client2.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = c.IsSuccessStatusCode ? await c.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

            return View();
        }
    }
}

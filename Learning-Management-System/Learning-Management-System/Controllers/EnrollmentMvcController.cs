using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class EnrollmentMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://localhost:5171";

        public EnrollmentMvcController(IHttpClientFactory httpClientFactory)
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

        // Coordinator/Admin: list enrollments for a batch
        public async Task<IActionResult> Index(long? batchId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "CourseCoordinator" && role != "Admin")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserRole = role;
            ViewBag.BatchId = batchId;
            ViewBag.EnrollmentsJson = "[]";

            if (batchId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var response = await client.GetAsync($"{BaseUrl}/api/enrollment/batch/{batchId}");
                    if (response.IsSuccessStatusCode)
                        ViewBag.EnrollmentsJson = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            // Load batches for dropdown
            try
            {
                var client2 = CreateClient();
                var courses = await client2.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = courses.IsSuccessStatusCode
                    ? await courses.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

            return View();
        }

        // Student: my enrollments
        public async Task<IActionResult> MyEnrollments()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.EnrollmentsJson = "[]";

            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var client = CreateClient();
                    var response = await client.GetAsync($"{BaseUrl}/api/enrollment/student/{userId}");
                    if (response.IsSuccessStatusCode)
                        ViewBag.EnrollmentsJson = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            return View();
        }

        // Coordinator/Admin: enroll student page
        public async Task<IActionResult> Enroll()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.UserRole = role;
            try
            {
                var client = CreateClient();
                var courses = await client.GetAsync($"{BaseUrl}/api/course?pageSize=100");
                ViewBag.CoursesJson = courses.IsSuccessStatusCode
                    ? await courses.Content.ReadAsStringAsync() : "[]";
            }
            catch { ViewBag.CoursesJson = "[]"; }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(string studentId, long batchId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { studentId, batchId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{BaseUrl}/api/enrollment", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Student enrolled successfully!";
                    return RedirectToAction("Index", new { batchId });
                }

                var err = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Enrollment failed: {err}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Enroll");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(long id, string status, long? batchId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { status };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PutAsync($"{BaseUrl}/api/enrollment/{id}", content);
                TempData["Success"] = "Enrollment status updated.";
            }
            catch { TempData["Error"] = "Failed to update status."; }

            return RedirectToAction("Index", new { batchId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id, long? batchId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var client = CreateClient();
            await client.DeleteAsync($"{BaseUrl}/api/enrollment/{id}");
            TempData["Success"] = "Enrollment removed.";
            return RedirectToAction("Index", new { batchId });
        }
    }
}

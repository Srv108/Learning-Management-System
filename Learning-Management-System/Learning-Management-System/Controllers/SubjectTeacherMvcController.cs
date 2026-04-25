using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class SubjectTeacherMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<Learning_Management_System.Models.AppUser> _userManager;
        private const string BaseUrl = "http://localhost:5171";

        public SubjectTeacherMvcController(
            IHttpClientFactory httpClientFactory,
            UserManager<Learning_Management_System.Models.AppUser> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
        }

        private string? GetToken() => HttpContext.Session.GetString("JwtToken");
        private string GetRole() => HttpContext.Session.GetString("UserRole") ?? "Student";
        private bool IsAuthenticated() => !string.IsNullOrEmpty(GetToken());
        private bool CanManage() { var r = GetRole(); return r == "Admin" || r == "CourseCoordinator"; }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Index(long subjectId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!CanManage()) { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "CourseMvc"); }

            try
            {
                var client = CreateClient();
                var teachersTask = client.GetAsync($"{BaseUrl}/api/SubjectTeacher/subject/{subjectId}");
                var subjectTask = client.GetAsync($"{BaseUrl}/api/Subject/{subjectId}");
                await Task.WhenAll(teachersTask, subjectTask);

                ViewBag.TeachersJson = teachersTask.Result.IsSuccessStatusCode
                    ? await teachersTask.Result.Content.ReadAsStringAsync() : "[]";
                ViewBag.SubjectJson = subjectTask.Result.IsSuccessStatusCode
                    ? await subjectTask.Result.Content.ReadAsStringAsync() : "{}";

                // Get all teachers to populate the assign dropdown
                var allTeachers = await _userManager.GetUsersInRoleAsync("Teacher");
                var teacherList = allTeachers.Select(u => new { id = u.Id, name = u.FullName ?? u.Email, email = u.Email }).ToList();
                ViewBag.AllTeachersJson = JsonSerializer.Serialize(teacherList);
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; ViewBag.AllTeachersJson = "[]"; }

            ViewBag.SubjectId = subjectId;
            ViewBag.UserRole = GetRole();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Assign(long subjectId, string teacherId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!CanManage()) return RedirectToAction("Index", "CourseMvc");

            try
            {
                var client = CreateClient();
                var payload = new { subjectId, teacherId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{BaseUrl}/api/SubjectTeacher/assign", content);

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
                    ? "Teacher assigned successfully!"
                    : $"Failed: {await response.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Index", new { subjectId });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(long id, long subjectId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!CanManage()) return RedirectToAction("Index", "CourseMvc");

            try
            {
                var client = CreateClient();
                var response = await client.DeleteAsync($"{BaseUrl}/api/SubjectTeacher/{id}");
                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode ? "Teacher removed from subject." : "Failed to remove.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Index", new { subjectId });
        }
    }
}

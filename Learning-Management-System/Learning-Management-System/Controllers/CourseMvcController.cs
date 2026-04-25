using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class CourseMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://localhost:5171";

        public CourseMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private string? GetToken() => HttpContext.Session.GetString("JwtToken");
        private string GetRole() => HttpContext.Session.GetString("UserRole") ?? "Student";
        private bool IsAuthenticated() => !string.IsNullOrEmpty(GetToken());

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var response = await client.GetAsync($"{BaseUrl}/api/course?pageNumber={page}&pageSize=12");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var courses = JsonSerializer.Deserialize<List<dynamic>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    ViewBag.CoursesJson = json;
                }
                ViewBag.UserRole = GetRole();
                ViewBag.Page = page;
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View();
        }

        public async Task<IActionResult> Details(long id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var courseTask = client.GetAsync($"{BaseUrl}/api/course/{id}");
                var subjectsTask = client.GetAsync($"{BaseUrl}/api/subject/course/{id}");
                var batchesTask = client.GetAsync($"{BaseUrl}/api/coursebatch/course/{id}");
                await Task.WhenAll(courseTask, subjectsTask, batchesTask);

                ViewBag.CourseJson = await courseTask.Result.Content.ReadAsStringAsync();
                ViewBag.SubjectsJson = subjectsTask.Result.IsSuccessStatusCode
                    ? await subjectsTask.Result.Content.ReadAsStringAsync() : "[]";
                ViewBag.BatchesJson = batchesTask.Result.IsSuccessStatusCode
                    ? await batchesTask.Result.Content.ReadAsStringAsync() : "[]";
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            ViewBag.CourseId = id;
            ViewBag.UserRole = GetRole();
            return View();
        }

        public IActionResult Create()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Teacher" && role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index");
            ViewBag.UserRole = role;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string title, string description, int credits)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { title, description, credits };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{BaseUrl}/api/course", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Course created successfully!";
                    return RedirectToAction("Index");
                }

                var err = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to create course: {err}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.UserRole = GetRole();
            return View();
        }

        public async Task<IActionResult> Edit(long id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Teacher" && role != "CourseCoordinator" && role != "Admin")
                return RedirectToAction("Index");

            var client = CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/course/{id}");
            ViewBag.CourseJson = response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync() : "{}";
            ViewBag.CourseId = id;
            ViewBag.UserRole = role;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(long id, string title, string description, int credits, string status)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { title, description, credits, status };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{BaseUrl}/api/course/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Course updated successfully!";
                    return RedirectToAction("Details", new { id });
                }

                var err = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Failed to update: {err}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.CourseId = id;
            ViewBag.UserRole = GetRole();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var client = CreateClient();
            await client.DeleteAsync($"{BaseUrl}/api/course/{id}");
            TempData["Success"] = "Course deleted.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddSubject(long courseId, string name, string description)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { courseId, name, description };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync($"{BaseUrl}/api/subject", content);
                TempData["Success"] = "Subject added!";
            }
            catch { TempData["Error"] = "Failed to add subject."; }

            return RedirectToAction("Details", new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> AddBatch(long courseId, string batchName, DateTime startDate, DateTime endDate)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { courseId, batchName, startDate, endDate };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync($"{BaseUrl}/api/coursebatch", content);
                TempData["Success"] = "Batch added!";
            }
            catch { TempData["Error"] = "Failed to add batch."; }

            return RedirectToAction("Details", new { id = courseId });
        }
    }
}

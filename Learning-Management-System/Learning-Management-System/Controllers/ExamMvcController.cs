using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class ExamMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://localhost:5171";

        public ExamMvcController(IHttpClientFactory httpClientFactory)
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

        bool CanManageExam(string role) =>
            role == "Teacher" || role == "CourseCoordinator" || role == "ExamController" || role == "Admin";

        // All authorized users: list exams by subject
        public async Task<IActionResult> Index(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role == "Student") return RedirectToAction("MyResults");

            ViewBag.UserRole = role;
            ViewBag.SubjectId = subjectId;
            ViewBag.ExamsJson = "[]";

            if (subjectId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/exam/subject/{subjectId}");
                    if (res.IsSuccessStatusCode)
                        ViewBag.ExamsJson = await res.Content.ReadAsStringAsync();
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

        // Schedule exam
        public async Task<IActionResult> Create()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (!CanManageExam(role)) return RedirectToAction("Index", "Home");

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
        public async Task<IActionResult> Create(long subjectId, string title, string examType, int maxScore, DateTime examDate)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { subjectId, title, examType, maxScore, examDate };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/exam", content);

                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Exam scheduled!";
                    return RedirectToAction("Index", new { subjectId });
                }
                var err = await res.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed: {err}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Create");
        }

        // Record results
        public async Task<IActionResult> RecordResults(long? examId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (!CanManageExam(role)) return RedirectToAction("Index", "Home");

            ViewBag.UserRole = role;
            ViewBag.ExamId = examId;
            ViewBag.ResultsJson = "[]";
            ViewBag.ExamJson = "{}";

            if (examId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var examTask = client.GetAsync($"{BaseUrl}/api/exam/{examId}");
                    var resultsTask = client.GetAsync($"{BaseUrl}/api/exam/{examId}/results");
                    await Task.WhenAll(examTask, resultsTask);

                    ViewBag.ExamJson = examTask.Result.IsSuccessStatusCode
                        ? await examTask.Result.Content.ReadAsStringAsync() : "{}";
                    ViewBag.ResultsJson = resultsTask.Result.IsSuccessStatusCode
                        ? await resultsTask.Result.Content.ReadAsStringAsync() : "[]";
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
        public async Task<IActionResult> AddResult(long examId, string studentId, int marks, string grade)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { examId, studentId, marks, grade };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PostAsync($"{BaseUrl}/api/exam/result", content);
                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Result recorded!" : "Failed to record result.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("RecordResults", new { examId });
        }

        // Edit exam GET
        public async Task<IActionResult> Edit(long id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (!CanManageExam(role)) return RedirectToAction("Index", "Home");

            try
            {
                var client = CreateClient();
                var res = await client.GetAsync($"{BaseUrl}/api/exam/{id}");
                ViewBag.ExamJson = res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : "{}";
            }
            catch { ViewBag.ExamJson = "{}"; }

            ViewBag.ExamId = id;
            ViewBag.UserRole = role;
            return View();
        }

        // Edit exam POST
        [HttpPost]
        public async Task<IActionResult> Edit(long id, string title, string examType, int maxScore, DateTime examDate)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { title, examType, maxScore, examDate };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PutAsync($"{BaseUrl}/api/exam/{id}", content);

                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Exam updated!" : $"Failed: {await res.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Index");
        }

        // Edit exam result PUT
        [HttpPost]
        public async Task<IActionResult> UpdateResult(long resultId, int marks, string grade, long examId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var payload = new { marks, grade };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await client.PutAsync($"{BaseUrl}/api/exam/result/{resultId}", content);

                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Result updated!" : "Failed to update result.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("RecordResults", new { examId });
        }

        // Delete exam result
        [HttpPost]
        public async Task<IActionResult> DeleteResult(long resultId, long examId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                var client = CreateClient();
                var res = await client.DeleteAsync($"{BaseUrl}/api/exam/result/{resultId}");
                TempData[res.IsSuccessStatusCode ? "Success" : "Error"] =
                    res.IsSuccessStatusCode ? "Result deleted." : "Failed to delete.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("RecordResults", new { examId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteExam(long id, long? subjectId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var client = CreateClient();
            await client.DeleteAsync($"{BaseUrl}/api/exam/{id}");
            TempData["Success"] = "Exam deleted.";
            return RedirectToAction("Index", new { subjectId });
        }

        // Student: my results
        public async Task<IActionResult> MyResults(long? subjectId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.SubjectId = subjectId;
            ViewBag.SummaryJson = "null";
            ViewBag.AllResultsJson = "[]";

            // Load all results for this student
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var client = CreateClient();
                    var allRes = await client.GetAsync($"{BaseUrl}/api/exam/student/{userId}/all-results");
                    if (allRes.IsSuccessStatusCode)
                        ViewBag.AllResultsJson = await allRes.Content.ReadAsStringAsync();
                }
                catch { ViewBag.AllResultsJson = "[]"; }
            }

            // Load subject-specific summary if requested
            if (!string.IsNullOrEmpty(userId) && subjectId.HasValue)
            {
                try
                {
                    var client = CreateClient();
                    var res = await client.GetAsync($"{BaseUrl}/api/exam/student/{userId}/subject/{subjectId}/summary");
                    if (res.IsSuccessStatusCode)
                        ViewBag.SummaryJson = await res.Content.ReadAsStringAsync();
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
    }
}

using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Learning_Management_System.Actions
{
    public class SubjectMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SubjectMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(long courseId = 0)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                if (courseId > 0)
                {
                    // Get subjects for a specific course
                    var response = await client.GetAsync($"http://localhost:5171/api/subject/course/{courseId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var subjects = JsonSerializer.Deserialize<List<SubjectDto>>(json, options);
                        ViewBag.CourseId = courseId;
                        return View(subjects);
                    }
                }
                else
                {
                    // Get all subjects
                    var response = await client.GetAsync("http://localhost:5171/api/subject");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var subjects = JsonSerializer.Deserialize<List<SubjectDto>>(json, options);
                        return View(subjects);
                    }
                }

                TempData["Error"] = "Failed to load subjects";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading subjects: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var response = await client.GetAsync($"http://localhost:5171/api/subject/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var subject = JsonSerializer.Deserialize<SubjectDto>(json, options);
                    return View(subject);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading subject: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}

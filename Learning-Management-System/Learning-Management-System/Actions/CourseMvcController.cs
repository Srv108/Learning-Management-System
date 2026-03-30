using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Learning_Management_System.Actions
{
    public class CourseMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CourseMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var response = await client.GetAsync("http://localhost:5171/api/course");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var courses = JsonSerializer.Deserialize<List<CourseDto>>(json, options);
                    return View(courses);
                }

                TempData["Error"] = "Failed to load courses";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading courses: " + ex.Message;
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
                var response = await client.GetAsync($"http://localhost:5171/api/course/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var course = JsonSerializer.Deserialize<CourseDto>(json, options);
                    return View(course);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading course: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}

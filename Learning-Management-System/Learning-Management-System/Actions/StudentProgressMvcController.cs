using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Learning_Management_System.Actions
{
    public class StudentProgressMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StudentProgressMvcController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> GetStudentProgress()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                
                // This is a placeholder - the actual endpoint returns course-specific progress
                ViewBag.Message = "Student Progress feature coming soon!";
                ViewBag.Info = "Navigate to Subjects → Select Subject → View Assignments to track your performance";
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading progress: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }
    }
}

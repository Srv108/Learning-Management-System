using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Actions
{
    public class AssignmentMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AssignmentMvcController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // List assignments by subject
        public async Task<IActionResult> Index(long subjectId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var response = await client.GetAsync($"http://localhost:5171/api/assignment/subject/{subjectId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var assignments = JsonSerializer.Deserialize<List<AssignmentDto>>(json, options);
                    
                    ViewBag.SubjectId = subjectId;
                    return View(assignments);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading assignments: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // View assignment details with submissions
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                // Get assignment details
                var assignmentResponse = await client.GetAsync($"http://localhost:5171/api/assignment/{id}");
                if (!assignmentResponse.IsSuccessStatusCode)
                    return NotFound();

                var assignmentJson = await assignmentResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var assignment = JsonSerializer.Deserialize<AssignmentDto>(assignmentJson, options);

                // Get submissions
                var submissionsResponse = await client.GetAsync($"http://localhost:5171/api/assignment/{id}/submissions");
                List<SubmissionDto> submissions = new();

                if (submissionsResponse.IsSuccessStatusCode)
                {
                    var submissionsJson = await submissionsResponse.Content.ReadAsStringAsync();
                    submissions = JsonSerializer.Deserialize<List<SubmissionDto>>(submissionsJson, options) ?? new();
                }

                var viewModel = new AssignmentDetailsViewModel
                {
                    Assignment = assignment,
                    Submissions = submissions
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading assignment: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // Create assignment form (GET)
        public IActionResult Create(long subjectId)
        {
            var model = new CreateAssignmentDto { SubjectId = subjectId };
            ViewBag.SubjectId = subjectId;
            return View(model);
        }

        // Create assignment (POST)
        [HttpPost]
        public async Task<IActionResult> Create(CreateAssignmentDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.SubjectId = model.SubjectId;
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://localhost:5171/api/assignment", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Assignment created successfully!";
                    return RedirectToAction("Index", new { subjectId = model.SubjectId });
                }

                TempData["Error"] = "Failed to create assignment";
                ViewBag.SubjectId = model.SubjectId;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating assignment: " + ex.Message;
                ViewBag.SubjectId = model.SubjectId;
                return View(model);
            }
        }

        // Edit assignment form (GET)
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var response = await client.GetAsync($"http://localhost:5171/api/assignment/{id}");

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var assignment = JsonSerializer.Deserialize<AssignmentDto>(json, options);

                var editDto = new UpdateAssignmentDto
                {
                    Title = assignment.Title,
                    Description = assignment.Description,
                    MaxScore = assignment.MaxScore,
                    DueDate = assignment.DueDate
                };

                ViewBag.AssignmentId = id;
                return View(editDto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading assignment: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // Edit assignment (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(long id, UpdateAssignmentDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AssignmentId = id;
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"http://localhost:5171/api/assignment/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Assignment updated successfully!";
                    return RedirectToAction("Details", new { id });
                }

                TempData["Error"] = "Failed to update assignment";
                ViewBag.AssignmentId = id;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating assignment: " + ex.Message;
                ViewBag.AssignmentId = id;
                return View(model);
            }
        }

        // Delete assignment
        [HttpPost]
        public async Task<IActionResult> Delete(long id, long subjectId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                
                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var response = await client.DeleteAsync($"http://localhost:5171/api/assignment/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Assignment deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete assignment";
                }

                return RedirectToAction("Index", new { subjectId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting assignment: " + ex.Message;
                return RedirectToAction("Index", new { subjectId });
            }
        }

        // Student submit assignment
        public IActionResult Submit(long id)
        {
            var model = new CreateSubmissionDto { AssignmentId = id };
            ViewBag.AssignmentId = id;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(CreateSubmissionDto model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://localhost:5171/api/assignment/submit", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Assignment submitted successfully!";
                    return RedirectToAction("Details", new { id = model.AssignmentId });
                }

                TempData["Error"] = "Failed to submit assignment";
                ViewBag.AssignmentId = model.AssignmentId;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error submitting assignment: " + ex.Message;
                ViewBag.AssignmentId = model.AssignmentId;
                return View(model);
            }
        }

        // Teacher grade submission
        public IActionResult Grade(long submissionId)
        {
            var model = new AssignmentGradeDto { SubmissionId = submissionId };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Grade(AssignmentGradeDto model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = HttpContext.Session.GetString("JwtToken");

                if (string.IsNullOrEmpty(token))
                    return RedirectToAction("Login", "AuthMvc");

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://localhost:5171/api/assignment/grade", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Submission graded successfully!";
                    return RedirectToAction("Details", new { id = model.SubmissionId });
                }

                TempData["Error"] = "Failed to grade submission";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error grading submission: " + ex.Message;
                return View(model);
            }
        }
    }
}

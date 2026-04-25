using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class AdminMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<Learning_Management_System.Models.AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private const string BaseUrl = "http://localhost:5171";

        public AdminMvcController(
            IHttpClientFactory httpClientFactory,
            UserManager<Learning_Management_System.Models.AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private string? GetToken() => HttpContext.Session.GetString("JwtToken");
        private string GetRole() => HttpContext.Session.GetString("UserRole") ?? "Student";
        private bool IsAdmin() => GetRole() == "Admin";
        private bool IsAuthenticated() => !string.IsNullOrEmpty(GetToken());

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Users(string? search = null, string? filterRole = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!IsAdmin()) { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "Home"); }

            try
            {
                var allUsers = _userManager.Users.ToList();
                var userList = new List<object>();

                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var roleStr = roles.FirstOrDefault() ?? "No Role";

                    if (!string.IsNullOrEmpty(search) &&
                        !(user.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) &&
                        !(user.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrEmpty(filterRole) && filterRole != "All" && roleStr != filterRole)
                        continue;

                    userList.Add(new
                    {
                        id = user.Id,
                        email = user.Email,
                        fullName = user.FullName ?? user.Email,
                        role = roleStr,
                        allRoles = string.Join(", ", roles)
                    });
                }

                ViewBag.UsersJson = JsonSerializer.Serialize(userList);
                ViewBag.TotalUsers = allUsers.Count;
                ViewBag.Search = search ?? "";
                ViewBag.FilterRole = filterRole ?? "";

                var roleCounts = new Dictionary<string, int>();
                foreach (var role in new[] { "Student", "Teacher", "CourseCoordinator", "ExamController", "Admin" })
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    roleCounts[role] = usersInRole.Count;
                }
                ViewBag.RoleCountsJson = JsonSerializer.Serialize(roleCounts);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.UsersJson = "[]";
                ViewBag.RoleCountsJson = "{}";
                ViewBag.TotalUsers = 0;
            }

            ViewBag.UserRole = GetRole();
            return View();
        }

        public IActionResult AssignRole()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!IsAdmin()) { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "Home"); }
            ViewBag.UserRole = GetRole();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string email, string role)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            try
            {
                var client = CreateClient();
                var response = await client.PostAsync(
                    $"{BaseUrl}/api/auth/assign-role?email={Uri.EscapeDataString(email)}&role={Uri.EscapeDataString(role)}",
                    null);

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
                    ? $"Role '{role}' assigned to {email} successfully!"
                    : $"Failed: {await response.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("AssignRole");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                    TempData["Success"] = $"Role '{role}' removed from {user.Email}.";
                }
                else TempData["Error"] = "User not found.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("Users");
        }
    }
}

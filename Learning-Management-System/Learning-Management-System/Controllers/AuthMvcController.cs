using Learning_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class AuthMvcController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AuthMvcController(IHttpClientFactory httpClientFactory, IConfiguration configuration, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            // Check if user is already logged in via session
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var jwtToken = HttpContext.Session.GetString("JwtToken");
            
            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(jwtToken))
            {
                Console.WriteLine($"[REGISTER] User already authenticated, redirecting to home");
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = "http://localhost:5171"; // API is on same server

                var request = new
                {
                    email = model.Email,
                    password = model.Password,
                    fullName = model.FullName,
                    role = model.Role ?? "Student"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                Console.WriteLine($"[REGISTER] Attempting registration for: {model.Email}");
                var response = await client.PostAsync($"{baseUrl}/api/auth/register", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[REGISTER] API Response: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    using var jsonDoc = JsonDocument.Parse(responseBody);
                    var root = jsonDoc.RootElement;

                    bool succeeded = false;
                    if (root.TryGetProperty("succeeded", out var succeededProp))
                        succeeded = succeededProp.GetBoolean();
                    else if (root.TryGetProperty("Succeeded", out var succeededPropCaps))
                        succeeded = succeededPropCaps.GetBoolean();

                    string? token = null;
                    if (root.TryGetProperty("token", out var tokenProp))
                        token = tokenProp.GetString();
                    else if (root.TryGetProperty("Token", out var tokenPropCaps))
                        token = tokenPropCaps.GetString();

                    if (succeeded && !string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine($"[REGISTER] Registration successful, setting session");
                        HttpContext.Session.SetString("JwtToken", token);
                        HttpContext.Session.SetString("UserEmail", model.Email);
                        
                        // Get UserId from response
                        if (root.TryGetProperty("userId", out var userIdProp))
                            HttpContext.Session.SetString("UserId", userIdProp.GetString() ?? string.Empty);
                        else if (root.TryGetProperty("UserId", out var userIdPropCaps))
                            HttpContext.Session.SetString("UserId", userIdPropCaps.GetString() ?? string.Empty);
                        
                        // Sign in the user for cookie authentication
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        if (user != null)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            Console.WriteLine($"[REGISTER] User signed in with cookie auth");
                        }
                        
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REGISTER] Exception: {ex.Message}");
                ModelState.AddModelError("", $"Registration failed: {ex.Message}");
                return View(model);
            }
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Check if user is already logged in via session
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var jwtToken = HttpContext.Session.GetString("JwtToken");
            
            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(jwtToken))
            {
                Console.WriteLine($"[LOGIN] User already authenticated via session: {userEmail}, redirecting to home");
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = "http://localhost:5171";

                var request = new
                {
                    email = model.Email,
                    password = model.Password
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                Console.WriteLine($"[LOGIN] Attempting login for: {model.Email}");
                Console.WriteLine($"[LOGIN] Calling API: {baseUrl}/api/auth/login");

                var response = await client.PostAsync($"{baseUrl}/api/auth/login", content);

                Console.WriteLine($"[LOGIN] API Response Status: {response.StatusCode}");

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[LOGIN] API Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseBody);
                        var root = jsonDoc.RootElement;

                        // Try both "succeeded" and "Succeeded"
                        bool succeeded = false;
                        if (root.TryGetProperty("succeeded", out var succeededProp))
                            succeeded = succeededProp.GetBoolean();
                        else if (root.TryGetProperty("Succeeded", out var succeededPropCaps))
                            succeeded = succeededPropCaps.GetBoolean();

                        Console.WriteLine($"[LOGIN] Succeeded: {succeeded}");

                        string? token = null;
                        if (root.TryGetProperty("token", out var tokenProp))
                            token = tokenProp.GetString();
                        else if (root.TryGetProperty("Token", out var tokenPropCaps))
                            token = tokenPropCaps.GetString();

                        Console.WriteLine($"[LOGIN] Token received: {(!string.IsNullOrEmpty(token) ? "YES" : "NO")}");

                        if (succeeded && !string.IsNullOrEmpty(token))
                        {
                            Console.WriteLine($"[LOGIN] Setting session with user data");
                            HttpContext.Session.SetString("JwtToken", token);
                            HttpContext.Session.SetString("UserEmail", model.Email);
                            
                            // Get UserId from response
                            if (root.TryGetProperty("userId", out var userIdProp))
                                HttpContext.Session.SetString("UserId", userIdProp.GetString() ?? string.Empty);
                            else if (root.TryGetProperty("UserId", out var userIdPropCaps))
                                HttpContext.Session.SetString("UserId", userIdPropCaps.GetString() ?? string.Empty);
                            
                            Console.WriteLine($"[LOGIN] Session set - UserEmail: {model.Email}");

                            // Sign in the user for cookie authentication
                            var user = await _userManager.FindByEmailAsync(model.Email);
                            if (user != null)
                            {
                                await _signInManager.SignInAsync(user, isPersistent: false);
                                Console.WriteLine($"[LOGIN] User signed in with cookie auth");
                            }

                            Console.WriteLine($"[LOGIN] Redirecting to Home");
                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                return Redirect(returnUrl);

                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            Console.WriteLine($"[LOGIN] Invalid credentials - succeeded={succeeded}, token={(!string.IsNullOrEmpty(token))}");
                            ModelState.AddModelError("", "Invalid email or password.");
                            ViewData["ReturnUrl"] = returnUrl;
                            return View(model);
                        }
                    }
                    catch (JsonException jex)
                    {
                        Console.WriteLine($"[LOGIN] JSON Parse Error: {jex.Message}");
                        ModelState.AddModelError("", "Server response error.");
                        ViewData["ReturnUrl"] = returnUrl;
                        return View(model);
                    }
                }
                else
                {
                    Console.WriteLine($"[LOGIN] API Error - Status: {response.StatusCode}");
                    ModelState.AddModelError("", "Invalid email or password.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] Exception: {ex.Message}");
                Console.WriteLine($"[LOGIN] Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Login failed: {ex.Message}");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("JwtToken");
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserId");
            await _signInManager.SignOutAsync();
            Console.WriteLine("[LOGOUT] User logged out, session cleared and signed out");
            return RedirectToAction("Login");
        }
    }

    public class LoginViewModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Student";
    }
}

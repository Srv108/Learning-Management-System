using System.Diagnostics;
using Learning_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learning_Management_System.Actions
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Check if user logged in via session
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var jwtToken = HttpContext.Session.GetString("JwtToken");
            
            Console.WriteLine($"[HOME] Index called");
            Console.WriteLine($"[HOME] Session UserEmail: {userEmail}");
            Console.WriteLine($"[HOME] Session JwtToken present: {(!string.IsNullOrEmpty(jwtToken))}");
            
            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(jwtToken))
            {
                Console.WriteLine($"[HOME] User is authenticated via session, rendering home view");
                ViewBag.UserEmail = userEmail;
                return View();
            }
            
            Console.WriteLine($"[HOME] User is NOT authenticated, redirecting to login");
            return RedirectToAction("Login", "AuthMvc");
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

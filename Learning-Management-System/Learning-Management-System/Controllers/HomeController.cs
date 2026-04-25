using System.Diagnostics;
using Learning_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learning_Management_System.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var jwtToken = HttpContext.Session.GetString("JwtToken");

            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(jwtToken))
            {
                ViewBag.UserEmail = userEmail;
                ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "Student";
                ViewBag.UserFullName = HttpContext.Session.GetString("UserFullName") ?? userEmail;
                ViewBag.UserId = HttpContext.Session.GetString("UserId") ?? "";
                return View();
            }

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

        [AllowAnonymous]
        public IActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}

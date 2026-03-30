using Learning_Management_System.Models;
using Learning_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Learning_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthResponse { Succeeded = false, Error = "Email and password are required." });

            if (!await _roleManager.RoleExistsAsync(request.Role))
                return BadRequest(new AuthResponse { Succeeded = false, Error = "Invalid role." });

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new AuthResponse { Succeeded = false, Error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            await _userManager.AddToRoleAsync(user, request.Role);

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.CreateTokenAsync(user, roles);

            return Ok(new AuthResponse { Succeeded = true, Token = token, UserId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new AuthResponse { Succeeded = false, Error = "Invalid login." });

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized(new AuthResponse { Succeeded = false, Error = "Invalid login." });

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtTokenService.CreateTokenAsync(user, roles);

            return Ok(new AuthResponse { Succeeded = true, Token = token, UserId = user.Id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromQuery] string email, [FromQuery] string role)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("User not found.");

            if (!await _roleManager.RoleExistsAsync(role)) return BadRequest("Invalid role.");

            if (await _userManager.IsInRoleAsync(user, role)) return BadRequest("User already in role.");

            await _userManager.AddToRoleAsync(user, role);
            return Ok("Role assigned.");
        }
    }
}
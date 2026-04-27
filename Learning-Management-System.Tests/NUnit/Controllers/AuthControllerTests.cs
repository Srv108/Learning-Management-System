using Learning_Management_System.Controllers;
using Learning_Management_System.Models;
using Learning_Management_System.Services;
using Learning_Management_System.Tests.NUnit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Learning_Management_System.Tests.NUnit.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<UserManager<AppUser>> _userManager = null!;
        private Mock<RoleManager<IdentityRole>> _roleManager = null!;
        private Mock<IJwtTokenService> _jwtService = null!;
        private AuthController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _userManager = TestHelpers.MockUserManager();
            _roleManager = TestHelpers.MockRoleManager();
            _jwtService = new Mock<IJwtTokenService>();
            _controller = new AuthController(_userManager.Object, _roleManager.Object, _jwtService.Object);
        }

        // Register Tests
        [Test]
        public async Task Register_ValidRequest_ReturnsOkWithToken()
        {
            var request = new RegisterRequest { Email = "new@test.com", Password = "Pass@123", FullName = "New User", Role = "Student" };
            _roleManager.Setup(r => r.RoleExistsAsync("Student")).ReturnsAsync(true);
            _userManager.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), request.Password))
                        .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.AddToRoleAsync(It.IsAny<AppUser>(), "Student"))
                        .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.GetRolesAsync(It.IsAny<AppUser>()))
                        .ReturnsAsync(new List<string> { "Student" });
            _jwtService.Setup(j => j.CreateTokenAsync(It.IsAny<AppUser>(), It.IsAny<IList<string>>()))
                       .ReturnsAsync("fake.jwt.token");

            var result = await _controller.Register(request);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var response = ok!.Value as AuthResponse;
            Assert.That(response!.Succeeded, Is.True);
            Assert.That(response.Token, Is.EqualTo("fake.jwt.token"));
        }

        [Test]
        public async Task Register_EmptyEmail_ReturnsBadRequest()
        {
            var request = new RegisterRequest { Email = "", Password = "Pass@123", Role = "Student" };

            var result = await _controller.Register(request);

            var bad = result as BadRequestObjectResult;
            Assert.That(bad, Is.Not.Null);
            var response = bad!.Value as AuthResponse;
            Assert.That(response!.Succeeded, Is.False);
            Assert.That(response.Error, Does.Contain("required"));
        }

        [Test]
        public async Task Register_EmptyPassword_ReturnsBadRequest()
        {
            var request = new RegisterRequest { Email = "test@test.com", Password = "", Role = "Student" };

            var result = await _controller.Register(request);

            var bad = result as BadRequestObjectResult;
            Assert.That(bad, Is.Not.Null);
            var response = bad!.Value as AuthResponse;
            Assert.That(response!.Succeeded, Is.False);
        }

        [Test]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            var request = new RegisterRequest { Email = "test@test.com", Password = "Pass@123", Role = "InvalidRole" };
            _roleManager.Setup(r => r.RoleExistsAsync("InvalidRole")).ReturnsAsync(false);

            var result = await _controller.Register(request);

            var bad = result as BadRequestObjectResult;
            Assert.That(bad, Is.Not.Null);
            var response = bad!.Value as AuthResponse;
            Assert.That(response!.Error, Does.Contain("Invalid role"));
        }

        [Test]
        public async Task Register_UserCreationFails_ReturnsBadRequest()
        {
            var request = new RegisterRequest { Email = "test@test.com", Password = "weak", Role = "Student" };
            _roleManager.Setup(r => r.RoleExistsAsync("Student")).ReturnsAsync(true);
            _userManager.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), "weak"))
                        .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var result = await _controller.Register(request);

            var bad = result as BadRequestObjectResult;
            Assert.That(bad, Is.Not.Null);
            var response = bad!.Value as AuthResponse;
            Assert.That(response!.Succeeded, Is.False);
        }

        // Login Tests
        [Test]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            var user = TestHelpers.MakeUser("u1", "user@test.com", "User One");
            var request = new LoginRequest { Email = "user@test.com", Password = "Pass@123" };
            _userManager.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, "Pass@123")).ReturnsAsync(true);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _jwtService.Setup(j => j.CreateTokenAsync(user, It.IsAny<IList<string>>())).ReturnsAsync("valid.jwt");

            var result = await _controller.Login(request);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var response = ok!.Value as AuthResponse;
            Assert.That(response!.Succeeded, Is.True);
            Assert.That(response.Token, Is.EqualTo("valid.jwt"));
        }

        [Test]
        public async Task Login_UserNotFound_ReturnsUnauthorized()
        {
            _userManager.Setup(u => u.FindByEmailAsync("noone@test.com")).ReturnsAsync((AppUser?)null);

            var result = await _controller.Login(new LoginRequest { Email = "noone@test.com", Password = "x" });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        [Test]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            var user = TestHelpers.MakeUser("u1", "user@test.com");
            _userManager.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

            var result = await _controller.Login(new LoginRequest { Email = "user@test.com", Password = "wrong" });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        // AssignRole Tests
        [Test]
        public async Task AssignRole_ValidRequest_ReturnsOk()
        {
            var user = TestHelpers.MakeUser("u1", "user@test.com");
            _userManager.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _roleManager.Setup(r => r.RoleExistsAsync("Teacher")).ReturnsAsync(true);
            _userManager.Setup(u => u.IsInRoleAsync(user, "Teacher")).ReturnsAsync(false);
            _userManager.Setup(u => u.AddToRoleAsync(user, "Teacher")).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.AssignRole("user@test.com", "Teacher");

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task AssignRole_UserNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByEmailAsync("ghost@test.com")).ReturnsAsync((AppUser?)null);

            var result = await _controller.AssignRole("ghost@test.com", "Admin");

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task AssignRole_InvalidRole_ReturnsBadRequest()
        {
            var user = TestHelpers.MakeUser("u1", "user@test.com");
            _userManager.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _roleManager.Setup(r => r.RoleExistsAsync("FakeRole")).ReturnsAsync(false);

            var result = await _controller.AssignRole("user@test.com", "FakeRole");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task AssignRole_AlreadyInRole_ReturnsBadRequest()
        {
            var user = TestHelpers.MakeUser("u1", "user@test.com");
            _userManager.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            _roleManager.Setup(r => r.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _userManager.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);

            var result = await _controller.AssignRole("user@test.com", "Admin");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }
}

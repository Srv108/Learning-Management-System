using Learning_Management_System.Models;
using Learning_Management_System.Services;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;

namespace Learning_Management_System.Tests.NUnit.Services
{
    [TestFixture]
    public class JwtTokenServiceTests
    {
        private IJwtTokenService _service = null!;

        [SetUp]
        public void Setup()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Secret"] = "TestSuperSecretKey_AtLeast32Chars!!",
                    ["JwtSettings:Issuer"] = "LMSApi",
                    ["JwtSettings:Audience"] = "LMSApiClients",
                    ["JwtSettings:ExpiryMinutes"] = "60"
                })
                .Build();
            _service = new JwtTokenService(config);
        }

        [Test]
        public async Task CreateTokenAsync_ValidUser_ReturnsNonEmptyToken()
        {
            var user = new AppUser { Id = "u1", UserName = "test@test.com", Email = "test@test.com", FullName = "Test User" };
            var token = await _service.CreateTokenAsync(user, new List<string> { "Student" });
            Assert.That(token, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task CreateTokenAsync_TokenContainsUserEmail()
        {
            var user = new AppUser { Id = "u1", UserName = "alice@test.com", Email = "alice@test.com", FullName = "Alice" };
            var token = await _service.CreateTokenAsync(user, new List<string> { "Teacher" });

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(token);
            Assert.That(parsed.Claims.Any(c => c.Value == "alice@test.com"), Is.True);
        }

        [Test]
        public async Task CreateTokenAsync_TokenContainsRole()
        {
            var user = new AppUser { Id = "u2", UserName = "bob@test.com", Email = "bob@test.com", FullName = "Bob" };
            var token = await _service.CreateTokenAsync(user, new List<string> { "Admin", "Teacher" });

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(token);
            var roles = parsed.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                                     .Select(c => c.Value).ToList();
            Assert.That(roles, Does.Contain("Admin"));
            Assert.That(roles, Does.Contain("Teacher"));
        }

        [Test]
        public async Task CreateTokenAsync_TokenIsNotExpiredImmediately()
        {
            var user = new AppUser { Id = "u3", UserName = "carol@test.com", Email = "carol@test.com", FullName = "Carol" };
            var token = await _service.CreateTokenAsync(user, new List<string>());

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(token);
            Assert.That(parsed.ValidTo, Is.GreaterThan(DateTime.UtcNow));
        }

        [Test]
        public async Task CreateTokenAsync_MultipleRoles_AllIncluded()
        {
            var user = new AppUser { Id = "u4", UserName = "multi@test.com", Email = "multi@test.com", FullName = "Multi" };
            var roles = new List<string> { "Admin", "CourseCoordinator", "Teacher" };
            var token = await _service.CreateTokenAsync(user, roles);

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(token);
            var roleClaims = parsed.Claims
                .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value).ToList();

            Assert.That(roleClaims.Count, Is.EqualTo(3));
        }
    }
}

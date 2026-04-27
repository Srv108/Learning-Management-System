using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Learning_Management_System.Tests.NUnit.Helpers
{
    public static class TestHelpers
    {
        public static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        public static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            var mgr = new Mock<UserManager<AppUser>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<AppUser>>().Object,
                Array.Empty<IUserValidator<AppUser>>(),
                Array.Empty<IPasswordValidator<AppUser>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<AppUser>>>().Object);
            return mgr;
        }

        public static Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            var mgr = new Mock<RoleManager<IdentityRole>>(
                store.Object,
                Array.Empty<IRoleValidator<IdentityRole>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object);
            return mgr;
        }

        public static Mock<ILogger<T>> MockLogger<T>() => new Mock<ILogger<T>>();

        public static ControllerContext CreateControllerContext(
            string userId = "user-1",
            string userName = "testuser",
            string role = "Admin")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            return new ControllerContext { HttpContext = httpContext };
        }

        public static AppUser MakeUser(string id, string email, string fullName = "Test User") => new AppUser
        {
            Id = id,
            UserName = email,
            Email = email,
            FullName = fullName
        };

        public static Course MakeCourse(long id, string title = "Test Course") => new Course
        {
            Id = id,
            Title = title,
            Status = "ACTIVE",
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };

        public static Subject MakeSubject(long id, long courseId, string name = "Test Subject") => new Subject
        {
            Id = id,
            CourseId = courseId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        public static CourseBatch MakeBatch(long id, long courseId, string name = "Batch A") => new CourseBatch
        {
            Id = id,
            CourseId = courseId,
            BatchName = name,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow
        };

        public static Assignment MakeAssignment(long id, long subjectId, string title = "Assignment 1") => new Assignment
        {
            Id = id,
            SubjectId = subjectId,
            Title = title,
            MaxScore = 100,
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };

        public static Exam MakeExam(long id, long subjectId, string title = "Exam 1") => new Exam
        {
            Id = id,
            SubjectId = subjectId,
            Title = title,
            ExamType = "MIDTERM",
            MaxScore = 100,
            ExamDate = DateTime.UtcNow.AddDays(14),
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };

        public static AttendanceSession MakeSession(long id, long subjectId) => new AttendanceSession
        {
            Id = id,
            SubjectId = subjectId,
            SessionDate = DateTime.UtcNow.Date,
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };
    }
}

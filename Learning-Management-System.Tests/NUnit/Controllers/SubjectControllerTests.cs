using Learning_Management_System.Controllers;
using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Learning_Management_System.Tests.NUnit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Learning_Management_System.Tests.NUnit.Controllers
{
    [TestFixture]
    public class SubjectControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<AppUser>> _userManager = null!;
        private SubjectController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _userManager = TestHelpers.MockUserManager();
            _controller = new SubjectController(
                _context,
                _userManager.Object,
                TestHelpers.MockLogger<SubjectController>().Object);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("coord-1", "coord@lms.com", "CourseCoordinator");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var course = TestHelpers.MakeCourse(1, "Computer Science");
            course.CreatedById = "coord-1";
            var subject = TestHelpers.MakeSubject(1, 1, "Data Structures");

            _context.Courses.Add(course);
            _context.Subjects.Add(subject);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        // GetSubjectsByCourse
        [Test]
        public async Task GetSubjectsByCourse_ExistingCourse_ReturnsSubjects()
        {
            var result = await _controller.GetSubjectsByCourse(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var subjects = (ok!.Value as IEnumerable<SubjectDto>)!.ToList();
            Assert.That(subjects.Count, Is.EqualTo(1));
            Assert.That(subjects[0].Name, Is.EqualTo("Data Structures"));
        }

        [Test]
        public async Task GetSubjectsByCourse_CourseNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetSubjectsByCourse(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetSubjectsByCourse_DeletedCourse_ReturnsNotFound()
        {
            var course = _context.Courses.Find(1L)!;
            course.IsDeleted = true;
            _context.SaveChanges();

            var result = await _controller.GetSubjectsByCourse(1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetSubjectsByCourse_CourseWithMultipleSubjects_ReturnsAll()
        {
            _context.Subjects.Add(TestHelpers.MakeSubject(2, 1, "Algorithms"));
            _context.Subjects.Add(TestHelpers.MakeSubject(3, 1, "Operating Systems"));
            _context.SaveChanges();

            var result = await _controller.GetSubjectsByCourse(1);

            var ok = result.Result as OkObjectResult;
            var subjects = ok!.Value as IEnumerable<SubjectDto>;
            Assert.That(subjects!.Count(), Is.EqualTo(3));
        }

        // GetSubject
        [Test]
        public async Task GetSubject_ExistingId_ReturnsOkWithDto()
        {
            var result = await _controller.GetSubject(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var dto = ok!.Value as SubjectDto;
            Assert.That(dto!.Name, Is.EqualTo("Data Structures"));
            Assert.That(dto.CourseId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSubject_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetSubject(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetSubject_DeletedSubject_ReturnsNotFound()
        {
            var subject = _context.Subjects.Find(1L)!;
            subject.IsDeleted = true;
            _context.SaveChanges();

            var result = await _controller.GetSubject(1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // CreateSubject
        [Test]
        public async Task CreateSubject_ValidDto_ReturnsCreated()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com", "Coordinator");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);
            _userManager.Setup(u => u.GetRolesAsync(coord)).ReturnsAsync(new List<string> { "CourseCoordinator" });

            var dto = new CreateSubjectDto { CourseId = 1, Name = "Databases", Description = "DB course" };

            var result = await _controller.CreateSubject(dto);

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
            Assert.That(_context.Subjects.Count(s => !s.IsDeleted), Is.EqualTo(2));
        }

        [Test]
        public async Task CreateSubject_CourseNotFound_ReturnsNotFound()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);

            var result = await _controller.CreateSubject(new CreateSubjectDto { CourseId = 999, Name = "X" });

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateSubject_UserNotFound_ReturnsUnauthorized()
        {
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync((AppUser?)null);

            var result = await _controller.CreateSubject(new CreateSubjectDto { CourseId = 1, Name = "X" });

            Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        // UpdateSubject
        [Test]
        public async Task UpdateSubject_ValidUpdate_ReturnsNoContent()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);
            _userManager.Setup(u => u.GetRolesAsync(coord)).ReturnsAsync(new List<string> { "CourseCoordinator" });

            var dto = new UpdateSubjectDto { Name = "Advanced Data Structures", Description = "Updated" };

            var result = await _controller.UpdateSubject(1, dto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.Subjects.Find(1L)!.Name, Is.EqualTo("Advanced Data Structures"));
        }

        [Test]
        public async Task UpdateSubject_SubjectNotFound_ReturnsNotFound()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);
            _userManager.Setup(u => u.GetRolesAsync(coord)).ReturnsAsync(new List<string> { "CourseCoordinator" });

            var result = await _controller.UpdateSubject(999, new UpdateSubjectDto { Name = "X" });

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // DeleteSubject
        [Test]
        public async Task DeleteSubject_ExistingId_SoftDeletesAndReturnsNoContent()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);

            var result = await _controller.DeleteSubject(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.Subjects.Find(1L)!.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteSubject_NonExistentId_ReturnsNotFound()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);

            var result = await _controller.DeleteSubject(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task DeleteSubject_AlreadyDeleted_ReturnsNotFound()
        {
            var subject = _context.Subjects.Find(1L)!;
            subject.IsDeleted = true;
            _context.SaveChanges();

            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(coord);

            var result = await _controller.DeleteSubject(1);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}

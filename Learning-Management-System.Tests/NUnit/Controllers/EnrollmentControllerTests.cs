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
    public class EnrollmentControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<AppUser>> _userManager = null!;
        private EnrollmentController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _userManager = TestHelpers.MockUserManager();
            _controller = new EnrollmentController(
                _context,
                _userManager.Object,
                TestHelpers.MockLogger<EnrollmentController>().Object);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("coord-1", "coord@lms.com", "CourseCoordinator");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            var course = TestHelpers.MakeCourse(1, "CS101");
            var batch = TestHelpers.MakeBatch(1, 1, "Batch A");

            _context.Users.Add(student);
            _context.Courses.Add(course);
            _context.CourseBatches.Add(batch);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        // GetEnrollmentsByBatch
        [Test]
        public async Task GetEnrollmentsByBatch_ExistingBatch_ReturnsOkWithList()
        {
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetEnrollmentsByBatch(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var enrollments = ok!.Value as IEnumerable<EnrollmentDto>;
            Assert.That(enrollments!.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetEnrollmentsByBatch_BatchNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetEnrollmentsByBatch(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetEnrollmentsByBatch_EmptyBatch_ReturnsEmptyList()
        {
            var result = await _controller.GetEnrollmentsByBatch(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var enrollments = ok!.Value as IEnumerable<EnrollmentDto>;
            Assert.That(enrollments, Is.Empty);
        }

        // GetEnrollmentsByStudent
        [Test]
        public async Task GetEnrollmentsByStudent_ExistingStudent_ReturnsOkWithList()
        {
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            var result = await _controller.GetEnrollmentsByStudent("student-1");

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var enrollments = ok!.Value as IEnumerable<EnrollmentDto>;
            Assert.That(enrollments!.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetEnrollmentsByStudent_StudentNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByIdAsync("ghost")).ReturnsAsync((AppUser?)null);

            var result = await _controller.GetEnrollmentsByStudent("ghost");

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetEnrollment
        [Test]
        public async Task GetEnrollment_ExistingId_ReturnsDto()
        {
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetEnrollment(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var dto = ok!.Value as EnrollmentDto;
            Assert.That(dto!.Status, Is.EqualTo("ACTIVE"));
        }

        [Test]
        public async Task GetEnrollment_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetEnrollment(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // CreateEnrollment
        [Test]
        public async Task CreateEnrollment_ValidRequest_ReturnsCreated()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _userManager.Setup(u => u.GetRolesAsync(student)).ReturnsAsync(new List<string> { "Student" });

            var dto = new CreateEnrollmentDto { StudentId = "student-1", BatchId = 1 };

            var result = await _controller.CreateEnrollment(dto);

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
            Assert.That(_context.Enrollments.Any(e => e.StudentId == "student-1" && e.BatchId == 1), Is.True);
        }

        [Test]
        public async Task CreateEnrollment_StudentNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByIdAsync("ghost")).ReturnsAsync((AppUser?)null);

            var result = await _controller.CreateEnrollment(new CreateEnrollmentDto { StudentId = "ghost", BatchId = 1 });

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateEnrollment_UserNotStudent_ReturnsBadRequest()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("teacher-1")).ReturnsAsync(teacher);
            _userManager.Setup(u => u.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { "Teacher" });

            var result = await _controller.CreateEnrollment(new CreateEnrollmentDto { StudentId = "teacher-1", BatchId = 1 });

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateEnrollment_BatchNotFound_ReturnsNotFound()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _userManager.Setup(u => u.GetRolesAsync(student)).ReturnsAsync(new List<string> { "Student" });

            var result = await _controller.CreateEnrollment(new CreateEnrollmentDto { StudentId = "student-1", BatchId = 999 });

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateEnrollment_DuplicateEnrollment_ReturnsBadRequest()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _userManager.Setup(u => u.GetRolesAsync(student)).ReturnsAsync(new List<string> { "Student" });
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.CreateEnrollment(new CreateEnrollmentDto { StudentId = "student-1", BatchId = 1 });

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        // UpdateEnrollment
        [Test]
        public async Task UpdateEnrollment_ValidUpdate_ReturnsNoContent()
        {
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(coord);

            var result = await _controller.UpdateEnrollment(1, new UpdateEnrollmentDto { Status = "COMPLETED" });

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.Enrollments.Find(1L)!.Status, Is.EqualTo("COMPLETED"));
        }

        [Test]
        public async Task UpdateEnrollment_NonExistentId_ReturnsNotFound()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(coord);

            var result = await _controller.UpdateEnrollment(999, new UpdateEnrollmentDto { Status = "DROPPED" });

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // DeleteEnrollment
        [Test]
        public async Task DeleteEnrollment_ExistingId_ReturnsNoContent()
        {
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(coord);

            var result = await _controller.DeleteEnrollment(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.Enrollments.Find(1L)!.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteEnrollment_NonExistentId_ReturnsNotFound()
        {
            var coord = TestHelpers.MakeUser("coord-1", "coord@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(coord);

            var result = await _controller.DeleteEnrollment(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // SelfEnroll
        [Test]
        public async Task SelfEnroll_ValidBatch_ReturnsCreated()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(student);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("student-1", "student@lms.com", "Student");

            var result = await _controller.SelfEnroll(new SelfEnrollDto { BatchId = 1 });

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
        }

        [Test]
        public async Task SelfEnroll_BatchNotFound_ReturnsNotFound()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(student);

            var result = await _controller.SelfEnroll(new SelfEnrollDto { BatchId = 999 });

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task SelfEnroll_AlreadyEnrolled_ReturnsBadRequest()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(student);
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.SelfEnroll(new SelfEnrollDto { BatchId = 1 });

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }
}

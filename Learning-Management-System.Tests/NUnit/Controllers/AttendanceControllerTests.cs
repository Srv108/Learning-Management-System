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
    public class AttendanceControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<AppUser>> _userManager = null!;
        private AttendanceController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _userManager = TestHelpers.MockUserManager();
            _controller = new AttendanceController(
                _context,
                _userManager.Object,
                TestHelpers.MockLogger<AttendanceController>().Object);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("teacher-1", "teacher@lms.com", "Teacher");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher One");
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            var course = TestHelpers.MakeCourse(1, "Biology");
            var subject = TestHelpers.MakeSubject(1, 1, "Cell Biology");
            var session = TestHelpers.MakeSession(1, 1);
            session.CreatedById = "teacher-1";

            _context.Users.AddRange(teacher, student);
            _context.Courses.Add(course);
            _context.Subjects.Add(subject);
            _context.AttendanceSessions.Add(session);
            _context.SubjectTeachers.Add(new SubjectTeacher
            {
                Id = 1, SubjectId = 1, TeacherId = "teacher-1", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        // GetSessionsBySubject
        [Test]
        public async Task GetSessionsBySubject_ExistingSubject_ReturnsOkWithSessions()
        {
            var result = await _controller.GetSessionsBySubject(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var sessions = ok!.Value as IEnumerable<AttendanceSessionDto>;
            Assert.That(sessions!.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetSessionsBySubject_SubjectNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetSessionsBySubject(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetSession
        [Test]
        public async Task GetSession_ExistingId_ReturnsOkWithDto()
        {
            var result = await _controller.GetSession(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var session = ok!.Value as AttendanceSessionDto;
            Assert.That(session!.SubjectId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSession_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetSession(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetSessionRecords
        [Test]
        public async Task GetSessionRecords_SessionWithRecords_ReturnsRecords()
        {
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = 1, SessionId = 1, StudentId = "student-1",
                Status = "PRESENT", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetSessionRecords(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var records = ok!.Value as IEnumerable<AttendanceRecordDto>;
            Assert.That(records!.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetSessionRecords_SessionNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetSessionRecords(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetStudentAttendanceSummary
        [Test]
        public async Task GetStudentAttendanceSummary_PresentStudent_ReturnsCorrectPercentage()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = 1, SessionId = 1, StudentId = "student-1",
                Status = "PRESENT", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetStudentAttendanceSummary("student-1", 1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var summary = ok!.Value as StudentAttendanceSummaryDto;
            Assert.That(summary!.AttendancePercentage, Is.EqualTo(100.0));
            Assert.That(summary.PresentCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetStudentAttendanceSummary_StudentNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByIdAsync("ghost")).ReturnsAsync((AppUser?)null);

            var result = await _controller.GetStudentAttendanceSummary("ghost", 1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetStudentAttendanceSummary_SubjectNotFound_ReturnsNotFound()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            var result = await _controller.GetStudentAttendanceSummary("student-1", 999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // CreateAttendanceSession
        [Test]
        public async Task CreateAttendanceSession_Valid_ReturnsCreated()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher One");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new CreateAttendanceSessionDto
            {
                SubjectId = 1,
                SessionDate = DateTime.UtcNow.AddDays(1)
            };

            var result = await _controller.CreateAttendanceSession(dto);

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
        }

        [Test]
        public async Task CreateAttendanceSession_SubjectNotFound_ReturnsNotFound()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new CreateAttendanceSessionDto { SubjectId = 999, SessionDate = DateTime.UtcNow };

            var result = await _controller.CreateAttendanceSession(dto);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateAttendanceSession_DuplicateForSameDate_ReturnsBadRequest()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            // Session for today already exists in SeedDatabase
            var dto = new CreateAttendanceSessionDto
            {
                SubjectId = 1,
                SessionDate = DateTime.UtcNow.Date // same date as seeded session
            };

            var result = await _controller.CreateAttendanceSession(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        // MarkAttendance
        [Test]
        public async Task MarkAttendance_NewRecord_ReturnsOk()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher");
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _userManager.Setup(u => u.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { "Teacher" });

            var dto = new MarkAttendanceDto
            {
                SessionId = 1,
                StudentId = "student-1",
                Status = "PRESENT"
            };

            var result = await _controller.MarkAttendance(dto);

            Assert.That(result, Is.InstanceOf<OkResult>());
            Assert.That(_context.AttendanceRecords.Any(r => r.StudentId == "student-1" && r.Status == "PRESENT"), Is.True);
        }

        [Test]
        public async Task MarkAttendance_UpdateExistingRecord_ChangesStatus()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher");
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student");
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = 1, SessionId = 1, StudentId = "student-1",
                Status = "ABSENT", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _userManager.Setup(u => u.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { "Teacher" });

            var dto = new MarkAttendanceDto { SessionId = 1, StudentId = "student-1", Status = "PRESENT" };

            await _controller.MarkAttendance(dto);

            Assert.That(_context.AttendanceRecords.Find(1L)!.Status, Is.EqualTo("PRESENT"));
        }

        [Test]
        public async Task MarkAttendance_SessionNotFound_ReturnsNotFound()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new MarkAttendanceDto { SessionId = 999, StudentId = "student-1", Status = "PRESENT" };

            var result = await _controller.MarkAttendance(dto);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // BulkMarkAttendance
        [Test]
        public async Task BulkMarkAttendance_MultipleStudents_CreatesAllRecords()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher");
            var student2 = TestHelpers.MakeUser("student-2", "student2@lms.com", "Student Two");
            _context.Users.Add(student2);
            _context.SaveChanges();

            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);
            _userManager.Setup(u => u.FindByIdAsync("student-1"))
                        .ReturnsAsync(TestHelpers.MakeUser("student-1", "student@lms.com"));
            _userManager.Setup(u => u.FindByIdAsync("student-2")).ReturnsAsync(student2);
            _userManager.Setup(u => u.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { "Teacher" });

            var dto = new BulkMarkAttendanceDto
            {
                SessionId = 1,
                Records = new List<AttendanceRecordDto>
                {
                    new AttendanceRecordDto { SessionId = 1, StudentId = "student-1", Status = "PRESENT" },
                    new AttendanceRecordDto { SessionId = 1, StudentId = "student-2", Status = "ABSENT" }
                }
            };

            var result = await _controller.BulkMarkAttendance(dto);

            Assert.That(result, Is.InstanceOf<OkResult>());
            Assert.That(_context.AttendanceRecords.Count(r => !r.IsDeleted), Is.EqualTo(2));
        }

        [Test]
        public async Task BulkMarkAttendance_SessionNotFound_ReturnsNotFound()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new BulkMarkAttendanceDto { SessionId = 999, Records = new List<AttendanceRecordDto>() };

            var result = await _controller.BulkMarkAttendance(dto);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // DeleteAttendanceSession
        [Test]
        public async Task DeleteAttendanceSession_ExistingId_ReturnsNoContent()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var result = await _controller.DeleteAttendanceSession(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.AttendanceSessions.Find(1L)!.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteAttendanceSession_NonExistentId_ReturnsNotFound()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var result = await _controller.DeleteAttendanceSession(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // Percentage calculation accuracy
        [Test]
        public async Task GetStudentAttendanceSummary_AbsentStudent_ReturnsZeroPercent()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = 1, SessionId = 1, StudentId = "student-1",
                Status = "ABSENT", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetStudentAttendanceSummary("student-1", 1);

            var ok = result.Result as OkObjectResult;
            var summary = ok!.Value as StudentAttendanceSummaryDto;
            Assert.That(summary!.AttendancePercentage, Is.EqualTo(0.0));
            Assert.That(summary.AbsentCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetStudentAttendanceSummary_NoSessions_ReturnsZeroPercent()
        {
            var student = TestHelpers.MakeUser("student-2", "student2@lms.com", "Student Two");
            _userManager.Setup(u => u.FindByIdAsync("student-2")).ReturnsAsync(student);

            // Add a new subject with no sessions
            _context.Subjects.Add(TestHelpers.MakeSubject(2, 1, "Genetics"));
            _context.SaveChanges();

            var result = await _controller.GetStudentAttendanceSummary("student-2", 2);

            var ok = result.Result as OkObjectResult;
            var summary = ok!.Value as StudentAttendanceSummaryDto;
            Assert.That(summary!.AttendancePercentage, Is.EqualTo(0.0));
            Assert.That(summary.TotalSessions, Is.EqualTo(0));
        }
    }
}

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
    public class AssignmentControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<AppUser>> _userManager = null!;
        private AssignmentController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _userManager = TestHelpers.MockUserManager();
            _controller = new AssignmentController(
                _context,
                _userManager.Object,
                TestHelpers.MockLogger<AssignmentController>().Object);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("teacher-1", "teacher@test.com", "Teacher");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com", "Teacher One");
            var course = TestHelpers.MakeCourse(1, "Mathematics");
            var subject = TestHelpers.MakeSubject(1, 1, "Linear Algebra");
            var assignment = TestHelpers.MakeAssignment(1, 1, "Assignment 1");
            assignment.CreatedById = "teacher-1";

            _context.Users.Add(teacher);
            _context.Courses.Add(course);
            _context.Subjects.Add(subject);
            _context.Assignments.Add(assignment);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        // GetAssignmentsBySubject
        [Test]
        public async Task GetAssignmentsBySubject_ExistingSubject_ReturnsOkWithAssignments()
        {
            var result = await _controller.GetAssignmentsBySubject(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var assignments = ok!.Value as IEnumerable<AssignmentDto>;
            Assert.That(assignments!.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAssignmentsBySubject_NonExistentSubject_ReturnsNotFound()
        {
            var result = await _controller.GetAssignmentsBySubject(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetAssignmentsBySubject_DeletedSubject_ReturnsNotFound()
        {
            var subject = _context.Subjects.Find(1L)!;
            subject.IsDeleted = true;
            _context.SaveChanges();

            var result = await _controller.GetAssignmentsBySubject(1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetAssignment
        [Test]
        public async Task GetAssignment_ExistingId_ReturnsOkWithDto()
        {
            var result = await _controller.GetAssignment(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var dto = ok!.Value as AssignmentDto;
            Assert.That(dto!.Title, Is.EqualTo("Assignment 1"));
            Assert.That(dto.SubjectId, Is.EqualTo(1));
            Assert.That(dto.CreatedByName, Is.EqualTo("Teacher One"));
        }

        [Test]
        public async Task GetAssignment_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetAssignment(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetAssignment_DeletedAssignment_ReturnsNotFound()
        {
            var a = _context.Assignments.Find(1L)!;
            a.IsDeleted = true;
            _context.SaveChanges();

            var result = await _controller.GetAssignment(1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // CreateAssignment
        [Test]
        public async Task CreateAssignment_ValidDto_ReturnsCreated()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com", "Teacher One");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new CreateAssignmentDto
            {
                SubjectId = 1,
                Title = "New Assignment",
                Description = "Description",
                MaxScore = 50,
                DueDate = DateTime.UtcNow.AddDays(10)
            };

            var result = await _controller.CreateAssignment(dto);

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
            Assert.That(created!.StatusCode, Is.EqualTo(201));
        }

        [Test]
        public async Task CreateAssignment_SubjectNotFound_ReturnsNotFound()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new CreateAssignmentDto { SubjectId = 999, Title = "X", MaxScore = 100, DueDate = DateTime.UtcNow };

            var result = await _controller.CreateAssignment(dto);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateAssignment_UserNotFound_ReturnsUnauthorized()
        {
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync((AppUser?)null);

            var dto = new CreateAssignmentDto { SubjectId = 1, Title = "X", MaxScore = 100, DueDate = DateTime.UtcNow };

            var result = await _controller.CreateAssignment(dto);

            Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        // UpdateAssignment
        [Test]
        public async Task UpdateAssignment_ValidUpdate_ReturnsNoContent()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var dto = new UpdateAssignmentDto { Title = "Updated Title", MaxScore = 80 };

            var result = await _controller.UpdateAssignment(1, dto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var updated = _context.Assignments.Find(1L)!;
            Assert.That(updated.Title, Is.EqualTo("Updated Title"));
            Assert.That(updated.MaxScore, Is.EqualTo(80));
        }

        [Test]
        public async Task UpdateAssignment_NonExistentId_ReturnsNotFound()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com");
            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);

            var result = await _controller.UpdateAssignment(999, new UpdateAssignmentDto { Title = "X" });

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // DeleteAssignment
        [Test]
        public async Task DeleteAssignment_ExistingId_ReturnsNoContent()
        {
            var result = await _controller.DeleteAssignment(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var deleted = _context.Assignments.Find(1L)!;
            Assert.That(deleted.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteAssignment_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.DeleteAssignment(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task DeleteAssignment_AlreadyDeleted_ReturnsNotFound()
        {
            var a = _context.Assignments.Find(1L)!;
            a.IsDeleted = true;
            _context.SaveChanges();

            var result = await _controller.DeleteAssignment(1);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // Submissions
        [Test]
        public async Task GetSubmissions_ExistingAssignment_ReturnsOkWithList()
        {
            var student = TestHelpers.MakeUser("student-1", "student@test.com", "Student One");
            _context.Users.Add(student);
            _context.Submissions.Add(new Submission
            {
                Id = 1,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetSubmissions(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var subs = ok!.Value as IEnumerable<SubmissionDto>;
            Assert.That(subs!.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetSubmissions_AssignmentNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetSubmissions(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetSubmission_ExistingId_ReturnsDto()
        {
            var student = TestHelpers.MakeUser("student-1", "student@test.com");
            _context.Users.Add(student);
            _context.Submissions.Add(new Submission
            {
                Id = 10,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetSubmission(10);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetSubmission_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetSubmission(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // CreateSubmission
        [Test]
        public async Task CreateSubmission_Valid_ReturnsCreated()
        {
            var student = TestHelpers.MakeUser("student-1", "student@test.com", "Student One");
            _context.Users.Add(student);
            _context.SaveChanges();
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            var dto = new CreateSubmissionDto
            {
                AssignmentId = 1,
                StudentId = "student-1",
                FileUrl = "https://github.com/student/work"
            };
            _controller.ControllerContext = TestHelpers.CreateControllerContext("student-1", "student@test.com", "Student");

            var result = await _controller.CreateSubmission(dto);

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
            Assert.That(created!.StatusCode, Is.EqualTo(201));
        }

        [Test]
        public async Task CreateSubmission_DuplicateSubmission_ReturnsBadRequest()
        {
            var student = TestHelpers.MakeUser("student-1", "student@test.com");
            _context.Users.Add(student);
            _context.Submissions.Add(new Submission
            {
                Id = 1,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            var dto = new CreateSubmissionDto { AssignmentId = 1, StudentId = "student-1" };

            var result = await _controller.CreateSubmission(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        // Grading
        [Test]
        public async Task GradeSubmission_NewGrade_ReturnsCreated()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com", "Teacher");
            var student = TestHelpers.MakeUser("student-1", "student@test.com", "Student");
            _context.Users.Add(student);
            _context.Submissions.Add(new Submission
            {
                Id = 5,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SubjectTeachers.Add(new SubjectTeacher
            {
                Id = 1,
                SubjectId = 1,
                TeacherId = "teacher-1",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);
            _userManager.Setup(u => u.GetRolesAsync(teacher))
                        .ReturnsAsync(new List<string> { "Teacher" });

            var dto = new CreateAssignmentGradeDto { SubmissionId = 5, Score = 85, Feedback = "Good work" };

            var result = await _controller.GradeSubmission(dto);

            var created = result.Result as CreatedAtActionResult;
            Assert.That(created, Is.Not.Null);
            var grade = _context.AssignmentGrades.First();
            Assert.That(grade.Score, Is.EqualTo(85));
        }

        [Test]
        public async Task GradeSubmission_UpdateExistingGrade_ReturnsOk()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@test.com", "Teacher");
            var student = TestHelpers.MakeUser("student-1", "student@test.com");
            _context.Users.Add(student);
            _context.Submissions.Add(new Submission
            {
                Id = 5,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1,
                SubmissionId = 5,
                Score = 60,
                GradedById = "teacher-1",
                GradedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SubjectTeachers.Add(new SubjectTeacher { Id = 1, SubjectId = 1, TeacherId = "teacher-1", CreatedAt = DateTime.UtcNow });
            _context.SaveChanges();

            _userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                        .ReturnsAsync(teacher);
            _userManager.Setup(u => u.GetRolesAsync(teacher))
                        .ReturnsAsync(new List<string> { "Teacher" });

            var dto = new CreateAssignmentGradeDto { SubmissionId = 5, Score = 90, Feedback = "Excellent" };

            var result = await _controller.GradeSubmission(dto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var grade = _context.AssignmentGrades.First();
            Assert.That(grade.Score, Is.EqualTo(90));
        }

        [Test]
        public async Task DeleteGrade_ExistingGrade_ReturnsNoContent()
        {
            _context.Submissions.Add(new Submission
            {
                Id = 5,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1,
                SubmissionId = 5,
                Score = 70,
                GradedById = "teacher-1",
                GradedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.DeleteGrade(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.AssignmentGrades.Find(1L)!.IsDeleted, Is.True);
        }

        [Test]
        public async Task GetGrade_ExistingGrade_ReturnsOk()
        {
            var student = TestHelpers.MakeUser("student-1", "student@test.com", "Student One");
            _context.Users.Add(student);
            _context.Submissions.Add(new Submission
            {
                Id = 5,
                AssignmentId = 1,
                StudentId = "student-1",
                Status = "SUBMITTED",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1,
                SubmissionId = 5,
                Score = 75,
                Feedback = "Well done",
                GradedById = "teacher-1",
                GradedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetGrade(1);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetGrade_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetGrade(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}

using Learning_Management_System.Controllers;
using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Tests.NUnit.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Learning_Management_System.Tests.NUnit.Controllers
{
    [TestFixture]
    public class StudentProgressControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<AppUser>> _userManager = null!;
        private StudentProgressController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _userManager = TestHelpers.MockUserManager();
            _controller = new StudentProgressController(_context, _userManager.Object);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("student-1", "student@lms.com", "Student");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher One");
            var course = TestHelpers.MakeCourse(1, "Mathematics");
            var subject = TestHelpers.MakeSubject(1, 1, "Calculus");
            var batch = TestHelpers.MakeBatch(1, 1, "Batch A");

            _context.Users.AddRange(student, teacher);
            _context.Courses.Add(course);
            _context.Subjects.Add(subject);
            _context.CourseBatches.Add(batch);
            _context.Enrollments.Add(new Enrollment
            {
                Id = 1, StudentId = "student-1", BatchId = 1,
                Status = "ACTIVE", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        // GetStudentCourseProgress
        [Test]
        public async Task GetStudentCourseProgress_ExistingRecord_ReturnsProgress()
        {
            _context.StudentCourseProgress.Add(new StudentCourseProgress
            {
                Id = 1,
                StudentId = "student-1",
                CourseId = 1,
                AttendancePercentage = 85.5m,
                AssignmentAvgScore = 78.0m,
                ExamAvgScore = 72.0m,
                OverallGrade = "B",
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetStudentCourseProgress(1, "student-1");

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetStudentCourseProgress_NoRecord_ReturnsNotFound()
        {
            var result = await _controller.GetStudentCourseProgress(1, "student-1");

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetStudentAllCoursesProgress
        [Test]
        public async Task GetStudentAllCoursesProgress_StudentWithProgress_ReturnsProgressList()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);
            _context.StudentCourseProgress.Add(new StudentCourseProgress
            {
                Id = 1, StudentId = "student-1", CourseId = 1,
                AttendancePercentage = 90m, AssignmentAvgScore = 85m,
                ExamAvgScore = 80m, OverallGrade = "A",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetStudentAllCoursesProgress("student-1");

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetStudentAllCoursesProgress_StudentNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByIdAsync("ghost")).ReturnsAsync((AppUser?)null);

            var result = await _controller.GetStudentAllCoursesProgress("ghost");

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // RecalculateStudentProgress — signature is (string studentId, long courseId)
        [Test]
        public async Task RecalculateStudentProgress_ValidStudent_CreatesOrUpdatesProgress()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            // RecalculateStudentProgress requires an existing progress record
            _context.StudentCourseProgress.Add(new StudentCourseProgress
            {
                Id = 1, StudentId = "student-1", CourseId = 1,
                AttendancePercentage = 0m, AssignmentAvgScore = 0m,
                ExamAvgScore = 0m, OverallGrade = "F",
                CreatedAt = DateTime.UtcNow
            });

            // Add attendance data
            var session = TestHelpers.MakeSession(1, 1);
            _context.AttendanceSessions.Add(session);
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = 1, SessionId = 1, StudentId = "student-1",
                Status = "PRESENT", CreatedAt = DateTime.UtcNow
            });

            // Add assignment data
            var assignment = TestHelpers.MakeAssignment(1, 1, "Assignment 1");
            _context.Assignments.Add(assignment);
            _context.Submissions.Add(new Submission
            {
                Id = 1, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1, SubmissionId = 1, Score = 80, GradedById = "teacher-1",
                GradedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });

            // Add exam data
            var exam = TestHelpers.MakeExam(1, 1, "Midterm");
            _context.Exams.Add(exam);
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1, ExamId = 1, StudentId = "student-1",
                Marks = 75, Grade = "B", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.RecalculateStudentProgress("student-1", 1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var progress = _context.StudentCourseProgress
                .FirstOrDefault(p => p.StudentId == "student-1" && p.CourseId == 1);
            Assert.That(progress, Is.Not.Null);
        }

        [Test]
        public async Task RecalculateStudentProgress_StudentNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByIdAsync("ghost")).ReturnsAsync((AppUser?)null);

            var result = await _controller.RecalculateStudentProgress("ghost", 1);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task RecalculateStudentProgress_CourseNotFound_ReturnsNotFound()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            var result = await _controller.RecalculateStudentProgress("student-1", 999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetCourseStudentsProgress
        [Test]
        public async Task GetCourseStudentsProgress_ValidCourse_ReturnsPagedResults()
        {
            _context.StudentCourseProgress.Add(new StudentCourseProgress
            {
                Id = 1, StudentId = "student-1", CourseId = 1,
                AttendancePercentage = 85m, AssignmentAvgScore = 80m,
                ExamAvgScore = 75m, OverallGrade = "B",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetCourseStudentsProgress(1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetCourseStudentsProgress_CourseNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetCourseStudentsProgress(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetAtRiskStudents
        [Test]
        public async Task GetAtRiskStudents_ValidCourse_ReturnsAtRiskList()
        {
            _context.StudentCourseProgress.Add(new StudentCourseProgress
            {
                Id = 1, StudentId = "student-1", CourseId = 1,
                AttendancePercentage = 50m, AssignmentAvgScore = 40m,
                ExamAvgScore = 35m, OverallGrade = "D",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetAtRiskStudents(courseId: 1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        // GetStudentPerformanceTrends
        [Test]
        public async Task GetStudentPerformanceTrends_ValidStudent_ReturnsOk()
        {
            var student = TestHelpers.MakeUser("student-1", "student@lms.com");
            _userManager.Setup(u => u.FindByIdAsync("student-1")).ReturnsAsync(student);

            var result = await _controller.GetStudentPerformanceTrends("student-1");

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetStudentPerformanceTrends_StudentNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByIdAsync("ghost")).ReturnsAsync((AppUser?)null);

            var result = await _controller.GetStudentPerformanceTrends("ghost");

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}

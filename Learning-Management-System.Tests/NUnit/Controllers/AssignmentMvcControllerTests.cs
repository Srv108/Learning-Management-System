using Learning_Management_System.Controllers;
using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Tests.NUnit.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;

namespace Learning_Management_System.Tests.NUnit.Controllers
{
    [TestFixture]
    public class AssignmentMvcControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<IHttpClientFactory> _httpClientFactory = null!;
        private AssignmentMvcController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _controller = new AssignmentMvcController(_httpClientFactory.Object, _context);

            SeedDatabase();
            SetupControllerContext();
        }

        private void SeedDatabase()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher One");
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            var course = TestHelpers.MakeCourse(1, "Mathematics");
            var subject = TestHelpers.MakeSubject(1, 1, "Algebra");
            var assignment = TestHelpers.MakeAssignment(1, 1, "Assignment 1");
            assignment.CreatedById = "teacher-1";

            _context.Users.AddRange(teacher, student);
            _context.Courses.Add(course);
            _context.Subjects.Add(subject);
            _context.Assignments.Add(assignment);
            _context.SaveChanges();
        }

        private void SetupControllerContext(string userId = "teacher-1", string role = "Teacher")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new MockSession();
            httpContext.Session.SetString("JwtToken", "fake.jwt.token");
            httpContext.Session.SetString("UserId", userId);
            httpContext.Session.SetString("UserRole", role);

            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _controller.Dispose();
        }

        // Index (Teacher)
        [Test]
        public async Task Index_TeacherWithNoSubjectId_ReturnsViewWithEmptyJson()
        {
            var result = await _controller.Index();

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            Assert.That(_controller.ViewBag.AssignmentsJson, Is.EqualTo("[]"));
        }

        [Test]
        public async Task Index_TeacherWithSubjectId_ReturnsViewWithSubjectIdSet()
        {
            // Index uses an HTTP self-call to the API; the mock HttpClient returns empty,
            // so AssignmentsJson stays "[]" in unit tests — verify the view is still returned
            var result = await _controller.Index(subjectId: 1);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            Assert.That(_controller.ViewBag.SubjectId, Is.EqualTo((long?)1));
        }

        [Test]
        public async Task Index_NotAuthenticated_RedirectsToLogin()
        {
            SetupControllerContext("", "");
            var httpContext = _controller.ControllerContext.HttpContext;
            httpContext.Session.Remove("JwtToken");

            var result = await _controller.Index();

            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(redirect!.ActionName, Is.EqualTo("Login"));
        }

        [Test]
        public async Task Index_StudentRole_RedirectsToHome()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.Index();

            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(redirect!.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
        }

        // MyAssignments (Student)
        [Test]
        public async Task MyAssignments_StudentWithSubjectId_ReturnsAssignmentsWithStatus()
        {
            SetupControllerContext("student-1", "Student");
            _context.Submissions.Add(new Submission
            {
                Id = 1, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.MyAssignments(subjectId: 1);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            var json = _controller.ViewBag.AssignmentsJson as string;
            Assert.That(json, Does.Contain("submitted"));
            Assert.That(json, Does.Contain("true"));
        }

        [Test]
        public async Task MyAssignments_StudentNoSubjectId_ReturnsEmptyJson()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.MyAssignments();

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            Assert.That(_controller.ViewBag.AssignmentsJson, Is.EqualTo("[]"));
        }

        [Test]
        public async Task MyAssignments_UnsubmittedAssignment_ShowsSubmittedFalse()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.MyAssignments(subjectId: 1);

            var json = _controller.ViewBag.AssignmentsJson as string;
            Assert.That(json, Does.Contain("\"submitted\":false"));
        }

        // Submit (Student POST)
        [Test]
        public async Task Submit_NewSubmission_CreatesRecordAndRedirects()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.Submit(assignmentId: 1, fileUrl: "https://github.com/student/work", subjectId: 1);

            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(redirect!.ActionName, Is.EqualTo("MyAssignments"));
            Assert.That(_context.Submissions.Any(s => s.StudentId == "student-1" && s.AssignmentId == 1), Is.True);
        }

        [Test]
        public async Task Submit_ReSubmission_UpdatesExistingRecord()
        {
            SetupControllerContext("student-1", "Student");
            _context.Submissions.Add(new Submission
            {
                Id = 1, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", FileUrl = "https://old-url.com",
                SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            await _controller.Submit(assignmentId: 1, fileUrl: "https://new-url.com", subjectId: 1);

            var sub = _context.Submissions.Find(1L)!;
            Assert.That(sub.FileUrl, Is.EqualTo("https://new-url.com"));
        }

        [Test]
        public async Task Submit_LateSubmission_SetsLateStatus()
        {
            SetupControllerContext("student-1", "Student");
            var assignment = _context.Assignments.Find(1L)!;
            assignment.DueDate = DateTime.UtcNow.AddDays(-1); // overdue
            _context.SaveChanges();

            await _controller.Submit(assignmentId: 1, fileUrl: null, subjectId: 1);

            var sub = _context.Submissions.Single(s => s.StudentId == "student-1" && s.AssignmentId == 1);
            Assert.That(sub.Status, Is.EqualTo("LATE"));
        }

        [Test]
        public async Task Submit_AssignmentNotFound_RedirectsWithError()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.Submit(assignmentId: 999, fileUrl: null, subjectId: 1);

            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
        }

        // Grade (Teacher POST)
        [Test]
        public async Task Grade_NewGrade_SavesAndRedirects()
        {
            _context.Submissions.Add(new Submission
            {
                Id = 5, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.Grade(submissionId: 5, score: 88, feedback: "Good work", assignmentId: 1);

            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(redirect!.ActionName, Is.EqualTo("Submissions"));
            var grade = _context.AssignmentGrades.FirstOrDefault(g => g.SubmissionId == 5);
            Assert.That(grade, Is.Not.Null);
            Assert.That(grade!.Score, Is.EqualTo(88));
        }

        [Test]
        public async Task Grade_UpdateExistingGrade_ChangesScore()
        {
            _context.Submissions.Add(new Submission
            {
                Id = 5, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1, SubmissionId = 5, Score = 70, GradedById = "teacher-1",
                GradedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            await _controller.Grade(submissionId: 5, score: 95, feedback: "Excellent", assignmentId: 1);

            Assert.That(_context.AssignmentGrades.Find(1L)!.Score, Is.EqualTo(95));
        }

        [Test]
        public async Task Grade_SubmissionNotFound_RedirectsWithError()
        {
            var result = await _controller.Grade(submissionId: 999, score: 50, feedback: null, assignmentId: 1);

            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(_controller.TempData["Error"], Is.Not.Null);
        }

        // MyGrades (Student)
        [Test]
        public async Task MyGrades_StudentWithGradedSubmission_ShowsGrade()
        {
            SetupControllerContext("student-1", "Student");
            _context.Submissions.Add(new Submission
            {
                Id = 5, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1, SubmissionId = 5, Score = 92, Feedback = "Excellent",
                GradedById = "teacher-1", GradedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.MyGrades(subjectId: 1);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            var json = _controller.ViewBag.GradesJson as string;
            Assert.That(json, Does.Contain("\"score\":92"));
            Assert.That(json, Does.Contain("Excellent"));
            Assert.That(json, Does.Contain("\"graded\":true"));
        }

        [Test]
        public async Task MyGrades_StudentWithNoSubmission_ShowsUngradedEntry()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.MyGrades(subjectId: 1);

            var json = _controller.ViewBag.GradesJson as string;
            Assert.That(json, Does.Contain("\"submitted\":false"));
            Assert.That(json, Does.Contain("\"graded\":false"));
        }

        [Test]
        public async Task MyGrades_NoSubjectId_ReturnsEmptyJson()
        {
            SetupControllerContext("student-1", "Student");

            var result = await _controller.MyGrades();

            Assert.That(_controller.ViewBag.GradesJson, Is.EqualTo("[]"));
        }

        // Submissions (Teacher)
        [Test]
        public async Task Submissions_TeacherViewsAssignment_ShowsSubmissionsWithGradeInfo()
        {
            _context.Submissions.Add(new Submission
            {
                Id = 5, AssignmentId = 1, StudentId = "student-1",
                Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.AssignmentGrades.Add(new AssignmentGrade
            {
                Id = 1, SubmissionId = 5, Score = 75, GradedById = "teacher-1",
                GradedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.Submissions(assignmentId: 1);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            var json = _controller.ViewBag.SubmissionsJson as string;
            Assert.That(json, Does.Contain("\"graded\":true"));
            Assert.That(json, Does.Contain("\"gradeScore\":75"));
        }

        [Test]
        public async Task Submissions_NoAssignmentId_ReturnsEmptySubmissions()
        {
            var result = await _controller.Submissions();

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            Assert.That(_controller.ViewBag.SubmissionsJson, Is.EqualTo("[]"));
        }
    }

    // Minimal ISession mock for testing
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}

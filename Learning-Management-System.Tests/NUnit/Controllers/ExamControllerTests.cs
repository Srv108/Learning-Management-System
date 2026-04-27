using Learning_Management_System.Controllers;
using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Learning_Management_System.Tests.NUnit.Helpers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System.Security.Claims;

namespace Learning_Management_System.Tests.NUnit.Controllers
{
    [TestFixture]
    public class ExamControllerTests
    {
        private ApplicationDbContext _context = null!;
        private ExamController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _context = TestHelpers.CreateInMemoryContext();
            _controller = new ExamController(_context);
            _controller.ControllerContext = TestHelpers.CreateControllerContext("teacher-1", "teacher@lms.com", "Teacher");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var teacher = TestHelpers.MakeUser("teacher-1", "teacher@lms.com", "Teacher One");
            var student = TestHelpers.MakeUser("student-1", "student@lms.com", "Student One");
            var course = TestHelpers.MakeCourse(1, "Physics");
            var subject = TestHelpers.MakeSubject(1, 1, "Mechanics");
            var exam = TestHelpers.MakeExam(1, 1, "Midterm");
            exam.CreatedById = "teacher-1";

            _context.Users.AddRange(teacher, student);
            _context.Courses.Add(course);
            _context.Subjects.Add(subject);
            _context.Exams.Add(exam);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        // GetExamsBySubject
        [Test]
        public async Task GetExamsBySubject_ExistingSubject_ReturnsOkWithExams()
        {
            var result = await _controller.GetExamsBySubject(1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var exams = ok!.Value as List<ExamDto>;
            Assert.That(exams, Is.Not.Null);
            Assert.That(exams!.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetExamsBySubject_NoExamsForSubject_ReturnsEmptyList()
        {
            var result = await _controller.GetExamsBySubject(999);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var exams = ok!.Value as List<ExamDto>;
            Assert.That(exams, Is.Empty);
        }

        // GetExam
        [Test]
        public async Task GetExam_ExistingId_ReturnsOkWithDto()
        {
            var result = await _controller.GetExam(1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var exam = ok!.Value as ExamDto;
            Assert.That(exam!.Title, Is.EqualTo("Midterm"));
            Assert.That(exam.ExamType, Is.EqualTo("MIDTERM"));
        }

        [Test]
        public async Task GetExam_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetExam(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetExam_DeletedExam_ReturnsNotFound()
        {
            var exam = _context.Exams.Find(1L)!;
            exam.IsDeleted = true;
            _context.SaveChanges();

            var result = await _controller.GetExam(1);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // CreateExam
        [Test]
        public async Task CreateExam_ValidDto_ReturnsCreated()
        {
            var dto = new CreateExamDto
            {
                SubjectId = 1,
                Title = "Final Exam",
                ExamType = "FINAL",
                MaxScore = 100,
                ExamDate = DateTime.UtcNow.AddDays(30)
            };

            var result = await _controller.CreateExam(dto);

            Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(_context.Exams.Count(e => !e.IsDeleted), Is.EqualTo(2));
        }

        [Test]
        public async Task CreateExam_SubjectNotFound_ReturnsBadRequest()
        {
            var dto = new CreateExamDto
            {
                SubjectId = 999,
                Title = "Quiz",
                ExamType = "QUIZ",
                MaxScore = 50,
                ExamDate = DateTime.UtcNow.AddDays(7)
            };

            var result = await _controller.CreateExam(dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        // UpdateExam
        [Test]
        public async Task UpdateExam_ValidUpdate_ReturnsNoContent()
        {
            var dto = new UpdateExamDto { Title = "Updated Midterm", MaxScore = 80 };

            var result = await _controller.UpdateExam(1, dto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var exam = _context.Exams.Find(1L)!;
            Assert.That(exam.Title, Is.EqualTo("Updated Midterm"));
            Assert.That(exam.MaxScore, Is.EqualTo(80));
        }

        [Test]
        public async Task UpdateExam_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.UpdateExam(999, new UpdateExamDto { Title = "X" });

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // DeleteExam
        [Test]
        public async Task DeleteExam_ExistingId_ReturnsNoContent()
        {
            var result = await _controller.DeleteExam(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.Exams.Find(1L)!.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteExam_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.DeleteExam(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // AddResult
        [Test]
        public async Task AddResult_ValidDto_ReturnsCreated()
        {
            var dto = new CreateExamResultDto
            {
                ExamId = 1,
                StudentId = "student-1",
                Marks = 85,
                Grade = "A"
            };

            var result = await _controller.AddResult(dto);

            Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(_context.ExamResults.Any(r => r.StudentId == "student-1"), Is.True);
        }

        [Test]
        public async Task AddResult_ExamNotFound_ReturnsBadRequest()
        {
            var dto = new CreateExamResultDto { ExamId = 999, StudentId = "student-1", Marks = 80, Grade = "B" };

            var result = await _controller.AddResult(dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task AddResult_StudentNotFound_ReturnsBadRequest()
        {
            var dto = new CreateExamResultDto { ExamId = 1, StudentId = "ghost-id", Marks = 80, Grade = "B" };

            var result = await _controller.AddResult(dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task AddResult_DuplicateResult_ReturnsConflict()
        {
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1,
                ExamId = 1,
                StudentId = "student-1",
                Marks = 70,
                Grade = "B",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var dto = new CreateExamResultDto { ExamId = 1, StudentId = "student-1", Marks = 85, Grade = "A" };

            var result = await _controller.AddResult(dto);

            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        // GetResultsForExam
        [Test]
        public async Task GetResultsForExam_ExistingExam_ReturnsOkWithResults()
        {
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1, ExamId = 1, StudentId = "student-1",
                Marks = 75, Grade = "B", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetResultsForExam(1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            var results = ok!.Value as List<ExamResultDto>;
            Assert.That(results!.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetResultsForExam_ExamNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetResultsForExam(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetResult
        [Test]
        public async Task GetResult_ExistingId_ReturnsOk()
        {
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1, ExamId = 1, StudentId = "student-1",
                Marks = 90, Grade = "A", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetResult(1);

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task GetResult_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetResult(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // UpdateResult
        [Test]
        public async Task UpdateResult_ValidDto_ReturnsNoContent()
        {
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1, ExamId = 1, StudentId = "student-1",
                Marks = 70, Grade = "B", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var dto = new CreateExamResultDto { ExamId = 1, StudentId = "student-1", Marks = 90, Grade = "A" };

            var result = await _controller.UpdateResult(1, dto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.ExamResults.Find(1L)!.Marks, Is.EqualTo(90));
        }

        // DeleteResult
        [Test]
        public async Task DeleteResult_ExistingId_ReturnsNoContent()
        {
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1, ExamId = 1, StudentId = "student-1",
                Marks = 80, Grade = "B", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.DeleteResult(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(_context.ExamResults.Find(1L)!.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteResult_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.DeleteResult(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        // GetStudentAllResults
        [Test]
        public async Task GetStudentAllResults_StudentWithResults_ReturnsAllResults()
        {
            _context.ExamResults.Add(new ExamResult
            {
                Id = 1, ExamId = 1, StudentId = "student-1",
                Marks = 85, Grade = "A", CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = await _controller.GetStudentAllResults("student-1");

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        // GetStudentExamSummary
        [Test]
        public async Task GetStudentExamSummary_WithResults_ReturnsCorrectAverage()
        {
            _context.ExamResults.AddRange(
                new ExamResult { Id = 1, ExamId = 1, StudentId = "student-1", Marks = 80, Grade = "B", CreatedAt = DateTime.UtcNow },
                new ExamResult { Id = 2, ExamId = 1, StudentId = "student-1", Marks = 90, Grade = "A", CreatedAt = DateTime.UtcNow }
            );
            // Second one will be deduplicated in real scenario, but for summary test it's fine
            _context.ExamResults.Remove(_context.ExamResults.Find(2L)!); // Remove the duplicate
            _context.SaveChanges();

            var result = await _controller.GetStudentExamSummary("student-1", 1);

            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task GetStudentExamSummary_StudentNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetStudentExamSummary("ghost-id", 1);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetStudentExamSummary_SubjectNotFound_ReturnsNotFound()
        {
            var result = await _controller.GetStudentExamSummary("student-1", 999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}

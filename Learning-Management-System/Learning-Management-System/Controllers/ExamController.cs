using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ExamController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [Route("subject/{subjectId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExamsBySubject(long subjectId)
        {
            var exams = await _dbContext.Exams
                .Where(e => !e.IsDeleted && e.SubjectId == subjectId)
                .Include(e => e.Subject)
                .Include(e => e.CreatedBy)
                .Select(e => new ExamDto
                {
                    Id = e.Id,
                    SubjectId = e.SubjectId,
                    SubjectName = e.Subject != null ? e.Subject.Name : string.Empty,
                    Title = e.Title,
                    ExamType = e.ExamType,
                    MaxScore = e.MaxScore,
                    ExamDate = e.ExamDate,
                    CreatedById = e.CreatedById,
                    CreatedByName = e.CreatedBy != null ? e.CreatedBy.FullName : string.Empty,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(exams);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExam(long id)
        {
            var exam = await _dbContext.Exams
                .Where(e => !e.IsDeleted && e.Id == id)
                .Include(e => e.Subject)
                .Include(e => e.CreatedBy)
                .Select(e => new ExamDto
                {
                    Id = e.Id,
                    SubjectId = e.SubjectId,
                    SubjectName = e.Subject != null ? e.Subject.Name : string.Empty,
                    Title = e.Title,
                    ExamType = e.ExamType,
                    MaxScore = e.MaxScore,
                    ExamDate = e.ExamDate,
                    CreatedById = e.CreatedById,
                    CreatedByName = e.CreatedBy != null ? e.CreatedBy.FullName : string.Empty,
                    CreatedAt = e.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (exam == null)
            {
                return NotFound(new { message = "Exam not found" });
            }

            return Ok(exam);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher")]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var subject = await _dbContext.Subjects.FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == dto.SubjectId);
            if (subject == null)
                return BadRequest(new { message = "Subject not found" });

            var currentUserId = User?.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid user" });

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == currentUserId);
            if (user == null)
                return Unauthorized(new { message = "Invalid user" });

            var exam = new Exam
            {
                SubjectId = dto.SubjectId,
                Title = dto.Title,
                ExamType = dto.ExamType,
                MaxScore = dto.MaxScore,
                ExamDate = dto.ExamDate,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.Exams.Add(exam);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExam), new { id = exam.Id }, new { exam.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher")]
        public async Task<IActionResult> UpdateExam(long id, [FromBody] UpdateExamDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == id);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            if (!string.IsNullOrEmpty(dto.Title)) exam.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.ExamType)) exam.ExamType = dto.ExamType;
            if (dto.MaxScore.HasValue) exam.MaxScore = dto.MaxScore.Value;
            if (dto.ExamDate.HasValue) exam.ExamDate = dto.ExamDate.Value;

            exam.UpdatedAt = DateTime.UtcNow;

            _dbContext.Exams.Update(exam);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator")]
        public async Task<IActionResult> DeleteExam(long id)
        {
            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == id);
            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            exam.IsDeleted = true;
            exam.UpdatedAt = DateTime.UtcNow;
            _dbContext.Exams.Update(exam);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{examId}/results")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,Student")]
        public async Task<IActionResult> GetResultsForExam(long examId)
        {
            var exists = await _dbContext.Exams.AnyAsync(e => !e.IsDeleted && e.Id == examId);
            if (!exists)
                return NotFound(new { message = "Exam not found" });

            var results = await _dbContext.ExamResults
                .Where(r => !r.IsDeleted && r.ExamId == examId)
                .Include(r => r.Exam)
                .Include(r => r.Student)
                .Include(r => r.GradedBy)
                .Select(r => new ExamResultDto
                {
                    Id = r.Id,
                    ExamId = r.ExamId,
                    ExamTitle = r.Exam != null ? r.Exam.Title : string.Empty,
                    SubjectId = r.Exam != null ? r.Exam.SubjectId : 0,
                    StudentId = r.StudentId,
                    StudentName = r.Student != null ? r.Student.FullName : string.Empty,
                    StudentEmail = r.Student != null ? r.Student.Email : string.Empty,
                    Marks = r.Marks ?? 0,
                    Grade = r.Grade ?? string.Empty,
                    GradedById = r.GradedById ?? string.Empty,
                    GradedByName = r.GradedBy != null ? r.GradedBy.FullName : string.Empty,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("result/{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,Student")]
        public async Task<IActionResult> GetResult(long id)
        {
            var result = await _dbContext.ExamResults
                .Where(r => !r.IsDeleted && r.Id == id)
                .Include(r => r.Exam)
                .Include(r => r.Student)
                .Include(r => r.GradedBy)
                .Select(r => new ExamResultDto
                {
                    Id = r.Id,
                    ExamId = r.ExamId,
                    ExamTitle = r.Exam != null ? r.Exam.Title : string.Empty,
                    SubjectId = r.Exam != null ? r.Exam.SubjectId : 0,
                    StudentId = r.StudentId,
                    StudentName = r.Student != null ? r.Student.FullName : string.Empty,
                    StudentEmail = r.Student != null ? r.Student.Email : string.Empty,
                    Marks = r.Marks ?? 0,
                    Grade = r.Grade ?? string.Empty,
                    GradedById = r.GradedById ?? string.Empty,
                    GradedByName = r.GradedBy != null ? r.GradedBy.FullName : string.Empty,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { message = "Result not found" });

            return Ok(result);
        }

        [HttpPost("result")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher")]
        public async Task<IActionResult> AddResult([FromBody] CreateExamResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == dto.ExamId);
            if (exam == null)
                return BadRequest(new { message = "Exam not found" });

            var student = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });

            var already = await _dbContext.ExamResults.AnyAsync(r => !r.IsDeleted && r.ExamId == dto.ExamId && r.StudentId == dto.StudentId);
            if (already)
                return Conflict(new { message = "Result already exists" });

            var graderId = User?.Identity?.Name ?? string.Empty;
            var grader = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == graderId);
            if (grader == null)
                return Unauthorized(new { message = "Invalid grader" });

            var result = new ExamResult
            {
                ExamId = dto.ExamId,
                StudentId = dto.StudentId,
                Marks = dto.Marks,
                Grade = dto.Grade,
                GradedById = grader.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.ExamResults.Add(result);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetResult), new { id = result.Id }, new { result.Id });
        }

        [HttpPut("result/{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher")]
        public async Task<IActionResult> UpdateResult(long id, [FromBody] CreateExamResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _dbContext.ExamResults.FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == id);
            if (result == null)
                return NotFound(new { message = "Result not found" });

            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == dto.ExamId);
            if (exam == null)
                return BadRequest(new { message = "Exam not found" });

            var student = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });

            var graderId = User?.Identity?.Name ?? string.Empty;
            var grader = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == graderId);
            if (grader == null)
                return Unauthorized(new { message = "Invalid grader" });

            result.ExamId = dto.ExamId;
            result.StudentId = dto.StudentId;
            result.Marks = dto.Marks;
            result.Grade = dto.Grade;
            result.GradedById = grader.Id;
            result.UpdatedAt = DateTime.UtcNow;

            _dbContext.ExamResults.Update(result);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("result/{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator")]
        public async Task<IActionResult> DeleteResult(long id)
        {
            var result = await _dbContext.ExamResults.FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == id);
            if (result == null)
                return NotFound(new { message = "Result not found" });

            result.IsDeleted = true;
            result.UpdatedAt = DateTime.UtcNow;

            _dbContext.ExamResults.Update(result);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("student/{studentId}/subject/{subjectId}/summary")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,Student")]
        public async Task<IActionResult> GetStudentExamSummary(string studentId, long subjectId)
        {
            var student = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var subject = await _dbContext.Subjects.FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == subjectId);
            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            var results = await _dbContext.ExamResults
                .Where(r => !r.IsDeleted && r.StudentId == studentId && r.Exam.SubjectId == subjectId)
                .Include(r => r.Exam)
                .ToListAsync();

            if (!results.Any())
                return Ok(new StudentExamSummaryDto
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    StudentEmail = student.Email,
                    SubjectId = subjectId,
                    SubjectName = subject.Name,
                    ExamsTaken = 0,
                    AverageScore = 0,
                    BestGrade = string.Empty
                });

            var average = results.Average(x => x.Marks ?? 0);
            var best = results.OrderByDescending(x => x.Marks ?? 0).First().Grade ?? string.Empty;

            return Ok(new StudentExamSummaryDto
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                StudentEmail = student.Email,
                SubjectId = subjectId,
                SubjectName = subject.Name,
                ExamsTaken = results.Count,
                AverageScore = average,
                BestGrade = best
            });
        }
    }
}

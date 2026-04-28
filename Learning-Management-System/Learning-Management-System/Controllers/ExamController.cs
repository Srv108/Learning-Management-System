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

        [HttpGet("{examId}/eligible-students")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController")]
        public async Task<IActionResult> GetEligibleStudentsForExam(long examId)
        {
            var exam = await _dbContext.Exams
                .Include(e => e.Subject)
                .ThenInclude(s => s!.Course)
                .FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == examId);

            if (exam == null)
                return NotFound(new { message = "Exam not found" });

            var courseId = exam.Subject?.CourseId;
            if (courseId == null)
                return Ok(Array.Empty<EligibleStudentDto>());

            // Students enrolled in any non-deleted batch for the course, whose result for this exam is not yet recorded.
            // ("Not yet recorded" means no non-deleted ExamResult exists for {examId, studentId}.)
            var students = await (
                    from e in _dbContext.Enrollments
                    join b in _dbContext.CourseBatches on e.BatchId equals b.Id
                    join u in _dbContext.Users on e.StudentId equals u.Id
                    where !e.IsDeleted
                          && !b.IsDeleted
                          && b.CourseId == courseId
                          && (e.Status ?? string.Empty).Trim().ToUpper() != "DROPPED"
                          && !_dbContext.ExamResults.Any(r => !r.IsDeleted && r.ExamId == examId && r.StudentId == u.Id)
                    select new { u.Id, u.FullName, u.Email }
                )
                .Distinct()
                .OrderBy(x => x.FullName)
                .Select(x => new EligibleStudentDto
                {
                    Id = x.Id,
                    FullName = x.FullName ?? string.Empty,
                    Email = x.Email ?? string.Empty
                })
                .ToListAsync();

            return Ok(students);
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

        /// <summary>
        /// Get all exam results for a specific student across all subjects
        /// </summary>
        [HttpGet("student/{studentId}/all-results")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStudentAllResults(string studentId)
        {
            var results = await _dbContext.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Subject)
                        .ThenInclude(s => s!.Course)
                .Where(r => r.StudentId == studentId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(results.Select(r => new
            {
                id = r.Id,
                examId = r.ExamId,
                examTitle = r.Exam?.Title ?? "",
                examType = r.Exam?.ExamType ?? "",
                maxScore = r.Exam?.MaxScore ?? 0,
                subjectName = r.Exam?.Subject?.Name ?? "",
                courseName = r.Exam?.Subject?.Course?.Title ?? "",
                marks = r.Marks ?? 0,
                grade = r.Grade ?? "",
                examDate = r.Exam?.ExamDate,
                createdAt = r.CreatedAt
            }));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController")]
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
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController")]
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
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController,Student")]
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
                // Sort by username/email to match UI expectations like student1/student2 ordering
                .OrderBy(r => r.Student != null ? (r.Student.UserName ?? r.Student.Email ?? string.Empty) : string.Empty)
                .ThenBy(r => r.Student != null ? (r.Student.Email ?? string.Empty) : string.Empty)
                .ThenBy(r => r.StudentId)
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
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController,Student")]
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
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController")]
        public async Task<IActionResult> AddResult([FromBody] CreateExamResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == dto.ExamId);
            if (exam == null)
                return BadRequest(new { message = "Exam not found" });

            var subject = await _dbContext.Subjects
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == exam.SubjectId);
            if (subject?.CourseId == null)
                return BadRequest(new { message = "Exam subject/course not found" });

            // StudentId can be either the user's Id OR their email/username (to support manual entry from UI)
            var student = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId);
            if (student == null)
            {
                student = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.StudentId || u.UserName == dto.StudentId);
            }
            if (student == null)
                return BadRequest(new { message = "Student not found" });

            var studentId = student.Id;

            // Ensure student is enrolled in the course for this exam
            var enrolled = await (
                from e in _dbContext.Enrollments
                join b in _dbContext.CourseBatches on e.BatchId equals b.Id
                where !e.IsDeleted
                      && !b.IsDeleted
                      && b.CourseId == subject.CourseId
                      && e.StudentId == studentId
                      && (e.Status ?? string.Empty).Trim().ToUpper() != "DROPPED"
                select e.Id
            ).AnyAsync();
            if (!enrolled)
                return BadRequest(new
                {
                    message = "Student is not enrolled in this course",
                    courseId = subject.CourseId,
                    courseTitle = subject.Course?.Title ?? string.Empty,
                    student = new { id = studentId, email = student.Email ?? string.Empty, userName = student.UserName ?? string.Empty }
                });

            // Handle uniqueness constraint (ExamId + StudentId) even with soft-delete.
            // If a deleted result exists, revive/update it instead of inserting a new row.
            var existing = await _dbContext.ExamResults
                .FirstOrDefaultAsync(r => r.ExamId == dto.ExamId && r.StudentId == studentId);
            if (existing != null && !existing.IsDeleted)
                return Conflict(new { message = "Result already exists" });

            var graderId = User?.Identity?.Name ?? string.Empty;
            var grader = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == graderId);
            if (grader == null)
                return Unauthorized(new { message = "Invalid grader" });

            if (existing != null)
            {
                existing.IsDeleted = false;
                existing.Marks = dto.Marks;
                existing.Grade = dto.Grade;
                existing.GradedById = grader.Id;
                existing.UpdatedAt = DateTime.UtcNow;

                _dbContext.ExamResults.Update(existing);
                await _dbContext.SaveChangesAsync();

                return Ok(new { existing.Id });
            }
            else
            {
                var result = new ExamResult
                {
                    ExamId = dto.ExamId,
                    StudentId = studentId,
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
        }

        [HttpPut("result/{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController")]
        public async Task<IActionResult> UpdateResult(long id, [FromBody] UpdateExamResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _dbContext.ExamResults.FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == id);
            if (result == null)
                return NotFound(new { message = "Result not found" });

            var graderId = User?.Identity?.Name ?? string.Empty;
            var grader = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == graderId);
            if (grader == null)
                return Unauthorized(new { message = "Invalid grader" });

            if (dto.Marks.HasValue) result.Marks = dto.Marks.Value;
            if (!string.IsNullOrWhiteSpace(dto.Grade)) result.Grade = dto.Grade;
            result.GradedById = grader.Id;
            result.UpdatedAt = DateTime.UtcNow;

            _dbContext.ExamResults.Update(result);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("result/{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator,Teacher,ExamController")]
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

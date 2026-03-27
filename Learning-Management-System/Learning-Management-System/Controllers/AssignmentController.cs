using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssignmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AssignmentController> _logger;

        public AssignmentController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<AssignmentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("subject/{subjectId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetAssignmentsBySubject(long subjectId)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == subjectId && !s.IsDeleted);

                if (subject == null)
                {
                    return NotFound("Subject not found");
                }

                var assignments = await _context.Assignments
                    .Include(a => a.CreatedBy)
                    .Where(a => a.SubjectId == subjectId && !a.IsDeleted)
                    .OrderByDescending(a => a.DueDate)
                    .ToListAsync();

                var result = assignments.Select(a => new AssignmentDto
                {
                    Id = a.Id,
                    SubjectId = a.SubjectId,
                    SubjectName = subject.Name,
                    Title = a.Title,
                    Description = a.Description,
                    MaxScore = a.MaxScore,
                    DueDate = a.DueDate,
                    CreatedById = a.CreatedById,
                    CreatedByName = a.CreatedBy?.FullName ?? "",
                    CreatedAt = a.CreatedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments for subject {SubjectId}", subjectId);
                return StatusCode(500, "An error occurred while retrieving assignments");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AssignmentDto>> GetAssignment(long id)
        {
            try
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Subject.Course)
                    .Include(a => a.CreatedBy)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (assignment == null)
                {
                    return NotFound("Assignment not found");
                }

                var dto = new AssignmentDto
                {
                    Id = assignment.Id,
                    SubjectId = assignment.SubjectId,
                    SubjectName = assignment.Subject?.Name ?? "",
                    Title = assignment.Title,
                    Description = assignment.Description,
                    MaxScore = assignment.MaxScore,
                    DueDate = assignment.DueDate,
                    CreatedById = assignment.CreatedById,
                    CreatedByName = assignment.CreatedBy?.FullName ?? "",
                    CreatedAt = assignment.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment {AssignmentId}", id);
                return StatusCode(500, "An error occurred while retrieving assignment");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<ActionResult<AssignmentDto>> CreateAssignment(CreateAssignmentDto dto)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == dto.SubjectId && !s.IsDeleted);
                if (subject == null)
                {
                    return NotFound("Subject not found");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("User not found");

                var roles = await _userManager.GetRolesAsync(user);
                var authorized = roles.Contains("Admin") || roles.Contains("CourseCoordinator");
                if (!authorized)
                {
                    var assigned = await _context.SubjectTeachers.AnyAsync(st => st.SubjectId == dto.SubjectId && st.TeacherId == user.Id && !st.IsDeleted);
                    if (!assigned) return Forbid("No permission to create assignments for this subject");
                }

                var assignment = new Assignment
                {
                    SubjectId = dto.SubjectId,
                    Title = dto.Title,
                    Description = dto.Description,
                    MaxScore = dto.MaxScore,
                    DueDate = dto.DueDate,
                    CreatedById = user.Id,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Assignment {AssignmentId} created by {UserId}", assignment.Id, user.Id);

                return CreatedAtAction(nameof(GetAssignment), new { id = assignment.Id }, new AssignmentDto
                {
                    Id = assignment.Id,
                    SubjectId = assignment.SubjectId,
                    SubjectName = subject.Name,
                    Title = assignment.Title,
                    Description = assignment.Description,
                    MaxScore = assignment.MaxScore,
                    DueDate = assignment.DueDate,
                    CreatedById = assignment.CreatedById,
                    CreatedByName = user.FullName ?? user.UserName,
                    CreatedAt = assignment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment");
                return StatusCode(500, "An error occurred while creating assignment");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> UpdateAssignment(long id, UpdateAssignmentDto dto)
        {
            try
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Subject)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
                if (assignment == null) return NotFound("Assignment not found");

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("User not found");

                var roles = await _userManager.GetRolesAsync(user);
                var authorized = roles.Contains("Admin") || roles.Contains("CourseCoordinator");
                if (!authorized)
                {
                    var assigned = await _context.SubjectTeachers.AnyAsync(st => st.SubjectId == assignment.SubjectId && st.TeacherId == user.Id && !st.IsDeleted);
                    if (!assigned) return Forbid("No permission to update assignment");
                }

                if (!string.IsNullOrWhiteSpace(dto.Title)) assignment.Title = dto.Title;
                if (dto.Description != null) assignment.Description = dto.Description;
                if (dto.MaxScore.HasValue) assignment.MaxScore = dto.MaxScore.Value;
                if (dto.DueDate.HasValue) assignment.DueDate = dto.DueDate.Value;

                assignment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Assignment {AssignmentId} updated by {UserId}", id, user.Id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment {AssignmentId}", id);
                return StatusCode(500, "An error occurred while updating assignment");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<IActionResult> DeleteAssignment(long id)
        {
            try
            {
                var assignment = await _context.Assignments.FindAsync(id);
                if (assignment == null || assignment.IsDeleted) return NotFound("Assignment not found");

                assignment.IsDeleted = true;
                assignment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Assignment {AssignmentId} deleted", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment {AssignmentId}", id);
                return StatusCode(500, "An error occurred while deleting assignment");
            }
        }

        [HttpGet("{assignmentId}/submissions")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetSubmissions(long assignmentId)
        {
            try
            {
                var assignment = await _context.Assignments.FindAsync(assignmentId);
                if (assignment == null || assignment.IsDeleted) return NotFound("Assignment not found");

                var submissions = await _context.Submissions
                    .Include(s => s.Student)
                    .Where(s => s.AssignmentId == assignmentId && !s.IsDeleted)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToListAsync();

                var result = submissions.Select(s => new SubmissionDto
                {
                    Id = s.Id,
                    AssignmentId = s.AssignmentId,
                    AssignmentTitle = assignment.Title,
                    StudentId = s.StudentId,
                    StudentName = s.Student?.FullName ?? "",
                    StudentEmail = s.Student?.Email ?? "",
                    FileUrl = s.FileUrl,
                    Status = s.Status,
                    SubmittedAt = s.SubmittedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission list for assignment {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while retrieving submissions");
            }
        }

        [HttpPost("submission")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<SubmissionDto>> CreateSubmission(CreateSubmissionDto dto)
        {
            try
            {
                var assignment = await _context.Assignments.FindAsync(dto.AssignmentId);
                if (assignment == null || assignment.IsDeleted) return NotFound("Assignment not found");

                var student = await _userManager.FindByIdAsync(dto.StudentId);
                if (student == null) return NotFound("Student not found");

                var existingSubmission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentId == dto.StudentId && !s.IsDeleted);
                if (existingSubmission != null) return BadRequest("Submission already exists");

                var status = dto.FileUrl == null ? "SUBMITTED" : (DateTime.UtcNow > assignment.DueDate ? "LATE" : "SUBMITTED");

                var submission = new Submission
                {
                    AssignmentId = dto.AssignmentId,
                    StudentId = dto.StudentId,
                    FileUrl = dto.FileUrl,
                    Status = status,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Submission {SubmissionId} created for assignment {AssignmentId} by student {StudentId}", submission.Id, dto.AssignmentId, dto.StudentId);

                return CreatedAtAction(nameof(GetSubmission), new { id = submission.Id }, new SubmissionDto
                {
                    Id = submission.Id,
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = assignment.Title,
                    StudentId = submission.StudentId,
                    StudentName = student.FullName ?? "",
                    StudentEmail = student.Email ?? "",
                    FileUrl = submission.FileUrl,
                    Status = submission.Status,
                    SubmittedAt = submission.SubmittedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission");
                return StatusCode(500, "An error occurred while creating submission");
            }
        }

        [HttpGet("submission/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<SubmissionDto>> GetSubmission(long id)
        {
            try
            {
                var submission = await _context.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.Student)
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (submission == null) return NotFound("Submission not found");

                var dto = new SubmissionDto
                {
                    Id = submission.Id,
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = submission.Assignment?.Title ?? "",
                    StudentId = submission.StudentId,
                    StudentName = submission.Student?.FullName ?? "",
                    StudentEmail = submission.Student?.Email ?? "",
                    FileUrl = submission.FileUrl,
                    Status = submission.Status,
                    SubmittedAt = submission.SubmittedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submission {SubmissionId}", id);
                return StatusCode(500, "An error occurred while retrieving submission");
            }
        }

        [HttpPost("grade")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<ActionResult<AssignmentGradeDto>> GradeSubmission(CreateAssignmentGradeDto dto)
        {
            try
            {
                var submission = await _context.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.Student)
                    .FirstOrDefaultAsync(s => s.Id == dto.SubmissionId && !s.IsDeleted);
                if (submission == null) return NotFound("Submission not found");

                var assignment = submission.Assignment;
                if (assignment == null || assignment.IsDeleted) return NotFound("Related assignment not found");

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("User not found");

                var roles = await _userManager.GetRolesAsync(user);
                var authorized = roles.Contains("Admin") || roles.Contains("CourseCoordinator");
                if (!authorized)
                {
                    var assigned = await _context.SubjectTeachers.AnyAsync(st => st.SubjectId == assignment.SubjectId && st.TeacherId == user.Id && !st.IsDeleted);
                    if (!assigned) return Forbid("No permission to grade this submission");
                }

                var existingGrade = await _context.AssignmentGrades.FirstOrDefaultAsync(g => g.SubmissionId == dto.SubmissionId && !g.IsDeleted);

                if (existingGrade != null)
                {
                    existingGrade.Score = dto.Score;
                    existingGrade.Feedback = dto.Feedback;
                    existingGrade.GradedById = user.Id;
                    existingGrade.GradedAt = DateTime.UtcNow;
                    existingGrade.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Grade updated for submission {SubmissionId} by {UserId}", dto.SubmissionId, user.Id);

                    return Ok(new AssignmentGradeDto
                    {
                        Id = existingGrade.Id,
                        SubmissionId = existingGrade.SubmissionId,
                        AssignmentId = assignment.Id,
                        AssignmentTitle = assignment.Title,
                        StudentId = submission.StudentId,
                        StudentName = submission.Student?.FullName ?? "",
                        Score = existingGrade.Score,
                        Feedback = existingGrade.Feedback,
                        GradedById = existingGrade.GradedById,
                        GradedByName = user.FullName ?? user.UserName ?? "",
                        GradedAt = existingGrade.GradedAt,
                        CreatedAt = existingGrade.CreatedAt
                    });
                }

                var grade = new AssignmentGrade
                {
                    SubmissionId = dto.SubmissionId,
                    Score = dto.Score,
                    Feedback = dto.Feedback,
                    GradedById = user.Id,
                    GradedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.AssignmentGrades.Add(grade);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Grade created for submission {SubmissionId} by {UserId}", dto.SubmissionId, user.Id);

                return CreatedAtAction(nameof(GetGrade), new { id = grade.Id }, new AssignmentGradeDto
                {
                    Id = grade.Id,
                    SubmissionId = grade.SubmissionId,
                    AssignmentId = assignment.Id,
                    AssignmentTitle = assignment.Title,
                    StudentId = submission.StudentId,
                    StudentName = submission.Student?.FullName ?? "",
                    Score = grade.Score,
                    Feedback = grade.Feedback,
                    GradedById = grade.GradedById,
                    GradedByName = user.FullName ?? user.UserName ?? "",
                    GradedAt = grade.GradedAt,
                    CreatedAt = grade.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading submission {SubmissionId}", dto.SubmissionId);
                return StatusCode(500, "An error occurred while grading submission");
            }
        }

        [HttpGet("grade/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AssignmentGradeDto>> GetGrade(long id)
        {
            try
            {
                var grade = await _context.AssignmentGrades
                    .Include(g => g.Submission!).ThenInclude(s => s.Assignment)
                    .Include(g => g.Submission).ThenInclude(s => s.Student)
                    .Include(g => g.GradedBy)
                    .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

                if (grade == null) return NotFound("Grade not found");

                var dto = new AssignmentGradeDto
                {
                    Id = grade.Id,
                    SubmissionId = grade.SubmissionId,
                    AssignmentId = grade.Submission?.AssignmentId ?? 0,
                    AssignmentTitle = grade.Submission?.Assignment?.Title ?? "",
                    StudentId = grade.Submission?.StudentId ?? "",
                    StudentName = grade.Submission?.Student?.FullName ?? "",
                    Score = grade.Score,
                    Feedback = grade.Feedback,
                    GradedById = grade.GradedById,
                    GradedByName = grade.GradedBy?.FullName ?? "",
                    GradedAt = grade.GradedAt,
                    CreatedAt = grade.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching grade {GradeId}", id);
                return StatusCode(500, "An error occurred while fetching grade");
            }
        }

        [HttpDelete("grade/{id}")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<IActionResult> DeleteGrade(long id)
        {
            try
            {
                var grade = await _context.AssignmentGrades.FindAsync(id);
                if (grade == null || grade.IsDeleted) return NotFound("Grade not found");

                grade.IsDeleted = true;
                grade.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Grade {GradeId} deleted", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting grade {GradeId}", id);
                return StatusCode(500, "An error occurred while deleting grade");
            }
        }
    }
}

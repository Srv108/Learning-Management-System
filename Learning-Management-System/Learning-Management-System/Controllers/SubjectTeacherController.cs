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
    public class SubjectTeacherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<SubjectTeacherController> _logger;

        public SubjectTeacherController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<SubjectTeacherController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all teachers for a subject
        /// </summary>
        [HttpGet("subject/{subjectId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SubjectTeacherDto>>> GetTeachersBySubject(long subjectId)
        {
            try
            {
                var subject = await _context.Subjects.FindAsync(subjectId);
                if (subject == null || subject.IsDeleted)
                {
                    return NotFound("Subject not found");
                }

                var subjectTeachers = await _context.SubjectTeachers
                    .Include(st => st.Subject)
                    .Include(st => st.Teacher)
                    .Where(st => st.SubjectId == subjectId && !st.IsDeleted)
                    .OrderBy(st => st.CreatedAt)
                    .ToListAsync();

                var teacherDtos = subjectTeachers.Select(st => new SubjectTeacherDto
                {
                    Id = st.Id,
                    SubjectId = st.SubjectId,
                    SubjectName = st.Subject?.Name ?? "",
                    TeacherId = st.TeacherId,
                    TeacherName = st.Teacher?.FullName ?? "",
                    TeacherEmail = st.Teacher?.Email ?? "",
                    AssignedAt = st.CreatedAt
                });

                return Ok(teacherDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching teachers for subject {SubjectId}", subjectId);
                return StatusCode(500, "An error occurred while fetching teachers");
            }
        }

        /// <summary>
        /// Get all subjects taught by a teacher
        /// </summary>
        [HttpGet("teacher/{teacherId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SubjectTeacherDto>>> GetSubjectsByTeacher(string teacherId)
        {
            try
            {
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null)
                {
                    return NotFound("Teacher not found");
                }

                var subjectTeachers = await _context.SubjectTeachers
                    .Include(st => st.Subject.Course)
                    .Include(st => st.Teacher)
                    .Where(st => st.TeacherId == teacherId && !st.IsDeleted)
                    .OrderBy(st => st.CreatedAt)
                    .ToListAsync();

                var subjectDtos = subjectTeachers.Select(st => new SubjectTeacherDto
                {
                    Id = st.Id,
                    SubjectId = st.SubjectId,
                    SubjectName = st.Subject?.Name ?? "",
                    TeacherId = st.TeacherId,
                    TeacherName = st.Teacher?.FullName ?? "",
                    TeacherEmail = st.Teacher?.Email ?? "",
                    AssignedAt = st.CreatedAt
                });

                return Ok(subjectDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subjects for teacher {TeacherId}", teacherId);
                return StatusCode(500, "An error occurred while fetching subjects");
            }
        }

        /// <summary>
        /// Assign a teacher to a subject
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<ActionResult<SubjectTeacherDto>> AssignTeacherToSubject(AssignTeacherToSubjectDto assignDto)
        {
            try
            {
                // Verify subject exists
                var subject = await _context.Subjects
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == assignDto.SubjectId && !s.IsDeleted);

                if (subject == null)
                {
                    return NotFound("Subject not found");
                }

                // Verify teacher exists and has Teacher role
                var teacher = await _userManager.FindByIdAsync(assignDto.TeacherId);
                if (teacher == null)
                {
                    return NotFound("Teacher not found");
                }

                var teacherRoles = await _userManager.GetRolesAsync(teacher);
                if (!teacherRoles.Contains("Teacher"))
                {
                    return BadRequest("User is not a teacher");
                }

                // Check if assignment already exists
                var existingAssignment = await _context.SubjectTeachers
                    .FirstOrDefaultAsync(st => st.SubjectId == assignDto.SubjectId && st.TeacherId == assignDto.TeacherId && !st.IsDeleted);

                if (existingAssignment != null)
                {
                    return BadRequest("Teacher is already assigned to this subject");
                }

                var subjectTeacher = new SubjectTeacher
                {
                    SubjectId = assignDto.SubjectId,
                    TeacherId = assignDto.TeacherId,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubjectTeachers.Add(subjectTeacher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Teacher {TeacherId} assigned to subject {SubjectId}", assignDto.TeacherId, assignDto.SubjectId);

                return CreatedAtAction(nameof(GetTeachersBySubject), new { subjectId = subjectTeacher.SubjectId }, new SubjectTeacherDto
                {
                    Id = subjectTeacher.Id,
                    SubjectId = subjectTeacher.SubjectId,
                    SubjectName = subject.Name,
                    TeacherId = subjectTeacher.TeacherId,
                    TeacherName = teacher.FullName,
                    TeacherEmail = teacher.Email ?? "",
                    AssignedAt = subjectTeacher.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning teacher to subject");
                return StatusCode(500, "An error occurred while assigning teacher to subject");
            }
        }

        /// <summary>
        /// Remove a teacher from a subject
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<IActionResult> RemoveTeacherFromSubject(long id)
        {
            try
            {
                var subjectTeacher = await _context.SubjectTeachers.FindAsync(id);
                if (subjectTeacher == null || subjectTeacher.IsDeleted)
                {
                    return NotFound("Assignment not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                subjectTeacher.IsDeleted = true;
                subjectTeacher.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Teacher {TeacherId} removed from subject {SubjectId} by {UserId}", subjectTeacher.TeacherId, subjectTeacher.SubjectId, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing teacher from subject {AssignmentId}", id);
                return StatusCode(500, "An error occurred while removing teacher from subject");
            }
        }

        /// <summary>
        /// Get assignment details
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<SubjectTeacherDto>> GetAssignment(long id)
        {
            try
            {
                var subjectTeacher = await _context.SubjectTeachers
                    .Include(st => st.Subject)
                    .Include(st => st.Teacher)
                    .FirstOrDefaultAsync(st => st.Id == id && !st.IsDeleted);

                if (subjectTeacher == null)
                {
                    return NotFound("Assignment not found");
                }

                var assignmentDto = new SubjectTeacherDto
                {
                    Id = subjectTeacher.Id,
                    SubjectId = subjectTeacher.SubjectId,
                    SubjectName = subjectTeacher.Subject?.Name ?? "",
                    TeacherId = subjectTeacher.TeacherId,
                    TeacherName = subjectTeacher.Teacher?.FullName ?? "",
                    TeacherEmail = subjectTeacher.Teacher?.Email ?? "",
                    AssignedAt = subjectTeacher.CreatedAt
                };

                return Ok(assignmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assignment {AssignmentId}", id);
                return StatusCode(500, "An error occurred while fetching the assignment");
            }
        }
    }
}
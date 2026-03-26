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
    public class SubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<SubjectController> _logger;

        public SubjectController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<SubjectController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all subjects for a course
        /// </summary>
        [HttpGet("course/{courseId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjectsByCourse(long courseId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null || course.IsDeleted)
                {
                    return NotFound("Course not found");
                }

                var subjects = await _context.Subjects
                    .Include(s => s.Course)
                    .Include(s => s.SubjectTeachers.Where(st => !st.IsDeleted))
                    .Where(s => s.CourseId == courseId && !s.IsDeleted)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                var subjectDtos = subjects.Select(s => new SubjectDto
                {
                    Id = s.Id,
                    CourseId = s.CourseId,
                    CourseName = s.Course?.Title ?? "",
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    TeacherCount = s.SubjectTeachers.Count
                });

                return Ok(subjectDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subjects for course {CourseId}", courseId);
                return StatusCode(500, "An error occurred while fetching subjects");
            }
        }

        /// <summary>
        /// Get subject by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<SubjectDto>> GetSubject(long id)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Course)
                    .Include(s => s.SubjectTeachers.Where(st => !st.IsDeleted))
                        .ThenInclude(st => st.Teacher)
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (subject == null)
                {
                    return NotFound("Subject not found");
                }

                var subjectDto = new SubjectDto
                {
                    Id = subject.Id,
                    CourseId = subject.CourseId,
                    CourseName = subject.Course?.Title ?? "",
                    Name = subject.Name,
                    Description = subject.Description,
                    CreatedAt = subject.CreatedAt,
                    TeacherCount = subject.SubjectTeachers.Count
                };

                return Ok(subjectDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subject {SubjectId}", id);
                return StatusCode(500, "An error occurred while fetching the subject");
            }
        }

        /// <summary>
        /// Create a new subject in a course
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<ActionResult<SubjectDto>> CreateSubject(CreateSubjectDto createSubjectDto)
        {
            try
            {
                var course = await _context.Courses.FindAsync(createSubjectDto.CourseId);
                if (course == null || course.IsDeleted)
                {
                    return NotFound("Course not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check authorization
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                if (course.CreatedById != currentUser.Id && !userRoles.Contains("Admin") && !userRoles.Contains("CourseCoordinator"))
                {
                    return Forbid("You do not have permission to create subjects in this course");
                }

                var subject = new Subject
                {
                    CourseId = createSubjectDto.CourseId,
                    Name = createSubjectDto.Name,
                    Description = createSubjectDto.Description,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subject created with ID {SubjectId} in course {CourseId}", subject.Id, course.Id);

                return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, new SubjectDto
                {
                    Id = subject.Id,
                    CourseId = subject.CourseId,
                    CourseName = course.Title,
                    Name = subject.Name,
                    Description = subject.Description,
                    CreatedAt = subject.CreatedAt,
                    TeacherCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subject");
                return StatusCode(500, "An error occurred while creating the subject");
            }
        }

        /// <summary>
        /// Update an existing subject
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> UpdateSubject(long id, UpdateSubjectDto updateSubjectDto)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (subject == null)
                {
                    return NotFound("Subject not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check authorization
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                if (subject.Course?.CreatedById != currentUser.Id && !userRoles.Contains("Admin") && !userRoles.Contains("CourseCoordinator"))
                {
                    return Forbid("You do not have permission to update this subject");
                }

                subject.Name = updateSubjectDto.Name;
                subject.Description = updateSubjectDto.Description;
                subject.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subject {SubjectId} updated by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subject {SubjectId}", id);
                return StatusCode(500, "An error occurred while updating the subject");
            }
        }

        /// <summary>
        /// Delete (soft delete) a subject
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator")]
        public async Task<IActionResult> DeleteSubject(long id)
        {
            try
            {
                var subject = await _context.Subjects.FindAsync(id);
                if (subject == null || subject.IsDeleted)
                {
                    return NotFound("Subject not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                subject.IsDeleted = true;
                subject.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subject {SubjectId} deleted by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subject {SubjectId}", id);
                return StatusCode(500, "An error occurred while deleting the subject");
            }
        }
    }
}
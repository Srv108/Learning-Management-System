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
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<CourseController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all active courses with pagination
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Subjects)
                    .Include(c => c.Batches)
                    .Where(c => !c.IsDeleted && c.Status == "ACTIVE")
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var courseDtos = courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Credits = c.Credits ?? 0,
                    CreatedById = c.CreatedById,
                    CreatedByName = c.CreatedBy?.FullName ?? c.CreatedBy?.UserName,
                    Status = c.Status ?? "ACTIVE",
                    CreatedAt = c.CreatedAt,
                    SubjectCount = c.Subjects.Count(s => !s.IsDeleted),
                    BatchCount = c.Batches.Count(b => !b.IsDeleted)
                });

                return Ok(courseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses");
                return StatusCode(500, "An error occurred while fetching courses");
            }
        }

        /// <summary>
        /// Get course by ID with all related data
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CourseDto>> GetCourse(long id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Subjects.Where(s => !s.IsDeleted))
                    .Include(c => c.Batches.Where(b => !b.IsDeleted))
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (course == null)
                {
                    return NotFound("Course not found");
                }

                var courseDto = new CourseDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Credits = course.Credits ?? 0,
                    CreatedById = course.CreatedById,
                    CreatedByName = course.CreatedBy?.FullName ?? course.CreatedBy?.UserName,
                    Status = course.Status ?? "ACTIVE",
                    CreatedAt = course.CreatedAt,
                    SubjectCount = course.Subjects.Count,
                    BatchCount = course.Batches.Count
                };

                return Ok(courseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching course {CourseId}", id);
                return StatusCode(500, "An error occurred while fetching the course");
            }
        }

        /// <summary>
        /// Create a new course (Teachers, Coordinators, Admins only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto createCourseDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                var course = new Course
                {
                    Title = createCourseDto.Title,
                    Description = createCourseDto.Description,
                    Credits = createCourseDto.Credits,
                    CreatedById = currentUser.Id,
                    Status = "ACTIVE",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Course created with ID {CourseId} by {UserId}", course.Id, currentUser.Id);

                return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, new CourseDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Credits = course.Credits ?? 0,
                    CreatedById = course.CreatedById,
                    CreatedByName = currentUser.FullName ?? currentUser.UserName,
                    Status = course.Status ?? "ACTIVE",
                    CreatedAt = course.CreatedAt,
                    SubjectCount = 0,
                    BatchCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, "An error occurred while creating the course");
            }
        }

        /// <summary>
        /// Update an existing course
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> UpdateCourse(long id, UpdateCourseDto updateCourseDto)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null || course.IsDeleted)
                {
                    return NotFound("Course not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                var userRoles = await _userManager.GetRolesAsync(currentUser);
                if (course.CreatedById != currentUser.Id && !userRoles.Contains("Admin") && !userRoles.Contains("CourseCoordinator"))
                {
                    return Forbid("You do not have permission to update this course");
                }

                course.Title = updateCourseDto.Title;
                course.Description = updateCourseDto.Description;
                course.Credits = updateCourseDto.Credits;
                course.Status = updateCourseDto.Status;
                course.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Course {CourseId} updated by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", id);
                return StatusCode(500, "An error occurred while updating the course");
            }
        }

        /// <summary>
        /// Delete (soft delete) a course
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator")]
        public async Task<IActionResult> DeleteCourse(long id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null || course.IsDeleted)
                {
                    return NotFound("Course not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                course.IsDeleted = true;
                course.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Course {CourseId} deleted by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                return StatusCode(500, "An error occurred while deleting the course");
            }
        }

        /// <summary>
        /// Get courses created by a specific teacher
        /// </summary>
        [HttpGet("by-teacher/{teacherId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesByTeacher(string teacherId)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Subjects)
                    .Include(c => c.Batches)
                    .Where(c => c.CreatedById == teacherId && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var courseDtos = courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Credits = c.Credits ?? 0,
                    CreatedById = c.CreatedById,
                    CreatedByName = c.CreatedBy?.FullName ?? c.CreatedBy?.UserName,
                    Status = c.Status ?? "ACTIVE",
                    CreatedAt = c.CreatedAt,
                    SubjectCount = c.Subjects.Count(s => !s.IsDeleted),
                    BatchCount = c.Batches.Count(b => !b.IsDeleted)
                });

                return Ok(courseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses for teacher {TeacherId}", teacherId);
                return StatusCode(500, "An error occurred while fetching courses");
            }
        }
    }
}
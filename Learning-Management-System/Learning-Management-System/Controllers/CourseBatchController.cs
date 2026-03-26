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
    public class CourseBatchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CourseBatchController> _logger;

        public CourseBatchController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<CourseBatchController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all batches for a course
        /// </summary>
        [HttpGet("course/{courseId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CourseBatchDto>>> GetBatchesByCourse(long courseId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null || course.IsDeleted)
                {
                    return NotFound("Course not found");
                }

                var batches = await _context.CourseBatches
                    .Include(b => b.Enrollments.Where(e => !e.IsDeleted))
                    .Where(b => b.CourseId == courseId && !b.IsDeleted)
                    .OrderBy(b => b.StartDate)
                    .ToListAsync();

                var batchDtos = batches.Select(b => new CourseBatchDto
                {
                    Id = b.Id,
                    CourseId = b.CourseId,
                    BatchName = b.BatchName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    CreatedAt = b.CreatedAt,
                    EnrollmentCount = b.Enrollments.Count
                });

                return Ok(batchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching batches for course {CourseId}", courseId);
                return StatusCode(500, "An error occurred while fetching batches");
            }
        }

        /// <summary>
        /// Get batch by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CourseBatchDto>> GetBatch(long id)
        {
            try
            {
                var batch = await _context.CourseBatches
                    .Include(b => b.Course)
                    .Include(b => b.Enrollments.Where(e => !e.IsDeleted))
                    .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

                if (batch == null)
                {
                    return NotFound("Batch not found");
                }

                var batchDto = new CourseBatchDto
                {
                    Id = batch.Id,
                    CourseId = batch.CourseId,
                    BatchName = batch.BatchName,
                    StartDate = batch.StartDate,
                    EndDate = batch.EndDate,
                    CreatedAt = batch.CreatedAt,
                    EnrollmentCount = batch.Enrollments.Count
                };

                return Ok(batchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching batch {BatchId}", id);
                return StatusCode(500, "An error occurred while fetching the batch");
            }
        }

        /// <summary>
        /// Get all enrollments in a batch
        /// </summary>
        [HttpGet("{id}/enrollments")]
        public async Task<ActionResult<IEnumerable<object>>> GetBatchEnrollments(long id)
        {
            try
            {
                var batch = await _context.CourseBatches
                    .Include(b => b.Enrollments.Where(e => !e.IsDeleted))
                        .ThenInclude(e => e.Student)
                    .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

                if (batch == null)
                {
                    return NotFound("Batch not found");
                }

                var enrollments = batch.Enrollments.Select(e => new
                {
                    e.Id,
                    e.StudentId,
                    StudentName = e.Student?.FullName ?? "",
                    StudentEmail = e.Student?.Email ?? "",
                    e.Status,
                    e.CreatedAt
                });

                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for batch {BatchId}", id);
                return StatusCode(500, "An error occurred while fetching enrollments");
            }
        }

        /// <summary>
        /// Create a new batch for a course
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<ActionResult<CourseBatchDto>> CreateBatch(CreateCourseBatchDto createBatchDto)
        {
            try
            {
                // Validate dates
                if (createBatchDto.EndDate <= createBatchDto.StartDate)
                {
                    return BadRequest("End date must be after start date");
                }

                var course = await _context.Courses.FindAsync(createBatchDto.CourseId);
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
                    return Forbid("You do not have permission to create batches for this course");
                }

                var batch = new CourseBatch
                {
                    CourseId = createBatchDto.CourseId,
                    BatchName = createBatchDto.BatchName,
                    StartDate = createBatchDto.StartDate,
                    EndDate = createBatchDto.EndDate,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CourseBatches.Add(batch);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch {BatchId} created for course {CourseId}", batch.Id, course.Id);

                return CreatedAtAction(nameof(GetBatch), new { id = batch.Id }, new CourseBatchDto
                {
                    Id = batch.Id,
                    CourseId = batch.CourseId,
                    BatchName = batch.BatchName,
                    StartDate = batch.StartDate,
                    EndDate = batch.EndDate,
                    CreatedAt = batch.CreatedAt,
                    EnrollmentCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch");
                return StatusCode(500, "An error occurred while creating the batch");
            }
        }

        /// <summary>
        /// Update an existing batch
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> UpdateBatch(long id, UpdateCourseBatchDto updateBatchDto)
        {
            try
            {
                var batch = await _context.CourseBatches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

                if (batch == null)
                {
                    return NotFound("Batch not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check authorization
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                if (batch.Course?.CreatedById != currentUser.Id && !userRoles.Contains("Admin") && !userRoles.Contains("CourseCoordinator"))
                {
                    return Forbid("You do not have permission to update this batch");
                }

                // Validate dates if both are provided
                if (updateBatchDto.StartDate.HasValue && updateBatchDto.EndDate.HasValue)
                {
                    if (updateBatchDto.EndDate <= updateBatchDto.StartDate)
                    {
                        return BadRequest("End date must be after start date");
                    }
                }

                if (!string.IsNullOrWhiteSpace(updateBatchDto.BatchName))
                {
                    batch.BatchName = updateBatchDto.BatchName;
                }

                if (updateBatchDto.StartDate.HasValue)
                {
                    batch.StartDate = updateBatchDto.StartDate.Value;
                }

                if (updateBatchDto.EndDate.HasValue)
                {
                    batch.EndDate = updateBatchDto.EndDate.Value;
                }

                batch.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch {BatchId} updated by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating batch {BatchId}", id);
                return StatusCode(500, "An error occurred while updating the batch");
            }
        }

        /// <summary>
        /// Delete (soft delete) a batch
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,CourseCoordinator")]
        public async Task<IActionResult> DeleteBatch(long id)
        {
            try
            {
                var batch = await _context.CourseBatches.FindAsync(id);
                if (batch == null || batch.IsDeleted)
                {
                    return NotFound("Batch not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                batch.IsDeleted = true;
                batch.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch {BatchId} deleted by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting batch {BatchId}", id);
                return StatusCode(500, "An error occurred while deleting the batch");
            }
        }
    }
}
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
    public class EnrollmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EnrollmentController> _logger;

        public EnrollmentController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<EnrollmentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all enrollments for a batch
        /// </summary>
        [HttpGet("batch/{batchId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByBatch(long batchId)
        {
            try
            {
                var batch = await _context.CourseBatches.FindAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    return NotFound("Batch not found");
                }

                var enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Batch)
                    .Where(e => e.BatchId == batchId && !e.IsDeleted)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();

                var enrollmentDtos = enrollments.Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "",
                    StudentEmail = e.Student?.Email ?? "",
                    BatchId = e.BatchId,
                    BatchName = e.Batch?.BatchName ?? "",
                    Status = e.Status,
                    EnrolledAt = e.EnrolledAt,
                    CreatedAt = e.CreatedAt
                });

                return Ok(enrollmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for batch {BatchId}", batchId);
                return StatusCode(500, "An error occurred while fetching enrollments");
            }
        }

        /// <summary>
        /// Get all enrollments for a student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByStudent(string studentId)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(studentId);
                if (student == null)
                {
                    return NotFound("Student not found");
                }

                var enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Batch.Course)
                    .Where(e => e.StudentId == studentId && !e.IsDeleted)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                var enrollmentDtos = enrollments.Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "",
                    StudentEmail = e.Student?.Email ?? "",
                    BatchId = e.BatchId,
                    BatchName = e.Batch?.BatchName ?? "",
                    Status = e.Status,
                    EnrolledAt = e.EnrolledAt,
                    CreatedAt = e.CreatedAt
                });

                return Ok(enrollmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for student {StudentId}", studentId);
                return StatusCode(500, "An error occurred while fetching enrollments");
            }
        }

        /// <summary>
        /// Get enrollment by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<EnrollmentDto>> GetEnrollment(long id)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Batch.Course)
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

                if (enrollment == null)
                {
                    return NotFound("Enrollment not found");
                }

                var enrollmentDto = new EnrollmentDto
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student?.FullName ?? "",
                    StudentEmail = enrollment.Student?.Email ?? "",
                    BatchId = enrollment.BatchId,
                    BatchName = enrollment.Batch?.BatchName ?? "",
                    Status = enrollment.Status,
                    EnrolledAt = enrollment.EnrolledAt,
                    CreatedAt = enrollment.CreatedAt
                };

                return Ok(enrollmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollment {EnrollmentId}", id);
                return StatusCode(500, "An error occurred while fetching the enrollment");
            }
        }

        /// <summary>
        /// Enroll a student in a batch
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<ActionResult<EnrollmentDto>> CreateEnrollment(CreateEnrollmentDto createEnrollmentDto)
        {
            try
            {
                // Verify student exists and has Student role
                var student = await _userManager.FindByIdAsync(createEnrollmentDto.StudentId);
                if (student == null)
                {
                    return NotFound("Student not found");
                }

                var studentRoles = await _userManager.GetRolesAsync(student);
                if (!studentRoles.Contains("Student"))
                {
                    return BadRequest("User is not a student");
                }

                // Verify batch exists
                var batch = await _context.CourseBatches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.Id == createEnrollmentDto.BatchId && !b.IsDeleted);

                if (batch == null)
                {
                    return NotFound("Batch not found");
                }

                // Check if enrollment already exists
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == createEnrollmentDto.StudentId && e.BatchId == createEnrollmentDto.BatchId && !e.IsDeleted);

                if (existingEnrollment != null)
                {
                    return BadRequest("Student is already enrolled in this batch");
                }

                var enrollment = new Enrollment
                {
                    StudentId = createEnrollmentDto.StudentId,
                    BatchId = createEnrollmentDto.BatchId,
                    Status = "ACTIVE",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} enrolled in batch {BatchId}", createEnrollmentDto.StudentId, createEnrollmentDto.BatchId);

                return CreatedAtAction(nameof(GetEnrollment), new { id = enrollment.Id }, new EnrollmentDto
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = student.FullName,
                    StudentEmail = student.Email ?? "",
                    BatchId = enrollment.BatchId,
                    BatchName = batch.BatchName,
                    Status = enrollment.Status,
                    EnrolledAt = enrollment.EnrolledAt,
                    CreatedAt = enrollment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enrollment");
                return StatusCode(500, "An error occurred while creating the enrollment");
            }
        }

        /// <summary>
        /// Update enrollment status
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<IActionResult> UpdateEnrollment(long id, UpdateEnrollmentDto updateEnrollmentDto)
        {
            try
            {
                var enrollment = await _context.Enrollments.FindAsync(id);
                if (enrollment == null || enrollment.IsDeleted)
                {
                    return NotFound("Enrollment not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                if (!string.IsNullOrWhiteSpace(updateEnrollmentDto.Status))
                {
                    enrollment.Status = updateEnrollmentDto.Status;
                }

                enrollment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Enrollment {EnrollmentId} updated by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enrollment {EnrollmentId}", id);
                return StatusCode(500, "An error occurred while updating the enrollment");
            }
        }

        /// <summary>
        /// Remove enrollment (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<IActionResult> DeleteEnrollment(long id)
        {
            try
            {
                var enrollment = await _context.Enrollments.FindAsync(id);
                if (enrollment == null || enrollment.IsDeleted)
                {
                    return NotFound("Enrollment not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                enrollment.IsDeleted = true;
                enrollment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Enrollment {EnrollmentId} deleted by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting enrollment {EnrollmentId}", id);
                return StatusCode(500, "An error occurred while deleting the enrollment");
            }
        }

        /// <summary>
        /// Student self-enroll in a batch
        /// </summary>
        [HttpPost("self-enroll")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<EnrollmentDto>> SelfEnroll([FromBody] SelfEnrollDto dto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized("User not found");

                var batch = await _context.CourseBatches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.Id == dto.BatchId && !b.IsDeleted);
                if (batch == null) return NotFound("Batch not found");

                var existing = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == currentUser.Id && e.BatchId == dto.BatchId && !e.IsDeleted);
                if (existing != null) return BadRequest("You are already enrolled in this batch");

                var enrollment = new Enrollment
                {
                    StudentId = currentUser.Id,
                    BatchId = dto.BatchId,
                    Status = "ACTIVE",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} self-enrolled in batch {BatchId}", currentUser.Id, dto.BatchId);

                return CreatedAtAction(nameof(GetEnrollment), new { id = enrollment.Id }, new EnrollmentDto
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = currentUser.FullName,
                    StudentEmail = currentUser.Email ?? "",
                    BatchId = enrollment.BatchId,
                    BatchName = batch.BatchName,
                    Status = enrollment.Status,
                    EnrolledAt = enrollment.EnrolledAt,
                    CreatedAt = enrollment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during self-enrollment");
                return StatusCode(500, "An error occurred while enrolling");
            }
        }
    }
}
using Learning_Management_System.Models;
using Learning_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Learning_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly ILogger<AssignmentController> _logger;

        public AssignmentController(IAssignmentService assignmentService, ILogger<AssignmentController> logger)
        {
            _assignmentService = assignmentService;
            _logger = logger;
        }

        /// <summary>
        /// Submit an assignment by a student
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAssignment([FromForm] SubmitAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                request.StudentId = userId;
                var result = await _assignmentService.SubmitAssignmentAsync(request);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitAssignment");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Grade a submitted assignment
        /// </summary>
        [HttpPost("grade")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> GradeAssignment([FromBody] GradeAssignmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data" });
                }

                var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(instructorId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _assignmentService.GradeAssignmentAsync(request, instructorId);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GradeAssignment");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get assignment status for a student
        /// </summary>
        [HttpGet("status/{assignmentId}")]
        public async Task<IActionResult> GetAssignmentStatus(int assignmentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _assignmentService.GetAssignmentStatusAsync(assignmentId, userId);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAssignmentStatus");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all assignments for current student
        /// </summary>
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments(int? subjectId = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _assignmentService.GetStudentAssignmentsAsync(userId, subjectId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyAssignments");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get assignments for grading (instructor only)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> GetPendingAssignments(int? subjectId = null)
        {
            try
            {
                var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(instructorId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _assignmentService.GetAssignmentsForGradingAsync(instructorId, subjectId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPendingAssignments");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get assignment by ID
        /// </summary>
        [HttpGet("{assignmentId}")]
        public async Task<IActionResult> GetAssignmentById(int assignmentId)
        {
            try
            {
                var result = await _assignmentService.GetAssignmentByIdAsync(assignmentId);

                if (result == null)
                {
                    return NotFound(new { success = false, message = "Assignment not found" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAssignmentById");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete an assignment (if not graded)
        /// </summary>
        [HttpDelete("{assignmentId}")]
        public async Task<IActionResult> DeleteAssignment(int assignmentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _assignmentService.DeleteAssignmentAsync(assignmentId, userId);

                if (!result)
                {
                    return BadRequest(new { success = false, message = "Cannot delete assignment (may already be graded)" });
                }

                return Ok(new { success = true, message = "Assignment deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAssignment");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}

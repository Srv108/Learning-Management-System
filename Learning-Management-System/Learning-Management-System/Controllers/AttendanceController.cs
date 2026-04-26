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
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<AttendanceController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all attendance sessions for a subject
        /// </summary>
        [HttpGet("subject/{subjectId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AttendanceSessionDto>>> GetSessionsBySubject(long subjectId)
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

                var sessions = await _context.AttendanceSessions
                    .Include(s => s.CreatedBy)
                    .Include(s => s.Records.Where(r => !r.IsDeleted))
                    .Where(s => s.SubjectId == subjectId && !s.IsDeleted)
                    .OrderByDescending(s => s.SessionDate)
                    .ToListAsync();

                var sessionDtos = sessions.Select(s => new AttendanceSessionDto
                {
                    Id = s.Id,
                    SubjectId = s.SubjectId,
                    SubjectName = subject.Name,
                    CourseName = subject.Course?.Title ?? "",
                    SessionDate = s.SessionDate,
                    CreatedById = s.CreatedById,
                    CreatedByName = s.CreatedBy?.FullName ?? "",
                    TotalRecords = s.Records.Count,
                    PresentCount = s.Records.Count(r => r.Status == "PRESENT"),
                    AbsentCount = s.Records.Count(r => r.Status == "ABSENT"),
                    CreatedAt = s.CreatedAt
                });

                return Ok(sessionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance sessions for subject {SubjectId}", subjectId);
                return StatusCode(500, "An error occurred while fetching attendance sessions");
            }
        }

        /// <summary>
        /// Get attendance session by ID
        /// </summary>
        [HttpGet("session/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AttendanceSessionDto>> GetSession(long id)
        {
            try
            {
                var session = await _context.AttendanceSessions
                    .Include(s => s.Subject.Course)
                    .Include(s => s.CreatedBy)
                    .Include(s => s.Records.Where(r => !r.IsDeleted))
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (session == null)
                {
                    return NotFound("Attendance session not found");
                }

                var sessionDto = new AttendanceSessionDto
                {
                    Id = session.Id,
                    SubjectId = session.SubjectId,
                    SubjectName = session.Subject?.Name ?? "",
                    CourseName = session.Subject?.Course?.Title ?? "",
                    SessionDate = session.SessionDate,
                    CreatedById = session.CreatedById,
                    CreatedByName = session.CreatedBy?.FullName ?? "",
                    TotalRecords = session.Records.Count,
                    PresentCount = session.Records.Count(r => r.Status == "PRESENT"),
                    AbsentCount = session.Records.Count(r => r.Status == "ABSENT"),
                    CreatedAt = session.CreatedAt
                };

                return Ok(sessionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance session {SessionId}", id);
                return StatusCode(500, "An error occurred while fetching the attendance session");
            }
        }

        /// <summary>
        /// Get attendance records for a session
        /// </summary>
        [HttpGet("session/{sessionId}/records")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetSessionRecords(long sessionId)
        {
            try
            {
                var session = await _context.AttendanceSessions.FindAsync(sessionId);
                if (session == null || session.IsDeleted)
                {
                    return NotFound("Attendance session not found");
                }

                var records = await _context.AttendanceRecords
                    .Include(r => r.Student)
                    .Where(r => r.SessionId == sessionId && !r.IsDeleted)
                    .OrderBy(r => r.Student != null ? r.Student.FullName : "")
                    .ToListAsync();

                var recordDtos = records.Select(r => new AttendanceRecordDto
                {
                    Id = r.Id,
                    SessionId = r.SessionId,
                    StudentId = r.StudentId,
                    StudentName = r.Student?.FullName ?? "",
                    StudentEmail = r.Student?.Email ?? "",
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });

                return Ok(recordDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance records for session {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while fetching attendance records");
            }
        }

        /// <summary>
        /// Get student attendance summary for a subject
        /// </summary>
        [HttpGet("student/{studentId}/subject/{subjectId}")]
        [AllowAnonymous]
        public async Task<ActionResult<StudentAttendanceSummaryDto>> GetStudentAttendanceSummary(string studentId, long subjectId)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(studentId);
                if (student == null)
                {
                    return NotFound("Student not found");
                }

                var subject = await _context.Subjects.FindAsync(subjectId);
                if (subject == null || subject.IsDeleted)
                {
                    return NotFound("Subject not found");
                }

                var sessions = await _context.AttendanceSessions
                    .Where(s => s.SubjectId == subjectId && !s.IsDeleted)
                    .Select(s => s.Id)
                    .ToListAsync();

                var records = await _context.AttendanceRecords
                    .Where(r => sessions.Contains(r.SessionId) && r.StudentId == studentId && !r.IsDeleted)
                    .ToListAsync();

                var totalSessions = sessions.Count;
                var presentCount = records.Count(r => r.Status == "PRESENT");
                var absentCount = records.Count(r => r.Status == "ABSENT");
                var attendancePercentage = totalSessions > 0 ? (double)presentCount / totalSessions * 100 : 0;

                var summary = new StudentAttendanceSummaryDto
                {
                    StudentId = studentId,
                    StudentName = student.FullName,
                    StudentEmail = student.Email ?? "",
                    SubjectId = subjectId,
                    SubjectName = subject.Name,
                    TotalSessions = totalSessions,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    AttendancePercentage = Math.Round(attendancePercentage, 2)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance summary for student {StudentId} in subject {SubjectId}", studentId, subjectId);
                return StatusCode(500, "An error occurred while fetching attendance summary");
            }
        }

        /// <summary>
        /// Create a new attendance session
        /// </summary>
        [HttpPost("session")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<ActionResult<AttendanceSessionDto>> CreateAttendanceSession(CreateAttendanceSessionDto createSessionDto)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Course)
                    .FirstOrDefaultAsync(s => s.Id == createSessionDto.SubjectId && !s.IsDeleted);

                if (subject == null)
                {
                    return NotFound("Subject not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check if session already exists for this date and subject
                var existingSession = await _context.AttendanceSessions
                    .FirstOrDefaultAsync(s => s.SubjectId == createSessionDto.SubjectId &&
                                             s.SessionDate.Date == createSessionDto.SessionDate.Date &&
                                             !s.IsDeleted);

                if (existingSession != null)
                {
                    return BadRequest("An attendance session already exists for this subject on this date");
                }

                var session = new AttendanceSession
                {
                    SubjectId = createSessionDto.SubjectId,
                    SessionDate = createSessionDto.SessionDate,
                    CreatedById = currentUser.Id,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AttendanceSessions.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attendance session created with ID {SessionId} for subject {SubjectId}", session.Id, subject.Id);

                return CreatedAtAction(nameof(GetSession), new { id = session.Id }, new AttendanceSessionDto
                {
                    Id = session.Id,
                    SubjectId = session.SubjectId,
                    SubjectName = subject.Name,
                    CourseName = subject.Course?.Title ?? "",
                    SessionDate = session.SessionDate,
                    CreatedById = session.CreatedById,
                    CreatedByName = currentUser.FullName ?? "",
                    TotalRecords = 0,
                    PresentCount = 0,
                    AbsentCount = 0,
                    CreatedAt = session.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendance session");
                return StatusCode(500, "An error occurred while creating the attendance session");
            }
        }

        /// <summary>
        /// Update attendance session
        /// </summary>
        [HttpPut("session/{id}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> UpdateAttendanceSession(long id, UpdateAttendanceSessionDto updateSessionDto)
        {
            try
            {
                var session = await _context.AttendanceSessions
                    .Include(s => s.Subject)
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (session == null)
                {
                    return NotFound("Attendance session not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check authorization
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAuthorized = userRoles.Contains("Admin") || userRoles.Contains("CourseCoordinator");

                if (!isAuthorized)
                {
                    var teacherAssignment = await _context.SubjectTeachers
                        .FirstOrDefaultAsync(st => st.SubjectId == session.SubjectId && st.TeacherId == currentUser.Id && !st.IsDeleted);

                    if (teacherAssignment == null)
                    {
                        return Forbid("You are not authorized to update this attendance session");
                    }
                }

                session.SessionDate = updateSessionDto.SessionDate;
                session.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Attendance session {SessionId} updated by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance session {SessionId}", id);
                return StatusCode(500, "An error occurred while updating the attendance session");
            }
        }

        /// <summary>
        /// Mark attendance for a single student
        /// </summary>
        [HttpPost("mark")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> MarkAttendance(MarkAttendanceDto markAttendanceDto)
        {
            try
            {
                var session = await _context.AttendanceSessions.FindAsync(markAttendanceDto.SessionId);
                if (session == null || session.IsDeleted)
                {
                    return NotFound("Attendance session not found");
                }

                var student = await _userManager.FindByIdAsync(markAttendanceDto.StudentId);
                if (student == null)
                {
                    return NotFound("Student not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check authorization
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAuthorized = userRoles.Contains("Admin") || userRoles.Contains("CourseCoordinator");

                if (!isAuthorized)
                {
                    var teacherAssignment = await _context.SubjectTeachers
                        .FirstOrDefaultAsync(st => st.SubjectId == session.SubjectId && st.TeacherId == currentUser.Id && !st.IsDeleted);

                    if (teacherAssignment == null)
                    {
                        return Forbid("You are not authorized to mark attendance for this session");
                    }
                }

                // Check if record already exists
                var existingRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(r => r.SessionId == markAttendanceDto.SessionId && r.StudentId == markAttendanceDto.StudentId && !r.IsDeleted);

                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.Status = markAttendanceDto.Status;
                    existingRecord.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new record
                    var record = new AttendanceRecord
                    {
                        SessionId = markAttendanceDto.SessionId,
                        StudentId = markAttendanceDto.StudentId,
                        Status = markAttendanceDto.Status,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AttendanceRecords.Add(record);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Attendance marked for student {StudentId} in session {SessionId}", markAttendanceDto.StudentId, markAttendanceDto.SessionId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance");
                return StatusCode(500, "An error occurred while marking attendance");
            }
        }

        /// <summary>
        /// Bulk mark attendance for multiple students
        /// </summary>
        [HttpPost("bulk-mark")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> BulkMarkAttendance(BulkMarkAttendanceDto bulkMarkDto)
        {
            try
            {
                var session = await _context.AttendanceSessions.FindAsync(bulkMarkDto.SessionId);
                if (session == null || session.IsDeleted)
                {
                    return NotFound("Attendance session not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                // Check authorization
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAuthorized = userRoles.Contains("Admin") || userRoles.Contains("CourseCoordinator");

                if (!isAuthorized)
                {
                    var teacherAssignment = await _context.SubjectTeachers
                        .FirstOrDefaultAsync(st => st.SubjectId == session.SubjectId && st.TeacherId == currentUser.Id && !st.IsDeleted);

                    if (teacherAssignment == null)
                    {
                        return Forbid("You are not authorized to mark attendance for this session");
                    }
                }

                foreach (var recordDto in bulkMarkDto.Records)
                {
                    var student = await _userManager.FindByIdAsync(recordDto.StudentId);
                    if (student == null)
                    {
                        continue; // Skip invalid students
                    }

                    // Check if record already exists
                    var existingRecord = await _context.AttendanceRecords
                        .FirstOrDefaultAsync(r => r.SessionId == bulkMarkDto.SessionId && r.StudentId == recordDto.StudentId && !r.IsDeleted);

                    if (existingRecord != null)
                    {
                        // Update existing record
                        existingRecord.Status = recordDto.Status;
                        existingRecord.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new record
                        var record = new AttendanceRecord
                        {
                            SessionId = bulkMarkDto.SessionId,
                            StudentId = recordDto.StudentId,
                            Status = recordDto.Status,
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.AttendanceRecords.Add(record);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk attendance marked for session {SessionId} by {UserId}", bulkMarkDto.SessionId, currentUser.Id);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk marking attendance");
                return StatusCode(500, "An error occurred while bulk marking attendance");
            }
        }

        /// <summary>
        /// Delete attendance session (soft delete)
        /// </summary>
        [HttpDelete("session/{id}")]
        [Authorize(Roles = "CourseCoordinator,Admin")]
        public async Task<IActionResult> DeleteAttendanceSession(long id)
        {
            try
            {
                var session = await _context.AttendanceSessions.FindAsync(id);
                if (session == null || session.IsDeleted)
                {
                    return NotFound("Attendance session not found");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User not found");
                }

                session.IsDeleted = true;
                session.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Attendance session {SessionId} deleted by {UserId}", id, currentUser.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance session {SessionId}", id);
                return StatusCode(500, "An error occurred while deleting the attendance session");
            }
        }
    }
}
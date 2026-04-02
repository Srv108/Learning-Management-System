using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Services
{
    public interface IEnrollmentService
    {
        Task<EnrollmentDto> EnrollStudentAsync(EnrollStudentDto enrollDto, string userId);
        Task<EnrollmentDto> UpdateEnrollmentAsync(long enrollmentId, UpdateEnrollmentDto enrollDto);
        Task<IEnumerable<AttendanceRecordDto>> GetAttendanceAsync(string studentId, long? subjectId = null);
        Task<IEnumerable<EnrollmentDto>> GetStudentEnrollmentsAsync(string studentId);
        Task<IEnumerable<EnrollmentDto>> GetBatchEnrollmentsAsync(long batchId);
        Task<bool> DropEnrollmentAsync(long enrollmentId);
        Task<AttendanceRecordDto> RecordAttendanceAsync(RecordAttendanceDto attendanceDto, string createdById);
        Task<IEnumerable<AttendanceRecordDto>> GetSessionAttendanceAsync(long sessionId);
    }

    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<EnrollmentService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }



        public async Task<EnrollmentDto> EnrollStudentAsync(EnrollStudentDto enrollDto, string userId)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(enrollDto.StudentId);
                if (student == null)
                {
                    throw new InvalidOperationException("Student not found");
                }

                var batch = await _context.CourseBatches.FindAsync(enrollDto.BatchId);
                if (batch == null || batch.IsDeleted)
                {
                    throw new InvalidOperationException("Batch not found");
                }

                
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == enrollDto.StudentId && e.BatchId == enrollDto.BatchId && !e.IsDeleted);

                if (existingEnrollment != null)
                {
                    throw new InvalidOperationException("Student is already enrolled in this batch");
                }

                var enrollment = new Enrollment
                {
                    StudentId = enrollDto.StudentId,
                    BatchId = enrollDto.BatchId,
                    Status = "ACTIVE",
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                var course = await _context.CourseBatches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.Id == enrollDto.BatchId);

                return new EnrollmentDto
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = student.FullName ?? student.UserName ?? "",
                    StudentEmail = student.Email ?? "",
                    BatchId = enrollment.BatchId,
                    BatchName = batch.BatchName,
                    Status = enrollment.Status,
                    EnrolledAt = enrollment.EnrolledAt,
                    CreatedAt = enrollment.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId}", enrollDto.StudentId);
                throw;
            }
        }



        public async Task<EnrollmentDto> UpdateEnrollmentAsync(long enrollmentId, UpdateEnrollmentDto enrollDto)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Batch)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId && !e.IsDeleted);

                if (enrollment == null)
                {
                    throw new InvalidOperationException("Enrollment not found");
                }

                enrollment.Status = enrollDto.Status ?? enrollment.Status;
                enrollment.UpdatedAt = DateTime.UtcNow;

                _context.Enrollments.Update(enrollment);
                await _context.SaveChangesAsync();

                return new EnrollmentDto
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student?.FullName ?? enrollment.Student?.UserName ?? "",
                    StudentEmail = enrollment.Student?.Email ?? "",
                    BatchId = enrollment.BatchId,
                    BatchName = enrollment.Batch?.BatchName ?? "",
                    Status = enrollment.Status,
                    EnrolledAt = enrollment.EnrolledAt,
                    CreatedAt = enrollment.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enrollment {EnrollmentId}", enrollmentId);
                throw;
            }
        }



        public async Task<IEnumerable<AttendanceRecordDto>> GetAttendanceAsync(string studentId, long? subjectId = null)
        {
            try
            {
                var query = _context.AttendanceRecords
                    .Include(r => r.Session.Subject)
                    .Where(r => r.StudentId == studentId && !r.IsDeleted);

                if (subjectId.HasValue)
                {
                    query = query.Where(r => r.Session.SubjectId == subjectId);
                }

                var records = await query
                    .OrderByDescending(r => r.Session.SessionDate)
                    .ToListAsync();

                return records.Select(r => new AttendanceRecordDto
                {
                    Id = r.Id,
                    SessionId = r.SessionId,
                    StudentId = r.StudentId,
                    SubjectId = r.Session.SubjectId,
                    SubjectName = r.Session.Subject?.Name ?? "",
                    SessionDate = r.Session.SessionDate,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance for student {StudentId}", studentId);
                throw;
            }
        }



        public async Task<IEnumerable<EnrollmentDto>> GetStudentEnrollmentsAsync(string studentId)
        {
            try
            {
                var enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Batch.Course)
                    .Where(e => e.StudentId == studentId && !e.IsDeleted)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                return enrollments.Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? e.Student?.UserName ?? "",
                    StudentEmail = e.Student?.Email ?? "",
                    BatchId = e.BatchId,
                    BatchName = e.Batch?.BatchName ?? "",
                    Status = e.Status,
                    EnrolledAt = e.EnrolledAt,
                    CreatedAt = e.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for student {StudentId}", studentId);
                throw;
            }
        }



        public async Task<IEnumerable<EnrollmentDto>> GetBatchEnrollmentsAsync(long batchId)
        {
            try
            {
                var batch = await _context.CourseBatches.FindAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    throw new InvalidOperationException("Batch not found");
                }

                var enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Batch)
                    .Where(e => e.BatchId == batchId && !e.IsDeleted)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();

                return enrollments.Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? e.Student?.UserName ?? "",
                    StudentEmail = e.Student?.Email ?? "",
                    BatchId = e.BatchId,
                    BatchName = e.Batch?.BatchName ?? "",
                    Status = e.Status,
                    EnrolledAt = e.EnrolledAt,
                    CreatedAt = e.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for batch {BatchId}", batchId);
                throw;
            }
        }



        public async Task<bool> DropEnrollmentAsync(long enrollmentId)
        {
            try
            {
                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId && !e.IsDeleted);

                if (enrollment == null)
                {
                    return false;
                }

                enrollment.Status = "DROPPED";
                enrollment.IsDeleted = true;
                enrollment.UpdatedAt = DateTime.UtcNow;

                _context.Enrollments.Update(enrollment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping enrollment {EnrollmentId}", enrollmentId);
                throw;
            }
        }



        public async Task<AttendanceRecordDto> RecordAttendanceAsync(RecordAttendanceDto attendanceDto, string createdById)
        {
            try
            {
                var session = await _context.AttendanceSessions.FindAsync(attendanceDto.SessionId);
                if (session == null || session.IsDeleted)
                {
                    throw new InvalidOperationException("Attendance session not found");
                }

                var student = await _userManager.FindByIdAsync(attendanceDto.StudentId);
                if (student == null)
                {
                    throw new InvalidOperationException("Student not found");
                }

                
                var existingRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(r => r.SessionId == attendanceDto.SessionId && r.StudentId == attendanceDto.StudentId && !r.IsDeleted);

                if (existingRecord != null)
                {
                    existingRecord.Status = attendanceDto.Status;
                    existingRecord.UpdatedAt = DateTime.UtcNow;
                    _context.AttendanceRecords.Update(existingRecord);
                }
                else
                {
                    var record = new AttendanceRecord
                    {
                        SessionId = attendanceDto.SessionId,
                        StudentId = attendanceDto.StudentId,
                        Status = attendanceDto.Status,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AttendanceRecords.Add(record);
                }

                await _context.SaveChangesAsync();

                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == session.SubjectId);

                return new AttendanceRecordDto
                {
                    Id = existingRecord?.Id ?? 0,
                    SessionId = session.Id,
                    StudentId = attendanceDto.StudentId,
                    SubjectId = session.SubjectId,
                    SubjectName = subject?.Name ?? "",
                    SessionDate = session.SessionDate,
                    Status = attendanceDto.Status,
                    CreatedAt = existingRecord?.CreatedAt ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording attendance for student {StudentId}", attendanceDto.StudentId);
                throw;
            }
        }



        public async Task<IEnumerable<AttendanceRecordDto>> GetSessionAttendanceAsync(long sessionId)
        {
            try
            {
                var session = await _context.AttendanceSessions
                    .Include(s => s.Subject)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted);

                if (session == null)
                {
                    throw new InvalidOperationException("Session not found");
                }

                var records = await _context.AttendanceRecords
                    .Include(r => r.Student)
                    .Where(r => r.SessionId == sessionId && !r.IsDeleted)
                    .OrderBy(r => r.Student.FullName)
                    .ToListAsync();

                return records.Select(r => new AttendanceRecordDto
                {
                    Id = r.Id,
                    SessionId = r.SessionId,
                    StudentId = r.StudentId,
                    SubjectId = session.SubjectId,
                    SubjectName = session.Subject?.Name ?? "",
                    SessionDate = session.SessionDate,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attendance for session {SessionId}", sessionId);
                throw;
            }
        }
    }
}


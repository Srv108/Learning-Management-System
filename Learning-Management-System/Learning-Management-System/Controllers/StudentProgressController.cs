using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Learning_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentProgressController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public StudentProgressController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get a student's progress in a specific course
        /// </summary>
        [HttpGet("course/{courseId}/student/{studentId}")]
        public async Task<IActionResult> GetStudentCourseProgress(long courseId, string studentId)
        {
            try
            {
                var progress = await _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Where(p => p.CourseId == courseId && p.StudentId == studentId)
                    .FirstOrDefaultAsync();

                if (progress == null)
                {
                    return NotFound(new { message = "No progress record found for this student in the course" });
                }

                return Ok(new
                {
                    id = progress.Id,
                    studentId = progress.StudentId,
                    courseId = progress.CourseId,
                    courseName = progress.Course?.Title,
                    attendancePercentage = progress.AttendancePercentage ?? 0,
                    assignmentAvgScore = progress.AssignmentAvgScore ?? 0,
                    examAvgScore = progress.ExamAvgScore ?? 0,
                    overallGrade = progress.OverallGrade,
                    lastUpdated = progress.LastUpdated,
                    gradeDistribution = new
                    {
                        attendance_weight = 0.20,
                        assignment_weight = 0.30,
                        exam_weight = 0.50
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get a student's progress across all enrolled courses
        /// </summary>
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentAllCoursesProgress(string studentId)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(studentId);
                if (student == null)
                {
                    return NotFound(new { message = "Student not found" });
                }

                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Where(p => p.StudentId == studentId && !p.IsDeleted)
                    .OrderByDescending(p => p.LastUpdated)
                    .ToListAsync();

                if (!progressList.Any())
                {
                    return Ok(new { message = "No progress records found", data = new List<object>() });
                }

                var data = progressList.Select(p => new
                {
                    id = p.Id,
                    courseName = p.Course?.Title,
                    courseId = p.CourseId,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    lastUpdated = p.LastUpdated,
                    status = GetProgressStatus(p.ExamAvgScore ?? 0, p.OverallGrade)
                }).ToList();

                // Calculate overall statistics
                var overallStats = new
                {
                    totalCourses = progressList.Count,
                    averageAttendance = Math.Round(progressList.Average(p => p.AttendancePercentage ?? 0), 2),
                    averageAssignmentScore = Math.Round(progressList.Average(p => p.AssignmentAvgScore ?? 0), 2),
                    averageExamScore = Math.Round(progressList.Average(p => p.ExamAvgScore ?? 0), 2),
                    gradeDistribution = GetGradeDistribution(progressList)
                };

                return Ok(new { studentName = student.FullName, overall = overallStats, courses = data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all students' progress in a specific course (Course Analytics)
        /// </summary>
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> GetCourseStudentsProgress(long courseId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                var totalStudents = await _context.StudentCourseProgress
                    .Where(p => p.CourseId == courseId && !p.IsDeleted)
                    .CountAsync();

                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Student)
                    .Where(p => p.CourseId == courseId && !p.IsDeleted)
                    .OrderByDescending(p => p.ExamAvgScore)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var students = progressList.Select(p => new
                {
                    id = p.Id,
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName,
                    email = p.Student?.Email,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    lastUpdated = p.LastUpdated,
                    status = GetProgressStatus(p.ExamAvgScore ?? 0, p.OverallGrade)
                }).ToList();

                var courseAnalytics = new
                {
                    courseName = course.Title,
                    courseId = courseId,
                    totalStudents = totalStudents,
                    averageAttendance = Math.Round(await _context.StudentCourseProgress
                        .Where(p => p.CourseId == courseId && !p.IsDeleted)
                        .AverageAsync(p => p.AttendancePercentage ?? 0), 2),
                    averageAssignmentScore = Math.Round(await _context.StudentCourseProgress
                        .Where(p => p.CourseId == courseId && !p.IsDeleted)
                        .AverageAsync(p => p.AssignmentAvgScore ?? 0), 2),
                    averageExamScore = Math.Round(await _context.StudentCourseProgress
                        .Where(p => p.CourseId == courseId && !p.IsDeleted)
                        .AverageAsync(p => p.ExamAvgScore ?? 0), 2),
                    passRate = GetCoursePassRate(courseId),
                    pagination = new { pageNumber, pageSize, totalStudents, totalPages = (int)Math.Ceiling(totalStudents / (double)pageSize) }
                };

                return Ok(new { analytics = courseAnalytics, students });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get batch-level analytics (all students in a course batch)
        /// </summary>
        [HttpGet("batch/{batchId}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> GetBatchAnalytics(long batchId)
        {
            try
            {
                var batch = await _context.CourseBatches
                    .Include(b => b.Course)
                    .FirstOrDefaultAsync(b => b.Id == batchId);

                if (batch == null)
                {
                    return NotFound(new { message = "Batch not found" });
                }

                // Get all students enrolled in this batch
                var enrollments = await _context.Enrollments
                    .Where(e => e.BatchId == batchId && !e.IsDeleted)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Student)
                    .Where(p => p.CourseId == batch.CourseId && enrollments.Contains(p.StudentId) && !p.IsDeleted)
                    .OrderByDescending(p => p.ExamAvgScore)
                    .ToListAsync();

                var batchStudents = progressList.Select(p => new
                {
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade
                }).ToList();

                var batchAnalytics = new
                {
                    batchName = batch.BatchName,
                    batchId = batchId,
                    courseId = batch.CourseId,
                    courseName = batch.Course?.Title,
                    startDate = batch.StartDate,
                    endDate = batch.EndDate,
                    totalStudentsEnrolled = enrollments.Count,
                    studentsWithProgress = progressList.Count,
                    averageAttendance = progressList.Any() ? Math.Round(progressList.Average(p => p.AttendancePercentage ?? 0), 2) : 0,
                    averageAssignmentScore = progressList.Any() ? Math.Round(progressList.Average(p => p.AssignmentAvgScore ?? 0), 2) : 0,
                    averageExamScore = progressList.Any() ? Math.Round(progressList.Average(p => p.ExamAvgScore ?? 0), 2) : 0,
                    gradeDistribution = GetGradeDistribution(progressList),
                    topPerformers = batchStudents.OrderByDescending(s => s.examAvgScore).Take(5).ToList(),
                    needsAttention = batchStudents.Where(s => s.examAvgScore < 50 || s.attendancePercentage < 75).Take(5).ToList()
                };

                return Ok(batchAnalytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance analytics with filtering and sorting
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> GetAnalytics(
            [FromQuery] long? courseId = null,
            [FromQuery] string? gradeFilter = null,
            [FromQuery] string sortBy = "exam_score",
            [FromQuery] string order = "desc",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Include(p => p.Student)
                    .Where(p => !p.IsDeleted);

                if (courseId.HasValue)
                {
                    query = query.Where(p => p.CourseId == courseId.Value);
                }

                if (!string.IsNullOrEmpty(gradeFilter))
                {
                    query = query.Where(p => p.OverallGrade == gradeFilter);
                }

                // Sorting
                query = sortBy.ToLower() switch
                {
                    "exam_score" => order == "desc" ? query.OrderByDescending(p => p.ExamAvgScore) : query.OrderBy(p => p.ExamAvgScore),
                    "assignment_score" => order == "desc" ? query.OrderByDescending(p => p.AssignmentAvgScore) : query.OrderBy(p => p.AssignmentAvgScore),
                    "attendance" => order == "desc" ? query.OrderByDescending(p => p.AttendancePercentage) : query.OrderBy(p => p.AttendancePercentage),
                    "name" => order == "desc" ? query.OrderByDescending(p => p.Student!.FullName) : query.OrderBy(p => p.Student!.FullName),
                    _ => query.OrderByDescending(p => p.ExamAvgScore)
                };

                var totalRecords = await query.CountAsync();

                var progressList = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var data = progressList.Select(p => new
                {
                    id = p.Id,
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName,
                    email = p.Student?.Email,
                    courseId = p.CourseId,
                    courseName = p.Course?.Title,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    lastUpdated = p.LastUpdated,
                    performanceLevel = GetPerformanceLevel(p.ExamAvgScore ?? 0)
                }).ToList();

                return Ok(new
                {
                    data,
                    pagination = new
                    {
                        pageNumber,
                        pageSize,
                        totalRecords,
                        totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get students who need attention (low attendance or failing grades)
        /// </summary>
        [HttpGet("at-risk")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> GetAtRiskStudents([FromQuery] long? courseId = null)
        {
            try
            {
                var query = _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Include(p => p.Student)
                    .Where(p => !p.IsDeleted && (p.ExamAvgScore < 50 || p.AttendancePercentage < 75));

                if (courseId.HasValue)
                {
                    query = query.Where(p => p.CourseId == courseId.Value);
                }

                var atRiskStudents = await query
                    .OrderBy(p => p.ExamAvgScore)
                    .ToListAsync();

                var data = atRiskStudents.Select(p => new
                {
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName,
                    email = p.Student?.Email,
                    courseName = p.Course?.Title,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    riskFactors = GetRiskFactors(p)
                }).ToList();

                return Ok(new { totalAtRisk = atRiskStudents.Count, students = data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance trends over time (if historical data available)
        /// </summary>
        [HttpGet("trends/{studentId}")]
        public async Task<IActionResult> GetStudentPerformanceTrends(string studentId, [FromQuery] long? courseId = null)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(studentId);
                if (student == null)
                {
                    return NotFound(new { message = "Student not found" });
                }

                var query = _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Where(p => p.StudentId == studentId && !p.IsDeleted);

                if (courseId.HasValue)
                {
                    query = query.Where(p => p.CourseId == courseId.Value);
                }

                var progressData = await query
                    .OrderBy(p => p.LastUpdated)
                    .ToListAsync();

                var trends = progressData.Select(p => new
                {
                    courseName = p.Course?.Title,
                    courseId = p.CourseId,
                    timestamp = p.LastUpdated,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade
                }).ToList();

                return Ok(new { studentName = student.FullName, trends });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Recalculate a student's course progress manually
        /// </summary>
        [HttpPost("recalculate/{studentId}/{courseId}")]
        [Authorize(Roles = "Admin,CourseCoordinator")]
        public async Task<IActionResult> RecalculateStudentProgress(string studentId, long courseId)
        {
            try
            {
                var progress = await _context.StudentCourseProgress
                    .FirstOrDefaultAsync(p => p.StudentId == studentId && p.CourseId == courseId);

                if (progress == null)
                {
                    return NotFound(new { message = "Progress record not found" });
                }

                // Recalculate attendance percentage
                var attendanceSessions = await _context.AttendanceSessions
                    .Include(s => s.Subject)
                    .Where(s => s.Subject.CourseId == courseId)
                    .ToListAsync();

                if (attendanceSessions.Any())
                {
                    var attendanceRecords = await _context.AttendanceRecords
                        .Where(r => r.StudentId == studentId && attendanceSessions.Contains(r.Session))
                        .CountAsync();

                    var presentCount = await _context.AttendanceRecords
                        .Where(r => r.StudentId == studentId && attendanceSessions.Contains(r.Session) && r.Status == "PRESENT")
                        .CountAsync();

                    progress.AttendancePercentage = attendanceSessions.Count > 0 
                        ? (decimal)(presentCount * 100 / attendanceSessions.Count) 
                        : 0;
                }

                // Recalculate assignment scores
                var assignments = await _context.Assignments
                    .Include(a => a.Subject)
                    .Where(a => a.Subject.CourseId == courseId)
                    .ToListAsync();

                if (assignments.Any())
                {
                    var submissionScores = await _context.Submissions
                        .Include(s => s.Grade)
                        .Where(s => s.StudentId == studentId && assignments.Contains(s.Assignment))
                        .Select(s => s.Grade != null ? s.Grade.Score : 0)
                        .ToListAsync();

                    progress.AssignmentAvgScore = submissionScores.Any() 
                        ? (decimal)submissionScores.Average() 
                        : 0;
                }

                // Recalculate exam scores
                var exams = await _context.Exams
                    .Include(e => e.Subject)
                    .Where(e => e.Subject.CourseId == courseId)
                    .ToListAsync();

                if (exams.Any())
                {
                    var examScores = await _context.ExamResults
                        .Where(er => er.StudentId == studentId && exams.Contains(er.Exam))
                        .Select(er => er.Marks ?? 0)
                        .ToListAsync();

                    progress.ExamAvgScore = examScores.Any() 
                        ? (decimal)(examScores.Average() / 100 * 100) 
                        : 0;
                }

                // Calculate overall grade based on weighted formula
                // Weights: Attendance 20%, Assignment 30%, Exam 50%
                var overallScore = (progress.AttendancePercentage * 0.2m) +
                                    (progress.AssignmentAvgScore * 0.3m) +
                                    (progress.ExamAvgScore * 0.5m);

                progress.OverallGrade = overallScore switch
                {
                    >= 90 => "A+",
                    >= 80 => "A",
                    >= 70 => "B",
                    >= 60 => "C",
                    >= 50 => "D",
                    _ => "F"
                };

                progress.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Progress recalculated successfully",
                    data = new
                    {
                        attendancePercentage = progress.AttendancePercentage,
                        assignmentAvgScore = progress.AssignmentAvgScore,
                        examAvgScore = progress.ExamAvgScore,
                        overallGrade = progress.OverallGrade,
                        lastUpdated = progress.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get export-ready comprehensive report
        /// </summary>
        [HttpGet("report/course/{courseId}")]
        [Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
        public async Task<IActionResult> GetCourseProgressReport(long courseId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(new { message = "Course not found" });
                }

                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Student)
                    .Where(p => p.CourseId == courseId && !p.IsDeleted)
                    .OrderByDescending(p => p.ExamAvgScore)
                    .ToListAsync();

                var report = new
                {
                    reportTitle = $"Course Progress Report - {course.Title}",
                    generatedAt = DateTime.UtcNow,
                    courseId = courseId,
                    courseName = course.Title,
                    courseDescription = course.Description,
                    summary = new
                    {
                        totalStudents = progressList.Count,
                        averageAttendance = progressList.Any() ? Math.Round(progressList.Average(p => p.AttendancePercentage ?? 0), 2) : 0,
                        averageAssignmentScore = progressList.Any() ? Math.Round(progressList.Average(p => p.AssignmentAvgScore ?? 0), 2) : 0,
                        averageExamScore = progressList.Any() ? Math.Round(progressList.Average(p => p.ExamAvgScore ?? 0), 2) : 0,
                        passRate = progressList.Any() ? Math.Round((progressList.Count(p => p.ExamAvgScore >= 50) * 100m) / progressList.Count, 2) : 0,
                        gradeDistribution = new
                        {
                            aPlus = progressList.Count(p => p.OverallGrade == "A+"),
                            a = progressList.Count(p => p.OverallGrade == "A"),
                            b = progressList.Count(p => p.OverallGrade == "B"),
                            c = progressList.Count(p => p.OverallGrade == "C"),
                            d = progressList.Count(p => p.OverallGrade == "D"),
                            f = progressList.Count(p => p.OverallGrade == "F")
                        }
                    },
                    students = progressList.Select(p => new
                    {
                        studentId = p.StudentId,
                        studentName = p.Student?.FullName,
                        email = p.Student?.Email,
                        attendancePercentage = p.AttendancePercentage ?? 0,
                        assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                        examAvgScore = p.ExamAvgScore ?? 0,
                        overallGrade = p.OverallGrade,
                        lastUpdated = p.LastUpdated
                    }).ToList()
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper methods
        private string GetProgressStatus(decimal examScore, string? grade)
        {
            if (examScore >= 70)
                return "Excellent";
            else if (examScore >= 60)
                return "Good";
            else if (examScore >= 50)
                return "Average";
            else
                return "Needs Improvement";
        }

        private string GetPerformanceLevel(decimal examScore)
        {
            return examScore switch
            {
                >= 90 => "Outstanding",
                >= 80 => "Very Good",
                >= 70 => "Good",
                >= 60 => "Satisfactory",
                >= 50 => "Acceptable",
                _ => "Below Standard"
            };
        }

        private object GetGradeDistribution(List<StudentCourseProgress> progressList)
        {
            if (!progressList.Any())
                return new { };

            return new
            {
                aPlus = progressList.Count(p => p.OverallGrade == "A+"),
                a = progressList.Count(p => p.OverallGrade == "A"),
                b = progressList.Count(p => p.OverallGrade == "B"),
                c = progressList.Count(p => p.OverallGrade == "C"),
                d = progressList.Count(p => p.OverallGrade == "D"),
                f = progressList.Count(p => p.OverallGrade == "F")
            };
        }

        private decimal GetCoursePassRate(long courseId)
        {
            var totalStudents = _context.StudentCourseProgress
                .Where(p => p.CourseId == courseId && !p.IsDeleted)
                .Count();

            if (totalStudents == 0)
                return 0;

            var passingStudents = _context.StudentCourseProgress
                .Where(p => p.CourseId == courseId && !p.IsDeleted && p.ExamAvgScore >= 50)
                .Count();

            return (decimal)(passingStudents * 100 / totalStudents);
        }

        private List<string> GetRiskFactors(StudentCourseProgress progress)
        {
            var factors = new List<string>();

            if ((progress.AttendancePercentage ?? 0) < 75)
                factors.Add("Low Attendance");

            if ((progress.AssignmentAvgScore ?? 0) < 60)
                factors.Add("Low Assignment Scores");

            if ((progress.ExamAvgScore ?? 0) < 50)
                factors.Add("Failing Exam Scores");

            if ((progress.ExamAvgScore ?? 0) < 60)
                factors.Add("Below Average Performance");

            return factors;
        }
    }
}

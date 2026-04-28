using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Learning_Management_System.Controllers
{
    public class ProgressMvcController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ProgressMvcController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? GetToken() => HttpContext.Session.GetString("JwtToken");
        private string GetRole() => HttpContext.Session.GetString("UserRole") ?? "Student";
        private string GetUserId() => HttpContext.Session.GetString("UserId") ?? "";
        private bool IsAuthenticated() => !string.IsNullOrEmpty(GetToken());

        private async Task LoadCoursesAsync()
        {
            var courses = await _context.Courses
                .Where(c => !c.IsDeleted && c.Status == "ACTIVE")
                .OrderBy(c => c.Title)
                .Select(c => new { id = c.Id, title = c.Title })
                .ToListAsync();
            ViewBag.CoursesJson = JsonSerializer.Serialize(courses, _jsonOptions);
        }

        // Student: my progress across all courses
        public async Task<IActionResult> MyProgress()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            var userId = GetUserId();
            ViewBag.UserRole = GetRole();
            ViewBag.ProgressJson = "null";

            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.ApiError = "User session not found. Please log out and log in again.";
                return View();
            }

            try
            {
                var student = await _userManager.FindByIdAsync(userId);
                if (student == null) { ViewBag.ApiError = "Student not found."; return View(); }

                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Where(p => p.StudentId == userId && !p.IsDeleted)
                    .OrderByDescending(p => p.LastUpdated)
                    .ToListAsync();

                // Auto-compute from enrollments if no records exist yet
                if (!progressList.Any())
                {
                    var enrolledCourseIds = await _context.Enrollments
                        .Include(e => e.Batch)
                        .Where(e => e.StudentId == userId && !e.IsDeleted)
                        .Select(e => e.Batch!.CourseId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var cId in enrolledCourseIds)
                    {
                        var computed = await ComputeAndSaveProgressAsync(userId, cId);
                        if (computed != null) progressList.Add(computed);
                    }

                    if (!progressList.Any())
                    {
                        ViewBag.ProgressJson = JsonSerializer.Serialize(
                            new { message = "No progress records found. Enroll in a course to begin.", data = new List<object>() },
                            _jsonOptions);
                        return View();
                    }
                }

                var courses = progressList.Select(p => new
                {
                    id = p.Id,
                    courseName = p.Course?.Title,
                    courseId = p.CourseId,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    lastUpdated = p.LastUpdated,
                    status = GetProgressStatus(p.ExamAvgScore ?? 0)
                }).ToList();

                var overall = new
                {
                    totalCourses = progressList.Count,
                    averageAttendance = Math.Round(progressList.Average(p => p.AttendancePercentage ?? 0), 2),
                    averageAssignmentScore = Math.Round(progressList.Average(p => p.AssignmentAvgScore ?? 0), 2),
                    averageExamScore = Math.Round(progressList.Average(p => p.ExamAvgScore ?? 0), 2),
                    gradeDistribution = GetGradeDistribution(progressList)
                };

                ViewBag.ProgressJson = JsonSerializer.Serialize(
                    new { studentName = student.FullName ?? student.Email, overall, courses },
                    _jsonOptions);
            }
            catch (Exception ex) { ViewBag.ApiError = ex.Message; }

            return View();
        }

        // Teacher/Coordinator/Admin: course analytics report
        public async Task<IActionResult> Analytics(long? courseId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role == "Student") return RedirectToAction("MyProgress");

            ViewBag.UserRole = role;
            ViewBag.CourseId = courseId;
            ViewBag.AnalyticsJson = "null";

            await LoadCoursesAsync();

            if (!courseId.HasValue) return View();

            try
            {
                var course = await _context.Courses.FindAsync(courseId.Value);
                if (course == null) return View();

                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Student)
                    .Where(p => p.CourseId == courseId.Value && !p.IsDeleted)
                    .OrderByDescending(p => p.ExamAvgScore)
                    .ToListAsync();

                var count = progressList.Count;
                var summary = new
                {
                    totalStudents = count,
                    averageAttendance = count > 0 ? Math.Round(progressList.Average(p => p.AttendancePercentage ?? 0), 2) : 0m,
                    averageAssignmentScore = count > 0 ? Math.Round(progressList.Average(p => p.AssignmentAvgScore ?? 0), 2) : 0m,
                    averageExamScore = count > 0 ? Math.Round(progressList.Average(p => p.ExamAvgScore ?? 0), 2) : 0m,
                    passRate = count > 0 ? Math.Round((progressList.Count(p => p.ExamAvgScore >= 50) * 100m) / count, 2) : 0m,
                    gradeDistribution = GetGradeDistribution(progressList)
                };

                var students = progressList.Select(p => new
                {
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName ?? p.Student?.UserName,
                    email = p.Student?.Email,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    lastUpdated = p.LastUpdated
                }).ToList();

                ViewBag.AnalyticsJson = JsonSerializer.Serialize(
                    new { reportTitle = $"Course Progress Report - {course.Title}", courseName = course.Title, courseId, summary, students },
                    _jsonOptions);
            }
            catch (Exception ex) { ViewBag.Error = ex.Message; }

            return View();
        }

        // Recalculate a student's progress
        [HttpPost]
        public async Task<IActionResult> Recalculate(string studentId, long courseId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");

            try
            {
                await ComputeAndSaveProgressAsync(studentId, courseId);
                TempData["Success"] = "Progress recalculated!";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("MyProgress");
        }

        // System-wide analytics (Admin/Coordinator)
        public async Task<IActionResult> SystemAnalytics()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Admin" && role != "CourseCoordinator")
            { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "Home"); }

            ViewBag.UserRole = role;

            try
            {
                var progressList = await _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Include(p => p.Student)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.ExamAvgScore)
                    .ToListAsync();

                var data = progressList.Select(p => new
                {
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName ?? p.Student?.UserName,
                    email = p.Student?.Email,
                    courseId = p.CourseId,
                    courseName = p.Course?.Title,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    lastUpdated = p.LastUpdated
                }).ToList();

                ViewBag.AnalyticsJson = JsonSerializer.Serialize(data, _jsonOptions);
            }
            catch { ViewBag.AnalyticsJson = "null"; }

            return View();
        }

        // Admin/Coordinator: seed demo data — select student
        [HttpGet]
        public async Task<IActionResult> SeedData()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Admin" && role != "CourseCoordinator")
            { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "Home"); }

            ViewBag.UserRole = role;
            var students = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.StudentsJson = JsonSerializer.Serialize(
                students.Select(s => new { id = s.Id, fullName = s.FullName, email = s.Email }),
                _jsonOptions);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeedDemoData(string studentId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role != "Admin" && role != "CourseCoordinator")
            { TempData["Error"] = "Access denied."; return RedirectToAction("Index", "Home"); }

            try
            {
                var student = await _userManager.FindByIdAsync(studentId);
                if (student == null) { TempData["Error"] = "Student not found."; return RedirectToAction("SeedData"); }

                var rng = new Random();
                int recordsCreated = 0;

                var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
                var graderId = teachers.FirstOrDefault()?.Id ?? studentId;

                var courses = await _context.Courses
                    .Include(c => c.Subjects)
                    .Include(c => c.Batches)
                    .Where(c => !c.IsDeleted)
                    .ToListAsync();

                foreach (var course in courses)
                {
                    var batch = course.Batches.FirstOrDefault(b => !b.IsDeleted);
                    if (batch == null) continue;

                    var existingEnrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.StudentId == studentId && e.BatchId == batch.Id && !e.IsDeleted);

                    if (existingEnrollment == null)
                    {
                        _context.Enrollments.Add(new Enrollment
                        {
                            StudentId = studentId,
                            BatchId = batch.Id,
                            Status = "ACTIVE",
                            EnrolledAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        });
                        await _context.SaveChangesAsync();
                        recordsCreated++;
                    }

                    var subjectIds = course.Subjects.Where(s => !s.IsDeleted).Select(s => s.Id).ToList();

                    foreach (var subjectId in subjectIds)
                    {
                        var sessions = await _context.AttendanceSessions
                            .Where(s => s.SubjectId == subjectId).ToListAsync();

                        foreach (var session in sessions)
                        {
                            var existingRecord = await _context.AttendanceRecords
                                .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == studentId);
                            if (existingRecord == null)
                            {
                                _context.AttendanceRecords.Add(new AttendanceRecord
                                {
                                    SessionId = session.Id,
                                    StudentId = studentId,
                                    Status = rng.Next(100) > 20 ? "PRESENT" : "ABSENT",
                                    CreatedAt = DateTime.UtcNow
                                });
                                recordsCreated++;
                            }
                        }

                        var assignments = await _context.Assignments
                            .Where(a => a.SubjectId == subjectId && !a.IsDeleted).ToListAsync();

                        foreach (var assignment in assignments)
                        {
                            var existingSub = await _context.Submissions
                                .FirstOrDefaultAsync(s => s.AssignmentId == assignment.Id && s.StudentId == studentId);
                            if (existingSub == null)
                            {
                                var sub = new Submission
                                {
                                    AssignmentId = assignment.Id,
                                    StudentId = studentId,
                                    FileUrl = $"demo/assignment_{assignment.Id}.pdf",
                                    SubmittedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 10)),
                                    Status = "SUBMITTED",
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.Submissions.Add(sub);
                                await _context.SaveChangesAsync();

                                var score = rng.Next(60, 100);
                                _context.AssignmentGrades.Add(new AssignmentGrade
                                {
                                    SubmissionId = sub.Id,
                                    Score = score,
                                    Feedback = score >= 80 ? "Excellent work!" : score >= 65 ? "Good effort." : "Needs improvement.",
                                    GradedById = graderId,
                                    GradedAt = DateTime.UtcNow,
                                    CreatedAt = DateTime.UtcNow
                                });
                                recordsCreated++;
                            }
                        }

                        var exams = await _context.Exams
                            .Where(e => e.SubjectId == subjectId && !e.IsDeleted).ToListAsync();

                        foreach (var exam in exams)
                        {
                            var existingResult = await _context.ExamResults
                                .FirstOrDefaultAsync(r => r.ExamId == exam.Id && r.StudentId == studentId);
                            if (existingResult == null)
                            {
                                var marks = rng.Next(55, 100);
                                _context.ExamResults.Add(new ExamResult
                                {
                                    ExamId = exam.Id,
                                    StudentId = studentId,
                                    Marks = marks,
                                    Grade = marks switch { >= 90 => "A+", >= 80 => "A", >= 70 => "B", >= 60 => "C", >= 50 => "D", _ => "F" },
                                    GradedById = graderId,
                                    CreatedAt = DateTime.UtcNow
                                });
                                recordsCreated++;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await ComputeAndSaveProgressAsync(studentId, course.Id);
                }

                TempData["Success"] = $"Demo data generated! {recordsCreated} records created. The student can now view their progress.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }

            return RedirectToAction("SeedData");
        }

        // Teacher/Coordinator/Admin: at-risk students
        public async Task<IActionResult> AtRisk(long? courseId = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "AuthMvc");
            var role = GetRole();
            if (role == "Student") return RedirectToAction("MyProgress");

            ViewBag.UserRole = role;
            ViewBag.CourseId = courseId;

            await LoadCoursesAsync();

            try
            {
                var query = _context.StudentCourseProgress
                    .Include(p => p.Course)
                    .Include(p => p.Student)
                    .Where(p => !p.IsDeleted && (p.ExamAvgScore < 50 || p.AttendancePercentage < 75));

                if (courseId.HasValue)
                    query = query.Where(p => p.CourseId == courseId.Value);

                var atRisk = await query.OrderBy(p => p.ExamAvgScore).ToListAsync();

                var data = atRisk.Select(p => new
                {
                    studentId = p.StudentId,
                    studentName = p.Student?.FullName ?? p.Student?.UserName,
                    email = p.Student?.Email,
                    courseName = p.Course?.Title,
                    attendancePercentage = p.AttendancePercentage ?? 0,
                    assignmentAvgScore = p.AssignmentAvgScore ?? 0,
                    examAvgScore = p.ExamAvgScore ?? 0,
                    overallGrade = p.OverallGrade,
                    riskFactors = GetRiskFactors(p)
                }).ToList();

                ViewBag.AtRiskJson = JsonSerializer.Serialize(
                    new { totalAtRisk = atRisk.Count, students = data },
                    _jsonOptions);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.AtRiskJson = "null";
            }

            return View();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private async Task<StudentCourseProgress?> ComputeAndSaveProgressAsync(string studentId, long courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return null;

            var subjectIds = await _context.Subjects
                .Where(s => s.CourseId == courseId && !s.IsDeleted)
                .Select(s => s.Id).ToListAsync();

            // Attendance
            var sessionIds = await _context.AttendanceSessions
                .Where(s => subjectIds.Contains(s.SubjectId))
                .Select(s => s.Id).ToListAsync();
            var presentCount = sessionIds.Count > 0
                ? await _context.AttendanceRecords.CountAsync(
                    r => r.StudentId == studentId && r.Status == "PRESENT" && sessionIds.Contains(r.SessionId))
                : 0;
            var attendance = sessionIds.Count > 0
                ? Math.Round((decimal)presentCount * 100 / sessionIds.Count, 2) : 0;

            // Assignments
            var assignmentIds = await _context.Assignments
                .Where(a => subjectIds.Contains(a.SubjectId) && !a.IsDeleted)
                .Select(a => a.Id).ToListAsync();
            var assignmentScores = assignmentIds.Any()
                ? await (from sub in _context.Submissions
                         join g in _context.AssignmentGrades on sub.Id equals g.SubmissionId
                         where sub.StudentId == studentId && assignmentIds.Contains(sub.AssignmentId)
                         select g.Score ?? 0).ToListAsync()
                : new List<int>();
            var assignmentAvg = assignmentScores.Any()
                ? Math.Round((decimal)assignmentScores.Average(), 2) : 0;

            // Exams
            var examIds = await _context.Exams
                .Where(e => subjectIds.Contains(e.SubjectId) && !e.IsDeleted)
                .Select(e => e.Id).ToListAsync();
            var examScores = examIds.Any()
                ? await _context.ExamResults
                    .Where(r => r.StudentId == studentId && examIds.Contains(r.ExamId))
                    .Select(r => r.Marks ?? 0).ToListAsync()
                : new List<int>();
            var examAvg = examScores.Any()
                ? Math.Round((decimal)examScores.Average(), 2) : 0;

            var overall = (attendance * 0.2m) + (assignmentAvg * 0.3m) + (examAvg * 0.5m);
            var grade = overall switch
            {
                >= 90 => "A+", >= 80 => "A", >= 70 => "B",
                >= 60 => "C", >= 50 => "D", _ => "F"
            };

            var existing = await _context.StudentCourseProgress
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.CourseId == courseId);

            if (existing != null)
            {
                existing.AttendancePercentage = attendance;
                existing.AssignmentAvgScore = assignmentAvg;
                existing.ExamAvgScore = examAvg;
                existing.OverallGrade = grade;
                existing.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                existing.Course = course;
                return existing;
            }

            var progress = new StudentCourseProgress
            {
                StudentId = studentId,
                CourseId = courseId,
                AttendancePercentage = attendance,
                AssignmentAvgScore = assignmentAvg,
                ExamAvgScore = examAvg,
                OverallGrade = grade,
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.StudentCourseProgress.Add(progress);
            await _context.SaveChangesAsync();
            progress.Course = course;
            return progress;
        }

        private static string GetProgressStatus(decimal examScore) => examScore switch
        {
            >= 70 => "Excellent",
            >= 60 => "Good",
            >= 50 => "Average",
            _ => "Needs Improvement"
        };

        private static object GetGradeDistribution(List<StudentCourseProgress> list) => new
        {
            aPlus = list.Count(p => p.OverallGrade == "A+"),
            a = list.Count(p => p.OverallGrade == "A"),
            b = list.Count(p => p.OverallGrade == "B"),
            c = list.Count(p => p.OverallGrade == "C"),
            d = list.Count(p => p.OverallGrade == "D"),
            f = list.Count(p => p.OverallGrade == "F")
        };

        private static List<string> GetRiskFactors(StudentCourseProgress p)
        {
            var factors = new List<string>();
            if ((p.AttendancePercentage ?? 0) < 75) factors.Add("Low Attendance");
            if ((p.AssignmentAvgScore ?? 0) < 60) factors.Add("Low Assignment Scores");
            if ((p.ExamAvgScore ?? 0) < 50) factors.Add("Failing Exam Scores");
            else if ((p.ExamAvgScore ?? 0) < 60) factors.Add("Below Average Performance");
            return factors;
        }
    }
}

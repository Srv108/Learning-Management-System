using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DataSeeder(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedDataAsync()
        {
            // Check if data already exists
            if (await _context.Courses.AnyAsync())
            {
                return; // Data already seeded
            }

            await SeedUsersAndRoles();
            await SeedCoursesAndSubjects();
            await SeedCourseBatchesAndEnrollments();
            await SeedAttendance();
            await SeedAssignments();
            await SeedExams();
            await SeedProgress();

            await _context.SaveChangesAsync();
        }

        private async Task SeedUsersAndRoles()
        {
            var roles = new[] { "Student", "Teacher", "CourseCoordinator", "ExamController", "Admin" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create Teachers
            var teachers = new[]
            {
                new { Username = "teacher1", Email = "teacher1@lms.com", Name = "Prof. John Smith", Role = "Teacher" },
                new { Username = "teacher2", Email = "teacher2@lms.com", Name = "Prof. Sarah Johnson", Role = "Teacher" },
                new { Username = "teacher3", Email = "teacher3@lms.com", Name = "Prof. Mike Davis", Role = "Teacher" }
            };

            foreach (var teacher in teachers)
            {
                var user = await _userManager.FindByEmailAsync(teacher.Email);
                if (user == null)
                {
                    user = new AppUser
                    {
                        UserName = teacher.Username,
                        Email = teacher.Email,
                        FullName = teacher.Name,
                        PhoneNumber = "123456789"
                    };
                    await _userManager.CreateAsync(user, "Password@123");
                    await _userManager.AddToRoleAsync(user, teacher.Role);
                }
            }

            // Create Students
            var students = new[]
            {
                "student1", "student2", "student3", "student4", "student5",
                "student6", "student7", "student8", "student9", "student10"
            };

            for (int i = 0; i < students.Length; i++)
            {
                var email = $"{students[i]}@lms.com";
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new AppUser
                    {
                        UserName = students[i],
                        Email = email,
                        FullName = $"Student {i + 1}",
                        PhoneNumber = $"987654{i:D3}"
                    };
                    await _userManager.CreateAsync(user, "Password@123");
                    await _userManager.AddToRoleAsync(user, "Student");
                }
            }

            // Create Course Coordinator
            var coordinator = await _userManager.FindByEmailAsync("coordinator@lms.com");
            if (coordinator == null)
            {
                coordinator = new AppUser
                {
                    UserName = "coordinator",
                    Email = "coordinator@lms.com",
                    FullName = "Course Coordinator",
                    PhoneNumber = "555666777"
                };
                await _userManager.CreateAsync(coordinator, "Password@123");
                await _userManager.AddToRoleAsync(coordinator, "CourseCoordinator");
            }

            // Create Exam Controller
            var examController = await _userManager.FindByEmailAsync("examcontroller@lms.com");
            if (examController == null)
            {
                examController = new AppUser
                {
                    UserName = "examcontroller",
                    Email = "examcontroller@lms.com",
                    FullName = "Exam Controller",
                    PhoneNumber = "555777888"
                };
                await _userManager.CreateAsync(examController, "Password@123");
                await _userManager.AddToRoleAsync(examController, "ExamController");
            }

            // Create Admin
            var admin = await _userManager.FindByEmailAsync("admin@lms.com");
            if (admin == null)
            {
                admin = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@lms.com",
                    FullName = "System Administrator",
                    PhoneNumber = "555888999"
                };
                await _userManager.CreateAsync(admin, "Password@123");
                await _userManager.AddToRoleAsync(admin, "Admin");
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedCoursesAndSubjects()
        {
            var teacher1 = await _userManager.FindByEmailAsync("teacher1@lms.com");
            var teacher2 = await _userManager.FindByEmailAsync("teacher2@lms.com");
            var teacher3 = await _userManager.FindByEmailAsync("teacher3@lms.com");

            // Create Courses
            var courses = new[]
            {
                new { Title = "Mathematics", Description = "Advanced Mathematics Course", Credits = 4 },
                new { Title = "Computer Science", Description = "Introduction to Computer Science", Credits = 3 },
                new { Title = "Physics", Description = "Fundamental Physics Concepts", Credits = 4 }
            };

            foreach (var courseData in courses)
            {
                var course = new Course
                {
                    Title = courseData.Title,
                    Description = courseData.Description,
                    Credits = courseData.Credits,
                    CreatedById = teacher1!.Id,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                // Create Subjects for each course
                var subjects = courseData.Title switch
                {
                    "Mathematics" => new[] { "Calculus", "Linear Algebra", "Discrete Math" },
                    "Computer Science" => new[] { "Programming", "Algorithms", "Data Structures" },
                    "Physics" => new[] { "Mechanics", "Thermodynamics", "Waves" },
                    _ => new string[] { }
                };

                var subjectTeacher = courseData.Title switch
                {
                    "Mathematics" => teacher1,
                    "Computer Science" => teacher2,
                    "Physics" => teacher3,
                    _ => teacher1
                };

                foreach (var subjectName in subjects)
                {
                    var subject = new Subject
                    {
                        CourseId = course.Id,
                        Name = subjectName,
                        Description = $"{subjectName} - Part of {courseData.Title}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Subjects.Add(subject);
                    await _context.SaveChangesAsync();

                    // Add subject teacher mapping
                    var subjectTeacherMap = new SubjectTeacher
                    {
                        SubjectId = subject.Id,
                        TeacherId = subjectTeacher!.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SubjectTeachers.Add(subjectTeacherMap);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedCourseBatchesAndEnrollments()
        {
            var courses = await _context.Courses.ToListAsync();
            var students = await _userManager.GetUsersInRoleAsync("Student");

            foreach (var course in courses)
            {
                // Create batch
                var batch = new CourseBatch
                {
                    CourseId = course.Id,
                    BatchName = $"{course.Title} - Spring 2026",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(4),
                    CreatedAt = DateTime.UtcNow
                };
                _context.CourseBatches.Add(batch);
                await _context.SaveChangesAsync();

                // Enroll 8 random students in each batch
                var enrolledStudents = students.OrderBy(x => Guid.NewGuid()).Take(8).ToList();
                foreach (var student in enrolledStudents)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = student.Id,
                        BatchId = batch.Id,
                        EnrolledAt = DateTime.UtcNow,
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Enrollments.Add(enrollment);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedAttendance()
        {
            var subjects = await _context.Subjects.ToListAsync();
            var enrollments = await _context.Enrollments.Include(e => e.Batch).ThenInclude(b => b.Course).ToListAsync();

            foreach (var subject in subjects)
            {
                // Create 10 attendance sessions
                for (int i = 0; i < 10; i++)
                {
                    var session = new AttendanceSession
                    {
                        SubjectId = subject.Id,
                        SessionDate = DateTime.Now.AddDays(-i),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AttendanceSessions.Add(session);
                    await _context.SaveChangesAsync();

                    // Add attendance records for enrolled students
                    var subjectEnrollments = enrollments.Where(e => e.Batch.Course.Id == subject.CourseId).ToList();
                    foreach (var enrollment in subjectEnrollments)
                    {
                        var random = new Random();
                        var isPresent = random.Next(0, 100) > 20; // 80% attendance rate

                        var record = new AttendanceRecord
                        {
                            SessionId = session.Id,
                            StudentId = enrollment.StudentId,
                            Status = isPresent ? "PRESENT" : "ABSENT",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.AttendanceRecords.Add(record);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedAssignments()
        {
            var subjects = await _context.Subjects.ToListAsync();
            var enrollments = await _context.Enrollments.Include(e => e.Batch).ThenInclude(b => b.Course).ToListAsync();
            var teacher1 = await _userManager.FindByEmailAsync("teacher1@lms.com");

            foreach (var subject in subjects)
            {
                // Create 3 assignments per subject
                for (int i = 1; i <= 3; i++)
                {
                    var assignment = new Assignment
                    {
                        SubjectId = subject.Id,
                        Title = $"{subject.Name} Assignment {i}",
                        Description = $"Complete the tasks for assignment {i}",
                        MaxScore = 100,
                        DueDate = DateTime.Now.AddDays(7 * i),
                        CreatedById = teacher1!.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Assignments.Add(assignment);
                    await _context.SaveChangesAsync();

                    // Create submissions for enrolled students
                    var subjectEnrollments = enrollments.Where(e => e.Batch.Course.Id == subject.CourseId).ToList();
                    foreach (var enrollment in subjectEnrollments)
                    {
                        var random = new Random();
                        var isSubmitted = random.Next(0, 100) > 15; // 85% submission rate
                        var isLate = random.Next(0, 100) > 80; // 20% late submission rate

                        if (isSubmitted)
                        {
                            var submission = new Submission
                            {
                                AssignmentId = assignment.Id,
                                StudentId = enrollment.StudentId,
                                FileUrl = $"submissions/assignment_{assignment.Id}_student_{enrollment.StudentId}.pdf",
                                SubmittedAt = isLate ? assignment.DueDate.AddDays(2) : assignment.DueDate.AddDays(-1),
                                Status = isLate ? "LATE" : "SUBMITTED",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Submissions.Add(submission);
                            await _context.SaveChangesAsync();

                            // Create grade if submitted
                            var grade = new AssignmentGrade
                            {
                                SubmissionId = submission.Id,
                                Score = random.Next(60, 100),
                                Feedback = $"Good work on assignment {i}",
                                GradedById = teacher1.Id,
                                GradedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.AssignmentGrades.Add(grade);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedExams()
        {
            var subjects = await _context.Subjects.ToListAsync();
            var enrollments = await _context.Enrollments.Include(e => e.Batch).ThenInclude(b => b.Course).ToListAsync();
            var teacher1 = await _userManager.FindByEmailAsync("teacher1@lms.com");

            foreach (var subject in subjects)
            {
                // Create 2 exams per subject (Midterm and Final)
                var examTypes = new[] { "MIDTERM", "FINAL" };
                foreach (var examType in examTypes)
                {
                    var exam = new Exam
                    {
                        SubjectId = subject.Id,
                        Title = $"{subject.Name} {examType} Exam",
                        ExamType = examType,
                        ExamDate = examType == "MIDTERM" ? DateTime.Now.AddDays(30) : DateTime.Now.AddDays(90),
                        MaxScore = 100,
                        CreatedById = teacher1!.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Exams.Add(exam);
                    await _context.SaveChangesAsync();

                    // Create exam results for students
                    var subjectEnrollments = enrollments.Where(e => e.Batch.Course.Id == subject.CourseId).ToList();
                    foreach (var enrollment in subjectEnrollments)
                    {
                        var random = new Random();
                        var marks = random.Next(40, 100);

                        var grade = marks switch
                        {
                            >= 90 => "A+",
                            >= 80 => "A",
                            >= 70 => "B",
                            >= 60 => "C",
                            >= 50 => "D",
                            _ => "F"
                        };

                        var result = new ExamResult
                        {
                            ExamId = exam.Id,
                            StudentId = enrollment.StudentId,
                            Marks = marks,
                            Grade = grade,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ExamResults.Add(result);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedProgress()
        {
            var courses = await _context.Courses.ToListAsync();
            var enrollments = await _context.Enrollments.ToListAsync();
            var random = new Random();

            foreach (var enrollment in enrollments)
            {
                var course = courses.FirstOrDefault(c => c.Id == enrollment.Batch.CourseId);
                if (course != null)
                {
                    var progress = new StudentCourseProgress
                    {
                        StudentId = enrollment.StudentId,
                        CourseId = course.Id,
                        AttendancePercentage = random.Next(20, 100),
                        AssignmentAvgScore = random.Next(50, 95),
                        ExamAvgScore = random.Next(50, 95),
                        OverallGrade = new[] { "A+", "A", "B", "C", "D", "F" }[random.Next(0, 6)],
                        LastUpdated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StudentCourseProgress.Add(progress);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
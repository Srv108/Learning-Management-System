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

            // Create Teachers (5 instead of 3)
            var teachers = new[]
            {
                new { Username = "teacher1", Email = "teacher1@lms.com", Name = "Prof. John Smith" },
                new { Username = "teacher2", Email = "teacher2@lms.com", Name = "Prof. Sarah Johnson" },
                new { Username = "teacher3", Email = "teacher3@lms.com", Name = "Prof. Mike Davis" },
                new { Username = "teacher4", Email = "teacher4@lms.com", Name = "Prof. Emily Brown" },
                new { Username = "teacher5", Email = "teacher5@lms.com", Name = "Prof. James Wilson" }
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
                        PhoneNumber = $"100{teachers.ToList().IndexOf(teacher):D7}"
                    };
                    await _userManager.CreateAsync(user, "Password@123");
                    await _userManager.AddToRoleAsync(user, "Teacher");
                }
            }

            // Create 30 Students (instead of 10)
            var studentCount = 30;
            for (int i = 1; i <= studentCount; i++)
            {
                var username = $"student{i}";
                var email = $"{username}@lms.com";
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new AppUser
                    {
                        UserName = username,
                        Email = email,
                        FullName = $"Student {i}",
                        PhoneNumber = $"200{i:D7}"
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
            var teachers = new[] { "teacher1@lms.com", "teacher2@lms.com", "teacher3@lms.com", "teacher4@lms.com", "teacher5@lms.com" };
            var teacherObjects = new List<AppUser>();
            foreach (var email in teachers)
            {
                var teacher = await _userManager.FindByEmailAsync(email);
                if (teacher != null) teacherObjects.Add(teacher);
            }

            // Create 6 Courses
            var courses = new[]
            {
                new { Title = "Mathematics", Description = "Advanced Mathematics Course", Credits = 4, Teacher = 0 },
                new { Title = "Computer Science", Description = "Introduction to Computer Science", Credits = 3, Teacher = 1 },
                new { Title = "Physics", Description = "Fundamental Physics Concepts", Credits = 4, Teacher = 2 },
                new { Title = "Chemistry", Description = "Organic and Inorganic Chemistry", Credits = 3, Teacher = 3 },
                new { Title = "English Literature", Description = "Classic and Modern Literature", Credits = 3, Teacher = 4 },
                new { Title = "Biology", Description = "Life Sciences and Ecology", Credits = 4, Teacher = 0 }
            };

            int courseIndex = 0;
            foreach (var courseData in courses)
            {
                var course = new Course
                {
                    Title = courseData.Title,
                    Description = courseData.Description,
                    Credits = courseData.Credits,
                    CreatedById = teacherObjects[courseData.Teacher].Id,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                // Create Subjects for each course
                var subjects = courseData.Title switch
                {
                    "Mathematics" => new[] { "Calculus I", "Calculus II", "Linear Algebra", "Discrete Mathematics" },
                    "Computer Science" => new[] { "Programming Basics", "Object-Oriented Programming", "Algorithms", "Data Structures", "Database Systems" },
                    "Physics" => new[] { "Classical Mechanics", "Thermodynamics", "Waves and Oscillations", "Electromagnetism" },
                    "Chemistry" => new[] { "Organic Chemistry", "Inorganic Chemistry", "Physical Chemistry", "Analytical Chemistry" },
                    "English Literature" => new[] { "Shakespeare Studies", "Modern Poetry", "Victorian Novel", "Contemporary Drama" },
                    "Biology" => new[] { "Cell Biology", "Genetics", "Ecology", "Molecular Biology", "Evolutionary Biology" },
                    _ => new string[] { }
                };

                var subjectTeacher = teacherObjects[courseData.Teacher];

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
                        TeacherId = subjectTeacher.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SubjectTeachers.Add(subjectTeacherMap);

                    // Create Course Modules
                    var moduleCount = 3;
                    for (int m = 1; m <= moduleCount; m++)
                    {
                        var module = new CourseModule
                        {
                            CourseId = course.Id,
                            Title = $"{subjectName} - Module {m}",
                            Description = $"Module {m} content for {subjectName}",
                            OrderIndex = m,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.CourseModules.Add(module);
                        await _context.SaveChangesAsync();

                        // Create Lessons for each module
                        for (int l = 1; l <= 3; l++)
                        {
                            var lesson = new Lesson
                            {
                                ModuleId = module.Id,
                                Title = $"{subjectName} - Lesson {m}.{l}",
                                Description = $"Learning content for lesson {m}.{l}",
                                ContentUrl = $"content/lesson_{module.Id}_{l}.mp4",
                                Duration = 45 + (l * 10),
                                OrderIndex = l,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Lessons.Add(lesson);
                        }
                    }
                }

                courseIndex++;
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedCourseBatchesAndEnrollments()
        {
            var courses = await _context.Courses.ToListAsync();
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var studentList = students.ToList();

            foreach (var course in courses)
            {
                for (int batchNum = 1; batchNum <= 2; batchNum++) // 2 batches per course
                {
                    var batch = new CourseBatch
                    {
                        CourseId = course.Id,
                        BatchName = $"{course.Title} - Batch {batchNum} (Spring 2026)",
                        StartDate = DateTime.Now.AddDays(-batchNum * 30),
                        EndDate = DateTime.Now.AddMonths(4).AddDays(-batchNum * 30),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CourseBatches.Add(batch);
                    await _context.SaveChangesAsync();

                    // Enroll 15 students per batch (distributed from the 30 students)
                    var startIndex = (int)(((course.Id - 1) * 5 + batchNum * 7) % studentList.Count);
                    for (int i = 0; i < 15; i++)
                    {
                        var studentIndex = (startIndex + i) % studentList.Count;
                        var student = studentList[studentIndex];

                        var enrollment = new Enrollment
                        {
                            StudentId = student.Id,
                            BatchId = batch.Id,
                            EnrolledAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60)),
                            Status = "ACTIVE",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Enrollments.Add(enrollment);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedAttendance()
        {
            var subjects = await _context.Subjects.ToListAsync();
            var sessions = await _context.AttendanceSessions.ToListAsync();
            var enrollments = await _context.Enrollments.Include(e => e.Batch).ThenInclude(b => b.Course).ToListAsync();

            foreach (var subject in subjects)
            {
                // Create 15 attendance sessions per subject
                for (int i = 0; i < 15; i++)
                {
                    var session = new AttendanceSession
                    {
                        SubjectId = subject.Id,
                        SessionDate = DateTime.Now.AddDays(-i),
                        CreatedById = (await _userManager.FindByEmailAsync("teacher1@lms.com"))!.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AttendanceSessions.Add(session);
                    await _context.SaveChangesAsync();

                    // Add attendance records for enrolled students
                    var subjectEnrollments = enrollments.Where(e => e.Batch.Course.Subjects.Any(s => s.Id == subject.Id)).ToList();
                    if (subjectEnrollments.Count == 0)
                    {
                        // Fallback: get enrollments from same course
                        subjectEnrollments = enrollments.Where(e => e.Batch.Course.Id == subject.CourseId).ToList();
                    }

                    // Deduplicate enrollments by StudentId to avoid duplicate attendance records
                    var uniqueEnrollments = subjectEnrollments.GroupBy(e => e.StudentId).Select(g => g.First()).ToList();
                    
                    foreach (var enrollment in uniqueEnrollments)
                    {
                        var isPresent = Random.Shared.Next(0, 100) > 20; // 80% attendance rate

                        var record = new AttendanceRecord
                        {
                            SessionId = session.Id,
                            StudentId = enrollment.StudentId,
                            Status = isPresent ? "PRESENT" : "ABSENT",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.AttendanceRecords.Add(record);
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedAssignments()
        {
            var subjects = await _context.Subjects.ToListAsync();
            var enrollments = await _context.Enrollments.Include(e => e.Batch).ThenInclude(b => b.Course).ToListAsync();
            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            var teacherList = teachers.ToList();

            foreach (var subject in subjects)
            {
                var subjectTeacher = teacherList[(int)((subject.Id - 1) % teacherList.Count)];

                // Create 5 assignments per subject
                for (int i = 1; i <= 5; i++)
                {
                    var assignment = new Assignment
                    {
                        SubjectId = subject.Id,
                        Title = $"{subject.Name} Assignment {i}",
                        Description = $"Complete all tasks for assignment {i}. Topics covered: Basic concepts, Applications, Problem solving",
                        MaxScore = 100,
                        DueDate = DateTime.Now.AddDays(7 * i),
                        CreatedById = subjectTeacher.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Assignments.Add(assignment);
                    await _context.SaveChangesAsync();

                    // Create submissions for enrolled students (deduplicate by StudentId)
                    var subjectEnrollments = enrollments.Where(e => e.Batch.Course.Id == subject.CourseId)
                        .GroupBy(e => e.StudentId)
                        .Select(g => g.First())
                        .ToList();
                    
                    foreach (var enrollment in subjectEnrollments)
                    {
                        var isSubmitted = Random.Shared.Next(0, 100) > 15; // 85% submission rate
                        var isLate = Random.Shared.Next(0, 100) > 80; // 20% late submission rate

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
                            var score = Random.Shared.Next(60, 100);
                            var grade = new AssignmentGrade
                            {
                                SubmissionId = submission.Id,
                                Score = score,
                                Feedback = score >= 80 ? "Excellent work!" : score >= 70 ? "Good effort, needs improvement" : "Needs more work",
                                GradedById = subjectTeacher.Id,
                                GradedAt = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 5)),
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
            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            var teacherList = teachers.ToList();

            foreach (var subject in subjects)
            {
                var subjectTeacher = teacherList[(int)((subject.Id - 1) % teacherList.Count)];

                // Create 3 exams per subject (Quiz, Midterm, Final)
                var examTypes = new[] { "QUIZ", "MIDTERM", "FINAL" };
                int dayOffset = 0;
                foreach (var examType in examTypes)
                {
                    var exam = new Exam
                    {
                        SubjectId = subject.Id,
                        Title = $"{subject.Name} {examType}",
                        ExamType = examType,
                        ExamDate = examType switch
                        {
                            "QUIZ" => DateTime.Now.AddDays(15 + dayOffset),
                            "MIDTERM" => DateTime.Now.AddDays(30 + dayOffset),
                            "FINAL" => DateTime.Now.AddDays(90 + dayOffset),
                            _ => DateTime.Now.AddDays(dayOffset)
                        },
                        MaxScore = examType == "QUIZ" ? 50 : 100,
                        CreatedById = subjectTeacher.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Exams.Add(exam);
                    await _context.SaveChangesAsync();

                    dayOffset += 5;

                    // Create exam results for students (deduplicate by StudentId)
                    var subjectEnrollments = enrollments.Where(e => e.Batch.Course.Id == subject.CourseId)
                        .GroupBy(e => e.StudentId)
                        .Select(g => g.First())
                        .ToList();
                    
                    foreach (var enrollment in subjectEnrollments)
                    {
                        var marks = Random.Shared.Next(40, 100);
                        var maxMarks = examType == "QUIZ" ? 50 : 100;
                        var scaledMarks = (marks * maxMarks) / 100;

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
                            Marks = scaledMarks,
                            Grade = grade,
                            GradedById = subjectTeacher.Id,
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
            var enrollments = await _context.Enrollments.Include(e => e.Batch).ToListAsync();

            foreach (var enrollment in enrollments)
            {
                var course = courses.FirstOrDefault(c => c.Id == enrollment.Batch.CourseId);
                if (course != null)
                {
                    var attendancePercentage = Random.Shared.Next(20, 100);
                    var assignmentScore = Random.Shared.Next(50, 95);
                    var examScore = Random.Shared.Next(50, 95);

                    // Calculate overall grade based on weighted average
                    var overallScore = (attendancePercentage * 0.2) + (assignmentScore * 0.3) + (examScore * 0.5);
                    var overallGrade = overallScore switch
                    {
                        >= 90 => "A+",
                        >= 80 => "A",
                        >= 70 => "B",
                        >= 60 => "C",
                        >= 50 => "D",
                        _ => "F"
                    };

                    var progress = new StudentCourseProgress
                    {
                        StudentId = enrollment.StudentId,
                        CourseId = course.Id,
                        AttendancePercentage = new decimal(attendancePercentage),
                        AssignmentAvgScore = new decimal(assignmentScore),
                        ExamAvgScore = new decimal(examScore),
                        OverallGrade = overallGrade,
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
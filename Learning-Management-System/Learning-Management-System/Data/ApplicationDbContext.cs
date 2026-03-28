using Learning_Management_System.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace Learning_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add future DB sets here
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseModule> CourseModules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<CourseBatch> CourseBatches { get; set; }
        public DbSet<SubjectTeacher> SubjectTeachers { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<AssignmentGrade> AssignmentGrades { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<StudentCourseProgress> StudentCourseProgress { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure delete behaviors first to avoid cycles with multiple paths
            // All foreign keys to Course should be NoAction to prevent multiple cascade paths
            builder.Entity<Subject>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Subjects)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CourseBatch>()
                .HasOne(cb => cb.Course)
                .WithMany(c => c.Batches)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CourseModule>()
                .HasOne(cm => cm.Course)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<StudentCourseProgress>()
                .HasOne(scp => scp.Course)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Batch)
                .WithMany(b => b.Enrollments)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Submission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.Session)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ExamResult>()
                .HasOne(er => er.Exam)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AssignmentGrade>()
                .HasOne(ag => ag.Submission)
                .WithOne(s => s.Grade)
                .OnDelete(DeleteBehavior.NoAction);

            // Enrollment unique constraint
            builder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.BatchId })
                .IsUnique();

            // AttendanceRecord unique constraint
            builder.Entity<AttendanceRecord>()
                .HasIndex(ar => new { ar.SessionId, ar.StudentId })
                .IsUnique();

            // Submission unique constraint
            builder.Entity<Submission>()
                .HasIndex(s => new { s.AssignmentId, s.StudentId })
                .IsUnique();

            // ExamResult unique constraint
            builder.Entity<ExamResult>()
                .HasIndex(er => new { er.ExamId, er.StudentId })
                .IsUnique();

            // StudentCourseProgress unique constraint
            builder.Entity<StudentCourseProgress>()
                .HasIndex(scp => new { scp.StudentId, scp.CourseId })
                .IsUnique();

            // Indexes for performance
            builder.Entity<Enrollment>()
                .HasIndex(e => e.StudentId);

            builder.Entity<Assignment>()
                .HasIndex(a => a.SubjectId);

            builder.Entity<Exam>()
                .HasIndex(e => e.SubjectId);

            builder.Entity<AttendanceSession>()
                .HasIndex(s => s.SubjectId);

            builder.Entity<ExamResult>()
                .HasIndex(er => er.ExamId);

            builder.Entity<Submission>()
                .HasIndex(s => s.AssignmentId);
        }
    }
}
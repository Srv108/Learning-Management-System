using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class Subject
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<SubjectTeacher> SubjectTeachers { get; set; } = new List<SubjectTeacher>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
        public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
    }

    public class CourseBatch
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        [StringLength(100)]
        public string BatchName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }

    public class SubjectTeacher
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [ForeignKey("TeacherId")]
        public AppUser? Teacher { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
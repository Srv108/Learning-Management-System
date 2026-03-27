using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class Enrollment
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public AppUser? Student { get; set; }

        [Required]
        public long BatchId { get; set; }

        [ForeignKey("BatchId")]
        public CourseBatch? Batch { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, DROPPED, COMPLETED

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    public class AttendanceSession
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        public DateTime SessionDate { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public AppUser? CreatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }

    public class AttendanceRecord
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SessionId { get; set; }

        [ForeignKey("SessionId")]
        public AttendanceSession? Session { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public AppUser? Student { get; set; }

        [StringLength(10)]
        public string Status { get; set; } = "PRESENT"; // PRESENT, ABSENT

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class Course
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? Credits { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public AppUser? CreatedBy { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "ACTIVE";

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<CourseBatch> Batches { get; set; } = new List<CourseBatch>();
        // public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        // TODO: Add when implementing enrollment system
        // public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        // TODO: Add when implementing assignment system
        // public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
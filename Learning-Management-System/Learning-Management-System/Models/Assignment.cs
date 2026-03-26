using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class Assignment
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int MaxScore { get; set; }

        public DateTime DueDate { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public AppUser? CreatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }

    public class Submission
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long AssignmentId { get; set; }

        [ForeignKey("AssignmentId")]
        public Assignment? Assignment { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public AppUser? Student { get; set; }

        [StringLength(1000)]
        public string? FileUrl { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "SUBMITTED"; // SUBMITTED, LATE

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public AssignmentGrade? Grade { get; set; }
    }

    public class AssignmentGrade
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SubmissionId { get; set; }

        [ForeignKey("SubmissionId")]
        public Submission? Submission { get; set; }

        public int? Score { get; set; }

        [StringLength(1000)]
        public string? Feedback { get; set; }

        [Required]
        public string GradedById { get; set; } = string.Empty;

        [ForeignKey("GradedById")]
        public AppUser? GradedBy { get; set; }

        public DateTime? GradedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
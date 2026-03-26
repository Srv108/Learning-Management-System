using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class Exam
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(20)]
        public string ExamType { get; set; } = "MIDTERM"; // MIDTERM, FINAL, QUIZ, PRACTICAL

        [Required]
        public DateTime ExamDate { get; set; }

        public int MaxScore { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public AppUser? CreatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
    }

    public class ExamResult
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ExamId { get; set; }

        [ForeignKey("ExamId")]
        public Exam? Exam { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public AppUser? Student { get; set; }

        public int? Marks { get; set; }

        [StringLength(5)]
        public string? Grade { get; set; } // A, B, C, D, F

        public string? GradedById { get; set; }

        [ForeignKey("GradedById")]
        public AppUser? GradedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
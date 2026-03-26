using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class StudentCourseProgress
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public AppUser? Student { get; set; }

        [Required]
        public long CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Column(TypeName = "DECIMAL(5,2)")]
        public decimal? AttendancePercentage { get; set; }

        [Column(TypeName = "DECIMAL(5,2)")]
        public decimal? AssignmentAvgScore { get; set; }

        [Column(TypeName = "DECIMAL(5,2)")]
        public decimal? ExamAvgScore { get; set; }

        [StringLength(5)]
        public string? OverallGrade { get; set; }

        public DateTime? LastUpdated { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
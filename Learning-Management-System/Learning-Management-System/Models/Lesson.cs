using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class Lesson
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ModuleId { get; set; }

        [ForeignKey("ModuleId")]
        public CourseModule? Module { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? ContentUrl { get; set; } // Video, PDF, or link

        public int? Duration { get; set; } // Duration in minutes

        public int OrderIndex { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
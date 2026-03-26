using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class CourseModule
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int OrderIndex { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
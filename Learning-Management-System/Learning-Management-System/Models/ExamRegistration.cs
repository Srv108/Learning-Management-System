using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learning_Management_System.Models
{
    public class ExamRegistration
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

        [StringLength(20)]
        public string Status { get; set; } = "REGISTERED"; // REGISTERED, CANCELLED

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
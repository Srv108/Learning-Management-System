using System.ComponentModel.DataAnnotations;

namespace Learning_Management_System.Models.Dtos
{
    public class EnrollStudentDto
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public long BatchId { get; set; }
    }

    public class RecordAttendanceDto
    {
        [Required]
        public long SessionId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Status { get; set; } = "PRESENT";
    }
}

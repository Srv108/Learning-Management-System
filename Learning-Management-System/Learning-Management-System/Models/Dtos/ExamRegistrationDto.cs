using System;

namespace Learning_Management_System.Models.Dtos
{
    public class ExamRegistrationDto
    {
        public long Id { get; set; }
        public long ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Optional details
        public int MaxScore { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}

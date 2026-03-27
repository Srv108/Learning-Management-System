namespace Learning_Management_System.Models
{
    public class Assignment
    {
        public int AssignmentId { get; set; }
        public int SubjectId { get; set; }
        public string? StudentId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? FileUrl { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal? Grade { get; set; }
        public string? GradeComments { get; set; }
        public AssignmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Foreign Keys
        public Subject? Subject { get; set; }
        public AppUser? Student { get; set; }
    }

    public enum AssignmentStatus
    {
        NotSubmitted = 0,
        Submitted = 1,
        Graded = 2,
        Returned = 3,
        Late = 4
    }
}

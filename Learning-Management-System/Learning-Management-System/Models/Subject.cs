namespace Learning_Management_System.Models
{
    public class Subject
    {
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string? Description { get; set; }
        public string? InstructorId { get; set; }
        public AppUser? Instructor { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Assignment>? Assignments { get; set; }
    }
}

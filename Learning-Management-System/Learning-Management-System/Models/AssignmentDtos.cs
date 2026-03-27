namespace Learning_Management_System.Models
{
    // Request DTOs
    public class SubmitAssignmentRequest
    {
        public int AssignmentId { get; set; }
        public string? StudentId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public IFormFile? SubmissionFile { get; set; }
    }

    public class GradeAssignmentRequest
    {
        public int AssignmentId { get; set; }
        public decimal Grade { get; set; }
        public string? Comments { get; set; }
    }

    public class AssignmentStatusRequest
    {
        public int AssignmentId { get; set; }
        public string? StudentId { get; set; }
    }

    // Response DTOs
    public class AssignmentResponse
    {
        public int AssignmentId { get; set; }
        public int SubjectId { get; set; }
        public string? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? FileUrl { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal? Grade { get; set; }
        public string? GradeComments { get; set; }
        public string? Status { get; set; }
        public bool IsLate { get; set; }
        public int DaysLate { get; set; }
    }

    public class SubmitAssignmentResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public AssignmentResponse? Data { get; set; }
    }

    public class GradeAssignmentResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public AssignmentResponse? Data { get; set; }
    }

    public class AssignmentStatusResponse
    {
        public int AssignmentId { get; set; }
        public string? Status { get; set; }
        public decimal? Grade { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsLate { get; set; }
        public string? GradeComments { get; set; }
    }

    public class AssignmentListResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<AssignmentResponse>? Data { get; set; }
        public int Total { get; set; }
    }
}

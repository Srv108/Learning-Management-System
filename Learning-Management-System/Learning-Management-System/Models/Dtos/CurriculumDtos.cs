using System.ComponentModel.DataAnnotations;

namespace Learning_Management_System.Models.Dtos
{
    // Course DTOs
    public class CreateCourseDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 10)]
        public int Credits { get; set; }
    }

    public class UpdateCourseDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 10)]
        public int Credits { get; set; }

        public string Status { get; set; } = "ACTIVE";
    }

    public class CourseDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int SubjectCount { get; set; }
        public int BatchCount { get; set; }
    }

    // Subject DTOs
    public class CreateSubjectDto
    {
        [Required]
        public long CourseId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateSubjectDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class SubjectDto
    {
        public long Id { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TeacherCount { get; set; }
    }

    // CourseBatch DTOs
    public class CreateCourseBatchDto
    {
        [Required]
        public long CourseId { get; set; }

        [Required]
        [StringLength(100)]
        public string BatchName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    public class UpdateCourseBatchDto
    {
        [StringLength(100)]
        public string? BatchName { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public class CourseBatchDto
    {
        public long Id { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EnrollmentCount { get; set; }
    }

    // SubjectTeacher DTOs
    public class AssignTeacherToSubjectDto
    {
        [Required]
        public long SubjectId { get; set; }

        [Required]
        public string TeacherId { get; set; } = string.Empty;
    }

    public class SubjectTeacherDto
    {
        public long Id { get; set; }
        public long SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}

// Enrollment DTOs
public class CreateEnrollmentDto
{
    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public long BatchId { get; set; }
}

public class UpdateEnrollmentDto
{
    [StringLength(20)]
    public string? Status { get; set; } // ACTIVE, DROPPED, COMPLETED
}

public class SelfEnrollDto
{
    [Required]
    public long BatchId { get; set; }
}

public class EnrollmentDto
{
    public long Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public long BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Attendance DTOs
public class CreateAttendanceSessionDto
{
    [Required]
    public long SubjectId { get; set; }

    [Required]
    public DateTime SessionDate { get; set; }
}

public class UpdateAttendanceSessionDto
{
    [Required]
    public DateTime SessionDate { get; set; }
}

public class AttendanceSessionDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MarkAttendanceDto
{
    [Required]
    public long SessionId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Status { get; set; } = "PRESENT"; // PRESENT, ABSENT
}

public class BulkMarkAttendanceDto
{
    [Required]
    public long SessionId { get; set; }

    [Required]
    public List<AttendanceRecordDto> Records { get; set; } = new List<AttendanceRecordDto>();
}

public class AttendanceRecordDto
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StudentAttendanceSummaryDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendancePercentage { get; set; }
}

// Exam + Result DTOs
public class CreateExamDto
{
    [Required]
    public long SubjectId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ExamType { get; set; } = string.Empty; // "MIdterm", "Final"

    [Range(1, 1000)]
    public int MaxScore { get; set; }

    [Required]
    public DateTime ExamDate { get; set; }
}

public class UpdateExamDto
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(50)]
    public string? ExamType { get; set; }

    [Range(1, 1000)]
    public int? MaxScore { get; set; }

    public DateTime? ExamDate { get; set; }
}

public class ExamDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public int MaxScore { get; set; }
    public DateTime ExamDate { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateExamResultDto
{
    [Required]
    public long ExamId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Range(0, 1000)]
    public int Marks { get; set; }

    [StringLength(10)]
    public string Grade { get; set; } = string.Empty;
}

public class UpdateExamResultDto
{
    [Range(0, 1000)]
    public int? Marks { get; set; }

    [StringLength(10)]
    public string? Grade { get; set; }
}

public class EligibleStudentDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ExamResultDto
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int Marks { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string GradedById { get; set; } = string.Empty;
    public string GradedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StudentExamSummaryDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int ExamsTaken { get; set; }
    public double AverageScore { get; set; }
    public string BestGrade { get; set; } = string.Empty;
}

// Assignment + Grading DTOs
public class CreateAssignmentDto
{
    [Required]
    public long SubjectId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 1000)]
    public int MaxScore { get; set; }

    [Required]
    public DateTime DueDate { get; set; }
}

public class UpdateAssignmentDto
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 1000)]
    public int? MaxScore { get; set; }

    public DateTime? DueDate { get; set; }
}

public class AssignmentDto
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public DateTime DueDate { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateSubmissionDto
{
    [Required]
    public long AssignmentId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? FileUrl { get; set; }
}

public class SubmissionDto
{
    public long Id { get; set; }
    public long AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

public class CreateAssignmentGradeDto
{
    [Required]
    public long SubmissionId { get; set; }

    [Range(0, 1000)]
    public int Score { get; set; }

    [StringLength(1000)]
    public string? Feedback { get; set; }
}

public class AssignmentGradeDto
{
    public long Id { get; set; }
    public long SubmissionId { get; set; }
    public long AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public string GradedById { get; set; } = string.Empty;
    public string GradedByName { get; set; } = string.Empty;
    public DateTime? GradedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
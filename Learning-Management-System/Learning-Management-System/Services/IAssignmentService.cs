using Learning_Management_System.Models;

namespace Learning_Management_System.Services
{
    public interface IAssignmentService
    {
        /// <summary>
        /// Submit an assignment by a student
        /// </summary>
        Task<SubmitAssignmentResponse> SubmitAssignmentAsync(SubmitAssignmentRequest request);

        /// <summary>
        /// Grade a submitted assignment
        /// </summary>
        Task<GradeAssignmentResponse> GradeAssignmentAsync(GradeAssignmentRequest request, string instructorId);

        /// <summary>
        /// Get assignment status for a student
        /// </summary>
        Task<AssignmentStatusResponse> GetAssignmentStatusAsync(int assignmentId, string studentId);

        /// <summary>
        /// Get all assignments for a student
        /// </summary>
        Task<AssignmentListResponse> GetStudentAssignmentsAsync(string studentId, int? subjectId = null);

        /// <summary>
        /// Get all assignments for grading (instructor view)
        /// </summary>
        Task<AssignmentListResponse> GetAssignmentsForGradingAsync(string instructorId, int? subjectId = null);

        /// <summary>
        /// Get assignment by ID
        /// </summary>
        Task<AssignmentResponse?> GetAssignmentByIdAsync(int assignmentId);

        /// <summary>
        /// Delete an assignment (if not graded)
        /// </summary>
        Task<bool> DeleteAssignmentAsync(int assignmentId, string studentId);
    }
}

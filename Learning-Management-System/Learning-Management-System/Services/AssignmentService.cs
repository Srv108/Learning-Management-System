using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssignmentService> _logger;
        private readonly string _uploadDirectory;

        public AssignmentService(ApplicationDbContext context, ILogger<AssignmentService> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "assignments");

            // Create directory if it doesn't exist
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        /// <summary>
        /// Submit an assignment by a student
        /// </summary>
        public async Task<SubmitAssignmentResponse> SubmitAssignmentAsync(SubmitAssignmentRequest request)
        {
            try
            {
                _logger.LogInformation($"Submitting assignment {request.AssignmentId} by student {request.StudentId}");

                // Validate assignment exists
                var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == request.AssignmentId);
                if (assignment == null)
                {
                    return new SubmitAssignmentResponse
                    {
                        Success = false,
                        Message = "Assignment not found"
                    };
                }

                // Validate student
                var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.StudentId);
                if (student == null)
                {
                    return new SubmitAssignmentResponse
                    {
                        Success = false,
                        Message = "Student not found"
                    };
                }

                // Check if already graded
                if (assignment.Status == AssignmentStatus.Graded)
                {
                    return new SubmitAssignmentResponse
                    {
                        Success = false,
                        Message = "This assignment has already been graded and cannot be resubmitted"
                    };
                }

                // Handle file upload
                string? fileUrl = null;
                if (request.SubmissionFile != null)
                {
                    fileUrl = await UploadFileAsync(request.SubmissionFile, request.AssignmentId, request.StudentId);
                    if (string.IsNullOrEmpty(fileUrl))
                    {
                        return new SubmitAssignmentResponse
                        {
                            Success = false,
                            Message = "Failed to upload file"
                        };
                    }
                }

                // Update assignment
                assignment.Title = request.Title ?? assignment.Title;
                assignment.Description = request.Description ?? assignment.Description;
                assignment.FileUrl = fileUrl ?? assignment.FileUrl;
                assignment.SubmissionDate = DateTime.UtcNow;
                assignment.Status = DateTime.UtcNow > assignment.DueDate ? AssignmentStatus.Late : AssignmentStatus.Submitted;
                assignment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Assignment {request.AssignmentId} submitted successfully");

                return new SubmitAssignmentResponse
                {
                    Success = true,
                    Message = "Assignment submitted successfully",
                    Data = MapToAssignmentResponse(assignment, student)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting assignment");
                return new SubmitAssignmentResponse
                {
                    Success = false,
                    Message = $"Error submitting assignment: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Grade a submitted assignment
        /// </summary>
        public async Task<GradeAssignmentResponse> GradeAssignmentAsync(GradeAssignmentRequest request, string instructorId)
        {
            try
            {
                _logger.LogInformation($"Grading assignment {request.AssignmentId} by instructor {instructorId}");

                // Validate assignment exists
                var assignment = await _context.Assignments.Include(a => a.Subject).FirstOrDefaultAsync(a => a.AssignmentId == request.AssignmentId);
                if (assignment == null)
                {
                    return new GradeAssignmentResponse
                    {
                        Success = false,
                        Message = "Assignment not found"
                    };
                }

                // Verify instructor owns the subject
                var subject = assignment.Subject;
                if (subject?.InstructorId != instructorId)
                {
                    return new GradeAssignmentResponse
                    {
                        Success = false,
                        Message = "You don't have permission to grade this assignment"
                    };
                }

                // Validate grade
                if (request.Grade < 0 || request.Grade > 100)
                {
                    return new GradeAssignmentResponse
                    {
                        Success = false,
                        Message = "Grade must be between 0 and 100"
                    };
                }

                // Update assignment
                assignment.Grade = request.Grade;
                assignment.GradeComments = request.Comments;
                assignment.Status = AssignmentStatus.Graded;
                assignment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Assignment {request.AssignmentId} graded with {request.Grade}");

                var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == assignment.StudentId);

                return new GradeAssignmentResponse
                {
                    Success = true,
                    Message = "Assignment graded successfully",
                    Data = MapToAssignmentResponse(assignment, student!)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading assignment");
                return new GradeAssignmentResponse
                {
                    Success = false,
                    Message = $"Error grading assignment: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get assignment status for a student
        /// </summary>
        public async Task<AssignmentStatusResponse> GetAssignmentStatusAsync(int assignmentId, string studentId)
        {
            try
            {
                var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.StudentId == studentId);
                if (assignment == null)
                {
                    return new AssignmentStatusResponse
                    {
                        AssignmentId = assignmentId,
                        Status = "Not Found"
                    };
                }

                var isLate = assignment.SubmissionDate > assignment.DueDate;
                var daysLate = isLate ? (int)(assignment.SubmissionDate - assignment.DueDate).TotalDays : 0;

                return new AssignmentStatusResponse
                {
                    AssignmentId = assignmentId,
                    Status = assignment.Status.ToString(),
                    Grade = assignment.Grade,
                    SubmissionDate = assignment.SubmissionDate,
                    DueDate = assignment.DueDate,
                    IsLate = isLate,
                    GradeComments = assignment.GradeComments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment status");
                return new AssignmentStatusResponse
                {
                    AssignmentId = assignmentId,
                    Status = "Error"
                };
            }
        }

        /// <summary>
        /// Get all assignments for a student
        /// </summary>
        public async Task<AssignmentListResponse> GetStudentAssignmentsAsync(string studentId, int? subjectId = null)
        {
            try
            {
                var query = _context.Assignments
                    .Include(a => a.Student)
                    .Where(a => a.StudentId == studentId);

                if (subjectId.HasValue)
                {
                    query = query.Where(a => a.SubjectId == subjectId);
                }

                var assignments = await query.OrderByDescending(a => a.DueDate).ToListAsync();
                var mappedData = assignments.Select(a => MapToAssignmentResponse(a, a.Student!)).ToList();

                return new AssignmentListResponse
                {
                    Success = true,
                    Message = "Assignments retrieved successfully",
                    Data = mappedData,
                    Total = mappedData.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student assignments");
                return new AssignmentListResponse
                {
                    Success = false,
                    Message = $"Error retrieving assignments: {ex.Message}",
                    Data = new List<AssignmentResponse>(),
                    Total = 0
                };
            }
        }

        /// <summary>
        /// Get all assignments for grading (instructor view)
        /// </summary>
        public async Task<AssignmentListResponse> GetAssignmentsForGradingAsync(string instructorId, int? subjectId = null)
        {
            try
            {
                var query = _context.Assignments
                    .Include(a => a.Subject)
                    .Include(a => a.Student)
                    .Where(a => a.Subject != null && a.Subject.InstructorId == instructorId && a.Status == AssignmentStatus.Submitted);

                if (subjectId.HasValue)
                {
                    query = query.Where(a => a.SubjectId == subjectId);
                }

                var assignments = await query.OrderByDescending(a => a.SubmissionDate).ToListAsync();
                var mappedData = assignments.Select(a => MapToAssignmentResponse(a, a.Student!)).ToList();

                return new AssignmentListResponse
                {
                    Success = true,
                    Message = "Pending assignments for grading retrieved successfully",
                    Data = mappedData,
                    Total = mappedData.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments for grading");
                return new AssignmentListResponse
                {
                    Success = false,
                    Message = $"Error retrieving assignments: {ex.Message}",
                    Data = new List<AssignmentResponse>(),
                    Total = 0
                };
            }
        }

        /// <summary>
        /// Get assignment by ID
        /// </summary>
        public async Task<AssignmentResponse?> GetAssignmentByIdAsync(int assignmentId)
        {
            try
            {
                var assignment = await _context.Assignments.Include(a => a.Student).FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
                return assignment != null ? MapToAssignmentResponse(assignment, assignment.Student!) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment by ID");
                return null;
            }
        }

        /// <summary>
        /// Delete an assignment (if not graded)
        /// </summary>
        public async Task<bool> DeleteAssignmentAsync(int assignmentId, string studentId)
        {
            try
            {
                var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.StudentId == studentId);
                if (assignment == null)
                {
                    _logger.LogWarning($"Assignment {assignmentId} not found for deletion");
                    return false;
                }

                if (assignment.Status == AssignmentStatus.Graded)
                {
                    _logger.LogWarning($"Cannot delete graded assignment {assignmentId}");
                    return false;
                }

                // Delete file if exists
                if (!string.IsNullOrEmpty(assignment.FileUrl))
                {
                    try
                    {
                        var filePath = Path.Combine(_uploadDirectory, assignment.FileUrl);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting file");
                    }
                }

                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Assignment {assignmentId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment");
                return false;
            }
        }

        // Helper Methods
        private async Task<string?> UploadFileAsync(IFormFile file, int assignmentId, string studentId)
        {
            try
            {
                if (file.Length == 0)
                    return null;

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("File size exceeds 10MB limit");
                    return null;
                }

                // Allowed extensions
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".pptx", ".zip" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"File extension {extension} not allowed");
                    return null;
                }

                var fileName = $"{assignmentId}_{studentId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(_uploadDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File uploaded successfully: {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return null;
            }
        }

        private AssignmentResponse MapToAssignmentResponse(Assignment assignment, AppUser student)
        {
            var isLate = assignment.SubmissionDate > assignment.DueDate;
            var daysLate = isLate ? (int)(assignment.SubmissionDate - assignment.DueDate).TotalDays : 0;

            return new AssignmentResponse
            {
                AssignmentId = assignment.AssignmentId,
                SubjectId = assignment.SubjectId,
                StudentId = assignment.StudentId,
                StudentName = student?.FullName ?? student?.UserName,
                Title = assignment.Title,
                Description = assignment.Description,
                FileUrl = assignment.FileUrl,
                SubmissionDate = assignment.SubmissionDate,
                DueDate = assignment.DueDate,
                Grade = assignment.Grade,
                GradeComments = assignment.GradeComments,
                Status = assignment.Status.ToString(),
                IsLate = isLate,
                DaysLate = daysLate
            };
        }
    }
}

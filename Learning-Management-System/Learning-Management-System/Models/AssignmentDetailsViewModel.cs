using Learning_Management_System.Models.Dtos;

namespace Learning_Management_System.Models
{
    public class AssignmentDetailsViewModel
    {
        public AssignmentDto Assignment { get; set; }
        public List<SubmissionDto> Submissions { get; set; } = new();
    }
}

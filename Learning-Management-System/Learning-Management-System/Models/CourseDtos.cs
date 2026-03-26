using System.ComponentModel.DataAnnotations;

namespace Learning_Management_System.Models.AuthDtos
{
    // Course DTOs
    public class CreateCourseDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
    }

    public class UpdateCourseDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ModuleCount { get; set; }
        public int EnrollmentCount { get; set; }
    }

    // Module DTOs
    public class CreateModuleDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int OrderIndex { get; set; }
    }

    public class UpdateModuleDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ModuleDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LessonCount { get; set; }
    }

    // Lesson DTOs
    public class CreateLessonDto
    {
        [Required]
        public int ModuleId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? ContentUrl { get; set; }

        public int? Duration { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdateLessonDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? ContentUrl { get; set; }

        public int? Duration { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LessonDto
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContentUrl { get; set; }
        public int? Duration { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
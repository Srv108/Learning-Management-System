using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Services
{
    public interface ICourseService
    {
        Task<CourseDto> AddCourseAsync(CreateCourseDto courseDto, string userId);
        Task<CourseDto> UpdateCourseAsync(long courseId, UpdateCourseDto courseDto, string userId);
        Task<CourseDto> GetCourseDetailsAsync(long courseId);
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteCourseAsync(long courseId, string userId);
        Task<IEnumerable<SubjectDto>> GetCourseSubjectsAsync(long courseId);
        Task<IEnumerable<CourseBatchDto>> GetCourseBatchesAsync(long courseId);
    }

    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseService> _logger;

        public CourseService(ApplicationDbContext context, ILogger<CourseService> logger)
        {
            _context = context;
            _logger = logger;
        }



        public async Task<CourseDto> AddCourseAsync(CreateCourseDto courseDto, string userId)
        {
            try
            {
                var course = new Course
                {
                    Title = courseDto.Title,
                    Description = courseDto.Description,
                    Credits = courseDto.Credits,
                    CreatedById = userId,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                
                return new CourseDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Credits = course.Credits ?? 0,
                    CreatedById = course.CreatedById,
                    CreatedByName = user?.FullName ?? user?.UserName ?? "",
                    Status = course.Status ?? "ACTIVE",
                    CreatedAt = course.CreatedAt,
                    SubjectCount = 0,
                    BatchCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding course");
                throw;
            }
        }



        public async Task<CourseDto> UpdateCourseAsync(long courseId, UpdateCourseDto courseDto, string userId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Subjects)
                    .Include(c => c.Batches)
                    .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

                if (course == null)
                {
                    throw new InvalidOperationException("Course not found");
                }

                course.Title = courseDto.Title ?? course.Title;
                course.Description = courseDto.Description ?? course.Description;
                course.Credits = courseDto.Credits > 0 ? courseDto.Credits : course.Credits;
                course.Status = courseDto.Status ?? course.Status;
                course.UpdatedAt = DateTime.UtcNow;

                _context.Courses.Update(course);
                await _context.SaveChangesAsync();

                return new CourseDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Credits = course.Credits.HasValue ? course.Credits.Value : 0,
                    CreatedById = course.CreatedById,
                    CreatedByName = course.CreatedBy?.FullName ?? course.CreatedBy?.UserName ?? "",
                    Status = course.Status ?? "ACTIVE",
                    CreatedAt = course.CreatedAt,
                    SubjectCount = course.Subjects.Count(s => !s.IsDeleted),
                    BatchCount = course.Batches.Count(b => !b.IsDeleted)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", courseId);
                throw;
            }
        }



        public async Task<CourseDto> GetCourseDetailsAsync(long courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Subjects.Where(s => !s.IsDeleted))
                    .Include(c => c.Batches.Where(b => !b.IsDeleted))
                    .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

                if (course == null)
                {
                    throw new InvalidOperationException("Course not found");
                }

                return new CourseDto
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Credits = course.Credits ?? 0,
                    CreatedById = course.CreatedById,
                    CreatedByName = course.CreatedBy?.FullName ?? course.CreatedBy?.UserName ?? "",
                    Status = course.Status ?? "ACTIVE",
                    CreatedAt = course.CreatedAt,
                    SubjectCount = course.Subjects.Count,
                    BatchCount = course.Batches.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course details for {CourseId}", courseId);
                throw;
            }
        }



        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.CreatedBy)
                    .Include(c => c.Subjects)
                    .Include(c => c.Batches)
                    .Where(c => !c.IsDeleted && c.Status == "ACTIVE")
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Credits = c.Credits ?? 0,
                    CreatedById = c.CreatedById,
                    CreatedByName = c.CreatedBy?.FullName ?? c.CreatedBy?.UserName ?? "",
                    Status = c.Status ?? "ACTIVE",
                    CreatedAt = c.CreatedAt,
                    SubjectCount = c.Subjects.Count(s => !s.IsDeleted),
                    BatchCount = c.Batches.Count(b => !b.IsDeleted)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all courses");
                throw;
            }
        }



        public async Task<bool> DeleteCourseAsync(long courseId, string userId)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

                if (course == null)
                {
                    return false;
                }

                course.IsDeleted = true;
                course.UpdatedAt = DateTime.UtcNow;

                _context.Courses.Update(course);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                throw;
            }
        }



        public async Task<IEnumerable<SubjectDto>> GetCourseSubjectsAsync(long courseId)
        {
            try
            {
                var subjects = await _context.Subjects
                    .Where(s => s.CourseId == courseId && !s.IsDeleted)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return subjects.Select(s => new SubjectDto
                {
                    Id = s.Id,
                    CourseId = s.CourseId,
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subjects for course {CourseId}", courseId);
                throw;
            }
        }



        public async Task<IEnumerable<CourseBatchDto>> GetCourseBatchesAsync(long courseId)
        {
            try
            {
                var batches = await _context.CourseBatches
                    .Where(b => b.CourseId == courseId && !b.IsDeleted)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                return batches.Select(b => new CourseBatchDto
                {
                    Id = b.Id,
                    CourseId = b.CourseId,
                    BatchName = b.BatchName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    CreatedAt = b.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching batches for course {CourseId}", courseId);
                throw;
            }
        }
    }
}


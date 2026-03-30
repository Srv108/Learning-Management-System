using Learning_Management_System.Data;
using Learning_Management_System.Models;
using Learning_Management_System.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Learning_Management_System.Controllers
{
    public class ExamMvcController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public ExamMvcController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var exams = await _dbContext.Exams
                .Where(e => !e.IsDeleted)
                .Include(e => e.Subject)
                .Include(e => e.CreatedBy)
                .OrderBy(e => e.ExamDate)
                .Select(e => new ExamDto
                {
                    Id = e.Id,
                    SubjectId = e.SubjectId,
                    SubjectName = e.Subject != null ? e.Subject.Name : string.Empty,
                    Title = e.Title,
                    ExamType = e.ExamType,
                    MaxScore = e.MaxScore,
                    ExamDate = e.ExamDate,
                    CreatedByName = e.CreatedBy != null ? e.CreatedBy.FullName : string.Empty,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return View(exams);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Subjects = await _dbContext.Subjects
                .Where(s => !s.IsDeleted)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            return View(new CreateExamDto { ExamDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create(CreateExamDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = await _dbContext.Subjects
                    .Where(s => !s.IsDeleted)
                    .Select(s => new { s.Id, s.Name })
                    .ToListAsync();
                return View(dto);
            }

            // Get the current user's ID from session
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;

            var exam = new Exam
            {
                SubjectId = dto.SubjectId,
                Title = dto.Title,
                ExamType = dto.ExamType,
                MaxScore = dto.MaxScore,
                ExamDate = dto.ExamDate,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.Exams.Add(exam);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Results(long examId)
        {
            var exam = await _dbContext.Exams
                .Where(e => !e.IsDeleted && e.Id == examId)
                .Include(e => e.Subject)
                .FirstOrDefaultAsync();

            if (exam == null)
                return NotFound();

            var results = await _dbContext.ExamResults
                .Where(r => !r.IsDeleted && r.ExamId == examId)
                .Include(r => r.Student)
                .ToListAsync();

            ViewBag.Exam = exam;
            ViewBag.Students = await _dbContext.Users
                .Where(u => !u.LockoutEnabled && _dbContext.UserRoles.Any(ur => ur.UserId == u.Id && _dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Student")))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            return View(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResult(CreateExamResultDto dto)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Results), new { examId = dto.ExamId });

            var exists = await _dbContext.ExamResults
                .AnyAsync(r => !r.IsDeleted && r.ExamId == dto.ExamId && r.StudentId == dto.StudentId);

            if (exists)
            {
                TempData["Message"] = "Result already exists for this student and exam.";
                return RedirectToAction(nameof(Results), new { examId = dto.ExamId });
            }

            var result = new ExamResult
            {
                ExamId = dto.ExamId,
                StudentId = dto.StudentId,
                Marks = dto.Marks,
                Grade = dto.Grade,
                GradedById = HttpContext.Session.GetString("UserId") ?? string.Empty,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ExamResults.Add(result);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Results), new { examId = dto.ExamId });
        }
    }
}

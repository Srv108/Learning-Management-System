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
            var examsQuery = _dbContext.Exams.AsQueryable();

            // If current user is a student, collect their enrolled exam IDs so the UI can show enrolled state
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;

            var exams = await examsQuery
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

            // If user is a student, mark which exams they're enrolled in
            if (!string.IsNullOrEmpty(userId))
            {
                var registeredExamIds = await _dbContext.Set<Models.ExamRegistration>()
                    .Where(r => !r.IsDeleted && r.StudentId == userId)
                    .Select(r => r.ExamId)
                    .ToListAsync();

                foreach (var ex in exams)
                {
                    ex.IsEnrolled = registeredExamIds.Contains(ex.Id);
                }
            }

            // If current user is a student, collect their enrolled exam IDs so the UI can show enrolled state
            var enrolledIds = new List<long>();
            try
            {
                if (User.IsInRole("Student"))
                {
                    var sessionUserId = HttpContext.Session.GetString("UserId") ?? string.Empty;
                    if (!string.IsNullOrEmpty(sessionUserId))
                    {
                        enrolledIds = await _dbContext.Set<Models.ExamRegistration>()
                            .Where(r => !r.IsDeleted && r.StudentId == sessionUserId)
                            .Select(r => r.ExamId)
                            .ToListAsync();
                    }
                }
            }
            catch
            {
                // ignore session/role issues; enrolledIds remains empty
            }

            ViewBag.EnrolledExamIds = enrolledIds;
            return View(exams);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(long examId)
        {
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Message"] = "Unable to determine current user.";
                return RedirectToAction(nameof(Index));
            }

            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == examId);
            if (exam == null)
            {
                TempData["Message"] = "Exam not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check for existing registration regardless of IsDeleted to avoid unique index conflicts
            var existing = await _dbContext.Set<Models.ExamRegistration>()
                .FirstOrDefaultAsync(r => r.ExamId == examId && r.StudentId == userId);

            if (existing != null)
            {
                if (!existing.IsDeleted)
                {
                    TempData["Message"] = "You are already registered for this exam.";
                    return RedirectToAction(nameof(Index));
                }

                // Restore soft-deleted registration instead of inserting a duplicate
                existing.IsDeleted = false;
                existing.Status = "REGISTERED";
                existing.UpdatedAt = DateTime.UtcNow;
                _dbContext.Update(existing);
                await _dbContext.SaveChangesAsync();

                TempData["Message"] = "Your previous registration has been restored.";
                return RedirectToAction(nameof(MyEnrollments));
            }

            var reg = new Models.ExamRegistration
            {
                ExamId = examId,
                StudentId = userId,
                Status = "REGISTERED",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.Add(reg);
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Successfully registered for the exam.";
            return RedirectToAction(nameof(MyEnrollments));
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyEnrollments()
        {
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "AuthMvc");
            }

            var regs = await _dbContext.Set<Models.ExamRegistration>()
                .Where(r => !r.IsDeleted && r.StudentId == userId)
                .Include(r => r.Exam)!.ThenInclude(e => e!.Subject)
                .Include(r => r.Exam)!.ThenInclude(e => e!.CreatedBy)
                .Select(r => new Learning_Management_System.Models.Dtos.ExamRegistrationDto
                {
                    Id = r.Id,
                    ExamId = r.ExamId,
                    Title = r.Exam != null ? r.Exam.Title : string.Empty,
                    SubjectName = r.Exam != null && r.Exam.Subject != null ? r.Exam.Subject.Name : string.Empty,
                    ExamDate = r.Exam != null ? r.Exam.ExamDate : DateTime.MinValue,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    MaxScore = r.Exam != null ? r.Exam.MaxScore : 0,
                    CreatedBy = r.Exam != null && r.Exam.CreatedBy != null ? r.Exam.CreatedBy.FullName : string.Empty
                })
                .OrderBy(r => r.ExamDate)
                .ToListAsync();

            return View(regs);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RegistrationDetails(long id)
        {
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "AuthMvc");

            var reg = await _dbContext.Set<Models.ExamRegistration>()
                .Where(r => !r.IsDeleted && r.Id == id && r.StudentId == userId)
                .Include(r => r.Exam)!.ThenInclude(e => e!.Subject)
                .Include(r => r.Exam)!.ThenInclude(e => e!.CreatedBy)
                .Select(r => new Learning_Management_System.Models.Dtos.ExamRegistrationDto
                {
                    Id = r.Id,
                    ExamId = r.ExamId,
                    Title = r.Exam != null ? r.Exam.Title : string.Empty,
                    SubjectName = r.Exam != null && r.Exam.Subject != null ? r.Exam.Subject.Name : string.Empty,
                    ExamDate = r.Exam != null ? r.Exam.ExamDate : DateTime.MinValue,
                    MaxScore = r.Exam != null ? r.Exam.MaxScore : 0,
                    CreatedBy = r.Exam != null && r.Exam.CreatedBy != null ? r.Exam.CreatedBy.FullName : string.Empty,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (reg == null)
                return NotFound();

            return View(reg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Unenroll(long registrationId)
        {
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Message"] = "Unable to determine current user.";
                return RedirectToAction(nameof(MyEnrollments));
            }

            var reg = await _dbContext.Set<Models.ExamRegistration>()
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == registrationId && r.StudentId == userId);

            if (reg == null)
            {
                TempData["Message"] = "Registration not found.";
                return RedirectToAction(nameof(MyEnrollments));
            }

            reg.IsDeleted = true;
            reg.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Your enrollment has been cancelled.";
            return RedirectToAction(nameof(MyEnrollments));
        }

        [Authorize(Roles = "ExamController")]
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
        [Authorize(Roles = "ExamController")]
        public async Task<IActionResult> Create(CreateExamDto dto)
        {
            // Allow manual subject/exam type inputs; if ModelState is invalid but the user provided manual inputs, continue.
            var formForValidation = HttpContext.Request.Form;
            var manualExamTypeForValidation = formForValidation.ContainsKey("ManualExamType") ? formForValidation["ManualExamType"].ToString().Trim() : string.Empty;
            var manualSubjectNameForValidation = formForValidation.ContainsKey("ManualSubjectName") ? formForValidation["ManualSubjectName"].ToString().Trim() : string.Empty;

            if (!ModelState.IsValid && string.IsNullOrEmpty(manualExamTypeForValidation) && string.IsNullOrEmpty(manualSubjectNameForValidation))
            {
                ViewBag.Subjects = await _dbContext.Subjects
                    .Where(s => !s.IsDeleted)
                    .Select(s => new { s.Id, s.Name })
                    .ToListAsync();
                return View(dto);
            }

            // Get the current user's ID from session
            var userId = HttpContext.Session.GetString("UserId") ?? string.Empty;

            // Allow manual exam type and manual subject name from the form
            var form = HttpContext.Request.Form;
            var manualExamType = form.ContainsKey("ManualExamType") ? form["ManualExamType"].ToString().Trim() : string.Empty;
            var manualSubjectName = form.ContainsKey("ManualSubjectName") ? form["ManualSubjectName"].ToString().Trim() : string.Empty;

            long subjectIdToUse = dto.SubjectId;

            if (!string.IsNullOrEmpty(manualSubjectName))
            {
                // Try find existing subject by name
                var existingSubject = await _dbContext.Subjects
                    .FirstOrDefaultAsync(s => !s.IsDeleted && s.Name.ToLower() == manualSubjectName.ToLower());

                if (existingSubject != null)
                {
                    subjectIdToUse = existingSubject.Id;
                }
                else
                {
                    // Find a default course to attach the new subject to
                    var defaultCourse = await _dbContext.Courses.FirstOrDefaultAsync(c => !c.IsDeleted);
                    if (defaultCourse == null)
                    {
                        TempData["Message"] = "No course available to attach the new subject. Create a course first or select an existing subject.";
                        ViewBag.Subjects = await _dbContext.Subjects
                            .Where(s => !s.IsDeleted)
                            .Select(s => new { s.Id, s.Name })
                            .ToListAsync();
                        return View(dto);
                    }

                    var newSubject = new Models.Subject
                    {
                        CourseId = defaultCourse.Id,
                        Name = manualSubjectName,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _dbContext.Subjects.Add(newSubject);
                    await _dbContext.SaveChangesAsync();

                    subjectIdToUse = newSubject.Id;
                }
            }

            var examTypeToUse = !string.IsNullOrEmpty(manualExamType) ? manualExamType : dto.ExamType;

            var exam = new Exam
            {
                SubjectId = subjectIdToUse,
                Title = dto.Title,
                ExamType = examTypeToUse,
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

        [Authorize(Roles = "ExamController")]
        public async Task<IActionResult> Edit(long id)
        {
            var exam = await _dbContext.Exams
                .Where(e => !e.IsDeleted && e.Id == id)
                .Include(e => e.Subject)
                .FirstOrDefaultAsync();
            if (exam == null)
                return NotFound();

            ViewBag.Subjects = await _dbContext.Subjects
                .Where(s => !s.IsDeleted)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            var dto = new CreateExamDto
            {
                SubjectId = exam.SubjectId,
                Title = exam.Title,
                ExamType = exam.ExamType,
                MaxScore = exam.MaxScore,
                ExamDate = exam.ExamDate
            };

            // Prefill manual inputs so the Create/Edit form (which uses manual text inputs)
            // will submit the existing subject name and exam type when editing.
            ViewBag.ManualSubjectName = exam.Subject != null ? exam.Subject.Name : string.Empty;
            ViewBag.ManualExamType = exam.ExamType ?? string.Empty;

            ViewBag.ExamId = exam.Id;
            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ExamController")]
        public async Task<IActionResult> Edit(long id, CreateExamDto dto)
        {
            var formForValidation = HttpContext.Request.Form;
            var manualExamTypeForValidation = formForValidation.ContainsKey("ManualExamType") ? formForValidation["ManualExamType"].ToString().Trim() : string.Empty;
            var manualSubjectNameForValidation = formForValidation.ContainsKey("ManualSubjectName") ? formForValidation["ManualSubjectName"].ToString().Trim() : string.Empty;

            if (!ModelState.IsValid && string.IsNullOrEmpty(manualExamTypeForValidation) && string.IsNullOrEmpty(manualSubjectNameForValidation))
            {
                ViewBag.Subjects = await _dbContext.Subjects
                    .Where(s => !s.IsDeleted)
                    .Select(s => new { s.Id, s.Name })
                    .ToListAsync();
                ViewBag.ExamId = id;
                return View("Create", dto);
            }

            // Allow manual exam type and manual subject name from the form (same behavior as Create)
            var form = HttpContext.Request.Form;
            var manualExamType = form.ContainsKey("ManualExamType") ? form["ManualExamType"].ToString().Trim() : string.Empty;
            var manualSubjectName = form.ContainsKey("ManualSubjectName") ? form["ManualSubjectName"].ToString().Trim() : string.Empty;

            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == id);
            if (exam == null)
                return NotFound();

            long subjectIdToUse = dto.SubjectId;

            if (!string.IsNullOrEmpty(manualSubjectName))
            {
                var existingSubject = await _dbContext.Subjects
                    .FirstOrDefaultAsync(s => !s.IsDeleted && s.Name.ToLower() == manualSubjectName.ToLower());

                if (existingSubject != null)
                {
                    subjectIdToUse = existingSubject.Id;
                }
                else
                {
                    var defaultCourse = await _dbContext.Courses.FirstOrDefaultAsync(c => !c.IsDeleted);
                    if (defaultCourse == null)
                    {
                        TempData["Message"] = "No course available to attach the new subject. Create a course first or select an existing subject.";
                        ViewBag.Subjects = await _dbContext.Subjects
                            .Where(s => !s.IsDeleted)
                            .Select(s => new { s.Id, s.Name })
                            .ToListAsync();
                        ViewBag.ExamId = id;
                        return View("Create", dto);
                    }

                    var newSubject = new Models.Subject
                    {
                        CourseId = defaultCourse.Id,
                        Name = manualSubjectName,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _dbContext.Subjects.Add(newSubject);
                    await _dbContext.SaveChangesAsync();

                    subjectIdToUse = newSubject.Id;
                }
            }

            var examTypeToUse = !string.IsNullOrEmpty(manualExamType) ? manualExamType : dto.ExamType;

            exam.SubjectId = subjectIdToUse;
            exam.Title = dto.Title;
            exam.ExamType = examTypeToUse;
            exam.MaxScore = dto.MaxScore;
            exam.ExamDate = dto.ExamDate;
            exam.UpdatedAt = DateTime.UtcNow;

            _dbContext.Exams.Update(exam);
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ExamController")]
        public async Task<IActionResult> Delete(long id)
        {
            var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == id);
            if (exam == null)
            {
                TempData["Message"] = "Exam not found.";
                return RedirectToAction(nameof(Index));
            }

            exam.IsDeleted = true;
            exam.UpdatedAt = DateTime.UtcNow;

            // Cancel registrations
            var regs = await _dbContext.Set<Models.ExamRegistration>()
                .Where(r => !r.IsDeleted && r.ExamId == id)
                .ToListAsync();

            foreach (var r in regs)
            {
                r.IsDeleted = true;
                r.Status = "CANCELLED";
                r.UpdatedAt = DateTime.UtcNow;
            }

            _dbContext.Exams.Update(exam);
            if (regs.Any()) _dbContext.Set<Models.ExamRegistration>().UpdateRange(regs);
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Exam deleted and registrations cancelled.";
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

            // Registered students for this exam
            var registered = await _dbContext.Set<Models.ExamRegistration>()
                .Where(r => !r.IsDeleted && r.ExamId == examId)
                .Include(r => r.Student)
                .Select(r => new { Id = r.Student!.Id, FullName = r.Student!.FullName, Email = r.Student!.Email })
                .ToListAsync();

            // Find attendance sessions on the exam date for the subject
            var start = exam.ExamDate.Date;
            var end = start.AddDays(1);

            var sessionIds = await _dbContext.AttendanceSessions
                .Where(s => !s.IsDeleted && s.SubjectId == exam.SubjectId && s.SessionDate >= start && s.SessionDate < end)
                .Select(s => s.Id)
                .ToListAsync();

            List<dynamic> students;
            if (sessionIds.Any())
            {
                var attendeeIds = await _dbContext.AttendanceRecords
                    .Where(ar => !ar.IsDeleted && sessionIds.Contains(ar.SessionId) && ar.Status == "PRESENT")
                    .Select(ar => ar.StudentId)
                    .Distinct()
                    .ToListAsync();

                students = registered.Where(r => attendeeIds.Contains(r.Id)).ToList<dynamic>();

                if (!students.Any())
                    students = registered.Cast<dynamic>().ToList();
            }
            else
            {
                students = registered.Cast<dynamic>().ToList();
            }

            ViewBag.Exam = exam;
            ViewBag.Students = students;

            return View(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
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

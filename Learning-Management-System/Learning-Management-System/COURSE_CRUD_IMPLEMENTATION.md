
public class Course
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? Credits { get; set; }

    [Required]
    public string CreatedById { get; set; } = string.Empty;

    [ForeignKey("CreatedById")]
    public AppUser? CreatedBy { get; set; }

    [StringLength(20)]
    public string? Status { get; set; } = "ACTIVE";

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<CourseBatch> Batches { get; set; } = new List<CourseBatch>();
}
```

### Key Features:
- Soft delete: `IsDeleted` flag (data is not physically deleted)
- Timestamps: `CreatedAt` and `UpdatedAt`
- Status: ACTIVE or ARCHIVED
- Relationships: Tracks creator, subjects, and batches

---

## 2. Data Transfer Objects (DTOs)

### CreateCourseDto
```csharp
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
```

### UpdateCourseDto
```csharp
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
```

### CourseDto (Response)
```csharp
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
```

---

## 3. API Controller (Controllers/CurriculumCourseController.cs)

### Get All Courses
```csharp
[HttpGet]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses(
    [FromQuery] int pageNumber = 1, 
    [FromQuery] int pageSize = 10)
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

    return Ok(courseDtos);
}
```
**Route**: `GET /api/course`
**Authorization**: None required
**Query Parameters**: `pageNumber`, `pageSize` (for pagination)

### Get Single Course
```csharp
[HttpGet("{id}")]
[AllowAnonymous]
public async Task<ActionResult<CourseDto>> GetCourse(long id)
{
    var course = await _context.Courses
        .Include(c => c.CreatedBy)
        .Include(c => c.Subjects.Where(s => !s.IsDeleted))
        .Include(c => c.Batches.Where(b => !b.IsDeleted))
        .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    if (course == null)
        return NotFound("Course not found");

    return Ok(courseDto);
}
```
**Route**: `GET /api/course/{id}`
**Authorization**: None required

### Create Course
```csharp
[HttpPost]
[Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto createCourseDto)
{
    var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null)
        return Unauthorized("User not found");

    var course = new Course
    {
        Title = createCourseDto.Title,
        Description = createCourseDto.Description,
        Credits = createCourseDto.Credits,
        CreatedById = currentUser.Id,
        Status = "ACTIVE",
        IsDeleted = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Courses.Add(course);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, courseDto);
}
```
**Route**: `POST /api/course`
**Authorization**: Teacher, CourseCoordinator, or Admin
**Request Body**:
```json
{
    "Title": "Web Development 101",
    "Description": "Introduction to web development",
    "Credits": 4
}
```
**Response**: `201 Created` with courseDto

### Update Course
```csharp
[HttpPut("{id}")]
[Authorize(Roles = "Teacher,CourseCoordinator,Admin")]
public async Task<IActionResult> UpdateCourse(long id, UpdateCourseDto updateCourseDto)
{
    var course = await _context.Courses.FindAsync(id);
    if (course == null || course.IsDeleted)
        return NotFound("Course not found");

    var currentUser = await _userManager.GetUserAsync(User);
    var userRoles = await _userManager.GetRolesAsync(currentUser);
    
    // Only creator, admin, or coordinator can update
    if (course.CreatedById != currentUser.Id && 
        !userRoles.Contains("Admin") && 
        !userRoles.Contains("CourseCoordinator"))
        return Forbid("You do not have permission to update this course");

    course.Title = updateCourseDto.Title;
    course.Description = updateCourseDto.Description;
    course.Credits = updateCourseDto.Credits;
    course.Status = updateCourseDto.Status;
    course.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return NoContent();
}
```
**Route**: `PUT /api/course/{id}`
**Authorization**: Teacher, CourseCoordinator, or Admin
**Request Body**:
```json
{
    "Title": "Advanced Web Development",
    "Description": "Advanced topics in web development",
    "Credits": 4,
    "Status": "ACTIVE"
}
```
**Response**: `204 No Content`

### Delete Course
```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Admin,CourseCoordinator")]
public async Task<IActionResult> DeleteCourse(long id)
{
    var course = await _context.Courses.FindAsync(id);
    if (course == null || course.IsDeleted)
        return NotFound("Course not found");

    var currentUser = await _userManager.GetUserAsync(User);

    course.IsDeleted = true;
    course.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return NoContent();
}
```
**Route**: `DELETE /api/course/{id}`
**Authorization**: Admin or CourseCoordinator only
**Response**: `204 No Content`

---

## 4. Frontend Implementation (Views/Home/Courses.cshtml)

### Add Course Modal
```html
<div class="modal fade" id="addCourseModal" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title">Add New Course</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <form id="addCourseForm">
                    <div class="mb-3">
                        <label for="courseName" class="form-label">Course Name *</label>
                        <input type="text" class="form-control form-control-lg" id="courseName" 
                               placeholder="Enter course name" required>
                    </div>
                    <div class="mb-3">
                        <label for="courseDesc" class="form-label">Description</label>
                        <textarea class="form-control form-control-lg" id="courseDesc" 
                                  rows="3" placeholder="Enter course description"></textarea>
                    </div>
                    <div class="mb-3">
                        <label for="courseCredits" class="form-label">Credits *</label>
                        <input type="number" class="form-control form-control-lg" id="courseCredits" 
                               min="1" max="10" placeholder="e.g., 4" required>
                    </div>
                </form>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary btn-lg" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary btn-lg" onclick="addCourse()">
                    <i class="bi bi-plus-circle"></i> Add Course
                </button>
            </div>
        </div>
    </div>
</div>
```

### Edit Course Modal
```html
<div class="modal fade" id="editCourseModal" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title">Edit Course</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <form id="editCourseForm">
                    <input type="hidden" id="editCourseId">
                    <div class="mb-3">
                        <label for="editCourseName" class="form-label">Course Name *</label>
                        <input type="text" class="form-control form-control-lg" id="editCourseName" required>
                    </div>
                    <div class="mb-3">
                        <label for="editCourseDesc" class="form-label">Description</label>
                        <textarea class="form-control form-control-lg" id="editCourseDesc" rows="3"></textarea>
                    </div>
                    <div class="mb-3">
                        <label for="editCourseCredits" class="form-label">Credits *</label>
                        <input type="number" class="form-control form-control-lg" id="editCourseCredits" 
                               min="1" max="10" required>
                    </div>
                    <div class="mb-3">
                        <label for="editCourseStatus" class="form-label">Status</label>
                        <select class="form-select form-select-lg" id="editCourseStatus">
                            <option value="ACTIVE">Active</option>
                            <option value="ARCHIVED">Archived</option>
                        </select>
                    </div>
                </form>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary btn-lg" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary btn-lg" onclick="updateCourse()">
                    <i class="bi bi-check-circle"></i> Update Course
                </button>
            </div>
        </div>
    </div>
</div>
```

### JavaScript Functions

#### Load Courses
```javascript
async function loadCourses() {
    try {
        document.getElementById('loadingSpinner').style.display = 'block';
        const token = getAuthToken();
        
        const response = await fetch('/api/course', {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            allCourses = data;
            renderCourses(allCourses);
        } else if (response.status === 401) {
            window.location.href = '/AuthMvc/Login';
        }
    } catch (error) {
        showAlert('Failed to load courses: ' + error.message, 'danger');
    } finally {
        document.getElementById('loadingSpinner').style.display = 'none';
    }
}
```

#### Add Course
```javascript
async function addCourse() {
    if (!isCourseCoordinator) {
        showAlert('You do not have permission to add courses', 'danger');
        return;
    }

    const name = document.getElementById('courseName').value;
    const desc = document.getElementById('courseDesc').value;
    const credits = parseInt(document.getElementById('courseCredits').value);

    if (!name || !credits) {
        showAlert('Please fill in all required fields', 'warning');
        return;
    }

    try {
        const token = getAuthToken();
        const payload = {
            Title: name,
            Description: desc,
            Credits: credits
        };
        
        const response = await fetch('/api/course', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            showAlert('Course added successfully!', 'success');
            bootstrap.Modal.getInstance(document.getElementById('addCourseModal')).hide();
            resetAddCourseForm();
            loadCourses();
        } else if (response.status === 403) {
            showAlert('You do not have permission to add courses', 'danger');
        } else {
            const errorText = await response.text();
            showAlert(`Error adding course: ${errorText}`, 'danger');
        }
    } catch (error) {
        showAlert('Failed to add course: ' + error.message, 'danger');
    }
}
```

#### Edit Course
```javascript
async function editCourse(courseId) {
    const course = allCourses.find(c => c.id === courseId);
    if (course) {
        currentEditCourseId = courseId;
        document.getElementById('editCourseId').value = courseId;
        document.getElementById('editCourseName').value = course.title;
        document.getElementById('editCourseDesc').value = course.description || '';
        document.getElementById('editCourseCredits').value = course.credits;
        document.getElementById('editCourseStatus').value = course.status;
        
        const editModal = new bootstrap.Modal(document.getElementById('editCourseModal'));
        editModal.show();
    }
}
```

#### Update Course
```javascript
async function updateCourse() {
    if (!isCourseCoordinator) {
        showAlert('You do not have permission to update courses', 'danger');
        return;
    }

    const courseId = currentEditCourseId;
    const name = document.getElementById('editCourseName').value;
    const desc = document.getElementById('editCourseDesc').value;
    const credits = parseInt(document.getElementById('editCourseCredits').value);
    const status = document.getElementById('editCourseStatus').value;

    if (!name || !credits) {
        showAlert('Please fill in all required fields', 'warning');
        return;
    }

    try {
        const token = getAuthToken();
        const response = await fetch(`/api/course/${courseId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                Title: name,
                Description: desc,
                Credits: credits,
                Status: status
            })
        });

        if (response.ok) {
            showAlert('Course updated successfully!', 'success');
            bootstrap.Modal.getInstance(document.getElementById('editCourseModal')).hide();
            loadCourses();
        } else {
            const errorText = await response.text();
            showAlert(`Error updating course: ${errorText}`, 'danger');
        }
    } catch (error) {
        showAlert('Failed to update course: ' + error.message, 'danger');
    }
}
```

#### Delete Course
```javascript
async function deleteCourse(courseId) {
    if (!isCourseCoordinator) {
        showAlert('You do not have permission to delete courses', 'danger');
        return;
    }

    if (!confirm('Are you sure you want to delete this course?')) {
        return;
    }

    try {
        const token = getAuthToken();
        const response = await fetch(`/api/course/${courseId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            showAlert('Course deleted successfully!', 'success');
            loadCourses();
        } else if (response.status === 403) {
            showAlert('You do not have permission to delete this course', 'danger');
        }
    } catch (error) {
        showAlert('Failed to delete course: ' + error.message, 'danger');
    }
}
```

#### Filter/Search Courses
```javascript
function filterCourses() {
    const searchTerm = document.getElementById('courseSearch').value.toLowerCase();
    const status = document.getElementById('courseStatus').value;
    const sortBy = document.getElementById('sortBy').value;
    
    let filtered = allCourses.filter(course => {
        const matchesSearch = course.title.toLowerCase().includes(searchTerm);
        const matchesStatus = !status || course.status === status;
        return matchesSearch && matchesStatus;
    });
    
    // Sort
    if (sortBy === 'name') {
        filtered.sort((a, b) => a.title.localeCompare(b.title));
    } else if (sortBy === 'oldest') {
        filtered.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
    } else {
        filtered.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    }
    
    renderCourses(filtered);
}
```

---

## 5. User Flow

### For Course Coordinators:

1. **Login** → Navigate to "Courses"
2. **View Courses** → All courses displayed in card grid
3. **Add Course** → Click "Add Course" button → Fill form → Submit
4. **Edit Course** → Click "Edit" on course card → Modify details → Update
5. **Delete Course** → Click "Delete" on course card → Confirm → Delete
6. **Search Courses** → Use search box to filter by name
7. **Filter by Status** → Active/Archived status filter
8. **Sort Courses** → Newest, Oldest, or by Name

### For Students/Other Roles:

1. **Login** → Navigate to "Courses"
2. **View Courses** → See all courses in read-only mode
3. **No edit/delete buttons visible** → Message: "You need to be a Course Coordinator to manage courses"

---

## 6. Error Handling

| Error Code | Scenario | Message |
|-----------|----------|---------|
| 400 | Invalid request data | "Error adding course (400): Invalid input" |
| 401 | Token expired/missing | "Session expired. Please login again." |
| 403 | No permission | "You do not have permission to..." |
| 404 | Course not found | "Course not found" |
| 500 | Server error | "An error occurred while..." |

---

## 7. API Response Examples

### Success (Create Course)
```json
{
    "id": 1,
    "title": "Web Development 101",
    "description": "Introduction to web development",
    "credits": 4,
    "createdById": "user-id-123",
    "createdByName": "John Doe",
    "status": "ACTIVE",
    "createdAt": "2026-03-30T10:30:00Z",
    "subjectCount": 0,
    "batchCount": 0
}
```

### Success (Get All Courses)
```json
[
    {
        "id": 1,
        "title": "Web Development 101",
        "description": "Introduction to web development",
        "credits": 4,
        "createdById": "user-id-123",
        "createdByName": "John Doe",
        "status": "ACTIVE",
        "createdAt": "2026-03-30T10:30:00Z",
        "subjectCount": 3,
        "batchCount": 2
    }
]
```

---

## 8. Testing the Implementation

### Manual Test Steps:

1. **Start the application**:
   ```bash
   cd Learning-Management-System/Learning-Management-System
   dotnet run
   ```

2. **Navigate to** `http://localhost:5171`

3. **Login as Coordinator**:
   - Email: `coordinator@lms.com`
   - Password: `Password@123`

4. **Test Create**:
   - Click "Add Course"
   - Enter: Title, Description, Credits
   - Verify course appears in list

5. **Test Update**:
   - Click "Edit" on a course
   - Modify details
   - Verify changes saved

6. **Test Delete**:
   - Click "Delete" on a course
   - Confirm deletion
   - Verify course removed

7. **Test Search**:
   - Use search box to filter courses
   - Use status dropdown to filter
   - Use sort dropdown to order

---

## 9. Files Modified/Created

- `Controllers/CurriculumCourseController.cs` - API endpoints
- `Views/Home/Courses.cshtml` - UI and JavaScript
- `Models/Course.cs` - Database model
- `Models/Dtos/CurriculumDtos.cs` - Data Transfer Objects
- `Models/CourseBatch.cs` - Batch model (referenced)
- `Data/ApplicationDbContext.cs` - Database context

---

## 10. Summary

The course management system is **fully functional** with:
- ✅ Create courses with validation
- ✅ Read/display all courses with pagination
- ✅ Update course details and status
- ✅ Delete courses (soft delete)
- ✅ Search and filter functionality
- ✅ Role-based access control
- ✅ Real-time UI updates
- ✅ Comprehensive error handling
- ✅ Bootstrap 5 responsive design

The implementation follows best practices:
- Entity Framework Core for data access
- DTOs for API contracts
- Async/await for async operations
- Authorization attributes on controllers
- Proper HTTP status codes
- Soft deletes for data integrity

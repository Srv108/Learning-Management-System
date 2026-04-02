# Code Examples & Integration Guide

## 1. How to Access the Course Details Page

### From the Courses Page
```html
<!-- In Views/Home/Courses.cshtml -->
<a href="/Home/CourseDetails?courseId=${course.id}" class="btn btn-sm btn-primary">
    <i class="bi bi-arrow-right-circle"></i> Go to Course
</a>
```

### Direct URL
```
http://localhost:5000/Home/CourseDetails?courseId=1
```

### C# Backend
```csharp
// In Controllers/HomeController.cs
public IActionResult CourseDetails()
{
    var userEmail = HttpContext.Session.GetString("UserEmail");
    var jwtToken = HttpContext.Session.GetString("JwtToken");
    var userRole = HttpContext.Session.GetString("UserRole") ?? "Student";
    
    if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(jwtToken))
    {
        ViewBag.UserEmail = userEmail;
        ViewBag.UserRole = userRole;
        return View();
    }
    
    return RedirectToAction("Login", "AuthMvc");
}
```

---

## 2. Course Details Page - Core Sections

### A. Get Course ID from URL
```javascript
function getCourseIdFromUrl() {
    const params = new URLSearchParams(window.location.search);
    return params.get('courseId');
}

// Usage
document.addEventListener('DOMContentLoaded', function() {
    currentCourseId = getCourseIdFromUrl();
    if (!currentCourseId) {
        showError('No course selected. Please select a course from the courses page.');
        return;
    }
    loadCourseDetails();
});
```

### B. Load Course Details from API
```javascript
async function loadCourseDetails() {
    try {
        document.getElementById('loadingSpinner').style.display = 'block';
        
        const response = await fetch(`/api/course/${currentCourseId}`, {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            currentCourse = data;
            renderCourseDetails(data);
            loadSubjects();
            loadBatches();
        } else if (response.status === 404) {
            showError('Course not found.');
        } else {
            showError('Failed to load course details.');
        }
    } catch (error) {
        console.error('Error loading course:', error);
        showError('An error occurred while loading the course.');
    } finally {
        document.getElementById('loadingSpinner').style.display = 'none';
    }
}
```

### C. Render Course Information
```javascript
function renderCourseDetails(course) {
    // Update page title
    document.getElementById('courseTitle').textContent = course.title || 'Course Details';
    
    // Display course information
    document.getElementById('courseCode').textContent = course.id;
    document.getElementById('courseCredits').textContent = course.credits + ' Credits';
    document.getElementById('courseDesc').textContent = course.description || 'No description provided';
    
    // Show status badge
    document.getElementById('courseStatus').innerHTML = 
        `<span class="badge bg-${course.status === 'ACTIVE' ? 'success' : 'secondary'}">
            ${course.status}
        </span>`;
    
    // Display dates
    document.getElementById('courseDate').textContent = 
        new Date(course.createdAt).toLocaleDateString();
    document.getElementById('timelineCreated').textContent = 
        new Date(course.createdAt).toLocaleString();
    
    // Show content section
    document.getElementById('courseInfoSection').style.display = 'block';
    
    // Show coordinator buttons if applicable
    if (isCourseCoordinator) {
        document.getElementById('actionButtons').innerHTML = `
            <button class="btn btn-primary me-2" onclick="editCurrentCourse()">
                <i class="bi bi-pencil"></i> Edit
            </button>
            <button class="btn btn-success me-2" data-bs-toggle="modal" data-bs-target="#addSubjectModal">
                <i class="bi bi-plus-circle"></i> Add Subject
            </button>
            <button class="btn btn-info" data-bs-toggle="modal" data-bs-target="#addBatchModal">
                <i class="bi bi-plus-circle"></i> Add Batch
            </button>
        `;
    }
}
```

---

## 3. Subjects Management

### HTML Structure
```html
<!-- Subjects Section -->
<div class="card shadow-sm mb-4">
    <div class="card-header bg-success text-white d-flex justify-content-between align-items-center">
        <h5 class="mb-0">📚 Subjects</h5>
        <span class="badge bg-light text-dark" id="subjectBadge">0</span>
    </div>
    <div class="card-body">
        <div id="subjectsContainer" class="row">
            <!-- Subjects rendered here by JavaScript -->
        </div>
    </div>
</div>
```

### Load Subjects
```javascript
async function loadSubjects() {
    try {
        const response = await fetch(`/api/course/${currentCourseId}/subjects`, {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const subjects = await response.json();
            renderSubjects(subjects);
        } else {
            renderSubjects([]);
        }
    } catch (error) {
        console.error('Error loading subjects:', error);
        renderSubjects([]);
    }
}
```

### Render Subjects Grid
```javascript
function renderSubjects(subjects) {
    const container = document.getElementById('subjectsContainer');
    document.getElementById('subjectCount').textContent = subjects.length;
    document.getElementById('subjectBadge').textContent = subjects.length;

    if (subjects.length === 0) {
        container.innerHTML = `
            <div class="col-12 text-center text-muted py-4">
                <p>No subjects added yet.</p>
                ${isCourseCoordinator ? 
                    '<button class="btn btn-sm btn-success" data-bs-toggle="modal" data-bs-target="#addSubjectModal">Add Subject</button>' 
                    : ''}
            </div>
        `;
        return;
    }

    container.innerHTML = subjects.map(subject => `
        <div class="col-md-6 mb-3">
            <div class="card subject-card">
                <div class="card-body">
                    <h6 class="card-title">${subject.name || subject.title}</h6>
                    <p class="card-text small text-muted">${subject.code || '-'}</p>
                    <span class="badge bg-info">${subject.status || 'ACTIVE'}</span>
                </div>
            </div>
        </div>
    `).join('');
}
```

### Add Subject
```javascript
async function addSubject() {
    const name = document.getElementById('subjectName').value;
    const code = document.getElementById('subjectCode').value;
    const desc = document.getElementById('subjectDesc').value;

    if (!name || !code) {
        showAlert('Please fill in required fields', 'warning');
        return;
    }

    try {
        const response = await fetch(`/api/course/${currentCourseId}/subjects`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: name,
                code: code,
                description: desc,
                courseId: currentCourseId
            })
        });

        if (response.ok) {
            showAlert('Subject added successfully!', 'success');
            document.getElementById('addSubjectForm').reset();
            bootstrap.Modal.getInstance(document.getElementById('addSubjectModal')).hide();
            loadSubjects();
        } else {
            showAlert('Error adding subject', 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('Failed to add subject', 'danger');
    }
}
```

---

## 4. Batches Management

### HTML Table Structure
```html
<!-- Batches Section -->
<div class="card shadow-sm mb-4">
    <div class="card-header bg-info text-white d-flex justify-content-between align-items-center">
        <h5 class="mb-0">👥 Course Batches</h5>
        <span class="badge bg-light text-dark" id="batchBadge">0</span>
    </div>
    <div class="card-body">
        <div id="batchesContainer" class="table-responsive">
            <table class="table table-hover table-striped">
                <thead class="table-light">
                    <tr>
                        <th>Batch Name</th>
                        <th>Academic Year</th>
                        <th>Semester</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody id="batchesTable">
                    <!-- Batches rendered here -->
                </tbody>
            </table>
        </div>
    </div>
</div>
```

### Load Batches
```javascript
async function loadBatches() {
    try {
        const response = await fetch(`/api/course/${currentCourseId}/batches`, {
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const batches = await response.json();
            renderBatches(batches);
        } else {
            renderBatches([]);
        }
    } catch (error) {
        console.error('Error loading batches:', error);
        renderBatches([]);
    }
}
```

### Render Batches Table
```javascript
function renderBatches(batches) {
    const table = document.getElementById('batchesTable');
    document.getElementById('batchCount').textContent = batches.length;
    document.getElementById('batchBadge').textContent = batches.length;

    if (batches.length === 0) {
        table.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-muted py-4">
                    No batches added yet. 
                    ${isCourseCoordinator ? 
                        '<button class="btn btn-sm btn-info text-white" data-bs-toggle="modal" data-bs-target="#addBatchModal">Add Batch</button>' 
                        : ''}
                </td>
            </tr>
        `;
        return;
    }

    table.innerHTML = batches.map(batch => `
        <tr class="batch-row">
            <td>${batch.name || '-'}</td>
            <td>${batch.academicYear || '-'}</td>
            <td>Sem ${batch.semester || '-'}</td>
            <td>
                <span class="badge bg-${batch.status === 'ACTIVE' ? 'success' : 'secondary'}">
                    ${batch.status || 'ACTIVE'}
                </span>
            </td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="viewBatch(${batch.id})">
                    <i class="bi bi-eye"></i>
                </button>
            </td>
        </tr>
    `).join('');
}
```

### Add Batch
```javascript
async function addBatch() {
    const name = document.getElementById('batchName').value;
    const year = document.getElementById('batchYear').value;
    const semester = document.getElementById('batchSemester').value;

    if (!name || !year || !semester) {
        showAlert('Please fill in all required fields', 'warning');
        return;
    }

    try {
        const response = await fetch(`/api/course/${currentCourseId}/batches`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: name,
                academicYear: year,
                semester: semester,
                courseId: currentCourseId
            })
        });

        if (response.ok) {
            showAlert('Batch added successfully!', 'success');
            document.getElementById('addBatchForm').reset();
            bootstrap.Modal.getInstance(document.getElementById('addBatchModal')).hide();
            loadBatches();
        } else {
            showAlert('Error adding batch', 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('Failed to add batch', 'danger');
    }
}
```

---

## 5. Course Management (Edit & Delete)

### Edit Course Modal Form
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
                    <div class="mb-3">
                        <label for="editCourseName" class="form-label">Course Name *</label>
                        <input type="text" class="form-control" id="editCourseName" required>
                    </div>
                    <div class="mb-3">
                        <label for="editCourseDesc" class="form-label">Description</label>
                        <textarea class="form-control" id="editCourseDesc" rows="3"></textarea>
                    </div>
                    <div class="mb-3">
                        <label for="editCourseCredits" class="form-label">Credits *</label>
                        <input type="number" class="form-control" id="editCourseCredits" min="1" max="10" required>
                    </div>
                    <div class="mb-3">
                        <label for="editCourseStatus" class="form-label">Status</label>
                        <select class="form-select" id="editCourseStatus">
                            <option value="ACTIVE">Active</option>
                            <option value="ARCHIVED">Archived</option>
                        </select>
                    </div>
                </form>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" onclick="updateCourse()">Update Course</button>
            </div>
        </div>
    </div>
</div>
```

### Edit Course Function
```javascript
function editCurrentCourse() {
    if (currentCourse) {
        document.getElementById('editCourseName').value = currentCourse.title;
        document.getElementById('editCourseDesc').value = currentCourse.description || '';
        document.getElementById('editCourseCredits').value = currentCourse.credits;
        document.getElementById('editCourseStatus').value = currentCourse.status;
        
        const editModal = new bootstrap.Modal(document.getElementById('editCourseModal'));
        editModal.show();
    }
}

async function updateCourse() {
    const name = document.getElementById('editCourseName').value;
    const desc = document.getElementById('editCourseDesc').value;
    const credits = document.getElementById('editCourseCredits').value;
    const status = document.getElementById('editCourseStatus').value;

    if (!name || !credits) {
        showAlert('Please fill in required fields', 'warning');
        return;
    }

    try {
        const response = await fetch(`/api/course/${currentCourseId}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                title: name,
                description: desc,
                credits: credits,
                status: status
            })
        });

        if (response.ok) {
            showAlert('Course updated successfully!', 'success');
            bootstrap.Modal.getInstance(document.getElementById('editCourseModal')).hide();
            loadCourseDetails();
        } else {
            showAlert('Error updating course', 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('Failed to update course', 'danger');
    }
}
```

### Delete Course Function
```javascript
async function deleteCurrentCourse() {
    if (!confirm('Are you sure you want to delete this course? This action cannot be undone.')) {
        return;
    }

    try {
        const response = await fetch(`/api/course/${currentCourseId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            showAlert('Course deleted successfully!', 'success');
            setTimeout(() => {
                window.location.href = '/Home/Courses';
            }, 1500);
        } else {
            showAlert('Error deleting course', 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('Failed to delete course', 'danger');
    }
}
```

---

## 6. Utility Functions

### Show Alert Notification
```javascript
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    const header = document.querySelector('h1');
    if (header) {
        header.parentNode.insertBefore(alertDiv, header.nextSibling);
    }
    
    setTimeout(() => alertDiv.remove(), 5000);
}
```

### Show Error State
```javascript
function showError(message) {
    document.getElementById('errorSection').style.display = 'block';
    document.getElementById('errorMessage').textContent = message;
    document.getElementById('courseInfoSection').style.display = 'none';
    document.getElementById('loadingSpinner').style.display = 'none';
}
```

---

## 7. Bootstrap Icons Used

```html
<!-- Back button -->
<i class="bi bi-arrow-left"></i>

<!-- Go to course button -->
<i class="bi bi-arrow-right-circle"></i>

<!-- View button -->
<i class="bi bi-eye"></i>

<!-- Edit button -->
<i class="bi bi-pencil"></i>

<!-- Delete button -->
<i class="bi bi-trash"></i>

<!-- Add button -->
<i class="bi bi-plus-circle"></i>
```

---

## 8. Required Bootstrap Icons CSS

```html
<!-- Add to Views/Shared/_Layout.cshtml -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.0/font/bootstrap-icons.min.css" />
```

---

## 9. API Response Format Examples

### Get Course Response
```json
{
  "id": 1,
  "title": "Object-Oriented Programming",
  "description": "Learn OOP concepts with Java",
  "credits": 4,
  "status": "ACTIVE",
  "createdAt": "2026-03-30T09:15:00Z",
  "updatedAt": "2026-03-30T10:30:00Z",
  "subjectCount": 5,
  "batchCount": 4
}
```

### Get Subjects Response
```json
[
  {
    "id": 1,
    "name": "OOP Concepts",
    "code": "CS101",
    "description": "Core OOP principles",
    "status": "ACTIVE",
    "courseId": 1
  },
  {
    "id": 2,
    "name": "Data Structures",
    "code": "CS102",
    "description": "DSA fundamentals",
    "status": "ACTIVE",
    "courseId": 1
  }
]
```

### Get Batches Response
```json
[
  {
    "id": 1,
    "name": "Batch A - 2026",
    "academicYear": "2025-2026",
    "semester": 1,
    "status": "ACTIVE",
    "courseId": 1
  },
  {
    "id": 2,
    "name": "Batch B - 2026",
    "academicYear": "2025-2026",
    "semester": 2,
    "status": "ACTIVE",
    "courseId": 1
  }
]
```

---

## 10. Complete Initialization Code

```javascript
// Page initialization
document.addEventListener('DOMContentLoaded', function() {
    // Get JWT token from Razor
    let jwtToken = "@jwtToken";
    let isCourseCoordinator = @Html.Raw((ViewBag.UserRole == "CourseCoordinator" || ViewBag.UserRole == "Admin") ? "true" : "false");
    
    // Get course ID from URL
    const params = new URLSearchParams(window.location.search);
    let currentCourseId = params.get('courseId');
    
    // Validate
    if (!currentCourseId) {
        showError('No course selected. Please select a course from the courses page.');
        return;
    }
    
    if (!jwtToken) {
        showError('Authentication token not available. Please login again.');
        return;
    }
    
    // Load all data
    loadCourseDetails();
});
```

---

This comprehensive code guide provides all the essential JavaScript, HTML, and C# code snippets needed to understand and extend the Course Details frontend implementation.

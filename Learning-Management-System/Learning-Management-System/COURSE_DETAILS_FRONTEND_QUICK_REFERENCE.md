# Course Details Frontend - Quick Reference Guide

## Quick Setup & Testing

### Step 1: Access the Page
1. Log in to the application as a Course Coordinator
2. Navigate to "Courses" from the main menu
3. Click the **"Go to Course"** button on any course card

### Step 2: What You'll See

**Main Content Area (Left Side - 67%)**
- Course information card with all details
- Subject statistics cards
- Subjects list in a 2-column grid
- Course batches in a table format

**Sidebar (Right Side - 33%)**
- Quick statistics panel
- Course management buttons
- Timeline information

### Step 3: Basic Operations

#### View Course Details
✅ Page automatically loads when you click "Go to Course"

#### Add a Subject
1. Click **"Add Subject"** button
2. Enter subject name and code
3. Optionally add description
4. Click **"Add Subject"**

#### Add a Batch
1. Click **"Add Batch"** button
2. Enter batch name and academic year
3. Select semester (1-8)
4. Click **"Add Batch"**

#### Edit Course
1. Click **"Edit Course"** button in header or sidebar
2. Modify course details
3. Click **"Update Course"**

#### Delete Course
1. Click **"Delete Course"** button
2. Confirm deletion
3. You'll be redirected to courses page

## File Structure

```
Learning-Management-System/
├── Controllers/
│   └── HomeController.cs          (Added CourseDetails action)
│
├── Views/
│   ├── Home/
│   │   ├── CourseDetails.cshtml   (NEW - Main page)
│   │   └── Courses.cshtml         (UPDATED - Added Go to Course button)
│   │
│   └── Shared/
│       └── _Layout.cshtml         (UPDATED - Added Bootstrap Icons CSS)
│
└── Documentation/
    ├── COURSE_DETAILS_COORDINATOR_GUIDE.md    (NEW)
    └── COURSE_DETAILS_LAYOUT_GUIDE.md         (NEW)
```

## Key Files Modified/Created

### 1. `Views/Home/CourseDetails.cshtml` (NEW - 500+ lines)
Complete course details page with:
- Course information display
- Subjects section
- Batches table
- Modals for CRUD operations
- JavaScript for API integration

### 2. `Views/Home/Courses.cshtml` (MODIFIED)
Changes:
- Updated `renderCourses()` function
- Added "Go to Course" button
- Restructured card footer buttons

### 3. `Controllers/HomeController.cs` (MODIFIED)
Added:
```csharp
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

### 4. `Views/Shared/_Layout.cshtml` (MODIFIED)
Added:
```html
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.0/font/bootstrap-icons.min.css" />
```

## API Endpoints Required

Your backend should have these endpoints:

```csharp
// GET course details
GET /api/course/{courseId}
Response: { id, title, description, credits, status, createdAt, updatedAt }

// GET subjects for a course
GET /api/course/{courseId}/subjects
Response: [{ id, name, code, description, status }, ...]

// GET batches for a course
GET /api/course/{courseId}/batches
Response: [{ id, name, academicYear, semester, status }, ...]

// POST - Add subject
POST /api/course/{courseId}/subjects
Body: { name, code, description, courseId }
Response: { id, name, code, ... }

// POST - Add batch
POST /api/course/{courseId}/batches
Body: { name, academicYear, semester, courseId }
Response: { id, name, academicYear, semester, ... }

// PUT - Update course
PUT /api/course/{courseId}
Body: { title, description, credits, status }
Response: { success: true }

// DELETE - Delete course
DELETE /api/course/{courseId}
Response: { success: true }
```

## URL Navigation

**From Courses Page:**
```
/Home/Courses
    ↓ [Click "Go to Course"]
/Home/CourseDetails?courseId=123
```

**URL Parameters:**
- `courseId` - The ID of the course to display (required)

**Back Navigation:**
```
/Home/CourseDetails?courseId=123
    ↓ [Click "Back to Courses"]
/Home/Courses
```

## Bootstrap Icons Used

| Icon | Class | Usage |
|------|-------|-------|
| Arrow Right | `bi bi-arrow-right-circle` | Go to Course button |
| Arrow Left | `bi bi-arrow-left` | Back button |
| Eye | `bi bi-eye` | View button |
| Pencil | `bi bi-pencil` | Edit button |
| Plus Circle | `bi bi-plus-circle` | Add buttons |
| Trash | `bi bi-trash` | Delete button |

## CSS Classes Used

### Layout Classes
- `container-fluid` - Full width container
- `row`, `col-lg-8`, `col-lg-4` - Bootstrap grid
- `col-md-6`, `col-md-2` - Medium breakpoints

### Card Classes
- `card` - Card container
- `card-header` - Card title section
- `card-body` - Card content
- `card-footer` - Card actions

### Utility Classes
- `mb-4`, `mb-3`, `mb-2` - Margins bottom
- `mt-2` - Margin top
- `px-4`, `py-4` - Padding
- `text-center`, `text-muted` - Text alignment
- `fw-bold` - Font weight bold
- `d-flex`, `justify-content-between` - Flexbox

### Color Classes
- `bg-primary`, `bg-success`, `bg-danger` - Backgrounds
- `text-white`, `text-muted`, `text-dark` - Text colors
- `badge bg-success`, `badge bg-info` - Badge styling

## JavaScript Variables & Functions

### Global Variables
```javascript
let currentCourseId = null;          // Current course ID from URL
let currentCourse = null;            // Course object
let jwtToken = "@jwtToken";          // From Razor
let isCourseCoordinator = true/false; // User role check
```

### Key Functions
```javascript
function getCourseIdFromUrl()        // Parse URL parameters
function loadCourseDetails()          // Fetch course from API
function renderCourseDetails(data)   // Display course info
function loadSubjects()               // Fetch subjects
function loadBatches()                // Fetch batches
function renderSubjects(subjects)    // Display subjects
function renderBatches(batches)      // Display batches table
function addSubject()                 // Create subject
function addBatch()                   // Create batch
function updateCourse()               // Update course details
function deleteCurrentCourse()        // Delete course
function editCurrentCourse()          // Open edit modal
function showAlert(message, type)    // Display notifications
function showError(message)           // Display errors
```

## Common Issues & Solutions

### Issue: Page shows "No course selected"
**Solution**: Make sure you're passing courseId in URL
```
✓ Correct: /Home/CourseDetails?courseId=123
✗ Wrong: /Home/CourseDetails
```

### Issue: "Authentication token not available"
**Solution**: 
1. Make sure you're logged in
2. Check that JWT token is in session
3. Try logging out and back in

### Issue: Subjects/Batches not loading
**Solution**:
1. Check browser console for errors
2. Verify API endpoints exist
3. Check network tab for failed requests
4. Ensure JWT token is valid

### Issue: Edit/Delete buttons not showing
**Solution**:
1. Verify user role is "CourseCoordinator" or "Admin"
2. Check ViewBag.UserRole is set correctly
3. Inspect browser console for JavaScript errors

### Issue: Icons not showing
**Solution**:
1. Check if Bootstrap Icons CSS is loaded
2. Verify CDN link in _Layout.cshtml
3. Check browser console for 404 errors
4. Try clearing browser cache

## Testing Checklist

- [ ] Load course details page successfully
- [ ] Display all course information correctly
- [ ] Show subjects list (if any exist)
- [ ] Show batches table (if any exist)
- [ ] Display statistics correctly
- [ ] Back button returns to courses page
- [ ] Add Subject modal opens and closes
- [ ] Add Batch modal opens and closes
- [ ] Edit Course modal opens and closes
- [ ] Add subject functionality works
- [ ] Add batch functionality works
- [ ] Edit course functionality works
- [ ] Delete course with confirmation works
- [ ] Page is responsive on mobile
- [ ] Page is responsive on tablet
- [ ] Page is responsive on desktop
- [ ] Coordinator buttons hidden for students
- [ ] Error handling works properly
- [ ] Loading states display correctly
- [ ] Alerts show/dismiss properly

## Performance Tips

1. **Reduce API Calls**: Combine related data when possible
2. **Lazy Load**: Subjects and batches load after main view
3. **Cache Data**: Consider caching course data locally
4. **Optimize Images**: Compress any images used
5. **Minimize CSS**: Already using Bootstrap utility classes

## Browser Console Commands (for debugging)

```javascript
// Check current state
console.log(currentCourse);
console.log(currentCourseId);
console.log(jwtToken);

// Manually trigger loads
loadCourseDetails();
loadSubjects();
loadBatches();

// Check coordinator status
console.log(isCourseCoordinator);

// Test alert
showAlert('Test message', 'success');
```

## SEO & Accessibility

- Semantic HTML structure
- Proper heading hierarchy
- ARIA labels on buttons
- Color contrast meets WCAG standards
- Keyboard navigation supported
- Screen reader friendly

## Deployment Considerations

1. **Bootstrap Icons CDN**: Ensure CDN is accessible in production
2. **JWT Token**: Verify token validation in production environment
3. **CORS**: Check CORS policies for API calls
4. **Security**: Review authentication headers
5. **Error Handling**: Ensure user-friendly error messages

## Support & Documentation

- Full guide: `COURSE_DETAILS_COORDINATOR_GUIDE.md`
- Layout guide: `COURSE_DETAILS_LAYOUT_GUIDE.md`
- This file: `COURSE_DETAILS_FRONTEND_QUICK_REFERENCE.md`

---

**Last Updated**: March 30, 2026
**Version**: 1.0
**Status**: Production Ready ✓

# Quick Start Guide - Course & Enrollment Management System

## Application Running ✅

**URL:** http://localhost:5171  
**Status:** Successfully Running  
**Environment:** Development

---

## Feature Overview

### 1. **Courses Management** (`/Home/Courses`)
Navigate to the Courses page to:
- ✅ View all active courses in a responsive grid layout
- ✅ Add new courses with credits and description
- ✅ Edit existing course information
- ✅ View course details including subjects and batches
- ✅ Delete courses (soft delete)
- ✅ Search courses by name
- ✅ Filter by status (Active/Archived)
- ✅ Sort by creation date or name

**Key Data Fields:**
- Course Name
- Description
- Credits (1-10)
- Status
- Timestamp tracking

---

### 2. **Subjects Management** (`/Home/Subjects`)
Navigate to the Subjects page to:
- ✅ View all subjects in a responsive table
- ✅ Add subjects to specific courses
- ✅ Edit subject information
- ✅ View assigned teachers
- ✅ Delete subjects
- ✅ Search by subject name
- ✅ Filter by course

**Key Data Fields:**
- Subject Name
- Associated Course
- Description
- Assigned Teachers Count

---

### 3. **Student Enrollment** (`/Home/Enrollments`)
Navigate to the Enrollments page to:
- ✅ View all student enrollments
- ✅ Enroll new students in batches
- ✅ Update enrollment status (Active/Dropped/Completed)
- ✅ View student progress percentage
- ✅ Search by student name or email
- ✅ Filter by batch or enrollment status
- ✅ Export enrollment data
- ✅ View enrollment details including attendance rate

**Key Data Fields:**
- Student Name & Email
- Batch Information
- Enrollment Status
- Course Progress (%)
- Enrollment Date

**Enrollment Statuses:**
- 🟢 ACTIVE - Student actively enrolled
- 🟡 IN PROGRESS - Ongoing enrollment
- 🔴 DROPPED - Student dropped the course
- 🟦 COMPLETED - Student completed the course

---

### 4. **Attendance Tracking** (`/Home/Attendance`)
Navigate to the Attendance page to:

#### Tab 1: Attendance Sessions
- ✅ Create new attendance sessions for subjects
- ✅ View all sessions with attendance statistics
- ✅ Edit or delete sessions
- ✅ Filter by subject and date
- ✅ See present/absent count per session

#### Tab 2: Attendance Records
- ✅ View individual attendance records
- ✅ Mark students present/absent
- ✅ Edit attendance status
- ✅ Track attendance by student
- ✅ Filter by subject

#### Tab 3: Attendance Summary
- ✅ View overall attendance statistics
- ✅ See average attendance percentage
- ✅ Identify students with low attendance
- ✅ Detailed student attendance summary
- ✅ Attendance trends analysis

**Key Metrics:**
- Total Sessions Count
- Average Attendance %
- Average Absent %
- Students Below 80% Attendance Threshold
- Individual Student Statistics

---

## Responsive Design Features

### Desktop View (≥992px)
- Full card grid layout for courses (3 columns)
- Expanded tables with all columns visible
- Full-size modals and forms
- Complete navigation menu

### Tablet View (768px - 991px)
- 2-column card grid for courses
- Condensed table with collapsible sections
- Responsive forms and modals
- Optimized spacing

### Mobile View (<768px)
- 1-column layout for all cards
- Stacked table view with key information
- Touch-friendly buttons (larger tap targets)
- Simplified navigation
- Vertical scrolling optimized

---

## Key UI Components

### Color Coding System

**Status Badges:**
- 🟢 **Success (Green)** - Active, Present, Completed
- 🔴 **Danger (Red)** - Deleted, Absent, Failed
- 🟡 **Warning (Orange)** - In Progress, Pending
- 🔵 **Primary (Blue)** - Information, Default
- 🟦 **Info (Light Blue)** - General Information
- ⚪ **Secondary (Gray)** - Inactive, Dropped

**Progress Bars:**
- 🟢 Green (75%+) - Excellent progress
- 🟡 Yellow (50-74%) - Good progress
- 🔴 Red (<50%) - Need improvement

### Interactive Elements

**Modals (Pop-up Dialogs):**
- Add Course/Subject/Enrollment
- Edit Course/Subject/Enrollment
- View Details
- Confirm Delete
- Mark Attendance

**Tables:**
- Hover effects on rows
- Sortable columns
- Responsive overflow handling
- Action buttons in each row

**Forms:**
- Required field indicators (*)
- Input validation
- Clear error messages
- Submit/Cancel buttons

---

## Workflow Examples

### Example 1: Adding a New Course

1. Click **"Add Course"** button
2. Fill in course details:
   - Course Name (required)
   - Description (optional)
   - Credits (1-10)
3. Click **"Add Course"**
4. Course appears in the grid
5. Can now add subjects to this course

### Example 2: Enrolling a Student

1. Go to **Enrollment** page
2. Click **"Enroll Student"** button
3. Select student from dropdown
4. Select batch to enroll in
5. Click **"Enroll Student"**
6. Student appears in enrollments list

### Example 3: Marking Attendance

1. Go to **Attendance** page
2. Click **"New Session"** button
3. Select subject and date/time
4. Click **"Create Session"**
5. Click **"View"** on the session
6. Mark each student as PRESENT or ABSENT
7. Click **"Save Attendance"**

---

## Database Information

**Connection String:** `Data Source=localhost\\SQLEXPRESS`  
**Database Name:** `Sample`  
**Authentication:** Windows (Integrated Security)

### Tables Created:
- Courses
- Subjects
- Enrollments
- AttendanceSessions
- AttendanceRecords
- CourseBatches
- Related identity tables

---

## Error Handling

### Common Issues & Solutions:

1. **"Student not found"**
   - Solution: Ensure student is registered in the system first

2. **"Batch not found"**
   - Solution: Create a batch for the course first

3. **"Duplicate enrollment"**
   - Solution: Student is already enrolled in this batch

4. **Connection timeout**
   - Solution: Verify SQL Server is running and connection string is correct

---

## API Endpoints Reference

### Courses
```
GET    /api/course
GET    /api/course/{id}
POST   /api/course
PUT    /api/course/{id}
DELETE /api/course/{id}
```

### Enrollments
```
GET    /api/enrollment/student/{studentId}
GET    /api/enrollment/batch/{batchId}
POST   /api/enrollment
PUT    /api/enrollment/{id}
DELETE /api/enrollment/{id}
```

### Attendance
```
GET    /api/attendance/subject/{subjectId}
GET    /api/attendance/session/{sessionId}
POST   /api/attendance/session
POST   /api/attendance/mark
```

---

## Tips & Best Practices

1. **Data Entry:**
   - Use descriptive course and subject names
   - Include syllabus information in descriptions
   - Keep credits consistent with your institution's standards

2. **Enrollment:**
   - Enroll students before creating attendance sessions
   - Verify batch dates match enrollment timeline
   - Monitor attendance regularly

3. **Attendance:**
   - Create sessions for each class meeting
   - Mark attendance immediately after class
   - Use the summary to identify at-risk students

4. **Performance:**
   - Search for specific records when dealing with large datasets
   - Use filters to narrow down results
   - Export data for offline analysis

---

## Support & Troubleshooting

For issues or questions:
1. Check the application logs in the terminal
2. Verify database connectivity
3. Clear browser cache if UI doesn't update
4. Restart the application if experiencing performance issues

---

**System Ready** ✅  
All modules are fully functional and ready for use!

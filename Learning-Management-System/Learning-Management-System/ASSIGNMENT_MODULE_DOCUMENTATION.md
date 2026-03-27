# Assignment Submission & Grading Module - Documentation

## Overview
This module handles assignment uploads, tracking, and grading within the Learning Management System.

## Features
- ✅ Student assignment submission with file upload
- ✅ Instructor grading and feedback
- ✅ Assignment status tracking (Not Submitted, Submitted, Graded, Returned, Late)
- ✅ Late submission detection
- ✅ File validation and security
- ✅ Role-based access control (Student, Instructor, Admin)
- ✅ RESTful API endpoints

## Database Schema

### Subject Entity
```
SubjectId (PK)        - int
SubjectName          - string
Description          - string
InstructorId (FK)    - string (AppUser)
CreatedAt            - datetime
```

### Assignment Entity
```
AssignmentId (PK)    - int
SubjectId (FK)       - int
StudentId (FK)       - string (AppUser)
Title                - string
Description          - string
FileUrl              - string
SubmissionDate       - datetime
DueDate              - datetime
Grade                - decimal (0-100)
GradeComments        - string
Status               - enum (NotSubmitted, Submitted, Graded, Returned, Late)
CreatedAt            - datetime
UpdatedAt            - datetime
```

## API Endpoints

### 1. Submit Assignment
**Endpoint:** `POST /api/assignment/submit`
**Authorization:** Required (Student)
**Content-Type:** multipart/form-data

**Request:**
```json
{
  "assignmentId": 1,
  "title": "Assignment Title",
  "description": "Assignment Description",
  "submissionFile": <file>
}
```

**Response:**
```json
{
  "success": true,
  "message": "Assignment submitted successfully",
  "data": {
    "assignmentId": 1,
    "subjectId": 1,
    "studentId": "user-id",
    "studentName": "John Doe",
    "title": "Assignment Title",
    "description": "Assignment Description",
    "fileUrl": "1_user-id_20260326120000.pdf",
    "submissionDate": "2026-03-26T12:00:00Z",
    "dueDate": "2026-03-28T23:59:59Z",
    "grade": null,
    "gradeComments": null,
    "status": "Submitted",
    "isLate": false,
    "daysLate": 0
  }
}
```

### 2. Grade Assignment
**Endpoint:** `POST /api/assignment/grade`
**Authorization:** Required (Instructor, Admin)
**Content-Type:** application/json

**Request:**
```json
{
  "assignmentId": 1,
  "grade": 85.5,
  "comments": "Good work! Please improve on section 3."
}
```

**Response:**
```json
{
  "success": true,
  "message": "Assignment graded successfully",
  "data": {
    "assignmentId": 1,
    "subjectId": 1,
    "studentId": "user-id",
    "studentName": "John Doe",
    "title": "Assignment Title",
    "description": "Assignment Description",
    "fileUrl": "1_user-id_20260326120000.pdf",
    "submissionDate": "2026-03-26T12:00:00Z",
    "dueDate": "2026-03-28T23:59:59Z",
    "grade": 85.5,
    "gradeComments": "Good work! Please improve on section 3.",
    "status": "Graded",
    "isLate": false,
    "daysLate": 0
  }
}
```

### 3. Get Assignment Status
**Endpoint:** `GET /api/assignment/status/{assignmentId}`
**Authorization:** Required (Student)

**Response:**
```json
{
  "success": true,
  "data": {
    "assignmentId": 1,
    "status": "Graded",
    "grade": 85.5,
    "submissionDate": "2026-03-26T12:00:00Z",
    "dueDate": "2026-03-28T23:59:59Z",
    "isLate": false,
    "gradeComments": "Good work! Please improve on section 3."
  }
}
```

### 4. Get My Assignments
**Endpoint:** `GET /api/assignment/my-assignments?subjectId=1`
**Authorization:** Required (Student)

**Response:**
```json
{
  "success": true,
  "message": "Assignments retrieved successfully",
  "data": [
    {
      "assignmentId": 1,
      "subjectId": 1,
      "studentId": "user-id",
      "studentName": "John Doe",
      "title": "Assignment 1",
      "description": "Description",
      "fileUrl": "1_user-id_20260326120000.pdf",
      "submissionDate": "2026-03-26T12:00:00Z",
      "dueDate": "2026-03-28T23:59:59Z",
      "grade": 85.5,
      "gradeComments": "Good work!",
      "status": "Graded",
      "isLate": false,
      "daysLate": 0
    }
  ],
  "total": 1
}
```

### 5. Get Pending Assignments (Instructor)
**Endpoint:** `GET /api/assignment/pending?subjectId=1`
**Authorization:** Required (Instructor, Admin)

**Response:**
```json
{
  "success": true,
  "message": "Pending assignments for grading retrieved successfully",
  "data": [
    {
      "assignmentId": 1,
      "subjectId": 1,
      "studentId": "user-id",
      "studentName": "John Doe",
      "title": "Assignment 1",
      "description": "Description",
      "fileUrl": "1_user-id_20260326120000.pdf",
      "submissionDate": "2026-03-26T12:00:00Z",
      "dueDate": "2026-03-28T23:59:59Z",
      "grade": null,
      "gradeComments": null,
      "status": "Submitted",
      "isLate": false,
      "daysLate": 0
    }
  ],
  "total": 1
}
```

### 6. Get Assignment by ID
**Endpoint:** `GET /api/assignment/{assignmentId}`
**Authorization:** Required

**Response:**
```json
{
  "success": true,
  "data": {
    "assignmentId": 1,
    "subjectId": 1,
    "studentId": "user-id",
    "studentName": "John Doe",
    "title": "Assignment 1",
    "description": "Description",
    "fileUrl": "1_user-id_20260326120000.pdf",
    "submissionDate": "2026-03-26T12:00:00Z",
    "dueDate": "2026-03-28T23:59:59Z",
    "grade": 85.5,
    "gradeComments": "Good work!",
    "status": "Graded",
    "isLate": false,
    "daysLate": 0
  }
}
```

### 7. Delete Assignment
**Endpoint:** `DELETE /api/assignment/{assignmentId}`
**Authorization:** Required (Student)

**Response:**
```json
{
  "success": true,
  "message": "Assignment deleted successfully"
}
```

## File Upload Specifications

### Allowed File Types
- .pdf
- .doc
- .docx
- .txt
- .xlsx
- .pptx
- .zip

### File Size Limit
- Maximum: 10 MB

### File Storage
- Location: `wwwroot/uploads/assignments/`
- Naming: `{assignmentId}_{studentId}_{timestamp}{extension}`

## Error Handling

### Common Error Responses

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "You don't have permission to grade this assignment"
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Assignment not found"
}
```

**400 Bad Request:**
```json
{
  "success": false,
  "message": "Invalid request data"
}
```

**500 Internal Server Error:**
```json
{
  "success": false,
  "message": "Internal server error",
  "error": "Error details"
}
```

## Business Logic

### Assignment Submission
1. Validate assignment exists and is not already graded
2. Validate student exists
3. Upload file (if provided)
4. Check if submission is late
5. Update assignment status based on due date
6. Save to database

### Assignment Grading
1. Verify assignment exists
2. Verify instructor owns the subject
3. Validate grade (0-100)
4. Update grade and comments
5. Mark assignment as graded
6. Save to database

### Late Submission Detection
- If `SubmissionDate > DueDate`, mark as late
- Calculate days late: `(SubmissionDate - DueDate).TotalDays`

## Security Features
- ✅ Authorization checks for all endpoints
- ✅ Role-based access control
- ✅ File type validation
- ✅ File size validation
- ✅ User ownership verification
- ✅ Logging for audit trails

## Next Steps

### 1. Create Migration
```bash
cd Learning-Management-System
dotnet ef migrations add AddAssignmentAndSubject
dotnet ef database update
```

### 2. Test the Endpoints
Use Postman or Swagger UI to test all endpoints.

### 3. Create Frontend
Build a responsive UI for:
- Student assignment submission form
- Instructor grading dashboard
- Student assignment status page

### 4. Add Additional Features
- Email notifications
- Resubmission tracking
- Grade statistics
- Bulk operations

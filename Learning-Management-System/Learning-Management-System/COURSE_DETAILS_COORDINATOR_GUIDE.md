# Course Details Coordinator Page - Frontend Implementation Guide

## Overview
A comprehensive course details page has been implemented for coordinators to manage courses, view subjects, manage batches, and see detailed course information.

## Features Implemented

### 1. **Course Management Page (Courses.cshtml - Updated)**
   - **"Go to Course" Button**: Each course card now displays a prominent "Go to Course" button that navigates to the course details page
   - **Course Statistics**: Shows number of subjects and batches for each course
   - **Status Badges**: Visual indicators for course status (Active/Archived)
   - **Responsive Design**: Works on mobile, tablet, and desktop screens

### 2. **Course Details Page (CourseDetails.cshtml - New)**

#### Main Course Information Section:
   - **Course Header**: Displays course title and quick action buttons
   - **Course Code**: Unique identifier for the course
   - **Credits**: Number of credits for the course
   - **Description**: Detailed course description
   - **Status**: Current status badge (Active/Archived)
   - **Created Date**: Course creation date

#### Course Statistics Section:
   - **Subjects Count**: Total number of subjects in the course
   - **Batches Count**: Total number of batches for the course
   - **Quick Stats Sidebar**: Shows enrollments, active subjects, and last update date

#### Subjects Section:
   - **Subject Cards**: Display subjects with name, code, and status
   - **Two-Column Layout**: Responsive grid layout for subjects
   - **Add Subject Modal**: Button to add new subjects (coordinator only)
   - **Subject Badges**: Visual indicators for subject status

#### Course Batches Section:
   - **Batch Table**: Comprehensive table showing all batches
   - **Columns**:
     - Batch Name
     - Academic Year
     - Semester
     - Status Badge
     - View Action Button
   - **Add Batch Modal**: Button to add new batches (coordinator only)
   - **Hover Effects**: Subtle animations for better UX

### 3. **Coordinator Control Panel (Sidebar)**

#### Course Management Buttons:
   - **Edit Course**: Modify course details (name, description, credits, status)
   - **Add Subject**: Add new subjects to the course
   - **Add Batch**: Create new course batches
   - **Delete Course**: Remove the entire course (with confirmation)

#### Quick Stats Card:
   - Total enrollments count
   - Number of active subjects
   - Current course status
   - Last update timestamp

#### Timeline Information:
   - Course creation date and time
   - Last modification date and time
   - Academic year information

### 4. **Modal Dialogs**

#### Add Subject Modal:
   - **Subject Name** (required)
   - **Subject Code** (required)
   - **Description** (optional)
   - Form validation before submission

#### Add Batch Modal:
   - **Batch Name** (required)
   - **Academic Year** (required)
   - **Semester Dropdown** (1-8 options)
   - Form validation before submission

#### Edit Course Modal:
   - **Course Name** (required)
   - **Description** (optional)
   - **Credits** (required, 1-10)
   - **Status Selection** (Active/Archived)
   - Pre-filled with current course data

### 5. **User Interface Elements**

#### Navigation:
   - Back to Courses button for easy navigation
   - Breadcrumb-style navigation
   - Clear heading hierarchy

#### Loading States:
   - Loading spinner during data fetch
   - Empty state messages when no data exists
   - Error handling with user-friendly messages

#### Responsive Design:
   - Mobile-friendly layout
   - Tablet-optimized views
   - Desktop-enhanced experience
   - Bootstrap grid system (col-md-6, col-lg-4, etc.)

#### Visual Styling:
   - Color-coded cards for different sections
   - Badge styling for status indicators
   - Hover effects on cards and buttons
   - Smooth transitions and animations
   - Professional color scheme

### 6. **Permission-Based Features**

#### Coordinator-Only Features:
   - Edit Course button and functionality
   - Add Subject button and form
   - Add Batch button and form
   - Delete Course button and confirmation
   - Coordinator control panel (full sidebar)

#### Student/Viewer Features:
   - View course information (read-only)
   - View subjects list
   - View batches table
   - See course statistics
   - No editing capabilities

### 7. **API Endpoints Used**

The frontend integrates with these endpoints:
```
GET  /api/course/{courseId}                  - Get course details
GET  /api/course/{courseId}/subjects         - List subjects
GET  /api/course/{courseId}/batches          - List batches
POST /api/course/{courseId}/subjects         - Add new subject
POST /api/course/{courseId}/batches          - Add new batch
PUT  /api/course/{courseId}                  - Update course
DELETE /api/course/{courseId}                - Delete course
```

## File Changes

### New Files:
- `Views/Home/CourseDetails.cshtml` - Complete course details page

### Modified Files:
- `Views/Home/Courses.cshtml` - Added "Go to Course" button and updated card layout
- `Controllers/HomeController.cs` - Added CourseDetails action method
- `Views/Shared/_Layout.cshtml` - Added Bootstrap Icons CSS link

## Styling Features

### Colors Used:
- **Primary Blue** (#007bff) - Main actions and highlights
- **Success Green** (#28a745) - Positive actions
- **Info Cyan** (#17a2b8) - Information display
- **Warning Yellow** (#ffc107) - Coordinator actions
- **Danger Red** (#dc3545) - Delete/danger actions

### Interactive Elements:
- Card hover effects (shadow and lift)
- Button hover transforms (slight scale up)
- Smooth transitions (0.3s)
- Active state indicators

## JavaScript Features

### Core Functions:
- **loadCourseDetails()** - Fetch and display course information
- **loadSubjects()** - Load and render subjects
- **loadBatches()** - Load and render batches
- **addSubject()** - Create new subject
- **addBatch()** - Create new batch
- **updateCourse()** - Modify course details
- **deleteCurrentCourse()** - Remove course with confirmation
- **showAlert()** - Display user notifications
- **showError()** - Handle error states

### Data Management:
- JWT token authentication for all requests
- Role-based permission checking
- Error handling with user feedback
- URL parameter parsing for course ID
- Form validation before submission

## How to Use

### For Coordinators:

1. **Navigate to Course Details**:
   - Go to Courses page
   - Click "Go to Course" button on any course card

2. **View Course Information**:
   - See detailed course information in the main card
   - Check subjects and batches lists
   - View quick statistics in sidebar

3. **Add Subject**:
   - Click "Add Subject" button
   - Fill in subject name and code
   - Click "Add Subject" in modal

4. **Add Batch**:
   - Click "Add Batch" button
   - Enter batch name, academic year, and semester
   - Click "Add Batch" in modal

5. **Edit Course**:
   - Click "Edit Course" button or card header button
   - Modify course details
   - Click "Update Course" to save

6. **Delete Course**:
   - Click "Delete Course" button
   - Confirm deletion in dialog
   - Course will be removed

### For Students/Viewers:
- View all course information
- See list of subjects
- View batch information
- Access read-only content only

## Responsive Behavior

### Desktop (>992px):
- Two-column layout (Main content + Sidebar)
- Full sidebar visible with all details
- Grid layout for subjects

### Tablet (768px - 992px):
- Stacked layout starting
- Sidebar below main content
- Adjusted font sizes

### Mobile (<768px):
- Full-width single column
- Sidebar below all content
- Compact button sizes
- Optimized spacing

## Browser Compatibility
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+
- Mobile browsers (iOS Safari, Chrome Mobile)

## Bootstrap Icons
Bootstrap Icons CSS is loaded from CDN for icon support:
- `bi bi-pencil` - Edit icon
- `bi bi-trash` - Delete icon
- `bi bi-eye` - View icon
- `bi bi-plus-circle` - Add icon
- `bi bi-arrow-left` - Back icon
- `bi bi-arrow-right-circle` - Go to course icon

## Security Features

### Authentication:
- JWT token verification for all API calls
- Session validation
- Token refresh on 401 response
- Automatic redirect to login if needed

### Authorization:
- Role-based access control
- Coordinator-only features hidden from students
- Confirmation dialogs for destructive actions

### Form Validation:
- Required field checking
- Client-side validation
- Server-side validation via API

## Performance Optimizations

1. **Lazy Loading**: Subjects and batches loaded after course details
2. **Minimal API Calls**: Efficient endpoint usage
3. **CSS Transitions**: Smooth animations using CSS (not JavaScript)
4. **Responsive Images**: Efficient image rendering
5. **Modal Reusability**: Shared modals for multiple actions

## Future Enhancements (Optional)

1. Add course schedule view
2. Implement student enrollment management
3. Add grade tracking interface
4. Course material upload section
5. Assignment creation interface
6. Attendance tracking
7. Student performance analytics
8. Batch assignment management

## Summary

This implementation provides a complete, professional frontend for coordinator course management. The page is fully responsive, feature-rich, and provides an intuitive interface for managing all course-related operations while maintaining security and usability for different user roles.

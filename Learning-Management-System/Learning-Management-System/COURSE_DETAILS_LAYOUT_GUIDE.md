# Course Details Page - Visual Layout Guide

## Page Structure Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Navigation Bar: Logo, Home, Courses, Subjects, etc.       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  [← Back to Courses]  Course Title (H1)   [Edit] [Add Subj] │
└─────────────────────────────────────────────────────────────┘

┌───────────────────────────────────┬───────────────────────────┐
│                                   │                           │
│    MAIN CONTENT AREA              │   SIDEBAR AREA           │
│    (col-lg-8)                     │   (col-lg-4)             │
│                                   │                           │
│ ┌──────────────────────────────┐  │ ┌──────────────────────┐ │
│ │  COURSE INFORMATION CARD     │  │ │ 📊 QUICK STATS       │ │
│ ├──────────────────────────────┤  │ ├──────────────────────┤ │
│ │ • Course Code                │  │ │ • Enrollments: 45    │ │
│ │ • Credits: 4                 │  │ │ • Active Subjects: 3 │ │
│ │ • Status: [Active Badge]     │  │ │ • Status: [Active]   │ │
│ │ • Created Date: XX/XX/20XX   │  │ │ • Last Updated: ...  │ │
│ │ • Description: ...text...    │  │ └──────────────────────┘ │
│ └──────────────────────────────┘  │                           │
│                                   │ ┌──────────────────────┐ │
│ ┌──────────────┬──────────────┐   │ │ ⚙️ COURSE MANAGE.   │ │
│ │  📚 Subjects │ 👥 Batches   │   │ ├──────────────────────┤ │
│ │     (5)      │    (4)       │   │ │ [Edit Course]        │ │
│ └──────────────┴──────────────┘   │ │ [Add Subject]        │ │
│                                   │ │ [Add Batch]          │ │
│ ┌──────────────────────────────┐  │ │ [Delete Course]      │ │
│ │ 📚 SUBJECTS SECTION          │  │ └──────────────────────┘ │
│ ├──────────────────────────────┤  │                           │
│ │ [Subject Card] [Subject Card]│  │ ┌──────────────────────┐ │
│ │ [Subject Card] [Subject Card]│  │ │ 📅 TIMELINE          │ │
│ │ [Subject Card]               │  │ ├──────────────────────┤ │
│ │                              │  │ │ Created: XX/XX/...   │ │
│ │ [+ Add Subject]              │  │ │ Modified: XX/XX/...  │ │
│ └──────────────────────────────┘  │ │ Academic Year: ...   │ │
│                                   │ └──────────────────────┘ │
│ ┌──────────────────────────────┐  │                           │
│ │ 👥 COURSE BATCHES            │  │                           │
│ ├──────────────────────────────┤  │                           │
│ │ Batch Name │ Year │ Sem│Status│  │                           │
│ ├──────────────────────────────┤  │                           │
│ │ Batch A    │2025 │ 1 │Active │  │                           │
│ │ Batch B    │2025 │ 2 │Active │  │                           │
│ │ Batch C    │2026 │ 1 │Active │  │                           │
│ │            │     │   │       │  │                           │
│ │ [+ Add Batch]                │  │                           │
│ └──────────────────────────────┘  │                           │
│                                   │                           │
└───────────────────────────────────┴───────────────────────────┘
```

## Card-by-Card Breakdown

### 1. Course Information Card (Primary)

```
┌─ Course Information (Blue Header) ─┐
│                                    │
│ Course Code:  CS101               │
│ Credits:      4 Credits            │
│ Status:       [Active Badge]        │
│ Created Date: 03/30/2026           │
│                                    │
│ Description:                       │
│ Object-Oriented Programming        │
│ fundamentals using Java...         │
│                                    │
└────────────────────────────────────┘
```

### 2. Statistics Cards (Row of 2)

```
┌──────────────────────┐  ┌──────────────────────┐
│   📚 Subjects        │  │    👥 Batches        │
│                      │  │                      │
│        5             │  │        4             │
│                      │  │                      │
│ Total subjects in    │  │ Total batches for    │
│ this course          │  │ this course          │
└──────────────────────┘  └──────────────────────┘
```

### 3. Subjects Section

```
┌─ 📚 Subjects (Green Header) ────────[Count: 5]─┐
│                                                │
│ ┌──────────────────┐  ┌──────────────────┐    │
│ │ OOP Concepts     │  │ Data Structures  │    │
│ │ CS101            │  │ CS102            │    │
│ │ [Active Badge]   │  │ [Active Badge]   │    │
│ └──────────────────┘  └──────────────────┘    │
│                                                │
│ ┌──────────────────┐  ┌──────────────────┐    │
│ │ Databases        │  │ Web Development  │    │
│ │ CS103            │  │ CS104            │    │
│ │ [Active Badge]   │  │ [Active Badge]   │    │
│ └──────────────────┘  └──────────────────┘    │
│                                                │
│ ┌──────────────────┐                          │
│ │ Algorithms       │                          │
│ │ CS105            │                          │
│ │ [Active Badge]   │                          │
│ └──────────────────┘                          │
│                                                │
└────────────────────────────────────────────────┘
```

### 4. Batches Section (Table)

```
┌─ 👥 Course Batches (Info Header) ────────[Count: 4]─┐
│                                                      │
│ Batch Name    │ Academic Year │ Semester │ Status   │
│───────────────┼───────────────┼──────────┼────────  │
│ Batch A 2026  │  2025-2026    │ Sem 1    │ [Active] │
│ Batch B 2026  │  2025-2026    │ Sem 2    │ [Active] │
│ Batch C 2026  │  2026-2027    │ Sem 1    │ [Active] │
│ Batch D 2026  │  2026-2027    │ Sem 2    │ [Active] │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### 5. Quick Stats Sidebar Card

```
┌─ 📊 Quick Stats (Dark Header) ─┐
│                                │
│ Total Enrollments:        45   │
│ Active Subjects:           3   │
│ Course Status:      [Active]   │
│                                │
│ ─────────────────────────────  │
│                                │
│ Last Updated:                  │
│ 03/30/2026 at 10:30 AM         │
│                                │
└────────────────────────────────┘
```

### 6. Course Management Sidebar

```
┌─ ⚙️ Course Management (Warning Header) ─┐
│                                         │
│  [Edit Course Button]                  │
│  [Add Subject Button]                  │
│  [Add Batch Button]                    │
│  [Delete Course Button - Red]          │
│                                         │
└─────────────────────────────────────────┘
```

### 7. Timeline Sidebar Card

```
┌─ 📅 Timeline (Secondary Header) ─┐
│                                  │
│ Created:                         │
│ 03/30/2026 at 09:15 AM          │
│                                  │
│ ──────────────────────────────   │
│                                  │
│ Last Modified:                   │
│ 03/30/2026 at 10:30 AM          │
│                                  │
│ ──────────────────────────────   │
│                                  │
│ Academic Year:                   │
│ 2025-2026                        │
│                                  │
└──────────────────────────────────┘
```

## Modal Dialogs

### Add Subject Modal

```
┌─ Add Subject to Course (Success Green Header) ─────────────┐
│ [X Close]                                                 │
├───────────────────────────────────────────────────────────┤
│                                                           │
│ Subject Name *                                            │
│ [______________________]                                 │
│                                                           │
│ Subject Code *                                            │
│ [______________________]                                 │
│                                                           │
│ Description                                               │
│ [______________________________]                         │
│ [______________________________]                         │
│                                                           │
├───────────────────────────────────────────────────────────┤
│                       [Cancel]  [Add Subject]            │
└───────────────────────────────────────────────────────────┘
```

### Add Batch Modal

```
┌─ Add Batch to Course (Info Cyan Header) ──────────────────┐
│ [X Close]                                                │
├──────────────────────────────────────────────────────────┤
│                                                          │
│ Batch Name *                                             │
│ [______________________]                                │
│                                                          │
│ Academic Year *                                          │
│ [______________________]                                │
│                                                          │
│ Semester *                                               │
│ ┌────────────────────────┐                              │
│ │ Select Semester    [▼] │                              │
│ │ Semester 1         [✓] │                              │
│ │ Semester 2             │                              │
│ │ ... (up to 8)          │                              │
│ └────────────────────────┘                              │
│                                                          │
├──────────────────────────────────────────────────────────┤
│                       [Cancel]  [Add Batch]             │
└──────────────────────────────────────────────────────────┘
```

### Edit Course Modal

```
┌─ Edit Course (Primary Blue Header) ────────────────────────┐
│ [X Close]                                                 │
├───────────────────────────────────────────────────────────┤
│                                                           │
│ Course Name *                                             │
│ [______________________]                                 │
│                                                           │
│ Description                                               │
│ [______________________________]                         │
│ [______________________________]                         │
│                                                           │
│ Credits *                                                 │
│ [______] (1-10)                                          │
│                                                           │
│ Status                                                    │
│ ┌────────────────────────┐                              │
│ │ Active             [▼] │                              │
│ │ Active             [✓] │                              │
│ │ Archived               │                              │
│ └────────────────────────┘                              │
│                                                           │
├───────────────────────────────────────────────────────────┤
│                    [Cancel]  [Update Course]             │
└───────────────────────────────────────────────────────────┘
```

## Color Coding Reference

| Element | Color | Meaning |
|---------|-------|---------|
| Course Info Card | Blue | Primary information |
| Subjects Section | Green | Subject management |
| Batches Section | Cyan/Info | Batch information |
| Management Panel | Yellow/Warning | Coordinator actions |
| Delete Button | Red | Dangerous action |
| Active Status | Green | Course is active |
| Archived Status | Gray | Course archived |
| Stats Sidebar | Dark | Quick reference |
| Timeline Sidebar | Secondary | Historical info |

## Responsive Breakpoints

### Desktop (>992px)
- Two-column layout (67% main, 33% sidebar)
- Full width cards
- All controls visible

### Tablet (768-992px)
- Sidebar moves below content
- Cards stack
- Some padding adjustments

### Mobile (<768px)
- Single column layout
- Full width cards
- Compact button sizes
- Stacked modals

## Interactive States

### Buttons
- **Hover**: Slight lift (translateY -2px) + shadow increase
- **Active**: Darker background
- **Disabled**: Reduced opacity

### Cards
- **Hover**: Shadow increase + lift effect
- **Focus**: Outline highlight

### Tables
- **Row Hover**: Light gray background
- **Action Button Hover**: Color change

## User Flow

```
1. Courses Page
    ↓
    [Click "Go to Course" Button]
    ↓
2. Course Details Page Loads
    ├─ Fetch Course Details
    ├─ Load Subjects
    └─ Load Batches
    ↓
3. Display All Information
    ├─ Main Content Area
    └─ Sidebar Controls
    ↓
4. Coordinator Actions (if role=coordinator)
    ├─ [Edit Course] → Edit Modal
    ├─ [Add Subject] → Add Subject Modal
    ├─ [Add Batch] → Add Batch Modal
    └─ [Delete Course] → Confirmation → Delete
    ↓
5. Navigation Options
    ├─ [Back to Courses] → Return to Courses Page
    ├─ [View Subject] → Subject Details (future)
    └─ [View Batch] → Batch Details (future)
```

## Accessibility Features

- Semantic HTML structure
- Proper heading hierarchy (H1, H4, H5)
- Color not the only indicator
- Button labels for icons
- Form labels properly associated
- ARIA roles where appropriate
- Keyboard navigation support
- Focus indicators on interactive elements

## Performance Metrics

- **Initial Load**: Course details visible immediately
- **Subjects Load**: Lazy loaded after initial view
- **Batches Load**: Lazy loaded after initial view
- **Modal Open**: <100ms
- **Form Submit**: API dependent (typically <500ms)

## Notes for Development

1. All modals are reusable Bootstrap modals
2. Form validation happens client-side before submission
3. All API calls include JWT authentication
4. Error messages display in dismissible alerts
5. Loading states prevent duplicate submissions
6. Course ID obtained from URL query parameter (?courseId=X)

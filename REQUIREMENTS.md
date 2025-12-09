# VulnRadar - Requirements Specification

## 1. Functional Requirements

### 1.1 Authentication & Authorization

#### FR-001: User Registration
- **Requirement**: System shall allow new users to create accounts
- **Details**:
  - User must provide: email, password, full name
  - Email must be valid format
  - Password must be minimum 8 characters
  - First registered user automatically becomes Admin
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-002: User Login
- **Requirement**: System shall authenticate users via Firebase
- **Details**:
  - Users log in with email and password
  - System validates credentials
  - Session token is generated and stored
  - User is redirected to role-appropriate dashboard
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-003: Role-Based Access Control
- **Requirement**: System shall enforce role-based permissions
- **Details**:
  - Three roles: Admin, Manager, Employee
  - Each role has different access levels
  - Backend validates role on each request
  - Frontend shows/hides features based on role
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-004: Session Management
- **Requirement**: System shall manage user sessions securely
- **Details**:
  - Session persists until logout or expiration
  - Token is verified on each API request
  - Invalid tokens result in authentication error
  - Logout invalidates session
- **Priority**: High
- **Status**: ✅ Implemented

---

### 1.2 Vendor Management

#### FR-005: Vendor Selection (Admin)
- **Requirement**: Admin shall select vendors for their company
- **Details**:
  - Admin can view all available vendors
  - Admin can search vendors by name
  - Admin can select multiple vendors
  - Selected vendors are saved to company profile
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-006: Vendor Display
- **Requirement**: System shall display company's selected vendors
- **Details**:
  - Shows vendor name, type, and description
  - Categorizes vendors (Hardware, Software, Both)
  - Displays statistics (total, hardware, software counts)
  - Modern, visually appealing interface
- **Priority**: Medium
- **Status**: ✅ Implemented

---

### 1.3 Vulnerability Management

#### FR-007: Vulnerability Ingestion
- **Requirement**: System shall import vulnerabilities from NVD API
- **Details**:
  - Admin can trigger ingestion for selected vendors
  - System connects to NVD API
  - Only relevant vulnerabilities are imported
  - Duplicates are prevented
  - Ingestion results are displayed
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-008: Vulnerability Filtering
- **Requirement**: System shall filter vulnerabilities by vendor and TLP
- **Details**:
  - Only vulnerabilities for selected vendors are shown
  - TLP filtering based on user role:
    - Admin: All TLP ratings
    - Manager: GREEN and AMBER
    - Employee: GREEN only
  - Admin can manually filter by TLP rating
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-009: Vulnerability Display
- **Requirement**: System shall display vulnerability information
- **Details**:
  - Shows: CVE ID, title, description, severity, vendor, TLP rating
  - Sorted by published date (newest first)
  - Pagination or limit (500 max)
  - Responsive table layout
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-010: Vulnerability Details
- **Requirement**: Users shall view detailed vulnerability information
- **Details**:
  - Modal displays full vulnerability details
  - Shows: CVE ID, title, description, severity, CVSS score
  - Shows: vendor, published date, affected products
  - Link to NVD page
  - Remediation guidance section
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-011: TLP Rating System
- **Requirement**: System shall implement Traffic Light Protocol
- **Details**:
  - Three TLP levels: RED, AMBER, GREEN
  - TLP is independent of severity
  - TLP determines visibility per role
  - TLP is assigned based on source, CVE status, and date
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-012: Remediation Guidance
- **Requirement**: System shall provide remediation guidance
- **Details**:
  - Step-by-step remediation instructions
  - Links to vendor security advisories
  - Link to CVE detail page
  - Priority warnings for critical/high severity
- **Priority**: Medium
- **Status**: ✅ Implemented

---

### 1.4 Task Management

#### FR-013: Task Assignment (Admin)
- **Requirement**: Admin shall assign vulnerabilities to users
- **Details**:
  - Admin selects user from dropdown
  - Admin can set priority level
  - Admin can add initial notes
  - Task is created and assigned
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-014: Task Claiming (Self-Assign)
- **Requirement**: Managers and employees shall claim vulnerabilities
- **Details**:
  - "Claim & Start Working" button on unassigned vulnerabilities
  - Creates task assigned to current user
  - Vulnerability disappears from main list
  - Prevents duplicate claims
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-015: Task Viewing
- **Requirement**: Users shall view their assigned tasks
- **Details**:
  - "My Tasks" page shows all user's tasks
  - Displays: CVE ID, title, status, priority, date
  - Tasks are sortable
  - Completed tasks viewable separately (admin)
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-016: Task Status Updates
- **Requirement**: Users shall update task status
- **Details**:
  - Statuses: Pending, In Progress, Resolved, Closed
  - Status changes are saved
  - Status is visible to admins/managers
  - Status history (future enhancement)
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-017: Task Notes
- **Requirement**: Users shall add notes to tasks
- **Details**:
  - Notes textarea in task update modal
  - Notes are appended (not replaced)
  - Notes show sender and timestamp
  - Conversation-style display
  - Admin notes are clearly labeled
- **Priority**: High
- **Status**: ✅ Implemented

#### FR-018: Assigned Vulnerability Filtering
- **Requirement**: System shall hide assigned vulnerabilities from main list
- **Details**:
  - Assigned vulnerabilities don't appear for non-admins
  - Admin can see all vulnerabilities including assigned
  - Assigned status is shown to admin
  - Prevents duplicate claims
- **Priority**: High
- **Status**: ✅ Implemented

---

### 1.5 Completed Vulnerabilities

#### FR-019: View Completed Vulnerabilities (Admin)
- **Requirement**: Admin shall view completed vulnerabilities
- **Details**:
  - Separate page for completed vulnerabilities
  - Shows all vulnerabilities with closed tasks
  - Displays who resolved each
  - Shows completion information
- **Priority**: Medium
- **Status**: ✅ Implemented

---

## 2. Non-Functional Requirements

### 2.1 Performance Requirements

#### NFR-001: Response Time
- **Requirement**: API responses shall be under 2 seconds
- **Details**:
  - Database queries optimized with indexes
  - Efficient SQL queries
  - Connection pooling
- **Priority**: Medium
- **Status**: ✅ Implemented

#### NFR-002: Scalability
- **Requirement**: System shall handle 1000+ vulnerabilities
- **Details**:
  - Pagination or limit on large datasets
  - Database indexes for performance
  - Efficient filtering at database level
- **Priority**: Medium
- **Status**: ✅ Implemented

---

### 2.2 Security Requirements

#### NFR-003: Authentication Security
- **Requirement**: System shall use secure authentication
- **Details**:
  - Firebase authentication (industry standard)
  - Password hashing (handled by Firebase)
  - Token-based session management
  - Secure token storage
- **Priority**: High
- **Status**: ✅ Implemented

#### NFR-004: Data Protection
- **Requirement**: System shall protect against common attacks
- **Details**:
  - SQL injection prevention (parameterized queries)
  - XSS protection (HTML escaping)
  - CORS configuration
  - Input validation
- **Priority**: High
- **Status**: ✅ Implemented

#### NFR-005: Access Control
- **Requirement**: System shall enforce access control
- **Details**:
  - Role-based permissions
  - TLP-based filtering
  - Company-based data isolation
  - Backend validation of all requests
- **Priority**: High
- **Status**: ✅ Implemented

---

### 2.3 Usability Requirements

#### NFR-006: User Interface
- **Requirement**: Interface shall be intuitive and modern
- **Details**:
  - Bootstrap 5 for consistent styling
  - Responsive design
  - Clear navigation
  - Helpful error messages
- **Priority**: High
- **Status**: ✅ Implemented

#### NFR-007: Browser Compatibility
- **Requirement**: System shall work on modern browsers
- **Details**:
  - Chrome 90+
  - Firefox 88+
  - Edge 90+
  - Safari 14+
- **Priority**: Medium
- **Status**: ✅ Implemented

---

### 2.4 Reliability Requirements

#### NFR-008: Error Handling
- **Requirement**: System shall handle errors gracefully
- **Details**:
  - User-friendly error messages
  - Logging of errors
  - Graceful degradation
  - No system crashes
- **Priority**: High
- **Status**: ✅ Implemented

#### NFR-009: Data Integrity
- **Requirement**: System shall maintain data integrity
- **Details**:
  - Foreign key constraints
  - Transaction support
  - Duplicate prevention
  - Data validation
- **Priority**: High
- **Status**: ✅ Implemented

---

### 2.5 Maintainability Requirements

#### NFR-010: Code Quality
- **Requirement**: Code shall be maintainable
- **Details**:
  - Clear code structure
  - Comments where needed
  - Consistent naming conventions
  - Modular design
- **Priority**: Medium
- **Status**: ✅ Implemented

#### NFR-011: Documentation
- **Requirement**: System shall be documented
- **Details**:
  - README with setup instructions
  - Code comments
  - API documentation
  - User guide
- **Priority**: Medium
- **Status**: ✅ Implemented

---

## 3. Business Requirements

### BR-001: Vendor-Based Filtering
- **Requirement**: System shall filter vulnerabilities by company vendors
- **Business Value**: Reduces noise, shows only relevant vulnerabilities
- **Priority**: High
- **Status**: ✅ Implemented

### BR-002: TLP Implementation
- **Requirement**: System shall implement TLP for information sensitivity
- **Business Value**: Ensures appropriate information sharing
- **Priority**: High
- **Status**: ✅ Implemented

### BR-003: Task Management
- **Requirement**: System shall track vulnerability remediation
- **Business Value**: Enables accountability and progress tracking
- **Priority**: High
- **Status**: ✅ Implemented

### BR-004: Role-Based Access
- **Requirement**: System shall provide different access levels
- **Business Value**: Security and appropriate access control
- **Priority**: High
- **Status**: ✅ Implemented

---

## 4. System Requirements

### SR-001: Backend Server
- **Requirement**: .NET 8.0 runtime
- **Details**: ASP.NET Core web API
- **Priority**: High
- **Status**: ✅ Implemented

### SR-002: Database
- **Requirement**: MySQL 8.0+
- **Details**: Relational database
- **Priority**: High
- **Status**: ✅ Implemented

### SR-003: Frontend
- **Requirement**: Modern web browser
- **Details**: JavaScript enabled
- **Priority**: High
- **Status**: ✅ Implemented

### SR-004: External Services
- **Requirement**: Firebase project, NVD API access
- **Details**: Internet connection required
- **Priority**: High
- **Status**: ✅ Implemented

---

## 5. Requirements Traceability

### Requirements by Epic

**Epic 1: Authentication**
- FR-001, FR-002, FR-003, FR-004

**Epic 2: Vendor Management**
- FR-005, FR-006

**Epic 3: Vulnerability Management**
- FR-007, FR-008, FR-009, FR-010, FR-011, FR-012

**Epic 4: Task Management**
- FR-013, FR-014, FR-015, FR-016, FR-017, FR-018

**Epic 5: Completed Vulnerabilities**
- FR-019

---

## 6. Requirements Status Summary

### Implemented Requirements
- ✅ All High Priority Functional Requirements
- ✅ All Security Requirements
- ✅ All Performance Requirements (basic)
- ✅ All Usability Requirements (basic)

### Future Requirements (Out of Scope)
- Email notifications
- Export functionality
- Advanced analytics
- Mobile app
- Multi-company support

---

**Document Version**: 1.0  
**Last Updated**: [Current Date]  
**Status**: Complete



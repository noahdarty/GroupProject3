# VulnRadar - Vulnerability Management System

A full-stack web application for managing cybersecurity vulnerabilities with role-based access control and Traffic Light Protocol (TLP) ratings.

## 🚀 Quick Access

### Live Application
- **Frontend + Backend (Deployed):** https://vulnradar2-6c4dc22c8f92.herokuapp.com
- **API Documentation (Swagger):** https://vulnradar2-6c4dc22c8f92.herokuapp.com/swagger
- **Database:** Heroku JawsDB MySQL (see Database Access section below)

---

## 📋 Table of Contents

1. [Database Access](#database-access)
2. [Running the Application Locally](#running-the-application-locally)
3. [Project Structure](#project-structure)
4. [Features](#features)
5. [Technology Stack](#technology-stack)
6. [Setup Instructions](#setup-instructions)

---

## 🗄️ Database Access

### For Teachers/Instructors

The database is hosted on Heroku JawsDB MySQL. To access it:

#### Connection String

**Full Connection String:**
```
mysql://u6ya6l1ogtazrvrs:l15y9wr9dj2i4yla@hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com:3306/qfewi9g30broii73
```

**Parsed Connection Details:**
- **Hostname:** `hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com`
- **Port:** `3306`
- **Username:** `u6ya6l1ogtazrvrs`
- **Password:** `l15y9wr9dj2i4yla`
- **Database:** `qfewi9g30broii73`

#### Method 1: MySQL Workbench (Recommended)

1. **Open MySQL Workbench**
2. **Click the + button** to create a new connection
3. **Enter the connection details:**
   - **Connection Name:** VulnRadar Heroku (or any name)
   - **Connection Method:** Standard (TCP/IP)
   - **Hostname:** `hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com`
   - **Port:** `3306`
   - **Username:** `u6ya6l1ogtazrvrs`
   - **Password:** `l15y9wr9dj2i4yla` (click "Store in Keychain")
   - **Default Schema:** `qfewi9g30broii73`
4. **Click "Test Connection"** to verify
5. **Click "OK"** to save
6. **Double-click the connection** to connect
7. **View tables:** Expand the database → Tables in the left sidebar
   - You'll see: Users, Companies, Vendors, Vulnerabilities, Tasks, CompanyVendors, UserCompanies

#### Method 2: Using the Connection String Directly

You can use the full connection string in any MySQL client that supports the format:
```
mysql://u6ya6l1ogtazrvrs:l15y9wr9dj2i4yla@hcm4e9frmbwfez47.cbetxkdyhwsb.us-east-1.rds.amazonaws.com:3306/qfewi9g30broii73
```

**Note:** The database is independent of the application - it can be accessed even if the app is down.

---

## 💻 Running the Application Locally

### Prerequisites
- Python 3.x (for frontend server) OR PowerShell (Windows)
- .NET 8.0 SDK (for backend, if running locally)
- MySQL Workbench (for database access)

### Option 1: Frontend Only (Connects to Heroku Backend)

The frontend is already configured to connect to the Heroku API.

1. **Double-click `start-frontend.bat`**
   - This starts a local web server
   - Opens browser at `http://localhost:8080`
   - Frontend connects to: `https://vulnradar2-6c4dc22c8f92.herokuapp.com`

2. **Or manually:**
   ```bash
   cd frontend
   python -m http.server 8080
   # Then open http://localhost:8080 in your browser
   ```

### Option 2: Full Stack (Frontend + Backend Locally)

1. **Start Backend:**
   ```bash
   # Double-click start-backend.bat
   # OR manually:
   cd backend/MyProject.API
   dotnet run
   ```
   - Backend runs on: `http://localhost:5155`

2. **Update Frontend Config:**
   - Edit `frontend/scripts/api-config.js`
   - Change to: `window.API_BASE_URL = 'http://localhost:5155';`

3. **Start Frontend:**
   ```bash
   # Double-click start-frontend.bat
   # Opens at http://localhost:8080
   ```

### Option 3: Use Deployed Heroku App

Simply visit: **https://vulnradar2-6c4dc22c8f92.herokuapp.com**

No setup needed - everything is already deployed and running!

---

## 📁 Project Structure

```
GroupProject3/
├── frontend/                 # Frontend (HTML, CSS, JavaScript)
│   ├── index.html           # Main HTML file
│   ├── scripts/             # JavaScript files
│   │   ├── api-config.js    # API URL configuration
│   │   ├── auth.js          # Authentication
│   │   ├── main.js          # Main app logic
│   │   ├── vulnerabilities.js
│   │   ├── tasks.js
│   │   └── ...
│   └── styles/              # CSS files
├── backend/                 # Backend (.NET API)
│   └── MyProject.API/
│       ├── Program.cs       # Main API code
│       ├── Data/            # Database service
│       ├── Models/          # Data models
│       ├── Services/        # Firebase service
│       └── Database/        # SQL scripts
│           ├── schema.sql   # Database schema
│           ├── seed_data.sql
│           └── ...
├── start-frontend.bat       # Start frontend server
├── start-backend.bat        # Start backend server
└── README.md                # This file
```

---

## ✨ Features

### User Roles
- **Admin:** Full access, can assign tasks, close vulnerabilities, see all TLP ratings
- **Manager:** Can see GREEN and AMBER vulnerabilities, manage assigned tasks
- **Employee:** Can see GREEN vulnerabilities only, manage assigned tasks

### TLP (Traffic Light Protocol) Ratings
- **RED:** Most sensitive - only admins can see and assign
- **AMBER:** Moderately sensitive - admins and managers can see
- **GREEN:** Shareable - all users can see

### Key Features
- ✅ User authentication with Firebase
- ✅ Company and vendor management
- ✅ Vulnerability tracking and assignment
- ✅ Task management with status updates
- ✅ TLP-based access control
- ✅ Role-based permissions
- ✅ Completed vulnerabilities tracking (admin only)

---

## 🛠️ Technology Stack

### Frontend
- **HTML5, CSS3, JavaScript (Vanilla)**
- **Bootstrap 5.3** (via CDN)
- **Firebase Authentication**

### Backend
- **.NET 8.0** (ASP.NET Core Web API)
- **MySQL** (via MySqlConnector)
- **Swagger/OpenAPI** (API documentation)

### Database
- **MySQL** (hosted on Heroku JawsDB)
- **Tables:** Users, Companies, Vendors, Vulnerabilities, Tasks, CompanyVendors, UserCompanies

### Deployment
- **Heroku** (Platform as a Service)
- **JawsDB** (MySQL addon)

---

## 🔧 Setup Instructions

### For Development

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd GroupProject3
   ```

2. **Backend Setup:**
   - Install .NET 8.0 SDK
   - Update `backend/MyProject.API/appsettings.json` with local database connection
   - Run: `cd backend/MyProject.API && dotnet run`

3. **Frontend Setup:**
   - No build step required (vanilla JavaScript)
   - Update `frontend/scripts/api-config.js` if using local backend
   - Run: `start-frontend.bat` or use a local web server

### For Production (Heroku)

The application is already deployed to Heroku:
- **App Name:** vulnradar2
- **URL:** https://vulnradar2-6c4dc22c8f92.herokuapp.com
- **Database:** JawsDB MySQL (automatically configured)

---

## 🔐 Default Accounts

After running the seed data script, you can create accounts through the signup page. The first admin account should be created manually or through the database.

---

## 📊 Database Schema

### Main Tables
- **Users:** User accounts with roles and TLP ratings
- **Companies:** Company information
- **Vendors:** Software/hardware vendors
- **Vulnerabilities:** CVE data with TLP ratings
- **Tasks:** Vulnerability assignments
- **CompanyVendors:** Company-vendor relationships
- **UserCompanies:** User-company relationships

See `backend/MyProject.API/Database/schema.sql` for full schema.

---

## 🌐 Important URLs

- **Live App:** https://vulnradar2-6c4dc22c8f92.herokuapp.com
- **API Swagger:** https://vulnradar2-6c4dc22c8f92.herokuapp.com/swagger
- **Heroku Dashboard:** https://dashboard.heroku.com/apps/vulnradar2

---

## 📝 API Endpoints

Key endpoints (see Swagger for full documentation):

- `POST /api/auth/verify-token` - User authentication
- `GET /api/vulnerabilities/company` - Get vulnerabilities for company
- `POST /api/tasks` - Assign vulnerability to user
- `GET /api/tasks` - Get user's tasks
- `PUT /api/tasks/{id}` - Update task status
- `GET /api/user/vendors` - Get company's vendors
- `POST /api/user/vendors` - Save vendor selections (admin)

---

## 🐛 Troubleshooting

### Frontend won't connect to backend
- Check `frontend/scripts/api-config.js` - ensure it points to the correct API URL
- Verify the Heroku app is running: https://vulnradar2-6c4dc22c8f92.herokuapp.com

### Database connection issues
- Verify the `JAWSDB_URL` in Heroku config vars
- Check that JawsDB addon is active in Heroku dashboard

### Can't see vulnerabilities
- Make sure vendors are selected for your company (admin must select vendors)
- Check your TLP rating matches the vulnerability TLP rating
- Verify you're logged in with the correct role

---

## 👥 Team Members

- Noah Darty
- Jeremy Kline
- Alex Herrin

---

## 📅 Project Timeline

- **Start Date:** Early November 2024
- **Due Date:** December 9, 2025
- **Duration:** ~1 month

---

## 📚 Additional Documentation

- `TEACHER_DATABASE_ACCESS.md` - Detailed database access instructions
- `HOW_TO_CONNECT_TO_DATABASE.md` - Step-by-step MySQL Workbench guide
- `FIREBASE_SETUP.md` - Firebase configuration details

---

## 🆘 Support

For issues or questions:
1. Check the Swagger API documentation: `/swagger`
2. Review the database schema: `backend/MyProject.API/Database/schema.sql`
3. Check Heroku logs: `heroku logs --tail --app vulnradar2`

---

**Last Updated:** December 2024

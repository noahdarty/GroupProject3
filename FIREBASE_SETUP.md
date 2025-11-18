# Firebase Authentication Setup Instructions

## Step 1: Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Add project" or "Create a project"
3. Enter project name: `vulnradar` (or your preferred name)
4. Click "Continue"
5. (Optional) Disable Google Analytics if you don't need it
6. Click "Create project"
7. Wait for project creation, then click "Continue"

## Step 2: Enable Authentication

1. In Firebase Console, look at the left sidebar
2. Find the "Build" section/category and expand it (click on it if it's collapsed)
3. Click "Authentication" under the Build category
4. Click "Get started" (if this is your first time)
5. Click on the "Sign-in method" tab at the top
6. Click on "Email/Password" in the list
7. Enable "Email/Password" (toggle the switch ON)
8. **IMPORTANT:** Make sure "Email link (passwordless sign-in)" is OFF (we're using password authentication)
9. Click "Save"

## Step 2b: Configure Authorized Domains (IMPORTANT for Email Verification)

1. In Firebase Console → Authentication
2. Click on the "Settings" tab (next to "Sign-in method")
3. Scroll down to "Authorized domains"
4. Make sure these domains are listed:
   - `localhost` (should be there by default)
   - Your Firebase auth domain (e.g., `vulnradar-e3865.firebaseapp.com`)
5. If `localhost` is missing, click "Add domain" and add `localhost`
6. **For production:** Add your actual domain when you deploy

## Step 2c: Configure Email Templates (Optional but Recommended)

1. In Firebase Console → Authentication
2. Click on the "Templates" tab
3. Click on "Email address verification"
4. You can customize the email template here
5. Make sure the "Action URL" points to your app (or leave default)
6. Click "Save"

## Step 3: Get Firebase Configuration

1. In Firebase Console, click the gear icon ⚙️ next to "Project Overview"
2. Click "Project settings"
3. Scroll down to "Your apps" section
4. Click the web icon `</>` to add a web app
5. Register app name: `VulnRadar` (or any name)
6. Click "Register app"
7. **Copy the `firebaseConfig` object** - you'll need these values:
   - `apiKey`
   - `authDomain`
   - `projectId`
   - `storageBucket`
   - `messagingSenderId`
   - `appId`

## Step 4: Update Frontend Configuration

1. Open `frontend/scripts/firebase-config.js`
2. Replace the placeholder values with your Firebase config:

```javascript
const firebaseConfig = {
  apiKey: "YOUR_ACTUAL_API_KEY",
  authDomain: "YOUR_PROJECT_ID.firebaseapp.com",
  projectId: "YOUR_ACTUAL_PROJECT_ID",
  storageBucket: "YOUR_PROJECT_ID.appspot.com",
  messagingSenderId: "YOUR_ACTUAL_MESSAGING_SENDER_ID",
  appId: "YOUR_ACTUAL_APP_ID"
};
```

## Step 5: Update Backend Configuration

1. Open `backend/MyProject.API/appsettings.json`
2. Replace `YOUR_FIREBASE_PROJECT_ID` with your actual Firebase Project ID:

```json
"Firebase": {
  "ProjectId": "your-actual-project-id"
}
```

3. Do the same in `backend/MyProject.API/appsettings.Development.json`

## Step 6: Add Firebase UID Column to Database

Run this SQL script in MySQL:

```sql
USE vulnradar;

ALTER TABLE Users 
ADD COLUMN firebase_uid VARCHAR(128) UNIQUE NULL AFTER password_hash;

CREATE INDEX idx_firebase_uid ON Users(firebase_uid);
```

Or run the file: `backend/MyProject.API/Database/add_firebase_uid.sql`

## Step 7: Install Backend Dependencies

In the backend directory, restore NuGet packages:

```bash
cd backend/MyProject.API
dotnet restore
```

This will install the FirebaseAdmin package.

## Step 8: Test the Setup

1. Start the backend: Double-click `start-backend.bat`
2. Start the frontend: Double-click `start-frontend.bat`
3. Navigate to `http://localhost:8080/login.html`
4. Try creating an account or signing in

## Troubleshooting

- **"Firebase: Error (auth/invalid-api-key)"**: Check that you copied the correct API key in `firebase-config.js`
- **"Project ID not found"**: Verify the Project ID in `appsettings.json` matches your Firebase project
- **Database errors**: Make sure you ran the `add_firebase_uid.sql` script
- **CORS errors**: Make sure the backend is running and CORS is configured

## Security Notes

- Never commit your Firebase config with real values to public repositories
- The Firebase Admin SDK in the backend only needs the Project ID (not the full config)
- Firebase handles all password security - you don't need to hash passwords yourself


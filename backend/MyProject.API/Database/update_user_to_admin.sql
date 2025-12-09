-- Update your user to admin role
-- Run this in MySQL Workbench connected to your Heroku database

-- STEP 1: Find your user (replace 'your-email@example.com' with your actual email)
SELECT id, email, role, tlp_rating FROM Users WHERE email = 'your-email@example.com';

-- STEP 2: Update your user to admin (replace 'your-email@example.com' with your actual email)
UPDATE Users 
SET role = 'admin', 
    tlp_rating = 'RED'
WHERE email = 'your-email@example.com';

-- STEP 3: Verify the update
SELECT id, email, role, tlp_rating FROM Users WHERE email = 'your-email@example.com';


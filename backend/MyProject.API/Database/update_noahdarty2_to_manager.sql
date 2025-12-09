-- Update noahdarty2 user to manager role
-- Run this in MySQL Workbench connected to your Heroku database

-- First, find the user
SELECT id, email, role, tlp_rating FROM Users WHERE email LIKE '%noahdarty2%';

-- Update to manager
UPDATE Users 
SET role = 'manager', 
    tlp_rating = 'AMBER'
WHERE email LIKE '%noahdarty2%';

-- Verify the update
SELECT id, email, role, tlp_rating FROM Users WHERE email LIKE '%noahdarty2%';


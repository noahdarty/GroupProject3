-- Link admin user to a company
-- Run this in MySQL Workbench connected to your Heroku database

-- Step 1: Find your admin user
SELECT id, email, role, company_name FROM Users WHERE role = 'admin' OR email LIKE '%noahdarty%';

-- Step 2: Find available companies
SELECT id, name FROM Companies ORDER BY id LIMIT 10;

-- Step 3: Link admin to company (replace @userId and @companyId with actual values)
-- Example: Link user ID 1 to company ID 1
-- First, find your user ID and a company ID from the queries above, then run:

-- Replace these values:
-- SET @userId = 1;  -- Your admin user ID from Step 1
-- SET @companyId = 1;  -- A company ID from Step 2

-- Then run this:
-- INSERT INTO UserCompanies (user_id, company_id, is_primary)
-- VALUES (@userId, @companyId, TRUE)
-- ON DUPLICATE KEY UPDATE is_primary = TRUE;

-- OR use this direct version (replace with your actual IDs):
-- INSERT INTO UserCompanies (user_id, company_id, is_primary)
-- VALUES (1, 1, TRUE)
-- ON DUPLICATE KEY UPDATE is_primary = TRUE;

-- Step 4: Verify the link
-- SELECT u.id, u.email, u.role, c.id as company_id, c.name as company_name
-- FROM Users u
-- INNER JOIN UserCompanies uc ON u.id = uc.user_id
-- INNER JOIN Companies c ON uc.company_id = c.id
-- WHERE u.role = 'admin' OR u.email LIKE '%noahdarty%';


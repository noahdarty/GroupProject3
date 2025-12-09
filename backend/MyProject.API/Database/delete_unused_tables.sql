-- Delete unused tables: AuditLogs, VulnerabilityRatings, and VulnerabilityResolutions
-- Run this in MySQL Workbench connected to your Heroku database

-- Step 1: Drop VulnerabilityResolutions (has foreign keys to Tasks, Vulnerabilities, Companies, Users)
DROP TABLE IF EXISTS VulnerabilityResolutions;

-- Step 2: Drop VulnerabilityRatings (has foreign keys to Vulnerabilities, Companies)
DROP TABLE IF EXISTS VulnerabilityRatings;

-- Step 3: Drop AuditLogs (has foreign key to Users)
DROP TABLE IF EXISTS AuditLogs;

-- Step 4: Verify the tables are deleted (should return empty results)
SHOW TABLES LIKE 'AuditLogs';
SHOW TABLES LIKE 'VulnerabilityRatings';
SHOW TABLES LIKE 'VulnerabilityResolutions';

-- Step 5: List all remaining tables to confirm
SHOW TABLES;


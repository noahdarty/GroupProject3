-- Add Firebase UID and company_name columns to Users table for Heroku database
-- Run this in MySQL Workbench connected to your Heroku database

-- Add firebase_uid column (ignore error if it already exists)
ALTER TABLE Users 
ADD COLUMN firebase_uid VARCHAR(128) UNIQUE NULL AFTER password_hash;

-- Add company_name column (ignore error if it already exists)
ALTER TABLE Users 
ADD COLUMN company_name VARCHAR(200) NULL AFTER tlp_rating;

-- Create index on firebase_uid for faster lookups (ignore error if it already exists)
CREATE INDEX idx_firebase_uid ON Users(firebase_uid);


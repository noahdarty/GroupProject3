-- Add TLP rating column to Vulnerabilities table
ALTER TABLE Vulnerabilities 
ADD COLUMN tlp_rating ENUM('RED', 'AMBER', 'GREEN') NOT NULL DEFAULT 'GREEN' AFTER severity_level;

-- Add index for TLP rating
ALTER TABLE Vulnerabilities 
ADD INDEX idx_tlp_rating (tlp_rating);

-- Set initial TLP ratings based on severity (can be adjusted later)
-- Critical/High = RED (most sensitive)
-- Medium = AMBER (moderately sensitive)
-- Low = GREEN (can share within community)
UPDATE Vulnerabilities SET tlp_rating = 'RED' WHERE severity_level IN ('Critical', 'High');
UPDATE Vulnerabilities SET tlp_rating = 'AMBER' WHERE severity_level = 'Medium';
UPDATE Vulnerabilities SET tlp_rating = 'GREEN' WHERE severity_level = 'Low';
UPDATE Vulnerabilities SET tlp_rating = 'GREEN' WHERE severity_level = 'Unknown';



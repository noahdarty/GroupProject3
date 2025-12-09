-- Fix User TLP Ratings Based on Role
-- Employee: GREEN, Manager: AMBER, Admin: RED

USE vulnradar;

-- Update employees to GREEN
UPDATE Users 
SET tlp_rating = 'GREEN' 
WHERE role = 'employee' 
  AND (tlp_rating IS NULL OR tlp_rating NOT IN ('RED', 'AMBER', 'GREEN') OR tlp_rating != 'GREEN');

-- Update managers to AMBER
UPDATE Users 
SET tlp_rating = 'AMBER' 
WHERE role = 'manager' 
  AND (tlp_rating IS NULL OR tlp_rating NOT IN ('RED', 'AMBER', 'GREEN') OR tlp_rating != 'AMBER');

-- Update admins to RED
UPDATE Users 
SET tlp_rating = 'RED' 
WHERE role = 'admin' 
  AND (tlp_rating IS NULL OR tlp_rating NOT IN ('RED', 'AMBER', 'GREEN') OR tlp_rating != 'RED');

-- Verify the updates
SELECT role, tlp_rating, COUNT(*) as count
FROM Users
GROUP BY role, tlp_rating
ORDER BY role, tlp_rating;



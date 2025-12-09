-- Remove unused tables: AuditLogs and VulnerabilityResolutions
-- These tables are not used by the application

-- Drop VulnerabilityResolutions first (has foreign keys)
DROP TABLE IF EXISTS VulnerabilityResolutions;

-- Drop AuditLogs (has foreign keys)
DROP TABLE IF EXISTS AuditLogs;


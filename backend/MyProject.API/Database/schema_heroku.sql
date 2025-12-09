-- VulnRadar Database Schema
-- MySQL Database Setup for Heroku
-- (No USE statement - connect directly to the database)

-- 1. Users Table
CREATE TABLE IF NOT EXISTS Users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    role ENUM('admin', 'manager', 'employee') NOT NULL DEFAULT 'employee',
    tlp_rating ENUM('RED', 'AMBER', 'GREEN') NOT NULL DEFAULT 'GREEN',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_username (username),
    INDEX idx_email (email),
    INDEX idx_role (role)
);

-- 2. Companies Table
CREATE TABLE IF NOT EXISTS Companies (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    industry VARCHAR(100),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_name (name)
);

-- 3. UserCompanies Table (Many-to-Many: Users <-> Companies)
CREATE TABLE IF NOT EXISTS UserCompanies (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    company_id INT NOT NULL,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE,
    FOREIGN KEY (company_id) REFERENCES Companies(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_company (user_id, company_id),
    INDEX idx_user_id (user_id),
    INDEX idx_company_id (company_id)
);

-- 4. Vendors Table
CREATE TABLE IF NOT EXISTS Vendors (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(200) UNIQUE NOT NULL,
    vendor_type ENUM('hardware', 'software', 'both') NOT NULL DEFAULT 'both',
    description TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_name (name),
    INDEX idx_vendor_type (vendor_type)
);

-- 5. CompanyVendors Table (Many-to-Many: Companies <-> Vendors)
CREATE TABLE IF NOT EXISTS CompanyVendors (
    id INT PRIMARY KEY AUTO_INCREMENT,
    company_id INT NOT NULL,
    vendor_id INT NOT NULL,
    use_case_description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES Companies(id) ON DELETE CASCADE,
    FOREIGN KEY (vendor_id) REFERENCES Vendors(id) ON DELETE CASCADE,
    UNIQUE KEY unique_company_vendor (company_id, vendor_id),
    INDEX idx_company_id (company_id),
    INDEX idx_vendor_id (vendor_id)
);

-- 6. Vulnerabilities Table
CREATE TABLE IF NOT EXISTS Vulnerabilities (
    id INT PRIMARY KEY AUTO_INCREMENT,
    cve_id VARCHAR(50) UNIQUE,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    source VARCHAR(100) NOT NULL,
    source_url VARCHAR(500),
    published_date DATE,
    severity_score DECIMAL(3,1),
    severity_level ENUM('Critical', 'High', 'Medium', 'Low', 'Unknown') NOT NULL DEFAULT 'Unknown',
    tlp_rating ENUM('RED', 'AMBER', 'GREEN') NOT NULL DEFAULT 'GREEN',
    affected_products TEXT,
    vendor_id INT,
    raw_data JSON,
    is_duplicate BOOLEAN NOT NULL DEFAULT FALSE,
    duplicate_of_id INT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (vendor_id) REFERENCES Vendors(id) ON DELETE SET NULL,
    FOREIGN KEY (duplicate_of_id) REFERENCES Vulnerabilities(id) ON DELETE SET NULL,
    INDEX idx_cve_id (cve_id),
    INDEX idx_source (source),
    INDEX idx_severity_level (severity_level),
    INDEX idx_tlp_rating (tlp_rating),
    INDEX idx_vendor_id (vendor_id),
    INDEX idx_is_duplicate (is_duplicate),
    INDEX idx_published_date (published_date)
);

-- 7. VulnerabilityRatings Table (AI Ratings per Company)
CREATE TABLE IF NOT EXISTS VulnerabilityRatings (
    id INT PRIMARY KEY AUTO_INCREMENT,
    vulnerability_id INT NOT NULL,
    company_id INT NOT NULL,
    relevance_score INT NOT NULL DEFAULT 0 CHECK (relevance_score >= 0 AND relevance_score <= 100),
    ai_reasoning TEXT,
    is_relevant BOOLEAN NOT NULL DEFAULT FALSE,
    vendor_match BOOLEAN NOT NULL DEFAULT FALSE,
    use_case_match BOOLEAN NOT NULL DEFAULT FALSE,
    rated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (vulnerability_id) REFERENCES Vulnerabilities(id) ON DELETE CASCADE,
    FOREIGN KEY (company_id) REFERENCES Companies(id) ON DELETE CASCADE,
    UNIQUE KEY unique_vuln_company (vulnerability_id, company_id),
    INDEX idx_vulnerability_id (vulnerability_id),
    INDEX idx_company_id (company_id),
    INDEX idx_is_relevant (is_relevant),
    INDEX idx_relevance_score (relevance_score)
);

-- 8. Tasks Table (Assignments)
CREATE TABLE IF NOT EXISTS Tasks (
    id INT PRIMARY KEY AUTO_INCREMENT,
    vulnerability_id INT NOT NULL,
    company_id INT NOT NULL,
    assigned_by_user_id INT NOT NULL,
    assigned_to_user_id INT NOT NULL,
    priority ENUM('Critical', 'High', 'Medium', 'Low') NOT NULL DEFAULT 'Medium',
    status ENUM('pending', 'in_progress', 'resolved', 'closed') NOT NULL DEFAULT 'pending',
    notes TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    resolved_at DATETIME,
    FOREIGN KEY (vulnerability_id) REFERENCES Vulnerabilities(id) ON DELETE CASCADE,
    FOREIGN KEY (company_id) REFERENCES Companies(id) ON DELETE CASCADE,
    FOREIGN KEY (assigned_by_user_id) REFERENCES Users(id) ON DELETE RESTRICT,
    FOREIGN KEY (assigned_to_user_id) REFERENCES Users(id) ON DELETE RESTRICT,
    INDEX idx_vulnerability_id (vulnerability_id),
    INDEX idx_company_id (company_id),
    INDEX idx_assigned_to_user_id (assigned_to_user_id),
    INDEX idx_status (status),
    INDEX idx_priority (priority)
);

-- 9. VulnerabilityResolutions Table
CREATE TABLE IF NOT EXISTS VulnerabilityResolutions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    task_id INT UNIQUE,
    vulnerability_id INT NOT NULL,
    company_id INT NOT NULL,
    owner_user_id INT NOT NULL,
    resolution_status ENUM('not_started', 'investigating', 'patching', 'patched', 'mitigated', 'false_positive') NOT NULL DEFAULT 'not_started',
    resolution_notes TEXT,
    patch_applied_date DATETIME,
    verified_date DATETIME,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (task_id) REFERENCES Tasks(id) ON DELETE CASCADE,
    FOREIGN KEY (vulnerability_id) REFERENCES Vulnerabilities(id) ON DELETE CASCADE,
    FOREIGN KEY (company_id) REFERENCES Companies(id) ON DELETE CASCADE,
    FOREIGN KEY (owner_user_id) REFERENCES Users(id) ON DELETE RESTRICT,
    INDEX idx_task_id (task_id),
    INDEX idx_vulnerability_id (vulnerability_id),
    INDEX idx_company_id (company_id),
    INDEX idx_owner_user_id (owner_user_id),
    INDEX idx_resolution_status (resolution_status)
);

-- 10. AuditLogs Table (Optional but Recommended)
CREATE TABLE IF NOT EXISTS AuditLogs (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT,
    action_type VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50),
    entity_id INT,
    details JSON,
    ip_address VARCHAR(45),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE SET NULL,
    INDEX idx_user_id (user_id),
    INDEX idx_action_type (action_type),
    INDEX idx_entity_type (entity_type),
    INDEX idx_created_at (created_at)
);


-- VulnRadar Seed Data
-- Initial data for testing and development

USE vulnradar;

-- Insert sample vendors
INSERT INTO Vendors (name, vendor_type, description) VALUES
('Microsoft', 'software', 'Microsoft software products including Windows, Office, Azure'),
('Cisco', 'both', 'Cisco networking hardware and software'),
('Oracle', 'software', 'Oracle database and enterprise software'),
('VMware', 'software', 'VMware virtualization software'),
('Fortinet', 'both', 'Fortinet security hardware and software'),
('Palo Alto Networks', 'both', 'Palo Alto Networks security solutions'),
('SAP', 'software', 'SAP enterprise software'),
('IBM', 'both', 'IBM hardware and software solutions'),
('Dell', 'hardware', 'Dell hardware products'),
('HP', 'hardware', 'HP hardware products')
ON DUPLICATE KEY UPDATE name=name;

-- Insert a default admin user (password: Admin123! - you should change this)
-- Password hash is for "Admin123!" using BCrypt (you'll need to generate proper hashes in your app)
INSERT INTO Users (username, email, password_hash, full_name, role, tlp_rating, is_active) VALUES
('admin', 'admin@vulnradar.com', '$2a$11$placeholder_hash_change_in_app', 'System Administrator', 'admin', 'RED', TRUE)
ON DUPLICATE KEY UPDATE username=username;

-- Insert a sample company
INSERT INTO Companies (name, description, industry) VALUES
('Bio-ISAC', 'Biotechnology Information Sharing and Analysis Center', 'Biotechnology')
ON DUPLICATE KEY UPDATE name=name;



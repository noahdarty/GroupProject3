-- Sample Vulnerabilities for Testing
-- Run this in MySQL Workbench connected to your Heroku database
-- Make sure you have vendors in the database first (from seed_data.sql)

-- STEP 1: First, get your vendor IDs by running this query:
-- SELECT id, name FROM Vendors;

-- STEP 2: Replace the vendor_id values below (1, 2, 3, etc.) with your actual vendor IDs
-- STEP 3: Remove or comment out the "USE vulnradar;" line if it exists
-- STEP 4: Run this entire script

-- Sample vulnerability for Microsoft (assuming vendor_id = 1)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES (
    'CVE-2024-0001',
    'Windows Remote Code Execution Vulnerability',
    'A critical vulnerability in Windows that allows remote code execution. This affects Windows 10 and Windows 11 systems.',
    'CVE Database',
    'https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2024-0001',
    '2024-01-15',
    9.8,
    'Critical',
    'RED',
    'Windows 10, Windows 11',
    1,  -- Replace with actual Microsoft vendor_id
    FALSE
);

-- Sample vulnerability for Cisco (assuming vendor_id = 2)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES (
    'CVE-2024-0002',
    'Cisco Router Authentication Bypass',
    'A high severity vulnerability in Cisco routers that allows authentication bypass.',
    'CVE Database',
    'https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2024-0002',
    '2024-02-01',
    8.5,
    'High',
    'AMBER',
    'Cisco IOS, Cisco IOS XE',
    2,  -- Replace with actual Cisco vendor_id
    FALSE
);

-- Sample vulnerability for Oracle (assuming vendor_id = 3)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES (
    'CVE-2024-0003',
    'Oracle Database SQL Injection',
    'A medium severity SQL injection vulnerability in Oracle Database.',
    'CVE Database',
    'https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2024-0003',
    '2024-02-10',
    6.5,
    'Medium',
    'GREEN',
    'Oracle Database 19c, Oracle Database 21c',
    3,  -- Replace with actual Oracle vendor_id
    FALSE
);

-- Sample vulnerability for VMware (assuming vendor_id = 4)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES (
    'CVE-2024-0004',
    'VMware vSphere Privilege Escalation',
    'A high severity privilege escalation vulnerability in VMware vSphere.',
    'CVE Database',
    'https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2024-0004',
    '2024-02-20',
    7.8,
    'High',
    'AMBER',
    'VMware vSphere 7.0, VMware vSphere 8.0',
    4,  -- Replace with actual VMware vendor_id
    FALSE
);

-- Sample vulnerability for Fortinet (assuming vendor_id = 5)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES (
    'CVE-2024-0005',
    'Fortinet FortiOS Buffer Overflow',
    'A critical buffer overflow vulnerability in Fortinet FortiOS.',
    'CVE Database',
    'https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2024-0005',
    '2024-03-01',
    9.1,
    'Critical',
    'RED',
    'FortiOS 7.0, FortiOS 7.2',
    5,  -- Replace with actual Fortinet vendor_id
    FALSE
);


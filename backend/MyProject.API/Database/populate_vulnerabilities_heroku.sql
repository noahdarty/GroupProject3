-- Populate Vulnerabilities for Heroku Database
-- This script inserts sample vulnerabilities for common vendors
-- Run this in MySQL Workbench connected to your Heroku database

-- STEP 1: First, get your vendor IDs by running this query:
-- SELECT id, name FROM Vendors ORDER BY id;

-- STEP 2: Replace the vendor_id values below with your actual vendor IDs from Step 1
-- STEP 3: Run this entire script

-- Microsoft vulnerabilities
-- REPLACE THE "1" BELOW WITH YOUR ACTUAL MICROSOFT VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-21338', 'Windows Kernel Elevation of Privilege', 'An elevation of privilege vulnerability exists in Windows Kernel. An attacker who successfully exploited this vulnerability could gain elevated privileges.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-21338', '2024-02-13', 7.8, 'High', 'AMBER', 'Windows 10, Windows 11', 1, FALSE),
('CVE-2024-20684', 'Windows Hyper-V Denial of Service', 'A denial of service vulnerability exists in Windows Hyper-V. An attacker could cause a denial of service.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20684', '2024-01-09', 5.5, 'Medium', 'GREEN', 'Windows Server 2019, Windows Server 2022', 1, FALSE),
('CVE-2024-20674', 'Windows Kernel Information Disclosure', 'An information disclosure vulnerability exists in Windows Kernel. An attacker could read memory contents.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20674', '2024-01-09', 5.5, 'Medium', 'GREEN', 'Windows 10, Windows 11', 1, FALSE),
('CVE-2024-20670', 'Windows Print Spooler Elevation of Privilege', 'An elevation of privilege vulnerability exists in Windows Print Spooler. An attacker could gain elevated privileges.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20670', '2024-01-09', 7.8, 'High', 'AMBER', 'Windows 10, Windows 11, Windows Server', 1, FALSE),
('CVE-2024-20656', 'Windows Kerberos Security Feature Bypass', 'A security feature bypass vulnerability exists in Windows Kerberos. An attacker could bypass security features.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20656', '2024-01-09', 8.1, 'High', 'AMBER', 'Windows Server 2016, Windows Server 2019, Windows Server 2022', 1, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- Cisco vulnerabilities
-- REPLACE THE "2" BELOW WITH YOUR ACTUAL CISCO VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-20272', 'Cisco IOS XE Software Command Injection', 'A command injection vulnerability in Cisco IOS XE Software could allow an authenticated attacker to execute arbitrary commands.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20272', '2024-01-24', 6.5, 'Medium', 'GREEN', 'Cisco IOS XE', 2, FALSE),
('CVE-2024-20273', 'Cisco IOS XE Software Privilege Escalation', 'A privilege escalation vulnerability in Cisco IOS XE Software could allow an authenticated attacker to gain elevated privileges.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20273', '2024-01-24', 7.2, 'High', 'AMBER', 'Cisco IOS XE', 2, FALSE),
('CVE-2024-20274', 'Cisco ASA/FTD Denial of Service', 'A denial of service vulnerability in Cisco Adaptive Security Appliance and Firepower Threat Defense.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20274', '2024-01-24', 5.3, 'Medium', 'GREEN', 'Cisco ASA, Cisco FTD', 2, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- Oracle vulnerabilities
-- REPLACE THE "3" BELOW WITH YOUR ACTUAL ORACLE VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-20931', 'Oracle Database Server SQL Injection', 'A SQL injection vulnerability in Oracle Database Server could allow an attacker to execute arbitrary SQL commands.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20931', '2024-01-16', 9.8, 'Critical', 'RED', 'Oracle Database 19c, Oracle Database 21c', 3, FALSE),
('CVE-2024-20945', 'Oracle WebLogic Server Remote Code Execution', 'A remote code execution vulnerability in Oracle WebLogic Server could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20945', '2024-01-16', 9.8, 'Critical', 'RED', 'Oracle WebLogic Server 12.2.1.4, 14.1.1.0', 3, FALSE),
('CVE-2024-20963', 'Oracle Java SE Information Disclosure', 'An information disclosure vulnerability in Oracle Java SE could allow an attacker to access sensitive information.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-20963', '2024-01-16', 5.3, 'Medium', 'GREEN', 'Oracle Java SE 8, 11, 17, 21', 3, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- VMware vulnerabilities
-- REPLACE THE "4" BELOW WITH YOUR ACTUAL VMWARE VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-22252', 'VMware vCenter Server Authentication Bypass', 'An authentication bypass vulnerability in VMware vCenter Server could allow an attacker to bypass authentication.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22252', '2024-02-13', 9.8, 'Critical', 'RED', 'VMware vCenter Server 7.0, 8.0', 4, FALSE),
('CVE-2024-22251', 'VMware ESXi Remote Code Execution', 'A remote code execution vulnerability in VMware ESXi could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22251', '2024-02-13', 9.8, 'Critical', 'RED', 'VMware ESXi 7.0, 8.0', 4, FALSE),
('CVE-2024-22250', 'VMware Workstation Information Disclosure', 'An information disclosure vulnerability in VMware Workstation could allow an attacker to access sensitive information.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22250', '2024-02-13', 5.5, 'Medium', 'GREEN', 'VMware Workstation 17.x', 4, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- Fortinet vulnerabilities
-- REPLACE THE "5" BELOW WITH YOUR ACTUAL FORTINET VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-23112', 'FortiOS Authentication Bypass', 'An authentication bypass vulnerability in FortiOS could allow an attacker to bypass authentication.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-23112', '2024-01-10', 9.8, 'Critical', 'RED', 'FortiOS 7.0, 7.2, 7.4', 5, FALSE),
('CVE-2024-23113', 'FortiGate SSL VPN Remote Code Execution', 'A remote code execution vulnerability in FortiGate SSL VPN could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-23113', '2024-01-10', 9.8, 'Critical', 'RED', 'FortiGate 6.4, 7.0, 7.2', 5, FALSE),
('CVE-2024-23114', 'FortiManager SQL Injection', 'A SQL injection vulnerability in FortiManager could allow an attacker to execute arbitrary SQL commands.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-23114', '2024-01-10', 7.2, 'High', 'AMBER', 'FortiManager 7.0, 7.2', 5, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- Palo Alto Networks vulnerabilities
-- REPLACE THE "6" BELOW WITH YOUR ACTUAL PALO ALTO NETWORKS VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-26304', 'Palo Alto Networks PAN-OS Remote Code Execution', 'A remote code execution vulnerability in Palo Alto Networks PAN-OS could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-26304', '2024-03-13', 9.8, 'Critical', 'RED', 'PAN-OS 10.2, 11.0, 11.1', 6, FALSE),
('CVE-2024-26305', 'Palo Alto Networks GlobalProtect Authentication Bypass', 'An authentication bypass vulnerability in Palo Alto Networks GlobalProtect could allow an attacker to bypass authentication.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-26305', '2024-03-13', 9.1, 'Critical', 'RED', 'GlobalProtect 5.2, 6.0', 6, FALSE),
('CVE-2024-26306', 'Palo Alto Networks Cortex XDR Denial of Service', 'A denial of service vulnerability in Palo Alto Networks Cortex XDR could allow an attacker to cause a denial of service.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-26306', '2024-03-13', 5.3, 'Medium', 'GREEN', 'Cortex XDR 7.0, 7.1', 6, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- SAP vulnerabilities
-- REPLACE THE "7" BELOW WITH YOUR ACTUAL SAP VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-22100', 'SAP NetWeaver AS ABAP Remote Code Execution', 'A remote code execution vulnerability in SAP NetWeaver AS ABAP could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22100', '2024-01-09', 9.8, 'Critical', 'RED', 'SAP NetWeaver 7.50, 7.52, 7.53', 7, FALSE),
('CVE-2024-22101', 'SAP BusinessObjects Information Disclosure', 'An information disclosure vulnerability in SAP BusinessObjects could allow an attacker to access sensitive information.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22101', '2024-01-09', 6.5, 'Medium', 'GREEN', 'SAP BusinessObjects 4.2, 4.3', 7, FALSE),
('CVE-2024-22102', 'SAP Solution Manager SQL Injection', 'A SQL injection vulnerability in SAP Solution Manager could allow an attacker to execute arbitrary SQL commands.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22102', '2024-01-09', 7.2, 'High', 'AMBER', 'SAP Solution Manager 7.2', 7, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- IBM vulnerabilities
-- REPLACE THE "8" BELOW WITH YOUR ACTUAL IBM VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-22301', 'IBM WebSphere Application Server Remote Code Execution', 'A remote code execution vulnerability in IBM WebSphere Application Server could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22301', '2024-01-16', 9.8, 'Critical', 'RED', 'WebSphere Application Server 9.0, 9.1', 8, FALSE),
('CVE-2024-22302', 'IBM Db2 Database Privilege Escalation', 'A privilege escalation vulnerability in IBM Db2 Database could allow an attacker to gain elevated privileges.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22302', '2024-01-16', 7.8, 'High', 'AMBER', 'IBM Db2 11.5, 12.0', 8, FALSE),
('CVE-2024-22303', 'IBM MQ Information Disclosure', 'An information disclosure vulnerability in IBM MQ could allow an attacker to access sensitive information.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-22303', '2024-01-16', 5.5, 'Medium', 'GREEN', 'IBM MQ 9.0, 9.1, 9.2', 8, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- Dell vulnerabilities
-- REPLACE THE "9" BELOW WITH YOUR ACTUAL DELL VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-25111', 'Dell PowerEdge Server Remote Code Execution', 'A remote code execution vulnerability in Dell PowerEdge Server could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-25111', '2024-01-10', 8.8, 'High', 'AMBER', 'PowerEdge R740, R750, R760', 9, FALSE),
('CVE-2024-25112', 'Dell iDRAC Authentication Bypass', 'An authentication bypass vulnerability in Dell iDRAC could allow an attacker to bypass authentication.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-25112', '2024-01-10', 9.1, 'Critical', 'RED', 'iDRAC 9.x', 9, FALSE),
('CVE-2024-25113', 'Dell OpenManage Privilege Escalation', 'A privilege escalation vulnerability in Dell OpenManage could allow an attacker to gain elevated privileges.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-25113', '2024-01-10', 7.2, 'High', 'AMBER', 'OpenManage 10.0, 10.1', 9, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- HP vulnerabilities
-- REPLACE THE "10" BELOW WITH YOUR ACTUAL HP VENDOR ID (the number before FALSE)
INSERT INTO Vulnerabilities (
    cve_id, title, description, source, source_url, published_date,
    severity_score, severity_level, tlp_rating, affected_products, vendor_id, is_duplicate
) VALUES
('CVE-2024-24101', 'HP iLO Remote Code Execution', 'A remote code execution vulnerability in HP Integrated Lights-Out (iLO) could allow an attacker to execute arbitrary code.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-24101', '2024-01-09', 9.8, 'Critical', 'RED', 'HP iLO 5, iLO 6', 10, FALSE),
('CVE-2024-24102', 'HP ProLiant Server Authentication Bypass', 'An authentication bypass vulnerability in HP ProLiant Server could allow an attacker to bypass authentication.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-24102', '2024-01-09', 8.1, 'High', 'AMBER', 'ProLiant Gen10, Gen11', 10, FALSE),
('CVE-2024-24103', 'HP OneView Information Disclosure', 'An information disclosure vulnerability in HP OneView could allow an attacker to access sensitive information.', 'NVD', 'https://nvd.nist.gov/vuln/detail/CVE-2024-24103', '2024-01-09', 5.3, 'Medium', 'GREEN', 'HP OneView 6.0, 6.1', 10, FALSE)
ON DUPLICATE KEY UPDATE title=title;

-- Verify the insertions
SELECT v.id, v.cve_id, v.title, v.severity_level, v.tlp_rating, ven.name as vendor_name
FROM Vulnerabilities v
INNER JOIN Vendors ven ON v.vendor_id = ven.id
ORDER BY v.published_date DESC
LIMIT 20;


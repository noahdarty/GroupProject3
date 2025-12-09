// Vulnerabilities Display Script
// Shows vulnerabilities filtered by company's vendors and user role

// Backend API base URL
if (typeof window.API_BASE_URL === 'undefined') {
  window.API_BASE_URL = 'http://localhost:5155';
}

// Load vulnerabilities for the user's company
async function loadVulnerabilities() {
  const vulnerabilitiesList = document.getElementById('vulnerabilitiesList');
  const vulnerabilitiesAlert = document.getElementById('vulnerabilitiesAlert');
  const refreshBtn = document.getElementById('refreshVulnerabilitiesBtn');
  const spinner = refreshBtn?.querySelector('.spinner-border');
  const tlpFilterSelect = document.getElementById('tlpFilterSelect');
  const tlpFilterContainer = document.getElementById('tlpFilterContainer');

  if (!vulnerabilitiesList) return;

  // Show loading state
  vulnerabilitiesList.innerHTML = `
    <div class="text-center">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading vulnerabilities...</span>
      </div>
    </div>
  `;
  if (vulnerabilitiesAlert) {
    vulnerabilitiesAlert.classList.add('d-none');
  }
  if (spinner) spinner.classList.remove('d-none');
  if (refreshBtn) refreshBtn.disabled = true;

  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      throw new Error('Not authenticated');
    }

    // Get TLP filter value if admin
    const tlpFilter = tlpFilterSelect?.value || '';
    const url = tlpFilter 
      ? `${window.API_BASE_URL}/api/vulnerabilities/company?tlpRating=${encodeURIComponent(tlpFilter)}`
      : `${window.API_BASE_URL}/api/vulnerabilities/company`;

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Failed to load vulnerabilities' }));
      throw new Error(errorData.message || `Server error: ${response.status}`);
    }

    const data = await response.json();

    if (!data.Success && !data.success) {
      throw new Error(data.Message || data.message || 'Failed to load vulnerabilities');
    }

    const vulnerabilities = data.Vulnerabilities || data.vulnerabilities || [];
    const userRole = data.UserRole || 'employee';

    // Show/hide TLP filter for admins
    if (tlpFilterContainer) {
      if (userRole === 'admin') {
        tlpFilterContainer.classList.remove('d-none');
      } else {
        tlpFilterContainer.classList.add('d-none');
      }
    }

    if (vulnerabilities.length === 0) {
      const filterText = tlpFilter ? `TLP:${tlpFilter} ` : '';
      vulnerabilitiesList.innerHTML = `
        <div class="alert alert-info">
          <strong>No vulnerabilities found.</strong><br>
          ${userRole === 'admin' 
            ? `No ${filterText}vulnerabilities found for your company's vendors.`
            : userRole === 'employee'
            ? 'No TLP:GREEN vulnerabilities found for your company\'s vendors.'
            : userRole === 'manager'
            ? 'No TLP:GREEN or TLP:AMBER vulnerabilities found for your company\'s vendors.'
            : 'No vulnerabilities match your role\'s visibility level.'}
        </div>
      `;
      return;
    }

    // Display vulnerabilities
    displayVulnerabilities(vulnerabilities, userRole, tlpFilter);

  } catch (error) {
    console.error('Error loading vulnerabilities:', error);
    vulnerabilitiesList.innerHTML = `
      <div class="alert alert-danger">
        <strong>Error loading vulnerabilities:</strong> ${error.message}
      </div>
    `;
  } finally {
    if (spinner) spinner.classList.add('d-none');
    if (refreshBtn) refreshBtn.disabled = false;
  }
}

// Display vulnerabilities in a table
function displayVulnerabilities(vulnerabilities, userRole, tlpFilter = '') {
  const vulnerabilitiesList = document.getElementById('vulnerabilitiesList');

    let html = `
    <div class="mb-3">
      <small class="text-muted">
        Showing <strong>${vulnerabilities.length}</strong> vulnerability/vulnerabilities 
        ${userRole === 'admin' 
          ? (tlpFilter ? `(Filtered: TLP:${tlpFilter})` : '(All TLP Ratings)')
          : userRole === 'employee' 
          ? '(TLP:GREEN only)' 
          : userRole === 'manager' 
          ? '(TLP:GREEN and TLP:AMBER)' 
          : '(All severities)'}
      </small>
    </div>
    <div class="table-responsive">
      <table class="table table-hover">
        <thead>
          <tr>
            <th>CVE ID</th>
            <th>Title</th>
            <th>Vendor</th>
            <th>Severity</th>
            <th>TLP Rating</th>
            <th>Published</th>
            <th>Assignment</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
  `;

  vulnerabilities.forEach(vuln => {
    const cveId = vuln.CveId || vuln.cveId || 'N/A';
    const title = (vuln.Title || vuln.title || 'No title').substring(0, 100) + (vuln.Title?.length > 100 ? '...' : '');
    const vendorName = vuln.VendorName || vuln.vendorName || 'Unknown';
    const severityLevel = vuln.SeverityLevel || vuln.severityLevel || 'Unknown';
    const publishedDate = vuln.PublishedDate || vuln.publishedDate || 'N/A';
    const sourceUrl = vuln.SourceUrl || vuln.sourceUrl || '#';
    const description = vuln.Description || vuln.description || '';
    
    // Get assignment info
    const taskId = vuln.TaskId || vuln.taskId;
    const taskStatus = vuln.TaskStatus || vuln.taskStatus;
    const assignedToEmail = vuln.AssignedToEmail || vuln.assignedToEmail;
    const assignedToName = vuln.AssignedToName || vuln.assignedToName;
    const isAssigned = taskId && taskStatus && taskStatus !== 'closed';
    
    // Get severity badge color
    let severityBadge = 'secondary';
    if (severityLevel === 'Critical') severityBadge = 'danger';
    else if (severityLevel === 'High') severityBadge = 'warning';
    else if (severityLevel === 'Medium') severityBadge = 'info';
    else if (severityLevel === 'Low') severityBadge = 'success';
    
    // Get TLP rating - just display as plain text
    const tlpRating = vuln.TlpRating || vuln.tlp_rating || 'N/A';
    
    // Build assignment display
    let assignmentHtml = '<span class="text-muted small">Not assigned</span>';
    if (isAssigned) {
      const assigneeName = assignedToName || assignedToEmail || 'Unknown';
      const assigneeDisplay = assignedToName ? `${assignedToName} (${assignedToEmail})` : assignedToEmail;
      
      // Get task status badge color
      let taskStatusBadge = 'secondary';
      if (taskStatus === 'resolved') taskStatusBadge = 'success';
      else if (taskStatus === 'in_progress') taskStatusBadge = 'primary';
      else if (taskStatus === 'pending') taskStatusBadge = 'warning';
      
      // For admins, show assignment info clearly
      if (userRole === 'admin') {
        assignmentHtml = `
          <div class="d-flex flex-column gap-1">
            <div>
              <span class="badge bg-${taskStatusBadge} small">${escapeHtml(taskStatus.replace('_', ' '))}</span>
            </div>
            <small class="text-dark">
              <strong>Assigned to:</strong><br>
              ${escapeHtml(assigneeDisplay)}
            </small>
          </div>
        `;
      } else {
        // For employees/managers, this shouldn't show (filtered out), but just in case
        assignmentHtml = `
          <div class="d-flex flex-column gap-1">
            <div>
              <span class="badge bg-${taskStatusBadge} small">${escapeHtml(taskStatus.replace('_', ' '))}</span>
            </div>
            <small class="text-muted">
              <strong>Assigned to:</strong><br>
              ${escapeHtml(assigneeDisplay)}
            </small>
          </div>
        `;
      }
    }

    html += `
      <tr>
        <td><code>${cveId}</code></td>
        <td>
          <strong>${escapeHtml(title)}</strong>
          ${description ? `<br><small class="text-muted">${escapeHtml(description.substring(0, 150))}${description.length > 150 ? '...' : ''}</small>` : ''}
        </td>
        <td>${escapeHtml(vendorName)}</td>
        <td><span class="badge bg-${severityBadge}">${severityLevel}</span></td>
        <td>${escapeHtml(tlpRating)}</td>
        <td><small>${publishedDate}</small></td>
        <td>${assignmentHtml}</td>
        <td>
          <button class="btn btn-sm btn-outline-primary" onclick="showVulnerabilityDetails(${vuln.Id || vuln.id})">View Details</button>
        </td>
      </tr>
    `;
  });

  html += `
        </tbody>
      </table>
    </div>
  `;

  vulnerabilitiesList.innerHTML = html;
}

// Helper function to escape HTML
function escapeHtml(text) {
  if (!text) return '';
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Initialize vulnerabilities display (called automatically, but section visibility is controlled by main.js)
window.initVulnerabilities = async function initVulnerabilities() {
  // This function is kept for compatibility but section visibility is now controlled by main.js
  // The section will be shown/hidden by the navbar links
};

// Setup refresh button and TLP filter
document.addEventListener('DOMContentLoaded', () => {
  const refreshBtn = document.getElementById('refreshVulnerabilitiesBtn');
  if (refreshBtn) {
    refreshBtn.addEventListener('click', async () => {
      await loadVulnerabilities();
    });
  }

  // Setup TLP filter change event
  const tlpFilterSelect = document.getElementById('tlpFilterSelect');
  if (tlpFilterSelect) {
    tlpFilterSelect.addEventListener('change', function() {
      loadVulnerabilities();
    });
  }
});

// Show vulnerability details in a modal
async function showVulnerabilityDetails(vulnerabilityId) {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      alert('Not authenticated');
      return;
    }

    // Fetch vulnerability details from backend
    const response = await fetch(`${window.API_BASE_URL}/api/vulnerabilities/${vulnerabilityId}`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      // If endpoint doesn't exist, try getting from the company vulnerabilities list
      const allVulnsResponse = await fetch(`${window.API_BASE_URL}/api/vulnerabilities/company`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (allVulnsResponse.ok) {
        const data = await allVulnsResponse.json();
        const vulnerabilities = data.Vulnerabilities || data.vulnerabilities || [];
        const vuln = vulnerabilities.find(v => (v.Id || v.id) === vulnerabilityId);
        
        if (vuln) {
          displayVulnerabilityModal(vuln);
          return;
        }
      }
      
      // Also try completed vulnerabilities endpoint
      const completedVulnsResponse = await fetch(`${window.API_BASE_URL}/api/vulnerabilities/completed`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (completedVulnsResponse.ok) {
        const data = await completedVulnsResponse.json();
        const vulnerabilities = data.Vulnerabilities || data.vulnerabilities || [];
        const vuln = vulnerabilities.find(v => (v.Id || v.id) === vulnerabilityId);
        
        if (vuln) {
          displayVulnerabilityModal(vuln);
          return;
        }
      }
      
      throw new Error('Failed to load vulnerability details');
    }

    const data = await response.json();
    const vuln = data.Vulnerability || data.vulnerability || data;
    displayVulnerabilityModal(vuln);
  } catch (error) {
    console.error('Error loading vulnerability details:', error);
    alert('Error loading vulnerability details: ' + error.message);
  }
}

// Display vulnerability details in a Bootstrap modal
function displayVulnerabilityModal(vuln) {
  const cveId = vuln.CveId || vuln.cveId || 'N/A';
  const title = vuln.Title || vuln.title || 'No title';
  const description = vuln.Description || vuln.description || 'No description available';
  const vendorName = vuln.VendorName || vuln.vendorName || 'Unknown';
  const severityLevel = vuln.SeverityLevel || vuln.severityLevel || 'Unknown';
  const severityScore = vuln.SeverityScore || vuln.severityScore || null;
  const publishedDate = vuln.PublishedDate || vuln.publishedDate || 'N/A';
  
  // Check if user is admin
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const userRole = (user.Role || user.role || '').toLowerCase();
  const isAdmin = userRole === 'admin';
  
  // Check if vulnerability is assigned
  const taskId = vuln.TaskId || vuln.taskId;
  const taskStatus = vuln.TaskStatus || vuln.taskStatus || '';
  const isAssigned = taskId && taskStatus && taskStatus !== 'closed';
  const isCompleted = taskStatus === 'closed';
  
  // Build NVD URL - always construct from CVE ID to ensure it works
  let sourceUrl = 'https://nvd.nist.gov/';
  if (cveId && cveId !== 'N/A' && cveId.trim() !== '') {
    const cleanCveId = cveId.trim();
    sourceUrl = `https://nvd.nist.gov/vuln/detail/${cleanCveId}`;
  }
  
  const affectedProducts = vuln.AffectedProducts || vuln.affectedProducts || 'Not specified';
  const source = vuln.Source || vuln.source || 'NVD';

  // Get severity badge color
  let severityBadge = 'secondary';
  if (severityLevel === 'Critical') severityBadge = 'danger';
  else if (severityLevel === 'High') severityBadge = 'warning';
  else if (severityLevel === 'Medium') severityBadge = 'info';
  else if (severityLevel === 'Low') severityBadge = 'success';

  // Create modal HTML
  const modalHtml = `
    <div class="modal fade" id="vulnerabilityDetailsModal" tabindex="-1" aria-labelledby="vulnerabilityDetailsModalLabel" aria-hidden="true">
      <div class="modal-dialog modal-lg modal-dialog-scrollable">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="vulnerabilityDetailsModalLabel">
              <code>${escapeHtml(cveId)}</code>
            </h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <div class="mb-4">
              <h6 class="fw-semibold mb-2">${escapeHtml(title)}</h6>
              <div class="d-flex gap-2 mb-3">
                <span class="badge bg-${severityBadge}">${severityLevel}</span>
                ${severityScore ? `<span class="badge bg-secondary">CVSS: ${severityScore}</span>` : ''}
                <span class="badge bg-info">${escapeHtml(vendorName)}</span>
              </div>
            </div>

            <div class="mb-4">
              <h6 class="fw-semibold mb-2">Description</h6>
              <p class="text-muted">${escapeHtml(description)}</p>
            </div>

            <div class="row mb-4">
              <div class="col-md-6">
                <h6 class="fw-semibold mb-2">Published Date</h6>
                <p class="text-muted mb-0">${publishedDate}</p>
              </div>
              <div class="col-md-6">
                <h6 class="fw-semibold mb-2">Source</h6>
                <p class="text-muted mb-0">${escapeHtml(source)}</p>
              </div>
            </div>

            <div class="mb-4">
              <h6 class="fw-semibold mb-2">Affected Products</h6>
              <p class="text-muted">${escapeHtml(affectedProducts)}</p>
            </div>

            <div class="d-grid gap-2 mb-4">
              <a href="${sourceUrl}" target="_blank" rel="noopener noreferrer" class="btn btn-primary" id="nvdLinkBtn">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
                  <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6M15 3h6v6M10 14L21 3"/>
                </svg>
                View on ${escapeHtml(source)}
              </a>
            </div>

            <!-- Remediation Guidance Section -->
            <div class="card border-info mb-3" id="remediationSection" style="display: none;">
              <div class="card-header bg-info bg-opacity-10">
                <h6 class="mb-0 fw-semibold">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
                    <path d="M9 11l3 3L22 4M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
                  </svg>
                  How to Resolve This Vulnerability
                </h6>
              </div>
              <div class="card-body">
                <div class="mb-3">
                  <h6 class="fw-semibold mb-2">General Remediation Steps:</h6>
                  <ol class="mb-0">
                    <li class="mb-2"><strong>Check for Updates:</strong> Visit the vendor's website to check for security patches or updates that address this vulnerability.</li>
                    <li class="mb-2"><strong>Review Vendor Advisories:</strong> Check the vendor's security advisory page for specific remediation guidance.</li>
                    <li class="mb-2"><strong>Apply Patches:</strong> Install the latest security patches or updates as recommended by the vendor.</li>
                    <li class="mb-2"><strong>Verify Fix:</strong> After applying patches, verify that the vulnerability has been resolved.</li>
                    <li class="mb-2"><strong>Document Resolution:</strong> Update your task notes with the remediation steps taken.</li>
                  </ol>
                </div>
                <div class="mb-3">
                  <h6 class="fw-semibold mb-2">Vendor Resources:</h6>
                  <div class="d-grid gap-2">
                    <a href="https://www.google.com/search?q=${encodeURIComponent(vendorName + ' security advisory ' + cveId)}" target="_blank" rel="noopener noreferrer" class="btn btn-sm btn-outline-primary">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
                        <circle cx="11" cy="11" r="8"/>
                        <path d="m21 21-4.35-4.35"/>
                      </svg>
                      Search ${escapeHtml(vendorName)} Security Advisories
                    </a>
                    <a href="${sourceUrl}" target="_blank" rel="noopener noreferrer" class="btn btn-sm btn-outline-secondary">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
                        <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6M15 3h6v6M10 14L21 3"/>
                      </svg>
                      View Detailed CVE Information
                    </a>
                  </div>
                </div>
                ${severityLevel === 'Critical' || severityLevel === 'High' ? `
                <div class="alert alert-warning mb-0">
                  <strong>Priority Action Required:</strong> This ${severityLevel.toLowerCase()} severity vulnerability should be addressed as soon as possible. Consider implementing temporary mitigations if patches are not immediately available.
                </div>
                ` : ''}
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-info" id="showRemediationBtn">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
                <path d="M9 11l3 3L22 4M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
              </svg>
              How to Resolve
            </button>
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
            ${!isCompleted ? (isAdmin ? `<button type="button" class="btn btn-warning" id="assignVulnBtn" data-vuln-id="${vuln.Id || vuln.id}">Assign Task</button>` : (!isAssigned ? `<button type="button" class="btn btn-success" id="claimVulnBtn" data-vuln-id="${vuln.Id || vuln.id}">Claim & Start Working</button>` : '')) : ''}
          </div>
        </div>
      </div>
    </div>
  `;

  // Remove existing modal if any
  const existingModal = document.getElementById('vulnerabilityDetailsModal');
  if (existingModal) {
    const existingModalInstance = bootstrap.Modal.getInstance(existingModal);
    if (existingModalInstance) {
      existingModalInstance.dispose();
    }
    existingModal.remove();
  }

  // Add modal to body
  document.body.insertAdjacentHTML('beforeend', modalHtml);

  // Show modal using Bootstrap
  const modal = new bootstrap.Modal(document.getElementById('vulnerabilityDetailsModal'));
  modal.show();
  
  // Ensure NVD link works after modal is shown
  setTimeout(() => {
    const nvdLink = document.getElementById('nvdLinkBtn');
    if (nvdLink) {
      // Verify and fix href if needed
      let currentHref = nvdLink.getAttribute('href');
      if (!currentHref || currentHref === '#' || !currentHref.startsWith('http')) {
        if (cveId && cveId !== 'N/A' && cveId.trim() !== '') {
          currentHref = `https://nvd.nist.gov/vuln/detail/${cveId.trim()}`;
          nvdLink.href = currentHref;
        }
      }
      // Ensure attributes are set
      nvdLink.setAttribute('target', '_blank');
      nvdLink.setAttribute('rel', 'noopener noreferrer');
      
      // Add click handler as backup
      nvdLink.addEventListener('click', function(e) {
        if (!this.href || this.href === '#' || !this.href.startsWith('http')) {
          e.preventDefault();
          if (cveId && cveId !== 'N/A' && cveId.trim() !== '') {
            window.open(`https://nvd.nist.gov/vuln/detail/${cveId.trim()}`, '_blank', 'noopener,noreferrer');
          }
        }
      });
    }
  }, 100);

  // Setup "How to Resolve" button
  const showRemediationBtn = document.getElementById('showRemediationBtn');
  if (showRemediationBtn) {
    let isRemediationVisible = false;
    showRemediationBtn.addEventListener('click', () => {
      const remediationSection = document.getElementById('remediationSection');
      if (remediationSection) {
        isRemediationVisible = !isRemediationVisible;
        remediationSection.style.display = isRemediationVisible ? 'block' : 'none';
        
        // Update button text and icon
        if (isRemediationVisible) {
          showRemediationBtn.innerHTML = `
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
              <path d="M18 6L6 18M6 6l12 12"/>
            </svg>
            Hide Resolution Guide
          `;
          // Scroll to remediation section
          setTimeout(() => {
            remediationSection.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
          }, 100);
        } else {
          showRemediationBtn.innerHTML = `
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
              <path d="M9 11l3 3L22 4M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
            </svg>
            How to Resolve
          `;
        }
      }
    });
  }

  // Setup assign button if admin and not completed
  if (isAdmin && !isCompleted) {
    const assignBtn = document.getElementById('assignVulnBtn');
    if (assignBtn) {
      assignBtn.addEventListener('click', () => {
        modal.hide();
        openAssignModal(vuln);
      });
    }
  } else if (!isAdmin && !isCompleted && !isAssigned) {
    // Setup claim button for employees/managers (only if not completed and not assigned)
    const claimBtn = document.getElementById('claimVulnBtn');
    if (claimBtn) {
      claimBtn.addEventListener('click', async () => {
        await claimVulnerability(vuln);
        modal.hide();
      });
    }
  }

  // Clean up when modal is hidden
  document.getElementById('vulnerabilityDetailsModal').addEventListener('hidden.bs.modal', function () {
    this.remove();
  });
}

// Open assign task modal
async function openAssignModal(vuln) {
  const vulnerabilityId = vuln.Id || vuln.id;
  if (!vulnerabilityId) {
    alert('Invalid vulnerability ID');
    return;
  }

  // Set vulnerability ID
  document.getElementById('assignVulnerabilityId').value = vulnerabilityId;

  // Set priority based on severity level
  const severityLevel = vuln.SeverityLevel || vuln.severityLevel || 'Low';
  const priority = severityLevel; // Use severity level as priority
  document.getElementById('assignTaskPriority').value = priority;
  
  // Display priority badge
  const priorityBadge = document.getElementById('assignTaskPriorityBadge');
  let badgeColor = 'secondary';
  if (severityLevel === 'Critical') badgeColor = 'danger';
  else if (severityLevel === 'High') badgeColor = 'warning';
  else if (severityLevel === 'Medium') badgeColor = 'info';
  else if (severityLevel === 'Low') badgeColor = 'success';
  
  if (priorityBadge) {
    priorityBadge.className = `badge bg-${badgeColor}`;
    priorityBadge.textContent = priority;
  }

  // Get TLP rating for filtering
  const tlpRating = vuln.TlpRating || vuln.tlp_rating || 'GREEN';

  // Load company users with TLP filter
  await loadCompanyUsers(tlpRating);

  // Show modal
  const assignModal = new bootstrap.Modal(document.getElementById('assignTaskModal'));
  assignModal.show();
}

// Load company users for assignment dropdown
// tlpRating: Filter users by TLP rating if vulnerability is RED
async function loadCompanyUsers(tlpRating = 'GREEN') {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      alert('Not authenticated');
      return;
    }

    // Get user's company ID
    const userResponse = await fetch(`${window.API_BASE_URL}/api/user/company`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!userResponse.ok) {
      const errorText = await userResponse.text();
      console.error('Failed to get user company:', userResponse.status, errorText);
      throw new Error(`Failed to get user company: ${userResponse.status}`);
    }

    const userData = await userResponse.json();
    console.log('User company data:', userData);
    const companyId = userData.Company?.Id || userData.company?.id || userData.CompanyId || userData.companyId;

    if (!companyId) {
      console.error('Company ID not found in response:', userData);
      throw new Error('Company ID not found. Please make sure you are associated with a company.');
    }

    console.log('Fetching users for company ID:', companyId);

    // Get users in company
    const response = await fetch(`${window.API_BASE_URL}/api/companies/${companyId}/users`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const errorText = await response.text();
      console.error('Failed to load company users:', response.status, errorText);
      let errorMessage = `Server error: ${response.status}`;
      try {
        const errorData = JSON.parse(errorText);
        errorMessage = errorData.Message || errorData.message || errorMessage;
      } catch (e) {
        // If JSON parsing fails, use the text
        errorMessage = errorText || errorMessage;
      }
      
      if (response.status === 403) {
        errorMessage = 'You do not have permission to assign tasks. Only admins can assign tasks.';
      } else if (response.status === 401) {
        errorMessage = 'Authentication failed. Please log out and log back in.';
      }
      
      throw new Error(errorMessage);
    }

    const data = await response.json();
    const users = data.Users || data.users || [];

    if (users.length === 0) {
      throw new Error('No users found in your company. Please add users to your company first.');
    }

    // Populate dropdown
    const select = document.getElementById('assignToUserId');
    if (!select) {
      throw new Error('Assignment dropdown not found');
    }
    
    select.innerHTML = '<option value="">Select a user...</option>';
    
    // Helper function to get valid TLP rating from user
    const getUserTlp = (user) => {
      let userTlp = (user.TlpRating || user.tlp_rating || 'GREEN').toUpperCase();
      // Validate TLP rating - only allow RED, AMBER, GREEN
      if (userTlp !== 'RED' && userTlp !== 'AMBER' && userTlp !== 'GREEN') {
        // If invalid, set based on role
        const role = (user.Role || user.role || 'employee').toLowerCase();
        if (role === 'admin') userTlp = 'RED';
        else if (role === 'manager') userTlp = 'AMBER';
        else userTlp = 'GREEN';
      }
      return userTlp;
    };
    
    // Filter out admin users (they shouldn't be assigned tasks)
    let nonAdminUsers = users.filter(user => {
      const role = (user.Role || user.role || '').toLowerCase();
      return role !== 'admin';
    });
    
    // Filter users based on vulnerability TLP rating
    // RED vulnerabilities → only RED users
    // AMBER vulnerabilities → AMBER or RED users (managers/admins)
    // GREEN vulnerabilities → all users (employees, managers, admins)
    const vulnTlpUpper = (tlpRating || 'GREEN').toUpperCase();
    if (vulnTlpUpper === 'RED') {
      nonAdminUsers = nonAdminUsers.filter(user => {
        const userTlp = getUserTlp(user);
        return userTlp === 'RED';
      });
      
      if (nonAdminUsers.length === 0) {
        select.innerHTML = '<option value="">No users with RED TLP rating available. RED vulnerabilities can only be assigned to users with RED TLP rating.</option>';
        return;
      }
    } else if (vulnTlpUpper === 'AMBER') {
      // AMBER can be assigned to AMBER or RED users (managers/admins)
      nonAdminUsers = nonAdminUsers.filter(user => {
        const userTlp = getUserTlp(user);
        return userTlp === 'AMBER' || userTlp === 'RED';
      });
      
      if (nonAdminUsers.length === 0) {
        select.innerHTML = '<option value="">No users with AMBER or RED TLP rating available. AMBER vulnerabilities can only be assigned to managers/admins.</option>';
        return;
      }
    }
    // GREEN vulnerabilities can be assigned to anyone, no filtering needed
    
    if (nonAdminUsers.length === 0) {
      select.innerHTML = '<option value="">No employees or managers available</option>';
      return;
    }
    
    nonAdminUsers.forEach(user => {
      const option = document.createElement('option');
      option.value = user.Id || user.id;
      const email = user.Email || user.email || 'Unknown';
      const firstName = user.FirstName || user.firstName || '';
      const lastName = user.LastName || user.lastName || '';
      const name = (firstName && lastName) ? `${firstName} ${lastName}` : email;
      
      // Get valid TLP rating (using same helper logic as filtering)
      const userTlp = getUserTlp(user);
      
      option.textContent = `${name} (${user.Role || user.role || 'employee'}, TLP:${userTlp})`;
      select.appendChild(option);
    });
  } catch (error) {
    console.error('Error loading company users:', error);
    const select = document.getElementById('assignToUserId');
    if (select) {
      select.innerHTML = `<option value="">Error: ${error.message}</option>`;
    }
    alert('Error loading company users: ' + error.message);
  }
}

// Claim vulnerability (self-assign) for employees/managers
async function claimVulnerability(vuln) {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      alert('Not authenticated');
      return;
    }

    const vulnerabilityId = vuln.Id || vuln.id;
    if (!vulnerabilityId) {
      alert('Invalid vulnerability ID');
      return;
    }

    // Create self-assignment task
    const response = await fetch(`${window.API_BASE_URL}/api/tasks/claim`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        VulnerabilityId: vulnerabilityId
      })
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.Message || errorData.message || 'Failed to claim vulnerability');
    }

    const data = await response.json();
    alert('Vulnerability claimed! You can now track your progress in "My Tasks".');
    
    // Reload vulnerabilities to show updated status (remove claimed vulnerability from list)
    if (typeof loadVulnerabilities === 'function') {
      await loadVulnerabilities();
    }
  } catch (error) {
    console.error('Error claiming vulnerability:', error);
    alert('Error claiming vulnerability: ' + error.message);
    
    // Reload vulnerabilities even on error to ensure list is up-to-date
    // (in case vulnerability was assigned by someone else)
    if (typeof loadVulnerabilities === 'function') {
      await loadVulnerabilities();
    }
  }
}

// Assign task
async function assignTask() {
  try {
    const vulnerabilityId = document.getElementById('assignVulnerabilityId').value;
    const assignedToUserId = document.getElementById('assignToUserId').value;
    const priority = document.getElementById('assignTaskPriority').value;
    const notes = document.getElementById('assignTaskNotes').value;

    if (!vulnerabilityId || !assignedToUserId) {
      alert('Please select a user to assign the task to');
      return;
    }

    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      alert('Not authenticated');
      return;
    }

    const response = await fetch(`${window.API_BASE_URL}/api/tasks`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        VulnerabilityId: parseInt(vulnerabilityId),
        AssignedToUserId: parseInt(assignedToUserId),
        Priority: priority,
        Notes: notes || null
      })
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.Message || errorData.message || 'Failed to assign task');
    }

    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('assignTaskModal'));
    if (modal) {
      modal.hide();
    }

    // Reset form
    document.getElementById('assignTaskForm').reset();

    // Show success message
    const alert = document.getElementById('vulnerabilitiesAlert');
    if (alert) {
      alert.className = 'alert alert-success';
      alert.textContent = 'Task assigned successfully!';
      alert.classList.remove('d-none');
      setTimeout(() => {
        alert.classList.add('d-none');
      }, 3000);
    }
  } catch (error) {
    console.error('Error assigning task:', error);
    alert('Error assigning task: ' + error.message);
  }
}

// Setup assign task form
document.addEventListener('DOMContentLoaded', () => {
  const assignTaskForm = document.getElementById('assignTaskForm');
  if (assignTaskForm) {
    assignTaskForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      await assignTask();
    });
  }
});

// Make function globally accessible
window.showVulnerabilityDetails = showVulnerabilityDetails;

// Load completed vulnerabilities (admin only - for their company)
async function loadCompletedVulnerabilities() {
  const completedVulnerabilitiesList = document.getElementById('completedVulnerabilitiesList');
  const completedVulnerabilitiesAlert = document.getElementById('completedVulnerabilitiesAlert');
  const refreshBtn = document.getElementById('refreshCompletedVulnerabilitiesBtn');
  const spinner = refreshBtn?.querySelector('.spinner-border');

  if (!completedVulnerabilitiesList) return;

  // Show loading state
  completedVulnerabilitiesList.innerHTML = `
    <div class="text-center">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading completed vulnerabilities...</span>
      </div>
    </div>
  `;
  if (completedVulnerabilitiesAlert) completedVulnerabilitiesAlert.classList.add('d-none');
  if (spinner) spinner.classList.remove('d-none');
  if (refreshBtn) refreshBtn.disabled = true;

  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await fetch(`${window.API_BASE_URL}/api/vulnerabilities/completed`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('Only administrators can view completed vulnerabilities.');
      }
      throw new Error(`Server error: ${response.status}`);
    }

    const data = await response.json();

    if (!data.Success && !data.success) {
      throw new Error(data.Message || data.message || 'Failed to load completed vulnerabilities');
    }

    const vulnerabilities = data.Vulnerabilities || data.vulnerabilities || [];

    if (vulnerabilities.length === 0) {
      completedVulnerabilitiesList.innerHTML = `
        <div class="alert alert-info">
          <strong>No completed vulnerabilities found.</strong><br>
          No vulnerabilities have been completed (closed) for your company yet.
        </div>
      `;
      return;
    }

    // Display completed vulnerabilities
    displayCompletedVulnerabilities(vulnerabilities);

  } catch (error) {
    console.error('Error loading completed vulnerabilities:', error);
    if (completedVulnerabilitiesAlert) {
      completedVulnerabilitiesAlert.className = 'alert alert-danger';
      completedVulnerabilitiesAlert.textContent = `Error: ${error.message}`;
      completedVulnerabilitiesAlert.classList.remove('d-none');
    }
    completedVulnerabilitiesList.innerHTML = `
      <div class="alert alert-danger">
        <strong>Error loading completed vulnerabilities:</strong> ${error.message}
      </div>
    `;
  } finally {
    if (spinner) spinner.classList.add('d-none');
    if (refreshBtn) refreshBtn.disabled = false;
  }
}

// Display completed vulnerabilities in a table
function displayCompletedVulnerabilities(vulnerabilities) {
  const completedVulnerabilitiesList = document.getElementById('completedVulnerabilitiesList');
  if (!completedVulnerabilitiesList) return;

  let html = `
    <div class="mb-3">
      <small class="text-muted">
        Showing <strong>${vulnerabilities.length}</strong> completed vulnerability/vulnerabilities for your company
      </small>
    </div>
    <div class="table-responsive">
      <table class="table table-hover">
        <thead>
          <tr>
            <th>CVE ID</th>
            <th>Title</th>
            <th>Vendor</th>
            <th>Severity</th>
            <th>Published</th>
            <th>Completed By</th>
            <th>Completed Date</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
  `;

  // Store vulnerabilities in a map for easy lookup
  const vulnMap = new Map();
  vulnerabilities.forEach(vuln => {
    const vulnId = vuln.Id || vuln.id;
    if (vulnId) {
      vulnMap.set(vulnId, vuln);
    }
  });

  vulnerabilities.forEach(vuln => {
    const cveId = vuln.CveId || vuln.cveId || 'N/A';
    const title = (vuln.Title || vuln.title || 'No title').substring(0, 100) + (vuln.Title?.length > 100 ? '...' : '');
    const vendorName = vuln.VendorName || vuln.vendorName || 'Unknown';
    const severityLevel = vuln.SeverityLevel || vuln.severityLevel || 'Unknown';
    const publishedDate = vuln.PublishedDate || vuln.publishedDate || 'N/A';
    const description = vuln.Description || vuln.description || '';
    const vulnId = vuln.Id || vuln.id;
    
    // Get assignment info (these will always be present for completed vulnerabilities)
    const taskId = vuln.TaskId || vuln.taskId;
    const taskStatus = vuln.TaskStatus || vuln.taskStatus || 'closed';
    const assignedToEmail = vuln.AssignedToEmail || vuln.assignedToEmail;
    const assignedToName = vuln.AssignedToName || vuln.assignedToName;
    const companyName = vuln.CompanyName || vuln.companyName;
    const resolvedAt = vuln.ResolvedAt || vuln.resolvedAt;
    const isAssigned = taskId && taskStatus;
    
    // Get severity badge color
    let severityBadge = 'secondary';
    if (severityLevel === 'Critical') severityBadge = 'danger';
    else if (severityLevel === 'High') severityBadge = 'warning';
    else if (severityLevel === 'Medium') severityBadge = 'info';
    else if (severityLevel === 'Low') severityBadge = 'success';
    
    // Build assignment display (all completed vulnerabilities should have assignment info)
    let assignmentHtml = '<span class="text-muted small">Unknown</span>';
    if (isAssigned) {
      const assigneeName = assignedToName || assignedToEmail || 'Unknown';
      const assigneeDisplay = assignedToName ? `${assignedToName} (${assignedToEmail})` : assignedToEmail;
      const resolvedDate = resolvedAt ? new Date(resolvedAt).toLocaleDateString() : 'N/A';
      
      assignmentHtml = `
        <div class="d-flex flex-column gap-1">
          <div>
            <span class="badge bg-success small">Completed</span>
          </div>
          <small class="text-muted">
            <strong>Completed by:</strong> ${escapeHtml(assigneeDisplay)}<br>
            <strong>Completed on:</strong> ${resolvedDate}
          </small>
        </div>
      `;
    }

    html += `
      <tr>
        <td><code>${cveId}</code></td>
        <td>
          <strong>${escapeHtml(title)}</strong>
          ${description ? `<br><small class="text-muted">${escapeHtml(description.substring(0, 150))}${description.length > 150 ? '...' : ''}</small>` : ''}
        </td>
        <td>${escapeHtml(vendorName)}</td>
        <td><span class="badge bg-${severityBadge}">${severityLevel}</span></td>
        <td><small>${publishedDate}</small></td>
        <td>${assignmentHtml}</td>
        <td><small>${resolvedAt ? new Date(resolvedAt).toLocaleDateString() : 'N/A'}</small></td>
        <td>
          <button class="btn btn-sm btn-outline-primary view-details-completed-btn" data-vuln-id="${vulnId}">View Details</button>
        </td>
      </tr>
    `;
  });

  html += `
        </tbody>
      </table>
    </div>
  `;

  completedVulnerabilitiesList.innerHTML = html;
  
  // Setup view details buttons for completed vulnerabilities - use stored vulnerability data
  const viewDetailsButtons = completedVulnerabilitiesList.querySelectorAll('.view-details-completed-btn');
  viewDetailsButtons.forEach(btn => {
    btn.addEventListener('click', function() {
      const vulnId = parseInt(this.getAttribute('data-vuln-id'));
      // Get vulnerability from the map we created
      const vuln = vulnMap.get(vulnId);
      if (vuln) {
        displayVulnerabilityModal(vuln);
      } else {
        // Fallback to ID-based lookup
        showVulnerabilityDetails(vulnId);
      }
    });
  });
}

// Setup refresh button for completed vulnerabilities
document.addEventListener('DOMContentLoaded', () => {
  const refreshBtn = document.getElementById('refreshCompletedVulnerabilitiesBtn');
  if (refreshBtn) {
    refreshBtn.addEventListener('click', async () => {
      await loadCompletedVulnerabilities();
    });
  }
});

// Export for use in main.js
window.loadVulnerabilities = loadVulnerabilities;
window.loadCompletedVulnerabilities = loadCompletedVulnerabilities;


// Vendor Selection Module (declare once globally)
if (typeof window.API_BASE_URL === 'undefined') {
  window.API_BASE_URL = 'http://localhost:5155';
}

let allVendors = [];
let selectedVendorIds = new Set();
let userRole = null;

// Make function globally accessible
window.initVendorSelection = async function initVendorSelection() {
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  userRole = (user.Role || user.role || '').toLowerCase();

  // Only show vendor selection for admin users
  if (userRole !== 'admin') {
    const vendorSection = document.getElementById('vendorSelectionSection');
    if (vendorSection) vendorSection.classList.add('d-none');
    return;
  }

  // Don't automatically show the section - it will be shown when link is clicked
  // Just load the data
  await ensureUserCompany();
  await loadVendors();
  await loadUserVendors();
  setupVendorSelection();
}

async function ensureUserCompany() {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) return;

    const response = await fetch(`${window.API_BASE_URL}/api/user/company`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      console.error(`Failed to get user company: ${response.status} ${response.statusText}`);
      return;
    }

    const data = await response.json();
    if (data.Success || data.success) {
      console.log('User company ready:', data.Company || data.company);
    }
  } catch (error) {
    console.error('Error ensuring user company:', error);
  }
}

async function loadVendors() {
  try {
    const response = await fetch(`${window.API_BASE_URL}/api/vendors`);
    
    if (!response.ok) {
      throw new Error(`Failed to load vendors: ${response.status} ${response.statusText}`);
    }
    
    const data = await response.json();
    
    if (data.Success || data.success) {
      allVendors = data.Vendors || data.vendors || [];
      console.log(`Loaded ${allVendors.length} vendors from database:`, allVendors);
      renderVendors();
    } else {
      throw new Error(data.Message || data.message || 'Failed to load vendors');
    }
  } catch (error) {
    console.error('Error loading vendors:', error);
    const container = document.getElementById('vendorsList');
    if (container) {
      container.innerHTML = `
        <div class="alert alert-warning">
          <strong>Unable to load vendors.</strong><br>
          Please make sure the backend is running and restart it if needed.<br>
          <small>Error: ${error.message}</small>
        </div>
      `;
    }
  }
}

async function loadUserVendors() {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) return;

    const response = await fetch(`${window.API_BASE_URL}/api/user/vendors`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      console.warn(`Failed to load user vendors: ${response.status} ${response.statusText}`);
      return;
    }

    const data = await response.json();
    
    if (data.Success || data.success) {
      const userVendors = data.Vendors || data.vendors || [];
      selectedVendorIds = new Set(userVendors.map(v => {
        const id = v.VendorId || v.vendorId;
        return typeof id === 'number' ? id : parseInt(id, 10);
      }).filter(id => !isNaN(id)));
      renderVendors();
    }
  } catch (error) {
    console.error('Error loading user vendors:', error);
  }
}

function renderVendors() {
  const container = document.getElementById('vendorsList');
  if (!container) {
    console.error('vendorsList container not found!');
    return;
  }

  if (allVendors.length === 0) {
    container.innerHTML = '<div class="alert alert-warning">No vendors loaded. Please refresh the page.</div>';
    return;
  }

  const filterType = document.getElementById('vendorTypeFilter')?.value || 'all';
  
  // Filter vendors based on selected type
  const filteredVendors = allVendors.filter(vendor => {
    if (filterType === 'all') return true;
    const type = (vendor.VendorType || vendor.vendorType || '').toLowerCase();
    if (filterType === 'hardware') {
      return type === 'hardware'; // Only hardware, exclude "both"
    } else if (filterType === 'software') {
      return type === 'software'; // Only software, exclude "both"
    } else if (filterType === 'both') {
      return type === 'both'; // Only "both" type
    }
    return type === filterType.toLowerCase();
  });

  if (filteredVendors.length === 0) {
    container.innerHTML = '<p class="text-muted">No vendors found matching the selected filter.</p>';
    return;
  }

  // Group vendors by type (avoid duplicates)
  const hardwareOnly = filteredVendors.filter(v => {
    const type = (v.VendorType || v.vendorType || '').toLowerCase();
    return type === 'hardware';
  });
  
  const softwareOnly = filteredVendors.filter(v => {
    const type = (v.VendorType || v.vendorType || '').toLowerCase();
    return type === 'software';
  });
  
  const bothVendors = filteredVendors.filter(v => {
    const type = (v.VendorType || v.vendorType || '').toLowerCase();
    return type === 'both';
  });

  let html = '';

  // Show "Both" vendors first
  if (bothVendors.length > 0) {
    html += '<div class="mb-4"><h5 class="mb-3">🔧💻 Hardware & Software Vendors</h5>';
    html += bothVendors.map(vendor => createVendorSelectionCard(vendor)).join('');
    html += '</div>';
  }

  // Show hardware-only vendors
  if (hardwareOnly.length > 0) {
    html += '<div class="mb-4"><h5 class="mb-3">🔧 Hardware Vendors</h5>';
    html += hardwareOnly.map(vendor => createVendorSelectionCard(vendor)).join('');
    html += '</div>';
  }

  // Show software-only vendors
  if (softwareOnly.length > 0) {
    html += '<div class="mb-4"><h5 class="mb-3">💻 Software Vendors</h5>';
    html += softwareOnly.map(vendor => createVendorSelectionCard(vendor)).join('');
    html += '</div>';
  }

  container.innerHTML = html;
}

function createVendorSelectionCard(vendor) {
  const vendorId = vendor.Id || vendor.id;
  const vendorName = vendor.Name || vendor.name;
  const vendorType = vendor.VendorType || vendor.vendorType;
  const vendorDescription = vendor.Description || vendor.description;
  const isSelected = selectedVendorIds.has(vendorId);

  // Get badge color based on vendor type
  let badgeColor = 'secondary';
  if (vendorType) {
    const typeLower = vendorType.toLowerCase();
    if (typeLower === 'hardware') badgeColor = 'primary';
    else if (typeLower === 'software') badgeColor = 'success';
    else if (typeLower === 'both') badgeColor = 'info';
  }

  // Ensure vendorId is valid
  if (!vendorId || isNaN(vendorId)) {
    console.error('Invalid vendor ID in createVendorCard:', vendor);
    return '';
  }

  // Build the checkbox HTML directly in the template literal
  const checkedAttr = isSelected ? 'checked' : '';
  
  // Create the card HTML with checkbox
  const cardHtml = `
    <div class="card mb-3 vendor-card ${isSelected ? 'border-primary border-2' : ''}">
      <div class="card-body">
        <div class="d-flex align-items-start">
          <div class="me-3" style="flex-shrink: 0; padding-top: 0.25rem;">
            <input class="form-check-input vendor-checkbox" type="checkbox" value="${vendorId}" id="vendor-${vendorId}" ${checkedAttr} style="width: 1.5rem !important; height: 1.5rem !important; margin: 0 !important; cursor: pointer !important; display: block !important; opacity: 1 !important; visibility: visible !important; position: relative !important; z-index: 1 !important;" />
          </div>
          <div class="flex-grow-1">
            <label class="form-check-label w-100" for="vendor-${vendorId}" style="cursor: pointer;">
              <h6 class="mb-1 fw-bold">${escapeHtml(vendorName)}</h6>
              <span class="badge bg-${badgeColor} mb-2">${escapeHtml(vendorType || 'Unknown')}</span>
              ${vendorDescription ? `<p class="text-muted small mb-0 mt-1">${escapeHtml(vendorDescription)}</p>` : ''}
            </label>
            <div class="mt-3 use-case-input" style="display: ${isSelected ? 'block' : 'none'};">
              <label for="useCase-${vendorId}" class="form-label small fw-semibold">How do you use this vendor?</label>
              <textarea 
                class="form-control form-control-sm" 
                id="useCase-${vendorId}" 
                rows="2" 
                placeholder="Describe your use case (optional)"
              ></textarea>
            </div>
          </div>
        </div>
      </div>
    </div>
  `;
  
  return cardHtml;
}

// Helper function to escape HTML
function escapeHtml(text) {
  if (!text) return '';
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

function setupVendorSelection() {
  const vendorsList = document.getElementById('vendorsList');
  if (!vendorsList) {
    console.error('vendorsList container not found in setupVendorSelection');
    return;
  }

  // Remove old event listeners by cloning the element (this removes all listeners)
  // But we'll use event delegation on the container, so we can add it fresh each time
  // Actually, let's use event delegation which works even when content is replaced
  
  // Handle checkbox changes (use event delegation - works even when HTML is replaced)
  vendorsList.removeEventListener('change', handleCheckboxChange);
  vendorsList.addEventListener('change', handleCheckboxChange);

  // Handle filter change
  const filter = document.getElementById('vendorTypeFilter');
  if (filter) {
    filter.removeEventListener('change', renderVendors);
    filter.addEventListener('change', renderVendors);
  }

  // Handle save button
  const saveBtn = document.getElementById('saveVendorsBtn');
  if (saveBtn) {
    saveBtn.removeEventListener('click', saveVendors);
    saveBtn.addEventListener('click', saveVendors);
  }
  
  // Always re-render vendors when setup is called (in case vendors were loaded)
  renderVendors();
}

// Separate handler function for checkbox changes
function handleCheckboxChange(e) {
  if (e.target.classList.contains('vendor-checkbox')) {
    const vendorId = parseInt(e.target.value, 10);
    if (isNaN(vendorId)) {
      console.error('Invalid vendor ID:', e.target.value);
      return;
    }
    const useCaseInput = document.getElementById(`useCase-${vendorId}`);
    const vendorCard = e.target.closest('.vendor-card');
    
    if (e.target.checked) {
      selectedVendorIds.add(vendorId);
      if (useCaseInput) {
        const useCaseContainer = useCaseInput.closest('.use-case-input');
        if (useCaseContainer) {
          useCaseContainer.style.display = 'block';
        }
      }
      if (vendorCard) {
        vendorCard.classList.add('border-primary', 'border-2');
      }
    } else {
      selectedVendorIds.delete(vendorId);
      if (useCaseInput) {
        const useCaseContainer = useCaseInput.closest('.use-case-input');
        if (useCaseContainer) {
          useCaseContainer.style.display = 'none';
        }
        useCaseInput.value = '';
      }
      if (vendorCard) {
        vendorCard.classList.remove('border-primary', 'border-2');
      }
    }
    console.log('Vendor selection updated:', Array.from(selectedVendorIds));
  }
}

async function saveVendors() {
  const saveBtn = document.getElementById('saveVendorsBtn');
  const token = localStorage.getItem('firebaseToken');
  
  if (!token) {
    showAlert('error', 'Please log in again.');
    return;
  }

  saveBtn.disabled = true;
  saveBtn.textContent = 'Saving...';

  try {
    // Collect use case descriptions
    const useCaseDescriptions = {};
    selectedVendorIds.forEach(vendorId => {
      const useCaseInput = document.getElementById(`useCase-${vendorId}`);
      if (useCaseInput && useCaseInput.value.trim()) {
        useCaseDescriptions[vendorId] = useCaseInput.value.trim();
      }
    });

    // Ensure vendor IDs are integers
    const vendorIdsArray = Array.from(selectedVendorIds).map(id => {
      const numId = typeof id === 'number' ? id : parseInt(id, 10);
      if (isNaN(numId)) {
        console.error('Invalid vendor ID:', id);
        return null;
      }
      return numId;
    }).filter(id => id !== null);

    console.log('Saving vendors:', {
      VendorIds: vendorIdsArray,
      UseCaseDescriptions: useCaseDescriptions,
      selectedVendorIds: Array.from(selectedVendorIds)
    });

    const response = await fetch(`${window.API_BASE_URL}/api/user/vendors`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        VendorIds: vendorIdsArray,
        UseCaseDescriptions: useCaseDescriptions
      })
    });

    const data = await response.json();

    if (data.Success || data.success) {
      showAlert('success', 'Vendors saved successfully!');
      // Reload user vendors to update the selection
      await loadUserVendors();
      // If My Vendors section is visible, refresh it
      const myVendorsSection = document.getElementById('myVendorsSection');
      if (myVendorsSection && !myVendorsSection.classList.contains('d-none')) {
        if (typeof window.loadMyVendors === 'function') {
          await window.loadMyVendors();
        }
      }
    } else {
      showAlert('error', data.Message || data.message || 'Failed to save vendors.');
    }
  } catch (error) {
    console.error('Error saving vendors:', error);
    showAlert('error', 'Failed to save vendors. Please try again.');
  } finally {
    saveBtn.disabled = false;
    saveBtn.textContent = 'Save Vendors';
  }
}

function showAlert(type, message) {
  const alertDiv = document.getElementById('vendorAlert');
  if (!alertDiv) return;

  alertDiv.className = `alert alert-${type === 'error' ? 'danger' : 'success'} alert-dismissible fade show`;
  alertDiv.innerHTML = `
    ${message}
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  `;
  alertDiv.classList.remove('d-none');

  // Auto-hide after 5 seconds
  setTimeout(() => {
    alertDiv.classList.add('d-none');
  }, 5000);
}



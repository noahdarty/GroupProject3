// Company Vendors Display Module (for non-admin users)
// Shows read-only list of company vendors on a dedicated page

if (typeof window.API_BASE_URL === 'undefined') {
  window.API_BASE_URL = 'http://localhost:5155';
}

// Make function globally accessible
window.initCompanyVendors = async function initCompanyVendors() {
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const userRole = (user.Role || user.role || '').toLowerCase();

  // Only show for non-admin users
  if (userRole === 'admin') {
    return;
  }

  // Show navbar for non-admin users
  const navbar = document.getElementById('userNavbar');
  const adminHeader = document.getElementById('adminHeader');
  
  if (navbar) {
    navbar.classList.remove('d-none');
  }
  if (adminHeader) {
    adminHeader.classList.add('d-none');
  }

  // Setup logout button in navbar
  const logoutBtnNav = document.getElementById('logoutBtnNav');
  if (logoutBtnNav) {
    logoutBtnNav.addEventListener('click', () => {
      if (typeof window.logout === 'function') {
        window.logout();
      }
    });
  }

  // Load vendors for the page display
  await loadCompanyVendorsForPage();
}

async function loadCompanyVendorsForPage() {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      console.error('No authentication token found');
      return;
    }

    const response = await fetch(`${window.API_BASE_URL}/api/user/vendors`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      console.warn(`Failed to load company vendors: ${response.status} ${response.statusText}`);
      updateVendorsPageList([]);
      return;
    }

    const data = await response.json();
    
    if (data.Success || data.success) {
      const vendors = data.Vendors || data.vendors || [];
      updateVendorsPageList(vendors);
    } else {
      updateVendorsPageList([]);
    }
  } catch (error) {
    console.error('Error loading company vendors:', error);
    updateVendorsPageList([]);
  }
}

function updateVendorsPageList(vendors) {
  const vendorsList = document.getElementById('companyVendorsPageList');
  if (!vendorsList) return;

  if (vendors.length === 0) {
    vendorsList.innerHTML = `
      <div class="card border-0 shadow-sm">
        <div class="card-body text-center py-5">
          <div class="mb-4">
            <div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-light" style="width: 80px; height: 80px;">
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="text-muted">
                <path d="M20 7h-4m0 0V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2m0 0H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2Z"/>
              </svg>
            </div>
          </div>
          <h5 class="fw-semibold mb-2">No vendors selected</h5>
          <p class="text-muted mb-0">Your company administrator needs to select vendors for your company.</p>
        </div>
      </div>
    `;
    return;
  }

  // Group vendors by type
  const hardwareVendors = vendors.filter(v => {
    const type = (v.VendorType || v.vendorType || '').toLowerCase();
    return type === 'hardware';
  });

  const softwareVendors = vendors.filter(v => {
    const type = (v.VendorType || v.vendorType || '').toLowerCase();
    return type === 'software';
  });

  const bothVendors = vendors.filter(v => {
    const type = (v.VendorType || v.vendorType || '').toLowerCase();
    return type === 'both';
  });

  let html = '';

  // Statistics cards at the top
  html += `
    <div class="row g-3 mb-4">
      <div class="col-md-4">
        <div class="card border-0 shadow-sm h-100" style="background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%); border-left: 4px solid #495057 !important;">
          <div class="card-body p-3">
            <div class="d-flex align-items-center justify-content-between">
              <div>
                <p class="text-muted small mb-1 fw-semibold text-uppercase" style="font-size: 0.7rem;">Total Vendors</p>
                <h3 class="mb-0 fw-bold text-dark">${vendors.length}</h3>
              </div>
              <div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-secondary bg-opacity-10" style="width: 48px; height: 48px;">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="text-secondary">
                  <path d="M20 7h-4m0 0V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2m0 0H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2Z"/>
                </svg>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card border-0 shadow-sm h-100" style="background: linear-gradient(135deg, #e7f1ff 0%, #ffffff 100%); border-left: 4px solid #0d6efd !important;">
          <div class="card-body p-3">
            <div class="d-flex align-items-center justify-content-between">
              <div>
                <p class="text-muted small mb-1 fw-semibold text-uppercase" style="font-size: 0.7rem;">Hardware</p>
                <h3 class="mb-0 fw-bold text-dark">${hardwareVendors.length + bothVendors.length}</h3>
              </div>
              <div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary bg-opacity-10" style="width: 48px; height: 48px;">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="text-primary">
                  <rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>
                </svg>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card border-0 shadow-sm h-100" style="background: linear-gradient(135deg, #d1e7dd 0%, #ffffff 100%); border-left: 4px solid #198754 !important;">
          <div class="card-body p-3">
            <div class="d-flex align-items-center justify-content-between">
              <div>
                <p class="text-muted small mb-1 fw-semibold text-uppercase" style="font-size: 0.7rem;">Software</p>
                <h3 class="mb-0 fw-bold text-dark">${softwareVendors.length + bothVendors.length}</h3>
              </div>
              <div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-success bg-opacity-10" style="width: 48px; height: 48px;">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="text-success">
                  <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
                </svg>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `;

  // Show "Both" vendors
  if (bothVendors.length > 0) {
    html += '<div class="mb-4">';
    html += '<div class="d-flex align-items-center mb-3">';
    html += '<div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary bg-opacity-10 me-2" style="width: 40px; height: 40px;">';
    html += '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="text-primary"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>';
    html += '</div>';
    html += '<div class="flex-grow-1">';
    html += '<h5 class="mb-0 fw-bold">Hardware & Software</h5>';
    html += '<p class="text-muted small mb-0" style="font-size: 0.8rem;">Vendors providing both hardware and software solutions</p>';
    html += '</div>';
    html += '<span class="badge bg-primary rounded-pill px-2 py-1">' + bothVendors.length + '</span>';
    html += '</div>';
    html += '<div class="row g-3">';
    bothVendors.forEach(vendor => {
      html += createVendorCard(vendor);
    });
    html += '</div></div>';
  }

  // Show hardware-only vendors
  if (hardwareVendors.length > 0) {
    html += '<div class="mb-4">';
    html += '<div class="d-flex align-items-center mb-3">';
    html += '<div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-info bg-opacity-10 me-2" style="width: 40px; height: 40px;">';
    html += '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="text-info"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>';
    html += '</div>';
    html += '<div class="flex-grow-1">';
    html += '<h5 class="mb-0 fw-bold">Hardware</h5>';
    html += '<p class="text-muted small mb-0" style="font-size: 0.8rem;">Physical devices and equipment vendors</p>';
    html += '</div>';
    html += '<span class="badge bg-info rounded-pill px-2 py-1">' + hardwareVendors.length + '</span>';
    html += '</div>';
    html += '<div class="row g-3">';
    hardwareVendors.forEach(vendor => {
      html += createVendorCard(vendor);
    });
    html += '</div></div>';
  }

  // Show software-only vendors
  if (softwareVendors.length > 0) {
    html += '<div class="mb-4">';
    html += '<div class="d-flex align-items-center mb-3">';
    html += '<div class="d-inline-flex align-items-center justify-content-center rounded-circle bg-success bg-opacity-10 me-2" style="width: 40px; height: 40px;">';
    html += '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="text-success"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>';
    html += '</div>';
    html += '<div class="flex-grow-1">';
    html += '<h5 class="mb-0 fw-bold">Software</h5>';
    html += '<p class="text-muted small mb-0" style="font-size: 0.8rem;">Application and software solution vendors</p>';
    html += '</div>';
    html += '<span class="badge bg-success rounded-pill px-2 py-1">' + softwareVendors.length + '</span>';
    html += '</div>';
    html += '<div class="row g-3">';
    softwareVendors.forEach(vendor => {
      html += createVendorCard(vendor);
    });
    html += '</div></div>';
  }

  vendorsList.innerHTML = html;
}

function createVendorCard(vendor) {
  const vendorName = vendor.VendorName || vendor.vendorName || vendor.Name || vendor.name || 'Unknown';
  const useCase = vendor.UseCaseDescription || vendor.useCaseDescription || '';
  const vendorType = vendor.VendorType || vendor.vendorType || 'both';
  
  // Get icon and colors based on type - professional with subtle visual interest
  let iconSvg = '';
  let borderColor = '';
  let cardBg = '';
  let iconBgClass = '';
  let badgeClass = '';
  let badgeText = '';
  
  if (vendorType.toLowerCase() === 'hardware') {
    iconSvg = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>';
    borderColor = '#0d6efd';
    cardBg = 'linear-gradient(135deg, #f0f7ff 0%, #ffffff 100%)';
    iconBgClass = 'bg-primary bg-opacity-10 text-primary';
    badgeClass = 'bg-primary';
    badgeText = 'Hardware';
  } else if (vendorType.toLowerCase() === 'software') {
    iconSvg = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>';
    borderColor = '#198754';
    cardBg = 'linear-gradient(135deg, #f0fdf4 0%, #ffffff 100%)';
    iconBgClass = 'bg-success bg-opacity-10 text-success';
    badgeClass = 'bg-success';
    badgeText = 'Software';
  } else {
    iconSvg = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>';
    borderColor = '#495057';
    cardBg = 'linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%)';
    iconBgClass = 'bg-secondary bg-opacity-10 text-secondary';
    badgeClass = 'bg-secondary';
    badgeText = 'Hardware & Software';
  }
  
  let card = `
    <div class="col-md-6 col-lg-4">
      <div class="card h-100 border-0 shadow-sm" style="transition: all 0.3s ease; border-radius: 10px !important; background: ${cardBg}; border-left: 4px solid ${borderColor} !important;">
        <div class="card-body p-3">
          <div class="d-flex align-items-start mb-2">
            <div class="d-inline-flex align-items-center justify-content-center rounded-circle ${iconBgClass} me-2" style="width: 48px; height: 48px; flex-shrink: 0;">
              ${iconSvg}
            </div>
            <div class="flex-grow-1">
              <h6 class="card-title mb-1 fw-bold text-dark" style="font-size: 1rem;">${escapeHtml(vendorName)}</h6>
              <span class="badge ${badgeClass} rounded-pill" style="font-size: 0.7rem;">${badgeText}</span>
            </div>
          </div>
          ${useCase ? `
            <div class="mt-2 pt-2 border-top border-opacity-25" style="border-color: ${borderColor} !important;">
              <p class="text-muted small mb-0" style="line-height: 1.5; font-size: 0.85rem;">
                <strong class="text-dark" style="font-size: 0.8rem;">Use Case:</strong><br>
                ${escapeHtml(useCase)}
              </p>
            </div>
          ` : `
            <div class="mt-2 pt-2 border-top border-opacity-25" style="border-color: ${borderColor} !important;">
              <p class="text-muted small mb-0 fst-italic" style="font-size: 0.8rem;">No use case description provided</p>
            </div>
          `}
        </div>
      </div>
    </div>
  `;
  return card;
}

function escapeHtml(text) {
  if (!text) return '';
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Export for use in main.js
window.loadCompanyVendorsForPage = loadCompanyVendorsForPage;



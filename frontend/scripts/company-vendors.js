// Company Vendors Display Module (for non-admin users)
// Shows read-only list of company vendors in navbar

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

  await loadCompanyVendors();
}

async function loadCompanyVendors() {
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
      updateVendorsList([]);
      return;
    }

    const data = await response.json();
    
    if (data.Success || data.success) {
      const vendors = data.Vendors || data.vendors || [];
      updateVendorsList(vendors);
    } else {
      updateVendorsList([]);
    }
  } catch (error) {
    console.error('Error loading company vendors:', error);
    updateVendorsList([]);
  }
}

function updateVendorsList(vendors) {
  const vendorsList = document.getElementById('companyVendorsList');
  if (!vendorsList) return;

  if (vendors.length === 0) {
    vendorsList.innerHTML = '<li><span class="dropdown-item-text text-muted">No vendors selected</span></li>';
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

  // Show "Both" vendors first
  if (bothVendors.length > 0) {
    html += '<li><h6 class="dropdown-header">🔧💻 Hardware & Software</h6></li>';
    bothVendors.forEach(vendor => {
      html += createVendorItem(vendor);
    });
    if (hardwareVendors.length > 0 || softwareVendors.length > 0) {
      html += '<li><hr class="dropdown-divider"></li>';
    }
  }

  // Show hardware-only vendors
  if (hardwareVendors.length > 0) {
    html += '<li><h6 class="dropdown-header">🔧 Hardware</h6></li>';
    hardwareVendors.forEach(vendor => {
      html += createVendorItem(vendor);
    });
    if (softwareVendors.length > 0) {
      html += '<li><hr class="dropdown-divider"></li>';
    }
  }

  // Show software-only vendors
  if (softwareVendors.length > 0) {
    html += '<li><h6 class="dropdown-header">💻 Software</h6></li>';
    softwareVendors.forEach(vendor => {
      html += createVendorItem(vendor);
    });
  }

  vendorsList.innerHTML = html;
}

function createVendorItem(vendor) {
  const vendorName = vendor.VendorName || vendor.vendorName || vendor.Name || vendor.name || 'Unknown';
  const useCase = vendor.UseCaseDescription || vendor.useCaseDescription || '';
  
  let item = `<li><a class="dropdown-item" href="#"><strong>${vendorName}</strong>`;
  
  if (useCase) {
    item += `<br><small class="text-muted">${useCase}</small>`;
  }
  
  item += `</a></li>`;
  return item;
}


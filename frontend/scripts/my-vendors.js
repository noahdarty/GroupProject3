// My Vendors Display Module (for admin users)
// Shows the list of vendors the company has selected

if (typeof window.API_BASE_URL === 'undefined') {
  window.API_BASE_URL = 'http://localhost:5155';
}

// Make function globally accessible
window.loadMyVendors = async function loadMyVendors() {
  const token = localStorage.getItem('firebaseToken');
  if (!token) {
    showMyVendorsAlert('error', 'Please log in again.');
    return;
  }

  const container = document.getElementById('myVendorsList');
  if (!container) return;

  // Show loading spinner
  container.innerHTML = `
    <div class="text-center">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading vendors...</span>
      </div>
    </div>
  `;

  try {
    const response = await fetch(`${window.API_BASE_URL}/api/user/vendors`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error(`Failed to load vendors: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();

    if (data.Success || data.success) {
      const vendors = data.Vendors || data.vendors || [];
      renderMyVendors(vendors);
    } else {
      throw new Error(data.Message || data.message || 'Failed to load vendors');
    }
  } catch (error) {
    console.error('Error loading my vendors:', error);
    container.innerHTML = `
      <div class="alert alert-warning">
        <strong>Unable to load vendors.</strong><br>
        <small>Error: ${error.message}</small>
      </div>
    `;
  }
}

function renderMyVendors(vendors) {
  const container = document.getElementById('myVendorsList');
  if (!container) return;

  if (vendors.length === 0) {
    container.innerHTML = `
      <div class="alert alert-info">
        <strong>No vendors selected yet.</strong><br>
        Go to <a href="#" id="goToChooseVendors" class="alert-link">Choose Vendors</a> to select vendors for your company.
      </div>
    `;
    
    // Setup link to go to Choose Vendors
    const goToLink = document.getElementById('goToChooseVendors');
    if (goToLink) {
      goToLink.addEventListener('click', (e) => {
        e.preventDefault();
        const chooseVendorsLink = document.getElementById('chooseVendorsNavLink');
        if (chooseVendorsLink) {
          chooseVendorsLink.click();
        }
      });
    }
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
    html += '<div class="mb-4"><h5 class="mb-3">🔧💻 Hardware & Software Vendors</h5>';
    html += bothVendors.map(vendor => createMyVendorCard(vendor)).join('');
    html += '</div>';
  }

  // Show hardware-only vendors
  if (hardwareVendors.length > 0) {
    html += '<div class="mb-4"><h5 class="mb-3">🔧 Hardware Vendors</h5>';
    html += hardwareVendors.map(vendor => createMyVendorCard(vendor)).join('');
    html += '</div>';
  }

  // Show software-only vendors
  if (softwareVendors.length > 0) {
    html += '<div class="mb-4"><h5 class="mb-3">💻 Software Vendors</h5>';
    html += softwareVendors.map(vendor => createMyVendorCard(vendor)).join('');
    html += '</div>';
  }

  container.innerHTML = html;
}

function createMyVendorCard(vendor) {
  const vendorId = vendor.VendorId || vendor.vendorId;
  const vendorName = vendor.VendorName || vendor.vendorName;
  const vendorType = vendor.VendorType || vendor.vendorType;
  const vendorDescription = vendor.VendorDescription || vendor.vendorDescription;
  const useCase = vendor.UseCaseDescription || vendor.useCaseDescription;

  return `
    <div class="card mb-3">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
          <div class="flex-grow-1">
            <h6 class="mb-1">${vendorName}</h6>
            <span class="badge bg-${vendorType.toLowerCase() === 'hardware' ? 'primary' : 'success'} mb-2">
              ${vendorType}
            </span>
            ${vendorDescription ? `<p class="text-muted small mb-2">${vendorDescription}</p>` : ''}
            ${useCase ? `<p class="mb-0"><strong>Use Case:</strong> ${useCase}</p>` : ''}
          </div>
        </div>
      </div>
    </div>
  `;
}

function showMyVendorsAlert(type, message) {
  const alertDiv = document.getElementById('myVendorsAlert');
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







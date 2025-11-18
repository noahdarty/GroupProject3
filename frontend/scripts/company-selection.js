// Company Selection Module
// Use global window.API_BASE_URL if defined, otherwise set it
if (typeof window.window.API_BASE_URL === 'undefined') {
  window.window.API_BASE_URL = 'http://localhost:5155';
}

let allCompanies = [];

async function initCompanySelection() {
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  
  // Check if user already has a company
  const hasCompany = await checkUserCompany();
  
  if (hasCompany) {
    // User has company, hide company selection
    document.getElementById('companySelectionSection').classList.add('d-none');
    return false; // Company already set
  }
  
  // User needs to select company
  document.getElementById('companySelectionSection').classList.remove('d-none');
  await loadCompanies();
  setupCompanyForm();
  return true; // Company selection needed
}

async function checkUserCompany() {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) return false;

    const response = await fetch(`${window.window.API_BASE_URL}/api/user/company`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      return false;
    }

    const data = await response.json();
    return (data.Success || data.success) && (data.Company || data.company);
  } catch (error) {
    console.error('Error checking user company:', error);
    return false;
  }
}

async function loadCompanies() {
  try {
    const response = await fetch(`${window.API_BASE_URL}/api/companies`);
    
    if (!response.ok) {
      throw new Error(`Failed to load companies: ${response.status}`);
    }
    
    const data = await response.json();
    
    if (data.Success || data.success) {
      allCompanies = data.Companies || data.companies || [];
      populateCompanySelect();
    }
  } catch (error) {
    console.error('Error loading companies:', error);
    showCompanyAlert('error', 'Failed to load companies. Please refresh the page.');
  }
}

function populateCompanySelect() {
  const select = document.getElementById('companySelect');
  if (!select) return;

  // Clear existing options except first two
  while (select.options.length > 2) {
    select.remove(2);
  }

  // Add companies
  allCompanies.forEach(company => {
    const option = document.createElement('option');
    option.value = company.Id || company.id;
    option.textContent = company.Name || company.name;
    select.appendChild(option);
  });
}

function setupCompanyForm() {
  const companySelect = document.getElementById('companySelect');
  const newCompanyFields = document.getElementById('newCompanyFields');
  const companyForm = document.getElementById('companyForm');
  const saveBtn = document.getElementById('saveCompanyBtn');

  if (companySelect) {
    companySelect.addEventListener('change', (e) => {
      if (e.target.value === 'new') {
        newCompanyFields.classList.remove('d-none');
        document.getElementById('newCompanyName').required = true;
      } else {
        newCompanyFields.classList.add('d-none');
        document.getElementById('newCompanyName').required = false;
      }
    });
  }

  if (companyForm) {
    companyForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      await saveCompanyAndRole();
    });
  }
}

async function saveCompanyAndRole() {
  const saveBtn = document.getElementById('saveCompanyBtn');
  const token = localStorage.getItem('firebaseToken');
  const companySelect = document.getElementById('companySelect');
  const userRole = document.getElementById('userRole');
  
  if (!token) {
    showCompanyAlert('error', 'Please log in again.');
    return;
  }

  const selectedCompanyId = companySelect.value;
  const role = userRole.value;

  if (!selectedCompanyId || !role) {
    showCompanyAlert('error', 'Please select a company and role.');
    return;
  }

  saveBtn.disabled = true;
  saveBtn.textContent = 'Saving...';

  try {
    let companyId = selectedCompanyId;

    // If creating new company
    if (selectedCompanyId === 'new') {
      const companyName = document.getElementById('newCompanyName').value;
      if (!companyName) {
        showCompanyAlert('error', 'Please enter a company name.');
        saveBtn.disabled = false;
        saveBtn.textContent = 'Continue';
        return;
      }

      const response = await fetch(`${window.API_BASE_URL}/api/companies`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          Name: companyName,
          Description: document.getElementById('newCompanyDescription').value || null,
          Industry: document.getElementById('newCompanyIndustry').value || null
        })
      });

      if (!response.ok) {
        throw new Error('Failed to create company');
      }

      const data = await response.json();
      if (data.Success || data.success) {
        companyId = (data.Company || data.company).Id || (data.Company || data.company).id;
      } else {
        throw new Error(data.Message || data.message || 'Failed to create company');
      }
    }

    // Update user with company and role
    const updateResponse = await fetch(`${window.API_BASE_URL}/api/user/update`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        CompanyId: companyId,
        Role: role
      })
    });

    if (!updateResponse.ok) {
      throw new Error('Failed to update user');
    }

    const updateData = await updateResponse.json();
    
    if (updateData.Success || updateData.success) {
      // Update user in localStorage
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      user.Role = role;
      user.CompanyId = companyId;
      localStorage.setItem('user', JSON.stringify(user));

      // Hide company selection, show vendor selection or admin section
      document.getElementById('companySelectionSection').classList.add('d-none');
      
      // Reinitialize app to show correct section
      if (typeof window.initApp === 'function') {
        window.initApp();
      }
    } else {
      throw new Error(updateData.Message || updateData.message || 'Failed to update user');
    }
  } catch (error) {
    console.error('Error saving company and role:', error);
    showCompanyAlert('error', error.message || 'Failed to save. Please try again.');
    saveBtn.disabled = false;
    saveBtn.textContent = 'Continue';
  }
}

function showCompanyAlert(type, message) {
  const alertDiv = document.getElementById('companyAlert');
  if (!alertDiv) return;

  alertDiv.className = `alert alert-${type === 'error' ? 'danger' : 'success'} alert-dismissible fade show`;
  alertDiv.innerHTML = `
    ${message}
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  `;
  alertDiv.classList.remove('d-none');

  setTimeout(() => {
    alertDiv.classList.add('d-none');
  }, 5000);
}

// Make function globally accessible
window.initCompanySelection = initCompanySelection;


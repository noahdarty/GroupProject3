// Backend API base URL (declare once globally)
if (typeof window.API_BASE_URL === 'undefined') {
  window.API_BASE_URL = 'http://localhost:5155';
}

// Test backend connection
async function testConnection() {
  const statusDiv = document.getElementById('status');
  statusDiv.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div> Testing connection...';
  
  try {
    const response = await fetch(`${window.API_BASE_URL}/weatherforecast`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (response.ok) {
      statusDiv.innerHTML = '<div class="alert alert-success">✓ Backend connection successful!</div>';
    } else {
      statusDiv.innerHTML = `<div class="alert alert-warning">Backend responded with status: ${response.status}</div>`;
    }
  } catch (error) {
      statusDiv.innerHTML = `<div class="alert alert-danger">✗ Connection failed: ${error.message}<br><small>Make sure the backend is running on ${window.API_BASE_URL}</small></div>`;
  }
}

// Fetch weather forecast
async function fetchWeatherForecast() {
  const resultsDiv = document.getElementById('weatherResults');
  resultsDiv.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div> Loading...';
  
  try {
    const response = await fetch(`${window.API_BASE_URL}/weatherforecast`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (response.ok) {
      const data = await response.json();
      displayWeatherData(data);
    } else {
      resultsDiv.innerHTML = `<div class="alert alert-warning">Failed to fetch weather: ${response.status}</div>`;
    }
  } catch (error) {
    resultsDiv.innerHTML = `<div class="alert alert-danger">Error: ${error.message}</div>`;
  }
}

// Display weather data
function displayWeatherData(forecasts) {
  const resultsDiv = document.getElementById('weatherResults');
  
  if (!forecasts || forecasts.length === 0) {
    resultsDiv.innerHTML = '<div class="alert alert-info">No weather data available</div>';
    return;
  }

  let html = '<div class="table-responsive"><table class="table table-striped"><thead><tr><th>Date</th><th>Temperature (°C)</th><th>Temperature (°F)</th><th>Summary</th></tr></thead><tbody>';
  
  forecasts.forEach(forecast => {
    html += `
      <tr>
        <td>${forecast.date}</td>
        <td>${forecast.temperatureC}</td>
        <td>${forecast.temperatureF}</td>
        <td>${forecast.summary || 'N/A'}</td>
      </tr>
    `;
  });
  
  html += '</tbody></table></div>';
  resultsDiv.innerHTML = html;
}

// Initialize app when app section is shown
window.initApp = async function initApp() {
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const userRole = (user.Role || user.role || '').toLowerCase();
  
  console.log('Initializing app for user role:', userRole, 'User:', user);
  console.log('Full user object from localStorage:', JSON.stringify(user, null, 2));
  
  // If role is missing, try to refresh user data from backend
  if (!userRole && localStorage.getItem('firebaseToken')) {
    console.log('Role missing, refreshing user data from backend...');
    try {
      const token = localStorage.getItem('firebaseToken');
      const response = await fetch(`${window.API_BASE_URL}/api/auth/me`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      
      if (response.ok) {
        const userData = await response.json();
        console.log('Refreshed user data from backend:', userData);
        localStorage.setItem('user', JSON.stringify(userData));
        // Recursively call initApp with updated user data
        return window.initApp();
      }
    } catch (error) {
      console.error('Error refreshing user data:', error);
    }
  }
  
  const adminNavbar = document.getElementById('adminNavbar');
  const userNavbar = document.getElementById('userNavbar');
  const adminSection = document.getElementById('adminSection');
  const vendorSection = document.getElementById('vendorSelectionSection');
  const myVendorsSection = document.getElementById('myVendorsSection');
  const vulnerabilitiesSection = document.getElementById('vulnerabilitiesSection');
  
  if (userRole === 'admin') {
    console.log('Showing admin section');
    // Show admin navbar, hide user navbar
    if (adminNavbar) adminNavbar.classList.remove('d-none');
    if (userNavbar) userNavbar.classList.add('d-none');
    
    // Hide all sections initially
    if (vendorSection) vendorSection.classList.add('d-none');
    if (myVendorsSection) myVendorsSection.classList.add('d-none');
    if (adminSection) adminSection.classList.add('d-none');
    if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
    const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
    if (completedVulnerabilitiesSection) completedVulnerabilitiesSection.classList.add('d-none');
    
    // Setup logout button in admin navbar
    const logoutBtnAdminNav = document.getElementById('logoutBtnAdminNav');
    if (logoutBtnAdminNav) {
      logoutBtnAdminNav.addEventListener('click', () => {
        if (typeof window.logout === 'function') {
          window.logout();
        }
      });
    }
    
    // Setup "Choose Vendors" nav link
    const chooseVendorsNavLink = document.getElementById('chooseVendorsNavLink');
    if (chooseVendorsNavLink) {
      chooseVendorsNavLink.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide all sections, show choose vendors section
        if (adminSection) adminSection.classList.add('d-none');
        if (myVendorsSection) myVendorsSection.classList.add('d-none');
        if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
        const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
        if (completedVulnerabilitiesSection) completedVulnerabilitiesSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) tasksSection.classList.add('d-none');
        if (vendorSection) {
          vendorSection.classList.remove('d-none');
          // Initialize vendor selection to load and display vendors
          // This will load vendors, load user's selected vendors, and setup event listeners
          if (typeof window.initVendorSelection === 'function') {
            await window.initVendorSelection();
          }
        }
        // Update active nav link
        document.querySelectorAll('#adminNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        chooseVendorsNavLink.classList.add('active');
      });
    }
    
    // Setup "My Vendors" nav link
    const myVendorsNavLink = document.getElementById('myVendorsNavLink');
    if (myVendorsNavLink) {
      myVendorsNavLink.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide all sections, show my vendors section
        if (adminSection) adminSection.classList.add('d-none');
        if (vendorSection) vendorSection.classList.add('d-none');
        if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
        const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
        if (completedVulnerabilitiesSection) completedVulnerabilitiesSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) tasksSection.classList.add('d-none');
        if (myVendorsSection) {
          myVendorsSection.classList.remove('d-none');
          // Load and display selected vendors
          if (typeof window.loadMyVendors === 'function') {
            await window.loadMyVendors();
          }
        }
        // Update active nav link
        document.querySelectorAll('#adminNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        myVendorsNavLink.classList.add('active');
      });
    }
    
    // Setup "Vulnerabilities" nav link
    const vulnerabilitiesNavLink = document.getElementById('vulnerabilitiesNavLink');
    if (vulnerabilitiesNavLink) {
      vulnerabilitiesNavLink.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide all sections, show vulnerabilities section
        if (adminSection) adminSection.classList.add('d-none');
        if (vendorSection) vendorSection.classList.add('d-none');
        if (myVendorsSection) myVendorsSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) tasksSection.classList.add('d-none');
        const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
        if (completedVulnerabilitiesSection) completedVulnerabilitiesSection.classList.add('d-none');
        if (vulnerabilitiesSection) {
          vulnerabilitiesSection.classList.remove('d-none');
          // Load and display vulnerabilities
          if (typeof window.loadVulnerabilities === 'function') {
            await window.loadVulnerabilities();
          }
        }
        // Update active nav link
        document.querySelectorAll('#adminNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        vulnerabilitiesNavLink.classList.add('active');
      });
    }
    
    // Setup "Completed Vulnerabilities" nav link for admin
    const completedVulnerabilitiesNavLink = document.getElementById('completedVulnerabilitiesNavLink');
    if (completedVulnerabilitiesNavLink) {
      completedVulnerabilitiesNavLink.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide all sections, show completed vulnerabilities section
        if (adminSection) adminSection.classList.add('d-none');
        if (vendorSection) vendorSection.classList.add('d-none');
        if (myVendorsSection) myVendorsSection.classList.add('d-none');
        if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) tasksSection.classList.add('d-none');
        const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
        if (completedVulnerabilitiesSection) {
          completedVulnerabilitiesSection.classList.remove('d-none');
          // Load and display completed vulnerabilities
          if (typeof window.loadCompletedVulnerabilities === 'function') {
            await window.loadCompletedVulnerabilities();
          }
        }
        // Update active nav link
        document.querySelectorAll('#adminNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        completedVulnerabilitiesNavLink.classList.add('active');
      });
    }
    
    // Setup "My Tasks" nav link for admin
    const tasksNavLinkAdmin = document.getElementById('tasksNavLinkAdmin');
    if (tasksNavLinkAdmin) {
      tasksNavLinkAdmin.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide all sections, show tasks section
        if (adminSection) adminSection.classList.add('d-none');
        if (vendorSection) vendorSection.classList.add('d-none');
        if (myVendorsSection) myVendorsSection.classList.add('d-none');
        if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
        const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
        if (completedVulnerabilitiesSection) completedVulnerabilitiesSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) {
          tasksSection.classList.remove('d-none');
          // Load and display tasks
          if (typeof window.initTasks === 'function') {
            await window.initTasks();
          }
        }
        // Update active nav link
        document.querySelectorAll('#adminNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        tasksNavLinkAdmin.classList.add('active');
      });
    }
    
    // Show "Choose Vendors" page by default when admin logs in
    if (vendorSection) {
      vendorSection.classList.remove('d-none');
      if (chooseVendorsNavLink) chooseVendorsNavLink.classList.add('active');
    }
    
    // Initialize vendor selection to load vendors immediately
    if (typeof window.initVendorSelection === 'function') {
      console.log('Calling initVendorSelection for admin');
      window.initVendorSelection();
    }
  } else {
    console.log('Showing content for non-admin user');
    // Show user navbar, hide admin navbar
    if (adminNavbar) adminNavbar.classList.add('d-none');
    if (userNavbar) userNavbar.classList.remove('d-none');
    
    // Hide vendor selection, admin sections, and completed vulnerabilities (admin only)
    if (vendorSection) vendorSection.classList.add('d-none');
    if (adminSection) adminSection.classList.add('d-none');
    if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
    const completedVulnerabilitiesSection = document.getElementById('completedVulnerabilitiesSection');
    if (completedVulnerabilitiesSection) completedVulnerabilitiesSection.classList.add('d-none');
    
    // Hide tasks section initially (will be shown when user clicks "My Tasks" nav link)
    const tasksSection = document.getElementById('tasksSection');
    if (tasksSection) tasksSection.classList.add('d-none');
    
    const companyVendorsSection = document.getElementById('companyVendorsSection');
    
    // Setup "Company Vendors" nav link for non-admin users
    const companyVendorsNavLink = document.getElementById('companyVendorsNavLink');
    if (companyVendorsNavLink) {
      companyVendorsNavLink.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide vulnerabilities and tasks, show company vendors
        if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) tasksSection.classList.add('d-none');
        if (companyVendorsSection) {
          companyVendorsSection.classList.remove('d-none');
          // Load vendors
          if (typeof window.loadCompanyVendorsForPage === 'function') {
            await window.loadCompanyVendorsForPage();
          }
        }
        // Update active nav link
        document.querySelectorAll('#userNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        companyVendorsNavLink.classList.add('active');
      });
    }
    
    // Setup "Vulnerabilities" nav link for non-admin users
    const vulnerabilitiesNavLinkUser = document.getElementById('vulnerabilitiesNavLinkUser');
    if (vulnerabilitiesNavLinkUser) {
      vulnerabilitiesNavLinkUser.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide company vendors and tasks, show vulnerabilities
        if (companyVendorsSection) companyVendorsSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) tasksSection.classList.add('d-none');
        if (vulnerabilitiesSection) {
          vulnerabilitiesSection.classList.remove('d-none');
          // Load vulnerabilities
          if (typeof window.loadVulnerabilities === 'function') {
            await window.loadVulnerabilities();
          }
        }
        // Update active nav link
        document.querySelectorAll('#userNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        vulnerabilitiesNavLinkUser.classList.add('active');
      });
    }
    
    // Setup "My Tasks" nav link for non-admin users
    const tasksNavLinkUser = document.getElementById('tasksNavLinkUser');
    if (tasksNavLinkUser) {
      tasksNavLinkUser.addEventListener('click', async (e) => {
        e.preventDefault();
        // Hide company vendors and vulnerabilities, show tasks
        if (companyVendorsSection) companyVendorsSection.classList.add('d-none');
        if (vulnerabilitiesSection) vulnerabilitiesSection.classList.add('d-none');
        const tasksSection = document.getElementById('tasksSection');
        if (tasksSection) {
          tasksSection.classList.remove('d-none');
          // Load and display tasks
          if (typeof window.initTasks === 'function') {
            await window.initTasks();
          }
        }
        // Update active nav link
        document.querySelectorAll('#userNavbar .nav-link').forEach(link => {
          link.classList.remove('active');
        });
        tasksNavLinkUser.classList.add('active');
      });
    }
    
    // Show Company Vendors page by default for non-admin users
    // Make sure tasks section is hidden (already hidden above, but ensure it stays hidden)
    if (tasksSection) tasksSection.classList.add('d-none');
    
    if (companyVendorsSection) {
      companyVendorsSection.classList.remove('d-none');
      if (companyVendorsNavLink) companyVendorsNavLink.classList.add('active');
      // Load vendors
      if (typeof window.loadCompanyVendorsForPage === 'function') {
        await window.loadCompanyVendorsForPage();
      }
    }
    
    // Initialize company vendors display for non-admin users (after showing the section)
    if (typeof window.initCompanyVendors === 'function') {
      console.log('Calling initCompanyVendors');
      window.initCompanyVendors();
    } else {
      console.error('initCompanyVendors function not found');
    }
  }
}

// Event listeners
document.addEventListener('DOMContentLoaded', () => {
  const testBtn = document.getElementById('testBtn');
  const fetchWeatherBtn = document.getElementById('fetchWeatherBtn');
  
  if (testBtn) {
    testBtn.addEventListener('click', testConnection);
  }
  
  if (fetchWeatherBtn) {
    fetchWeatherBtn.addEventListener('click', fetchWeatherForecast);
  }

  // Initialize app when authenticated (check on page load)
  const token = localStorage.getItem('firebaseToken');
  if (token) {
    // User is already logged in, initialize app
    setTimeout(() => {
      if (typeof window.initApp === 'function') {
        window.initApp();
      }
    }, 200);
  }

  // Watch for app section visibility changes
  const appSection = document.getElementById('appSection');
  if (appSection) {
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.target.id === 'appSection' && !mutation.target.classList.contains('d-none')) {
          setTimeout(() => {
            if (typeof window.initApp === 'function') {
              window.initApp();
            }
          }, 100);
        }
      });
    });

    observer.observe(appSection, { attributes: true, attributeFilter: ['class'] });
  }
});



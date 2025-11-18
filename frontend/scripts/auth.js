import { auth } from './firebase-config.js';
import { 
  signInWithEmailAndPassword, 
  createUserWithEmailAndPassword,
  signOut,
  sendEmailVerification,
  onAuthStateChanged,
  reload
} from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-auth.js';

// Backend API base URL (declare once globally)
if (typeof window.API_BASE_URL === 'undefined') {
  window.API_BASE_URL = 'http://localhost:5155';
}

// Check if user is already logged in
window.addEventListener('DOMContentLoaded', () => {
  checkAuthStatus();
  setupLoginForm();
  setupRegisterLink();
  setupResendVerification();
  setupLogout();
});

function checkAuthStatus() {
  const token = localStorage.getItem('firebaseToken');
  const loginSection = document.getElementById('loginSection');
  const appSection = document.getElementById('appSection');
  
  if (token) {
    // User is logged in, show app section
    if (loginSection) loginSection.classList.add('d-none');
    if (appSection) appSection.classList.remove('d-none');
    
    // Initialize app after showing the section
    setTimeout(() => {
      if (typeof window.initApp === 'function') {
        window.initApp();
      } else {
        console.warn('initApp function not yet available, will retry...');
        // Retry after a bit more time
        setTimeout(() => {
          if (typeof window.initApp === 'function') {
            window.initApp();
          }
        }, 500);
      }
    }, 100);
  } else {
    // User is not logged in, show login section
    if (loginSection) loginSection.classList.remove('d-none');
    if (appSection) appSection.classList.add('d-none');
  }
}

function setupLogout() {
  const logoutBtn = document.getElementById('logoutBtn');
  if (logoutBtn) {
    logoutBtn.addEventListener('click', async () => {
      await logout();
    });
  }
}

function setupLoginForm() {
  const loginForm = document.getElementById('loginForm');
  const loginBtn = document.getElementById('loginBtn');
  const errorAlert = document.getElementById('errorAlert');
  const successAlert = document.getElementById('successAlert');

  loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const btnText = loginBtn.querySelector('.btn-text');
    const btnSpinner = loginBtn.querySelector('.btn-spinner');
    const isRegister = btnText.textContent === 'Sign Up';
    
    // Get signup fields
    const signupFields = document.getElementById('signupFields');
    const signupRole = document.getElementById('signupRole');
    const signupCompany = document.getElementById('signupCompany');
    
    // Always remove required from hidden fields first to prevent validation errors
    if (signupFields && signupFields.classList.contains('d-none')) {
      if (signupRole) signupRole.removeAttribute('required');
      if (signupCompany) signupCompany.removeAttribute('required');
    }
    
    // Validate signup fields only if in register mode
    if (isRegister) {
      if (!signupRole || !signupRole.value) {
        errorAlert.textContent = 'Please select your role.';
        errorAlert.classList.remove('d-none');
        loginBtn.disabled = false;
        btnText.textContent = 'Sign Up';
        btnSpinner.classList.add('d-none');
        return;
      }
      if (!signupCompany || !signupCompany.value) {
        errorAlert.textContent = 'Please select your company.';
        errorAlert.classList.remove('d-none');
        loginBtn.disabled = false;
        btnText.textContent = 'Sign Up';
        btnSpinner.classList.add('d-none');
        return;
      }
    }

    // Clear previous alerts
    errorAlert.classList.add('d-none');
    successAlert.classList.add('d-none');
    loginBtn.disabled = true;
    btnText.textContent = isRegister ? 'Creating Account...' : 'Signing In...';
    btnSpinner.classList.remove('d-none');

    try {
      let userCredential;
      
      if (isRegister) {
        // Get role and company before creating account
        const role = document.getElementById('signupRole')?.value;
        const companyId = document.getElementById('signupCompany')?.value ? parseInt(document.getElementById('signupCompany').value) : null;
        
        // Store role and company in localStorage for first login
        if (role) {
          localStorage.setItem('pendingRole', role);
        }
        if (companyId) {
          localStorage.setItem('pendingCompanyId', companyId.toString());
        }
        
        // Create new user
        userCredential = await createUserWithEmailAndPassword(auth, email, password);
        
        // Send verification email for new users (use default Firebase action URL)
        await sendEmailVerification(userCredential.user);
        
        // Clear any previous errors
        errorAlert.classList.add('d-none');
        
        // Show success message
        successAlert.textContent = 'Account created! Please check your email to verify your account before signing in.';
        successAlert.classList.remove('d-none');
        
        // Reset form and button
        loginBtn.disabled = false;
        btnText.textContent = 'Sign In';
        btnSpinner.classList.add('d-none');
        
        // Switch back to login mode (but preserve the success alert)
        const registerLink = document.getElementById('registerLink');
        if (registerLink) {
          registerLink.innerHTML = 'Don\'t have an account? <strong>Sign up</strong>';
        }
        const loginSubtitle = document.querySelector('.login-subtitle');
        if (loginSubtitle) {
          loginSubtitle.textContent = 'Sign in to your account';
        }
        
        // Hide signup fields
        const signupFields = document.getElementById('signupFields');
        if (signupFields) {
          signupFields.classList.add('d-none');
          const signupRole = document.getElementById('signupRole');
          const signupCompany = document.getElementById('signupCompany');
          signupRole?.removeAttribute('required');
          signupCompany?.removeAttribute('required');
        }
        
        // Clear form but keep success message visible
        document.getElementById('email').value = '';
        document.getElementById('password').value = '';
        
        return;
      } else {
        // Sign in existing user
        userCredential = await signInWithEmailAndPassword(auth, email, password);
        
        // Reload user to get latest email verification status
        await reload(userCredential.user);
        
        // Get fresh user data after reload
        const freshUser = auth.currentUser;
        if (freshUser && !freshUser.emailVerified) {
          // Email still not verified, resend verification email
          try {
            await sendEmailVerification(freshUser);
            throw new Error('Please verify your email address. A fresh verification email has been sent to ' + email + '. Check your inbox and spam folder. After verifying, try logging in again.');
          } catch (verifyError) {
            throw new Error('Please verify your email address. Click "Resend" below to get a new verification email, then check your inbox and spam folder.');
          }
        }
      }

      // Get the current user (might be different after reload)
      const currentUser = auth.currentUser || userCredential.user;
      
      // Final check for email verification
      if (!currentUser.emailVerified) {
        throw new Error('Email address not verified. Please check your email and verify your account.');
      }

      // Get Firebase ID token (force refresh to get latest status)
      const idToken = await currentUser.getIdToken(true);

      // Get role and company from localStorage (set during signup) or from form if this is signup
      let role = null;
      let companyId = null;
      
      if (isRegister) {
        // During signup, get from form
        role = document.getElementById('signupRole')?.value;
        companyId = document.getElementById('signupCompany')?.value ? parseInt(document.getElementById('signupCompany').value) : null;
      } else {
        // During login, check localStorage for pending role/company (from signup)
        role = localStorage.getItem('pendingRole');
        const pendingCompanyId = localStorage.getItem('pendingCompanyId');
        if (pendingCompanyId) {
          companyId = parseInt(pendingCompanyId);
        }
      }

      // Verify token with backend and get user info
      const verifyRequest = {
        idToken: idToken
      };
      
      // Include role and companyId if available (from signup or first login after signup)
      if (role) {
        verifyRequest.Role = role;
      }
      if (companyId) {
        verifyRequest.CompanyId = companyId;
      }
      
      // Clear pending values after using them
      if (!isRegister && (role || companyId)) {
        localStorage.removeItem('pendingRole');
        localStorage.removeItem('pendingCompanyId');
      }

      const response = await fetch(`${window.API_BASE_URL}/api/auth/verify-token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(verifyRequest),
      });

      // Check if response is OK
      if (!response.ok) {
        let errorData;
        try {
          errorData = await response.json();
        } catch (e) {
          errorData = { message: `Server error: ${response.status} ${response.statusText}` };
        }
        console.error('Backend returned error status:', response.status);
        console.error('Error data:', JSON.stringify(errorData, null, 2));
        throw new Error(errorData.message || `Server error: ${response.status}`);
      }

      const data = await response.json();
      console.log('Backend response:', JSON.stringify(data, null, 2));

      // Backend returns PascalCase (Success), check both cases for compatibility
      if (data.Success || data.success) {
        // Store token and user info
        localStorage.setItem('firebaseToken', idToken);
        localStorage.setItem('user', JSON.stringify(data.User || data.user));

        // Show app section, hide login section
        checkAuthStatus();
        
        // Initialize app after a short delay to ensure DOM is ready
        setTimeout(() => {
          if (typeof window.initApp === 'function') {
            window.initApp();
          }
        }, 100);
      } else {
        // Log the error for debugging
        console.error('Backend auth error:', data);
        throw new Error(data.Message || data.message || 'Authentication failed');
      }
    } catch (error) {
      console.error('Auth error:', error);
      console.error('Error details:', {
        code: error.code,
        message: error.message,
        emailVerified: auth.currentUser?.emailVerified
      });
      
      let errorMessage = error.message || 'Authentication failed. Please try again.';
      
      // Provide more helpful error messages
      if (error.code === 'auth/email-not-verified') {
        errorMessage = 'Please verify your email address before signing in. Check your inbox for the verification email.';
      } else if (error.message?.includes('not verified')) {
        errorMessage = error.message;
      }
      
      errorAlert.textContent = errorMessage;
      errorAlert.classList.remove('d-none');
      loginBtn.disabled = false;
      btnText.textContent = isRegister ? 'Sign Up' : 'Sign In';
      btnSpinner.classList.add('d-none');
    }
  });
}

function setupRegisterLink() {
  const registerLink = document.getElementById('registerLink');
  const loginForm = document.getElementById('loginForm');
  const loginBtn = document.getElementById('loginBtn');
  const btnText = loginBtn.querySelector('.btn-text');
  const loginSubtitle = document.querySelector('.login-subtitle');
  const signupFields = document.getElementById('signupFields');
  const signupRole = document.getElementById('signupRole');
  const signupCompany = document.getElementById('signupCompany');

  registerLink.addEventListener('click', async (e) => {
    e.preventDefault();
    
    if (btnText.textContent === 'Sign In') {
      // Switch to register mode
      btnText.textContent = 'Sign Up';
      registerLink.innerHTML = 'Already have an account? <strong>Sign in</strong>';
      if (loginSubtitle) loginSubtitle.textContent = 'Create a new account';
      if (signupFields) {
        signupFields.classList.remove('d-none');
        // Don't set required attribute - we'll validate manually in JavaScript
      }
      
      // Load companies for dropdown
      await loadCompaniesForSignup();
    } else {
      // Switch to login mode
      btnText.textContent = 'Sign In';
      registerLink.innerHTML = 'Don\'t have an account? <strong>Sign up</strong>';
      if (loginSubtitle) loginSubtitle.textContent = 'Sign in to your account';
      if (signupFields) {
        signupFields.classList.add('d-none');
        // Remove required when fields are hidden to avoid validation errors
        if (signupRole) {
          signupRole.removeAttribute('required');
          signupRole.value = ''; // Clear value
        }
        if (signupCompany) {
          signupCompany.removeAttribute('required');
          signupCompany.value = ''; // Clear value
        }
      }
    }
    
    // Clear form and errors, but preserve success alerts
    loginForm.reset();
    document.getElementById('errorAlert').classList.add('d-none');
    // Don't clear success alert - let it stay visible if it was just shown
  });
}

async function loadCompaniesForSignup() {
  const companySelect = document.getElementById('signupCompany');
  if (!companySelect) return;

  try {
    const response = await fetch(`${window.API_BASE_URL}/api/companies`);
    
    if (!response.ok) {
      companySelect.innerHTML = '<option value="">Error loading companies</option>';
      return;
    }
    
    const data = await response.json();
    
    if (data.Success || data.success) {
      const companies = data.Companies || data.companies || [];
      
      if (companies.length === 0) {
        companySelect.innerHTML = '<option value="">No companies available</option>';
        return;
      }
      
      let html = '<option value="">Select a company...</option>';
      companies.forEach(company => {
        const companyId = company.Id || company.id;
        const companyName = company.Name || company.name;
        html += `<option value="${companyId}">${companyName}</option>`;
      });
      
      companySelect.innerHTML = html;
    } else {
      companySelect.innerHTML = '<option value="">Error loading companies</option>';
    }
  } catch (error) {
    console.error('Error loading companies:', error);
    companySelect.innerHTML = '<option value="">Error loading companies</option>';
  }
}

function setupResendVerification() {
  const resendLink = document.getElementById('resendVerificationLink');
  const errorAlert = document.getElementById('errorAlert');
  const successAlert = document.getElementById('successAlert');
  const emailInput = document.getElementById('email');
  const passwordInput = document.getElementById('password');

  resendLink.addEventListener('click', async (e) => {
    e.preventDefault();
    
    const email = emailInput.value;
    const password = passwordInput.value;
    
    if (!email) {
      errorAlert.textContent = 'Please enter your email address first.';
      errorAlert.classList.remove('d-none');
      successAlert.classList.add('d-none');
      return;
    }

    if (!password) {
      errorAlert.textContent = 'Please enter your password to resend verification email.';
      errorAlert.classList.remove('d-none');
      successAlert.classList.add('d-none');
      return;
    }

    try {
      // Sign in to get the user object
      const userCredential = await signInWithEmailAndPassword(auth, email, password);
      
      if (!userCredential.user.emailVerified) {
        // Resend verification email with fresh link (use default Firebase action URL)
        await sendEmailVerification(userCredential.user);
        successAlert.textContent = 'Fresh verification email sent! Check your inbox and spam folder. The link expires in a few minutes, so check your email soon.';
        successAlert.classList.remove('d-none');
        errorAlert.classList.add('d-none');
      } else {
        successAlert.textContent = 'Your email is already verified! You can sign in now.';
        successAlert.classList.remove('d-none');
        errorAlert.classList.add('d-none');
      }
    } catch (error) {
      errorAlert.textContent = 'Could not resend email: ' + error.message + '. Make sure you entered the correct email and password.';
      errorAlert.classList.remove('d-none');
      successAlert.classList.add('d-none');
    }
  });
}

// Export logout function for use in other pages
export async function logout() {
  try {
    await signOut(auth);
    localStorage.removeItem('firebaseToken');
    localStorage.removeItem('user');
    // Show login section, hide app section
    checkAuthStatus();
  } catch (error) {
    console.error('Logout error:', error);
  }
}

// Make logout globally accessible
window.logout = logout;


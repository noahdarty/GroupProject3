// Tasks management script

const API_BASE_URL = window.API_BASE_URL || 'http://localhost:5000';

// Store all tasks for filtering
let allTasks = [];

// Load tasks for the current user
async function loadTasks() {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      console.error('No authentication token found');
      return;
    }

    const response = await fetch(`${API_BASE_URL}/api/tasks`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    allTasks = data.Tasks || data.tasks || [];
    
    // Apply current filter
    applyTaskFilter();
  } catch (error) {
    console.error('Error loading tasks:', error);
    const tasksList = document.getElementById('tasksList');
    if (tasksList) {
      tasksList.innerHTML = `
        <div class="alert alert-danger" role="alert">
          Error loading tasks: ${error.message}
        </div>
      `;
    }
  }
}

// Apply filter to tasks
function applyTaskFilter() {
  const filterSelect = document.getElementById('taskStatusFilter');
  const selectedStatus = filterSelect ? filterSelect.value : 'all';
  
  let filteredTasks = allTasks;
  
  if (selectedStatus !== 'all') {
    filteredTasks = allTasks.filter(task => {
      const status = (task.Status || task.status || 'pending').toLowerCase();
      return status === selectedStatus.toLowerCase();
    });
  }
  
  displayTasks(filteredTasks);
}

// Display tasks in the UI
function displayTasks(tasks) {
  const tasksList = document.getElementById('tasksList');
  if (!tasksList) return;

  // Check if user is admin
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const userRole = (user.Role || user.role || '').toLowerCase();
  const isAdmin = userRole === 'admin';

  // Update section title and description for admins
  const tasksSection = document.getElementById('tasksSection');
  if (tasksSection) {
    const cardHeader = tasksSection.querySelector('.card-header h5');
    const cardBody = tasksSection.querySelector('.card-body p.text-muted');
    if (isAdmin) {
      if (cardHeader) cardHeader.textContent = 'Tasks Assigned';
      if (cardBody) cardBody.textContent = 'All tasks you have assigned to employees and managers. Monitor their progress and status.';
    } else {
      if (cardHeader) cardHeader.textContent = 'My Tasks';
      if (cardBody) cardBody.textContent = 'Vulnerabilities assigned to you. Update status and add notes as you work on them.';
    }
  }

  // Show filter info if filtering
  const filterSelect = document.getElementById('taskStatusFilter');
  const selectedStatus = filterSelect ? filterSelect.value : 'all';
  const isFiltered = selectedStatus !== 'all';
  const totalCount = allTasks.length;
  const filteredCount = tasks.length;

  if (tasks.length === 0) {
    let emptyMessage = '';
    if (isFiltered) {
      const statusLabel = selectedStatus.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
      emptyMessage = `No tasks with status "${statusLabel}" found.`;
    } else {
      emptyMessage = isAdmin 
        ? 'No tasks have been assigned yet. Assign vulnerabilities to employees or managers from the Vulnerabilities page.'
        : 'No tasks assigned to you yet.';
    }
    tasksList.innerHTML = `
      <div class="text-center py-5">
        <i class="bi bi-inbox display-4 text-muted mb-3"></i>
        <p class="text-muted">${emptyMessage}</p>
        ${isFiltered ? `<p class="text-muted small"><a href="#" onclick="document.getElementById('taskStatusFilter').value='all'; window.applyTaskFilter(); return false;">Show all tasks</a></p>` : ''}
      </div>
    `;
    return;
  }

  // Show count info if filtered
  let countInfo = '';
  if (isFiltered && totalCount > 0) {
    const statusLabel = selectedStatus.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
    countInfo = `<div class="mb-3"><small class="text-muted">Showing <strong>${filteredCount}</strong> of <strong>${totalCount}</strong> tasks (filtered by: ${statusLabel})</small></div>`;
  }

  let html = countInfo + '<div class="row">';
  
  tasks.forEach(task => {
    const taskId = task.Id || task.id;
    const cveId = task.CveId || task.cveId || 'N/A';
    const title = task.Title || task.title || 'No title';
    const status = task.Status || task.status || 'pending';
    const priority = task.Priority || task.priority || 'Medium';
    const notes = task.Notes || task.notes || '';
    const severityLevel = task.SeverityLevel || task.severityLevel || 'Unknown';
    const assignedByEmail = task.AssignedByEmail || task.assignedByEmail || 'Unknown';
    const assignedToEmail = task.AssignedToEmail || task.assignedToEmail || 'Unknown';
    const assignedByUserId = task.AssignedByUserId || task.assignedByUserId;
    const assignedToUserId = task.AssignedToUserId || task.assignedToUserId;
    const createdAt = task.CreatedAt || task.createdAt || '';
    const resolvedAt = task.ResolvedAt || task.resolvedAt || null;
    
    // Check if task is self-assigned
    const isSelfAssigned = assignedByUserId && assignedToUserId && assignedByUserId === assignedToUserId;

    // Status badge colors
    let statusBadge = 'secondary';
    if (status === 'resolved') statusBadge = 'success';
    else if (status === 'in_progress') statusBadge = 'primary';
    else if (status === 'closed') statusBadge = 'dark';
    else statusBadge = 'warning';

    // Priority badge colors
    let priorityBadge = 'secondary';
    if (priority === 'Critical') priorityBadge = 'danger';
    else if (priority === 'High') priorityBadge = 'warning';
    else if (priority === 'Medium') priorityBadge = 'info';
    else priorityBadge = 'success';

    // Severity badge colors
    let severityBadge = 'secondary';
    if (severityLevel === 'Critical') severityBadge = 'danger';
    else if (severityLevel === 'High') severityBadge = 'warning';
    else if (severityLevel === 'Medium') severityBadge = 'info';
    else if (severityLevel === 'Low') severityBadge = 'success';

    const createdDate = createdAt ? new Date(createdAt).toLocaleDateString() : 'N/A';
    const resolvedDate = resolvedAt ? new Date(resolvedAt).toLocaleDateString() : null;

    html += `
      <div class="col-md-6 col-lg-4 mb-4">
        <div class="card h-100 shadow-sm ${isSelfAssigned ? 'border-info border-2' : ''}">
          <div class="card-header bg-light">
            <div class="d-flex justify-content-between align-items-center">
              <code class="mb-0">${escapeHtml(cveId)}</code>
              <div class="d-flex gap-1 align-items-center">
                ${isSelfAssigned ? '<span class="badge bg-info" title="Self-assigned">🔵 Self</span>' : ''}
                <span class="badge bg-${statusBadge}">${escapeHtml(status)}</span>
              </div>
            </div>
          </div>
          <div class="card-body">
            <h6 class="card-title">${escapeHtml(title.substring(0, 80))}${title.length > 80 ? '...' : ''}</h6>
            <div class="mb-2">
              <span class="badge bg-${severityBadge}">${escapeHtml(severityLevel)}</span>
            </div>
            <p class="text-muted small mb-2">
              ${isAdmin ? `<strong>Assigned to:</strong> ${escapeHtml(assignedToEmail)}<br>` : `<strong>Assigned by:</strong> ${escapeHtml(assignedByEmail)}<br>`}
              ${isSelfAssigned ? '<span class="badge bg-info bg-opacity-25 text-info small">Self-assigned</span><br>' : ''}
              <strong>Created:</strong> ${createdDate}
              ${resolvedDate ? `<br><strong>Resolved:</strong> ${resolvedDate}` : ''}
            </p>
            ${notes ? `<p class="text-muted small"><strong>Notes:</strong> ${escapeHtml(notes.substring(0, 100))}${notes.length > 100 ? '...' : ''}</p>` : ''}
          </div>
          <div class="card-footer bg-white">
            ${isAdmin ? `
              <div class="d-flex flex-column gap-2">
                <small class="text-muted text-center">Assigned to: ${escapeHtml(assignedToEmail)}${isSelfAssigned ? ' <span class="badge bg-info bg-opacity-50 text-info">(Self)</span>' : ''}</small>
                <button class="btn btn-sm btn-primary w-100 update-task-btn" data-task-id="${taskId}" data-bs-toggle="modal" data-bs-target="#updateTaskModal">
                  Manage Task
                </button>
              </div>
            ` : `
              <button class="btn btn-sm btn-primary w-100 update-task-btn" data-task-id="${taskId}" data-bs-toggle="modal" data-bs-target="#updateTaskModal">
                Update Status
              </button>
            `}
          </div>
        </div>
      </div>
    `;
  });

  html += '</div>';
  tasksList.innerHTML = html;

  // Setup update buttons
  setupUpdateTaskButtons();
}

// Setup event listeners for update task buttons
function setupUpdateTaskButtons() {
  const updateButtons = document.querySelectorAll('.update-task-btn');
  updateButtons.forEach(btn => {
    btn.addEventListener('click', async () => {
      const taskId = btn.getAttribute('data-task-id');
      await loadTaskForUpdate(taskId);
    });
  });
}

// Load task details for update modal
async function loadTaskForUpdate(taskId) {
  try {
    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      alert('Not authenticated');
      return;
    }

    // Get task from the tasks list
    const response = await fetch(`${API_BASE_URL}/api/tasks`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error('Failed to load tasks');
    }

    const data = await response.json();
    const tasks = data.Tasks || data.tasks || [];
    const task = tasks.find(t => (t.Id || t.id) == taskId);

    if (!task) {
      alert('Task not found');
      return;
    }

    // Get current user info
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const userEmail = user.Email || user.email || 'You';
    const userRole = (user.Role || user.role || '').toLowerCase();
    const isAdmin = userRole === 'admin';
    
    // Populate update modal
    document.getElementById('updateTaskId').value = taskId;
    const statusSelect = document.getElementById('updateTaskStatus');
    const currentStatus = task.Status || task.status || 'pending';
    statusSelect.value = currentStatus;
    
    // Show "Closed" option only for admins
    const closedOption = statusSelect.querySelector('option[value="closed"]');
    if (closedOption) {
      if (isAdmin) {
        closedOption.style.display = '';
      } else {
        closedOption.style.display = 'none';
        // If current status is "closed", change it to "resolved" for display
        if (statusSelect.value === 'closed') {
          statusSelect.value = 'resolved';
        }
      }
    }
    
    // Handle notes
    const allNotes = task.Notes || task.notes || '';
    document.getElementById('updateTaskId').setAttribute('data-original-notes', allNotes);
    
    const allNotesDisplay = document.getElementById('allNotesDisplay');
    const notesTextarea = document.getElementById('updateTaskNotes');
    
    if (allNotesDisplay) {
      if (allNotes) {
        const formattedNotes = formatNotesForDisplay(allNotes);
        allNotesDisplay.innerHTML = formattedNotes;
      } else {
        allNotesDisplay.innerHTML = '<small class="text-muted">No notes yet</small>';
      }
    }
    
    if (notesTextarea) {
      // Always start with blank notes field - existing notes are shown in display area above
      notesTextarea.value = '';
      if (isAdmin) {
        notesTextarea.placeholder = 'Add or update notes...';
      } else {
        notesTextarea.placeholder = 'Add your notes or updates...';
      }
    }
  } catch (error) {
    console.error('Error loading task:', error);
    alert('Error loading task: ' + error.message);
  }
}

// Update task status and notes
async function updateTask() {
  try {
    const taskId = document.getElementById('updateTaskId').value;
    const status = document.getElementById('updateTaskStatus').value;
    const newNotes = document.getElementById('updateTaskNotes').value.trim();

    if (!taskId) {
      alert('Task ID is required');
      return;
    }

    const token = localStorage.getItem('firebaseToken');
    if (!token) {
      alert('Not authenticated');
      return;
    }

    // Get current user info
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const userEmail = user.Email || user.email || 'Unknown';
    const userRole = (user.Role || user.role || '').toLowerCase();
    const isAdmin = userRole === 'admin';
    
    // Prevent non-admin users from closing tasks
    if (!isAdmin && status === 'closed') {
      alert('Only administrators can close tasks. Please select "Resolved" instead.');
      return;
    }
    
    // Get existing notes from the data attribute (stored when modal was opened)
    const taskIdElement = document.getElementById('updateTaskId');
    const allExistingNotes = taskIdElement?.getAttribute('data-original-notes') || '';
    
    // Build notes - append employee notes with timestamp, or append admin notes with identifier
    let finalNotes = '';
    if (isAdmin) {
      // Admin appends their notes with admin identifier
      if (newNotes) {
        const timestamp = new Date().toLocaleString();
        if (allExistingNotes) {
          finalNotes = `${allExistingNotes}\n\n--- Admin (${userEmail}) (${timestamp}) ---\n${newNotes}`;
        } else {
          finalNotes = `--- Admin (${userEmail}) (${timestamp}) ---\n${newNotes}`;
        }
      } else {
        // No new notes, keep existing
        finalNotes = allExistingNotes;
      }
    } else {
      // Employee appends their notes to existing notes
      if (newNotes) {
        const timestamp = new Date().toLocaleString();
        if (allExistingNotes) {
          finalNotes = `${allExistingNotes}\n\n--- ${userEmail} (${timestamp}) ---\n${newNotes}`;
        } else {
          finalNotes = `--- ${userEmail} (${timestamp}) ---\n${newNotes}`;
        }
      } else {
        // No new notes, keep existing
        finalNotes = allExistingNotes;
      }
    }

    const response = await fetch(`${API_BASE_URL}/api/tasks/${taskId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        Status: status,
        Notes: finalNotes
      })
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.Message || 'Failed to update task');
    }

    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('updateTaskModal'));
    if (modal) {
      modal.hide();
    }

    // Reload tasks
    await loadTasks();

    // Show success message
    const alert = document.getElementById('tasksAlert');
    if (alert) {
      alert.className = 'alert alert-success';
      alert.textContent = 'Task updated successfully!';
      alert.classList.remove('d-none');
      setTimeout(() => {
        alert.classList.add('d-none');
      }, 3000);
    }
  } catch (error) {
    console.error('Error updating task:', error);
    alert('Error updating task: ' + error.message);
  }
}

// Initialize tasks display (called automatically, but section visibility is controlled by main.js)
window.initTasks = async function initTasks() {
  await loadTasks();
  
  // Setup filter dropdown
  const filterSelect = document.getElementById('taskStatusFilter');
  if (filterSelect) {
    filterSelect.addEventListener('change', () => {
      applyTaskFilter();
    });
  }
};

// Setup refresh button
document.addEventListener('DOMContentLoaded', () => {
  const refreshBtn = document.getElementById('refreshTasksBtn');
  if (refreshBtn) {
    refreshBtn.addEventListener('click', async () => {
      refreshBtn.querySelector('.spinner-border').classList.remove('d-none');
      await loadTasks();
      refreshBtn.querySelector('.spinner-border').classList.add('d-none');
    });
  }

  // Setup update task form submit
  const updateTaskForm = document.getElementById('updateTaskForm');
  if (updateTaskForm) {
    updateTaskForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      await updateTask();
    });
  }
});

// Helper function to escape HTML
function escapeHtml(text) {
  if (!text) return '';
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Format notes preview for task cards (shorter, more readable)
function formatNotesPreview(notes) {
  if (!notes || !notes.trim()) return '';
  
  // Check if notes contain employee separators
  const separatorPattern = /---\s*([^---]+?)\s*\(([^)]+)\)\s*---/g;
  const matches = [...notes.matchAll(separatorPattern)];
  
  let preview = '';
  
  if (matches.length === 0) {
    // Just admin notes
    const adminNotes = notes.trim();
    const truncated = adminNotes.length > 80 ? adminNotes.substring(0, 80) + '...' : adminNotes;
    preview = `<p class="text-muted small mb-0"><strong>Notes:</strong> ${escapeHtml(truncated)}</p>`;
  } else {
    // Has employee replies
    const firstPart = notes.substring(0, matches[0].index).trim();
    const adminPreview = firstPart.length > 60 ? firstPart.substring(0, 60) + '...' : firstPart;
    const replyCount = matches.length;
    preview = `<p class="text-muted small mb-0"><strong>Notes:</strong> ${escapeHtml(adminPreview)}</p>`;
    preview += `<p class="text-info small mb-0"><em>${replyCount} ${replyCount === 1 ? 'reply' : 'replies'}</em></p>`;
  }
  
  return preview;
}

// Format notes for display with conversation-style formatting
function formatNotesForDisplay(notes) {
  if (!notes || !notes.trim()) return '<small class="text-muted">No notes yet</small>';
  
  // Check if notes contain employee separators (format: --- email (timestamp) ---)
  // Updated regex to be more flexible with whitespace and handle newlines
  const separatorPattern = /---\s*([^(\n]+?)\s*\(([^)]+)\)\s*---/g;
  const matches = [...notes.matchAll(separatorPattern)];
  
  // If no separators, treat entire notes as admin notes (legacy format)
  if (matches.length === 0) {
    const adminNotes = escapeHtml(notes.trim()).replace(/\n/g, '<br>');
    return `<div class="mb-2 border-start border-3 border-primary ps-3"><strong class="text-primary">Admin:</strong><br><span class="text-dark">${adminNotes}</span></div>`;
  }
  
  let html = '';
  let lastIndex = 0;
  
  // Process each match
  matches.forEach((match, index) => {
    const matchStart = match.index;
    const email = match[1]?.trim() || 'Unknown';
    const timestamp = match[2]?.trim() || '';
    
    // Get text before this separator (could be admin notes or previous employee notes)
    const textBefore = notes.substring(lastIndex, matchStart).trim();
    if (textBefore) {
      if (index === 0) {
        // First text is admin notes (legacy format without separator)
        const adminNotes = escapeHtml(textBefore).replace(/\n/g, '<br>');
        html += `<div class="mb-2 border-start border-3 border-primary ps-3"><strong class="text-primary">Admin:</strong><br><span class="text-dark">${adminNotes}</span></div>`;
      }
    }
    
    // Get text after this separator (until next separator or end)
    const nextMatch = matches[index + 1];
    const textAfter = nextMatch 
      ? notes.substring(match.index + match[0].length, nextMatch.index).trim()
      : notes.substring(match.index + match[0].length).trim();
    
    if (textAfter) {
      const messageText = escapeHtml(textAfter).replace(/\n/g, '<br>');
      // Check if this is an admin message (email starts with "Admin")
      const isAdminMessage = email.trim().toLowerCase().startsWith('admin');
      let displayName = email.trim();
      let borderColor = 'info';
      let textColor = 'primary';
      
      if (isAdminMessage) {
        // Admin messages always show as just "Admin:" in blue
        displayName = 'Admin:';
        borderColor = 'primary';
        textColor = 'primary';
      } else {
        // Regular employee/manager message
        displayName = email;
        borderColor = 'info';
        textColor = 'info';
      }
      
      html += `<div class="mb-2 border-start border-3 border-${borderColor} ps-3"><strong class="text-${textColor}">${escapeHtml(displayName)}</strong> <small class="text-muted">(${escapeHtml(timestamp)})</small><br><span class="text-dark">${messageText}</span></div>`;
    }
    
    lastIndex = match.index + match[0].length;
  });
  
  return html || '<small class="text-muted">No notes yet</small>';
}

// Make applyTaskFilter globally accessible
window.applyTaskFilter = applyTaskFilter;

// Make applyTaskFilter globally accessible
window.applyTaskFilter = applyTaskFilter;


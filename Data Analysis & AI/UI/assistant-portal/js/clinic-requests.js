/* ================================
   Clinic Requests JavaScript (Assistant Portal)
================================ */

document.addEventListener("DOMContentLoaded", () => {
  displayAssistantId();
  renderPendingInvitations();
  renderConnectedClinics();
});

// Display assistant ID
function displayAssistantId() {
  const assistant = getData("assistant") || {};
  const idDisplay = document.getElementById("assistantIdDisplay");
  if (idDisplay && assistant.id) {
    idDisplay.textContent = assistant.id;
  }
}

// Copy assistant ID to clipboard
function copyAssistantId() {
  const assistant = getData("assistant") || {};
  const id = assistant.id || "asst_001";

  navigator.clipboard
    .writeText(id)
    .then(() => {
      showToast("Assistant ID copied to clipboard!", "success");
    })
    .catch(() => {
      // Fallback for older browsers
      const textArea = document.createElement("textarea");
      textArea.value = id;
      document.body.appendChild(textArea);
      textArea.select();
      document.execCommand("copy");
      document.body.removeChild(textArea);
      showToast("Assistant ID copied to clipboard!", "success");
    });
}

// Render pending invitations
function renderPendingInvitations() {
  const container = document.getElementById("pendingInvitationsList");
  const countBadge = document.getElementById("pendingCount");
  const requests = getData("clinicRequests") || [];
  const pendingRequests = requests.filter((r) => r.status === "pending");

  // Update count badge
  countBadge.textContent = `${pendingRequests.length} pending`;

  if (pendingRequests.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted);">
        <i class="fas fa-envelope-open" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No pending invitations</p>
        <p style="font-size: 0.85rem; margin-top: 8px;">When a doctor invites you, it will appear here</p>
      </div>
    `;
    return;
  }

  container.innerHTML = pendingRequests
    .map(
      (request) => `
    <div class="clinic-request-card" data-id="${request.id}">
      <img src="${request.image}" alt="${request.doctorName}" class="doctor-avatar" />
      <div class="request-info">
        <h4>${request.doctorName}</h4>
        <p>${request.specialty}</p>
        <p style="font-size: 0.85rem; color: var(--text-muted); margin-top: 4px;">
          <i class="fas fa-hospital"></i> ${request.clinicName}
        </p>
        <p style="font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">
          Received ${formatRelativeTime(request.requestedAt)}
        </p>
      </div>
      <div class="request-actions">
        <button class="btn btn-primary" onclick="handleAcceptInvitation('${request.id}')">
          <i class="fas fa-check"></i> Accept
        </button>
        <button class="btn btn-outline danger" onclick="handleDeclineInvitation('${request.id}')">
          <i class="fas fa-times"></i> Decline
        </button>
      </div>
    </div>
  `
    )
    .join("");
}

// Render connected clinics
function renderConnectedClinics() {
  const container = document.getElementById("connectedClinicsList");
  const linkedDoctors = getData("linkedDoctors") || [];

  if (linkedDoctors.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted); grid-column: 1 / -1;">
        <i class="fas fa-hospital" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No connected clinics yet</p>
        <p style="font-size: 0.85rem; margin-top: 8px;">Accept an invitation to get started</p>
      </div>
    `;
    return;
  }

  container.innerHTML = linkedDoctors
    .map(
      (doctor) => `
    <div class="team-card">
      <img src="${doctor.image}" alt="${doctor.name}" class="avatar" />
      <h4>${doctor.name}</h4>
      <p class="role">${doctor.specialty}</p>
      <p style="font-size: 0.8rem; color: var(--text-muted); margin-bottom: 12px;">
        <i class="fas fa-hospital"></i> ${doctor.clinicName}
      </p>
      <span class="status-dot ${doctor.status === "active" ? "online" : "offline"}">${doctor.status === "active" ? "Connected" : "Inactive"}</span>
      <div style="margin-top: 16px; display: flex; gap: 8px; justify-content: center; flex-wrap: wrap;">
        <a href="appointments.html?doctor=${doctor.id}" class="btn btn-sm btn-primary">
          <i class="fas fa-calendar"></i> Appointments
        </a>
        <a href="availability.html?doctor=${doctor.id}" class="btn btn-sm btn-outline">
          <i class="fas fa-clock"></i> Availability
        </a>
      </div>
    </div>
  `
    )
    .join("");
}

// Handle accept invitation
function handleAcceptInvitation(requestId) {
  if (approveClinicRequest(requestId)) {
    renderPendingInvitations();
    renderConnectedClinics();

    // Also update doctor portal's team (for demo cross-portal)
    updateDoctorTeam();
  }
}

// Handle decline invitation
function handleDeclineInvitation(requestId) {
  if (confirm("Are you sure you want to decline this invitation?")) {
    if (rejectClinicRequest(requestId)) {
      renderPendingInvitations();
    }
  }
}

// Update doctor portal team (for demo cross-portal functionality)
function updateDoctorTeam() {
  const assistant = getData("assistant") || {};
  const doctorClinicTeam = JSON.parse(
    localStorage.getItem("doctorPortal_clinicTeam") || "[]"
  );

  // Add assistant to doctor's team if not already there
  if (!doctorClinicTeam.some((m) => m.id === assistant.id)) {
    doctorClinicTeam.push({
      id: assistant.id || "asst_001",
      name: assistant.name || "Layla Mohamed",
      role: assistant.role || "Clinic Assistant",
      image:
        assistant.image ||
        "https://img.freepik.com/free-photo/female-doctor-hospital-with-stethoscope_23-2148827776.jpg?w=740",
      email: assistant.email || "layla@mamacare.com",
      status: "online",
    });
    localStorage.setItem(
      "doctorPortal_clinicTeam",
      JSON.stringify(doctorClinicTeam)
    );
  }
}

// Format relative time
function formatRelativeTime(dateString) {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now - date;
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return "just now";
  if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? "s" : ""} ago`;
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
  if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? "s" : ""} ago`;
  return formatDate(dateString);
}

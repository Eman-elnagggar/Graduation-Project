/* ================================
   Clinic Team Management JavaScript
================================ */

document.addEventListener("DOMContentLoaded", () => {
  renderTeamMembers();
  renderPendingInvitations();
  setupAddAssistantForm();
});

// Demo pending invitations sent by doctor
let pendingDoctorInvitations = JSON.parse(
  localStorage.getItem("doctorPortal_sentInvitations") || "[]"
);

// Setup add assistant form
function setupAddAssistantForm() {
  const form = document.getElementById("addAssistantForm");

  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const assistantId = document.getElementById("assistantId").value.trim();

    if (!assistantId) {
      showToast("Please enter an assistant ID", "error");
      return;
    }

    // Check if already invited
    if (
      pendingDoctorInvitations.some((inv) => inv.assistantId === assistantId)
    ) {
      showToast(
        "You have already sent an invitation to this assistant",
        "error"
      );
      return;
    }

    // Check if already a team member
    const clinicTeam = getData("clinicTeam") || [];
    if (clinicTeam.some((member) => member.id === assistantId)) {
      showToast("This assistant is already on your team", "error");
      return;
    }

    // Create invitation
    const invitation = {
      id: "inv_" + Date.now(),
      assistantId: assistantId,
      assistantName: "Assistant " + assistantId.replace("asst_", "#"),
      assistantEmail: assistantId + "@mamacare.com",
      status: "pending",
      sentAt: new Date().toISOString(),
    };

    // For demo: if asst_002, add realistic data
    if (assistantId === "asst_002") {
      invitation.assistantName = "Sara Ali";
      invitation.assistantEmail = "sara.ali@mamacare.com";
      invitation.assistantImage =
        "https://img.freepik.com/free-photo/portrait-smiling-young-woman-doctor-healthcare-medical-worker-pointing-fingers-left-showing-clini_1258-88433.jpg?w=740";
    }

    pendingDoctorInvitations.push(invitation);
    localStorage.setItem(
      "doctorPortal_sentInvitations",
      JSON.stringify(pendingDoctorInvitations)
    );

    // Also add to assistant portal's clinic requests (for demo cross-portal functionality)
    addToAssistantClinicRequests(invitation);

    showToast(`Invitation sent to ${invitation.assistantName}`, "success");
    document.getElementById("assistantId").value = "";
    renderPendingInvitations();
  });
}

// Add invitation to assistant portal (demo cross-portal)
function addToAssistantClinicRequests(invitation) {
  const doctor = getData("doctor") || {};
  const assistantRequests = JSON.parse(
    localStorage.getItem("assistantPortal_clinicRequests") || "[]"
  );

  // Check if request already exists
  if (
    !assistantRequests.some(
      (r) => r.doctorId === doctor.id && r.status === "pending"
    )
  ) {
    assistantRequests.push({
      id: "req_" + Date.now(),
      doctorId: doctor.id || "doc_001",
      doctorName: doctor.name || "Dr. Ahmed Hassan",
      specialty: doctor.specialty || "Gynecologist & Obstetrician",
      image:
        doctor.image ||
        "https://img.freepik.com/free-photo/doctor-with-his-arms-crossed-white-background_1368-5790.jpg?w=740",
      clinicName: doctor.clinicName || "MamaCare Women's Clinic",
      requestedAt: new Date().toISOString(),
      status: "pending",
    });
    localStorage.setItem(
      "assistantPortal_clinicRequests",
      JSON.stringify(assistantRequests)
    );
  }
}

// Render team members
function renderTeamMembers() {
  const container = document.getElementById("teamMembersList");
  const clinicTeam = getData("clinicTeam") || [];

  if (clinicTeam.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted); grid-column: 1 / -1;">
        <i class="fas fa-users" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No team members yet</p>
        <p style="font-size: 0.85rem; margin-top: 8px;">Add an assistant using the form above</p>
      </div>
    `;
    return;
  }

  container.innerHTML = clinicTeam
    .map(
      (member) => `
    <div class="team-card">
      <img src="${member.image}" alt="${member.name}" class="avatar" />
      <h4>${member.name}</h4>
      <p class="role">${member.role}</p>
      <p style="font-size: 0.8rem; color: var(--text-muted); margin-bottom: 12px;">${member.email}</p>
      <span class="status-dot ${member.status === "online" ? "online" : "offline"}">${member.status === "online" ? "Online" : "Offline"}</span>
      <div style="margin-top: 16px; display: flex; gap: 8px; justify-content: center;">
        <button class="btn btn-sm btn-outline" onclick="viewAssistantDetails('${member.id}')">
          <i class="fas fa-eye"></i> View
        </button>
        <button class="btn btn-sm btn-outline danger" onclick="removeTeamMember('${member.id}')">
          <i class="fas fa-user-minus"></i> Remove
        </button>
      </div>
    </div>
  `
    )
    .join("");
}

// Render pending invitations
function renderPendingInvitations() {
  const container = document.getElementById("pendingInvitationsList");
  const section = document.getElementById("pendingInvitationsSection");
  const pendingInvites = pendingDoctorInvitations.filter(
    (inv) => inv.status === "pending"
  );

  if (pendingInvites.length === 0) {
    section.style.display = "none";
    return;
  }

  section.style.display = "block";
  container.innerHTML = pendingInvites
    .map(
      (invitation) => `
    <div class="request-card" data-id="${invitation.id}">
      ${
        invitation.assistantImage
          ? `<img src="${invitation.assistantImage}" alt="${invitation.assistantName}" style="width: 50px; height: 50px; border-radius: 50%; object-fit: cover;" />`
          : `<div style="width: 50px; height: 50px; border-radius: 50%; background: var(--bg-primary); display: flex; align-items: center; justify-content: center;"><i class="fas fa-user" style="color: var(--text-muted);"></i></div>`
      }
      <div class="request-info">
        <h4>${invitation.assistantName}</h4>
        <p>${invitation.assistantEmail}</p>
        <p style="font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">
          Sent ${formatRelativeTime(invitation.sentAt)}
        </p>
      </div>
      <div class="request-actions">
        <span class="status-badge pending">Pending</span>
        <button class="btn btn-sm btn-outline danger" onclick="cancelInvitation('${invitation.id}')">
          <i class="fas fa-times"></i> Cancel
        </button>
      </div>
    </div>
  `
    )
    .join("");
}

// Cancel invitation
function cancelInvitation(invitationId) {
  if (confirm("Are you sure you want to cancel this invitation?")) {
    const index = pendingDoctorInvitations.findIndex(
      (inv) => inv.id === invitationId
    );
    if (index !== -1) {
      pendingDoctorInvitations.splice(index, 1);
      localStorage.setItem(
        "doctorPortal_sentInvitations",
        JSON.stringify(pendingDoctorInvitations)
      );
      showToast("Invitation cancelled", "info");
      renderPendingInvitations();
    }
  }
}

// Remove team member
function removeTeamMember(memberId) {
  if (
    confirm("Are you sure you want to remove this assistant from your team?")
  ) {
    const clinicTeam = getData("clinicTeam") || [];
    const index = clinicTeam.findIndex((m) => m.id === memberId);

    if (index !== -1) {
      const member = clinicTeam[index];
      clinicTeam.splice(index, 1);
      saveData("clinicTeam", clinicTeam);
      showToast(`${member.name} has been removed from your team`, "info");
      renderTeamMembers();
    }
  }
}

// View assistant details (placeholder)
function viewAssistantDetails(assistantId) {
  showToast("Assistant details view coming soon", "info");
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

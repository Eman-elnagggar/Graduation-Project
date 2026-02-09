// Patients List JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initDemoData();
  initSidebar();
  initNotifications();
  initPatientFilters();
  initViewToggle();
  initSearch();
  renderPatients();
  updateCounts();
});

let currentFilter = "all";
let currentView = "grid";
let currentTrimester = "";
let currentSort = "name";
let searchTerm = "";

// Initialize patient filters
function initPatientFilters() {
  const filterTabs = document.querySelectorAll(".filter-tab");
  filterTabs.forEach((tab) => {
    tab.addEventListener("click", function () {
      filterTabs.forEach((t) => t.classList.remove("active"));
      this.classList.add("active");
      currentFilter = this.dataset.filter;
      renderPatients();
    });
  });

  // Trimester filter
  document
    .getElementById("trimesterFilter")
    .addEventListener("change", function () {
      currentTrimester = this.value;
      renderPatients();
    });

  // Sort by
  document.getElementById("sortBy").addEventListener("change", function () {
    currentSort = this.value;
    renderPatients();
  });
}

// Initialize view toggle (grid/list)
function initViewToggle() {
  const viewBtns = document.querySelectorAll(".view-btn");
  viewBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      viewBtns.forEach((b) => b.classList.remove("active"));
      this.classList.add("active");
      currentView = this.dataset.view;
      toggleView();
    });
  });
}

// Toggle between grid and list view
function toggleView() {
  const grid = document.getElementById("patientsGrid");
  const table = document.getElementById("patientsTable");

  if (currentView === "grid") {
    grid.style.display = "grid";
    table.style.display = "none";
  } else {
    grid.style.display = "none";
    table.style.display = "block";
  }
  renderPatients();
}

// Initialize search
function initSearch() {
  const searchInput = document.getElementById("searchInput");
  searchInput.addEventListener("input", function () {
    searchTerm = this.value.toLowerCase();
    renderPatients();
  });
}

// Filter patients based on current filters
function getFilteredPatients() {
  let patients = getData("patients") || [];

  // Apply search
  if (searchTerm) {
    patients = patients.filter(
      (p) =>
        p.name.toLowerCase().includes(searchTerm) ||
        p.phone?.toLowerCase().includes(searchTerm)
    );
  }

  // Apply tab filter
  const today = new Date();
  const weekFromNow = new Date(today.getTime() + 7 * 24 * 60 * 60 * 1000);

  switch (currentFilter) {
    case "this-week":
      patients = patients.filter((p) => {
        if (!p.nextAppointment) return false;
        const apptDate = new Date(p.nextAppointment);
        return apptDate >= today && apptDate <= weekFromNow;
      });
      break;
    case "high-risk":
      patients = patients.filter((p) => p.riskLevel === "high");
      break;
    case "needs-attention":
      patients = patients.filter((p) => p.needsAttention);
      break;
  }

  // Apply trimester filter
  if (currentTrimester) {
    patients = patients.filter((p) => {
      const weeks = p.gestationalAge || 0;
      if (currentTrimester === "1") return weeks <= 12;
      if (currentTrimester === "2") return weeks > 12 && weeks <= 28;
      if (currentTrimester === "3") return weeks > 28;
      return true;
    });
  }

  // Apply sorting
  patients.sort((a, b) => {
    switch (currentSort) {
      case "name":
        return a.name.localeCompare(b.name);
      case "nextAppointment":
        if (!a.nextAppointment) return 1;
        if (!b.nextAppointment) return -1;
        return new Date(a.nextAppointment) - new Date(b.nextAppointment);
      case "gestationalAge":
        return (b.gestationalAge || 0) - (a.gestationalAge || 0);
      case "riskLevel":
        const riskOrder = { high: 0, medium: 1, low: 2 };
        return (riskOrder[a.riskLevel] || 2) - (riskOrder[b.riskLevel] || 2);
      default:
        return 0;
    }
  });

  return patients;
}

// Update filter counts
function updateCounts() {
  const patients = getData("patients") || [];
  const today = new Date();
  const weekFromNow = new Date(today.getTime() + 7 * 24 * 60 * 60 * 1000);

  document.getElementById("countAll").textContent = patients.length;

  document.getElementById("countWeek").textContent = patients.filter((p) => {
    if (!p.nextAppointment) return false;
    const apptDate = new Date(p.nextAppointment);
    return apptDate >= today && apptDate <= weekFromNow;
  }).length;

  document.getElementById("countHighRisk").textContent = patients.filter(
    (p) => p.riskLevel === "high"
  ).length;

  document.getElementById("countAttention").textContent = patients.filter(
    (p) => p.needsAttention
  ).length;
}

// Render patients list
function renderPatients() {
  const patients = getFilteredPatients();
  const emptyState = document.getElementById("emptyState");

  if (patients.length === 0) {
    document.getElementById("patientsGrid").style.display = "none";
    document.getElementById("patientsTable").style.display = "none";
    emptyState.style.display = "flex";
    return;
  }

  emptyState.style.display = "none";

  if (currentView === "grid") {
    renderPatientsGrid(patients);
  } else {
    renderPatientsTable(patients);
  }
}

// Render grid view
function renderPatientsGrid(patients) {
  const grid = document.getElementById("patientsGrid");
  grid.style.display = "grid";
  document.getElementById("patientsTable").style.display = "none";

  grid.innerHTML = patients
    .map((patient) => {
      const trimester = getTrimester(patient.gestationalAge);
      const avatarUrl =
        patient.image ||
        patient.avatar ||
        `https://ui-avatars.com/api/?name=${encodeURIComponent(patient.name)}&background=e91e8c&color=fff`;
      const nextAppt = patient.nextAppointment
        ? formatDate(patient.nextAppointment)
        : "Not scheduled";

      return `
            <div class="patient-card" onclick="viewPatient('${patient.id}')">
                <div class="patient-card-header">
                    <div class="patient-avatar">
                        <img src="${avatarUrl}" alt="${patient.name}" onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(patient.name)}&background=e91e8c&color=fff'">
                        <span class="risk-dot risk-${patient.riskLevel}"></span>
                    </div>
                    <div class="patient-card-info">
                        <h4>${patient.name}</h4>
                        <p>${patient.gestationalAge} weeks pregnant</p>
                    </div>
                    ${patient.needsAttention ? '<span class="attention-badge"><i class="fas fa-exclamation"></i></span>' : ""}
                </div>
                <div class="patient-card-body">
                    <div class="patient-detail">
                        <span class="label">Trimester</span>
                        <span class="value">${trimester}</span>
                    </div>
                    <div class="patient-detail">
                        <span class="label">Risk Level</span>
                        <span class="value risk-tag ${patient.riskLevel}">${capitalize(patient.riskLevel)}</span>
                    </div>
                    <div class="patient-detail">
                        <span class="label">Next Visit</span>
                        <span class="value">${nextAppt}</span>
                    </div>
                    <div class="patient-detail">
                        <span class="label">Last Contact</span>
                        <span class="value">${patient.lastContact ? formatDate(patient.lastContact) : "N/A"}</span>
                    </div>
                </div>
                <div class="patient-card-actions">
                    <button class="action-btn" onclick="event.stopPropagation(); openChat('${patient.id}')" title="Message">
                        <i class="fas fa-comment-medical"></i>
                    </button>
                    <button class="action-btn" onclick="event.stopPropagation(); viewPatient('${patient.id}')" title="View Details">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="action-btn" onclick="event.stopPropagation(); scheduleVisit('${patient.id}')" title="Schedule">
                        <i class="fas fa-calendar-plus"></i>
                    </button>
                </div>
            </div>
        `;
    })
    .join("");
}

// Render table view
function renderPatientsTable(patients) {
  const table = document.getElementById("patientsTable");
  const tbody = document.getElementById("patientsTableBody");
  document.getElementById("patientsGrid").style.display = "none";
  table.style.display = "block";

  tbody.innerHTML = patients
    .map((patient) => {
      const avatarUrl =
        patient.image ||
        patient.avatar ||
        `https://ui-avatars.com/api/?name=${encodeURIComponent(patient.name)}&background=e91e8c&color=fff`;
      const nextAppt = patient.nextAppointment
        ? formatDate(patient.nextAppointment)
        : "Not scheduled";

      return `
            <tr onclick="viewPatient('${patient.id}')" class="clickable-row">
                <td>
                    <div class="patient-cell">
                        <img src="${avatarUrl}" alt="${patient.name}" class="table-avatar" onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(patient.name)}&background=e91e8c&color=fff'">
                        <div>
                            <span class="patient-name">${patient.name}</span>
                            ${patient.needsAttention ? '<span class="attention-badge-sm"><i class="fas fa-exclamation"></i></span>' : ""}
                            <span class="patient-phone">${patient.phone || "No phone"}</span>
                        </div>
                    </div>
                </td>
                <td>${patient.gestationalAge} weeks</td>
                <td><span class="risk-tag ${patient.riskLevel}">${capitalize(patient.riskLevel)}</span></td>
                <td>${nextAppt}</td>
                <td>${patient.lastContact ? formatDate(patient.lastContact) : "N/A"}</td>
                <td>
                    <div class="table-actions">
                        <button class="action-btn" onclick="event.stopPropagation(); openChat('${patient.id}')" title="Message">
                            <i class="fas fa-comment-medical"></i>
                        </button>
                        <button class="action-btn" onclick="event.stopPropagation(); viewPatient('${patient.id}')" title="View">
                            <i class="fas fa-eye"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    })
    .join("");
}

// Helper functions
function getTrimester(weeks) {
  if (weeks <= 12) return "1st";
  if (weeks <= 28) return "2nd";
  return "3rd";
}

function capitalize(str) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

function formatDate(dateStr) {
  const date = new Date(dateStr);
  const today = new Date();
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);

  if (date.toDateString() === today.toDateString()) {
    return "Today";
  }
  if (date.toDateString() === tomorrow.toDateString()) {
    return "Tomorrow";
  }

  return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

// Navigation functions
function viewPatient(patientId) {
  window.location.href = `patient-details.html?id=${patientId}`;
}

function openChat(patientId) {
  window.location.href = `chat.html?patient=${patientId}`;
}

function scheduleVisit(patientId) {
  // In production, this would open a scheduling modal
  showToast("Schedule feature - would open scheduling modal", "info");
}

// Export functionality
document.getElementById("exportBtn")?.addEventListener("click", function () {
  showToast("Export feature coming soon", "info");
});

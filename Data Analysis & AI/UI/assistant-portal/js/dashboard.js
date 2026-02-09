/* ================================
   Assistant Dashboard JavaScript
================================ */

document.addEventListener("DOMContentLoaded", () => {
  ensureDashboardTestData();
  updateStats();
  renderTodaySchedule();
  renderLinkedDoctors();
  renderClinicInvitations();
});

function ensureDashboardTestData() {
  const today = new Date().toISOString().split("T")[0];

  const linkedDoctors = getData("linkedDoctors") || [];
  if (linkedDoctors.length === 0) {
    saveData("linkedDoctors", [
      {
        id: "doc_demo_001",
        name: "Dr. Ahmed Hassan",
        specialty: "Gynecologist & Obstetrician",
        image:
          "https://img.freepik.com/free-photo/doctor-with-his-arms-crossed-white-background_1368-5790.jpg?w=740",
        clinicName: "MamaCare Women's Clinic",
        status: "active",
      },
    ]);
  }

  const clinicRequests = getData("clinicRequests") || [];
  const hasPendingInvite = clinicRequests.some((r) => r.status === "pending");
  if (!hasPendingInvite) {
    clinicRequests.push({
      id: `req_demo_${Date.now()}`,
      doctorId: "doc_demo_002",
      doctorName: "Dr. Fatima Nour",
      specialty: "Obstetrician",
      image:
        "https://img.freepik.com/free-photo/woman-doctor-wearing-lab-coat-with-stethoscope-isolated_1303-29791.jpg?w=740",
      clinicName: "Cairo Medical Center",
      requestedAt: new Date().toISOString(),
      status: "pending",
    });
    saveData("clinicRequests", clinicRequests);
  }

  const confirmedAppointments = getData("confirmedAppointments") || [];
  const hasTodayAppointments = confirmedAppointments.some(
    (apt) => apt.date === today && apt.status !== "cancelled",
  );

  if (!hasTodayAppointments) {
    const demoTodayAppointments = [
      {
        id: `apt_demo_${Date.now()}_1`,
        patientId: "pat_demo_001",
        patientName: "Sarah Ahmed",
        patientImage:
          "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
        patientPhone: "+20 111 222 3333",
        doctorId: "doc_demo_001",
        doctorName: "Dr. Ahmed Hassan",
        date: today,
        time: "09:00",
        type: "Regular Checkup",
        status: "confirmed",
      },
      {
        id: `apt_demo_${Date.now()}_2`,
        patientId: "pat_demo_002",
        patientName: "Mona Ibrahim",
        patientImage:
          "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
        patientPhone: "+20 111 444 5555",
        doctorId: "doc_demo_001",
        doctorName: "Dr. Ahmed Hassan",
        date: today,
        time: "10:30",
        type: "Ultrasound",
        status: "confirmed",
      },
      {
        id: `apt_demo_${Date.now()}_3`,
        patientId: "pat_demo_003",
        patientName: "Fatima Ali",
        patientImage:
          "https://img.freepik.com/free-photo/portrait-beautiful-young-woman-with-curly-hair-brown-hat_1142-42780.jpg?w=740",
        patientPhone: "+20 111 333 4444",
        doctorId: "doc_demo_001",
        doctorName: "Dr. Ahmed Hassan",
        date: today,
        time: "14:00",
        type: "Follow-up",
        status: "confirmed",
      },
    ];

    saveData("confirmedAppointments", [
      ...confirmedAppointments,
      ...demoTodayAppointments,
    ]);
  }
}

// Update dashboard stats
function updateStats() {
  const confirmedAppointments = getData("confirmedAppointments") || [];
  const linkedDoctors = getData("linkedDoctors") || [];
  const clinicRequests = getData("clinicRequests") || [];
  const today = new Date().toISOString().split("T")[0];

  // Today's confirmed appointments
  const todayConfirmed = confirmedAppointments.filter(
    (apt) => apt.date === today && apt.status === "confirmed",
  );
  document.getElementById("confirmedCount").textContent = todayConfirmed.length;
  document.getElementById("todayAppointments").textContent =
    todayConfirmed.length;

  // Linked doctors count
  document.getElementById("linkedDoctors").textContent = linkedDoctors.length;

  // Clinic invitations count
  const pendingInvites = clinicRequests.filter(
    (r) => r.status === "pending",
  ).length;
  document.getElementById("clinicInvites").textContent = pendingInvites;

  // Today's date
  const dateOptions = {
    weekday: "long",
    year: "numeric",
    month: "long",
    day: "numeric",
  };
  document.getElementById("todayDate").textContent =
    new Date().toLocaleDateString("en-US", dateOptions);

  // Hide clinic invitations section if no invitations
  const clinicInvitationsSection = document.getElementById(
    "clinicInvitationsSection",
  );
  if (pendingInvites === 0) {
    clinicInvitationsSection.style.display = "none";
  }
}

// Render today's schedule
function renderTodaySchedule() {
  const container = document.getElementById("todaySchedule");
  const appointments = getData("confirmedAppointments") || [];
  const today = new Date().toISOString().split("T")[0];

  const todayAppointments = appointments
    .filter((apt) => apt.date === today && apt.status !== "cancelled")
    .sort((a, b) => a.time.localeCompare(b.time));

  if (todayAppointments.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted);">
        <i class="fas fa-calendar-check" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No appointments scheduled for today</p>
      </div>
    `;
    return;
  }

  container.innerHTML = todayAppointments
    .map((apt) => {
      const timeFormatted = formatTime(apt.time);
      return `
      <div class="request-card" data-id="${apt.id}">
        <img src="${apt.patientImage}" alt="${apt.patientName}" class="patient-avatar" />
        <div class="request-info">
          <h4>${apt.patientName}</h4>
          <p class="meta">${apt.doctorName}</p>
          <div class="request-details">
            <span><i class="fas fa-clock"></i> ${timeFormatted.time} ${timeFormatted.period}</span>
            <span><i class="fas fa-stethoscope"></i> ${apt.type}</span>
            <span><i class="fas fa-phone"></i> ${apt.patientPhone}</span>
          </div>
        </div>
        <div class="request-actions">
          <span class="appointment-status ${apt.status}">${apt.status}</span>
          <button class="btn btn-sm btn-outline" onclick="openModifyModal('${apt.id}')">
            <i class="fas fa-edit"></i> Modify
          </button>
        </div>
      </div>
    `;
    })
    .join("");
}

// Render linked doctors
function renderLinkedDoctors() {
  const container = document.getElementById("linkedDoctorsList");
  const doctors = getData("linkedDoctors") || [];

  if (doctors.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted);">
        <i class="fas fa-user-md" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No linked doctors yet</p>
        <p style="font-size: 0.85rem; margin-top: 8px;">Wait for doctor invitations or contact clinic administration</p>
      </div>
    `;
    return;
  }

  container.innerHTML = doctors
    .map(
      (doctor) => `
    <div class="team-card">
      <img src="${doctor.image}" alt="${doctor.name}" class="avatar" />
      <h4>${doctor.name}</h4>
      <p class="role">${doctor.specialty}</p>
      <p style="font-size: 0.8rem; color: var(--text-muted); margin-bottom: 12px;">${doctor.clinicName}</p>
      <span class="status-dot ${doctor.status === "active" ? "online" : "offline"}">${doctor.status === "active" ? "Active" : "Inactive"}</span>
    </div>
  `,
    )
    .join("");
}

// Render clinic invitations
function renderClinicInvitations() {
  const container = document.getElementById("clinicInvitationsList");
  const requests = getData("clinicRequests") || [];
  const pendingRequests = requests.filter((r) => r.status === "pending");

  if (pendingRequests.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted);">
        <i class="fas fa-envelope-open" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No pending invitations</p>
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
        <p>${request.specialty} • ${request.clinicName}</p>
      </div>
      <div class="request-actions">
        <button class="btn btn-sm btn-primary" onclick="handleApproveClinic('${request.id}')">
          <i class="fas fa-check"></i> Accept
        </button>
        <button class="btn btn-sm btn-outline danger" onclick="handleRejectClinic('${request.id}')">
          <i class="fas fa-times"></i> Decline
        </button>
      </div>
    </div>
  `,
    )
    .join("");
}

// Handle approve clinic invitation
function handleApproveClinic(requestId) {
  if (approveClinicRequest(requestId)) {
    renderClinicInvitations();
    renderLinkedDoctors();
    updateStats();
  }
}

// Handle reject clinic invitation
function handleRejectClinic(requestId) {
  if (confirm("Are you sure you want to decline this clinic invitation?")) {
    if (rejectClinicRequest(requestId)) {
      renderClinicInvitations();
      updateStats();
    }
  }
}

// Open modify appointment modal (placeholder)
function openModifyModal(appointmentId) {
  // For demo, redirect to appointments page with the appointment selected
  window.location.href = `appointments.html?modify=${appointmentId}`;
}

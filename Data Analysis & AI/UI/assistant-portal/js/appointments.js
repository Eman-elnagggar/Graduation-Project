/* ================================
   Appointments Management JavaScript (Assistant Portal)
================================ */

let currentTab = "confirmed";
let selectedDoctor = null;
let modifyingAppointmentId = null;

document.addEventListener("DOMContentLoaded", () => {
  renderDoctorSelector();
  updateTabCounts();
  renderCurrentTab();
  checkUrlParams();
  setupDateChangeListener();
});

// Check URL params for modify action
function checkUrlParams() {
  const urlParams = new URLSearchParams(window.location.search);
  const modifyId = urlParams.get("modify");
  if (modifyId) {
    openModifyModal(modifyId);
  }
}

// Render doctor selector
function renderDoctorSelector() {
  const container = document.getElementById("doctorSelector");
  const section = document.getElementById("doctorSelectorSection");
  const linkedDoctors = getData("linkedDoctors") || [];

  if (linkedDoctors.length <= 1) {
    section.style.display = "none";
    if (linkedDoctors.length === 1) {
      selectedDoctor = linkedDoctors[0].id;
    }
    return;
  }

  // Default to first doctor
  if (!selectedDoctor && linkedDoctors.length > 0) {
    selectedDoctor = linkedDoctors[0].id;
  }

  container.innerHTML = linkedDoctors
    .map(
      (doctor) => `
    <div class="doctor-option ${doctor.id === selectedDoctor ? "selected" : ""}" onclick="selectDoctor('${doctor.id}')">
      <img src="${doctor.image}" alt="${doctor.name}" />
      <div class="doctor-info">
        <h4>${doctor.name}</h4>
        <p>${doctor.specialty}</p>
      </div>
    </div>
  `
    )
    .join("");
}

// Select doctor
function selectDoctor(doctorId) {
  selectedDoctor = doctorId;
  renderDoctorSelector();
  renderCurrentTab();
  updateTabCounts();
}

// Switch tab
function switchTab(tab) {
  currentTab = tab;

  // Update tab buttons
  document.querySelectorAll(".tab-btn").forEach((btn) => {
    btn.classList.toggle("active", btn.dataset.tab === tab);
  });

  // Update tab content
  document.querySelectorAll(".tab-content").forEach((content) => {
    content.style.display = "none";
  });
  document.getElementById(`${tab}-tab`).style.display = "block";

  renderCurrentTab();
}

// Render current tab content
function renderCurrentTab() {
  switch (currentTab) {
    case "confirmed":
      renderConfirmedAppointments();
      break;
    case "modified":
      renderModifiedAppointments();
      break;
    case "cancelled":
      renderCancelledAppointments();
      break;
  }
}

// Update tab counts
function updateTabCounts() {
  const appointments = getData("confirmedAppointments") || [];
  const confirmedCount = appointments.filter(
    (a) => a.status === "confirmed"
  ).length;
  const modifiedCount = appointments.filter(
    (a) => a.status === "modified"
  ).length;
  const cancelledCount = appointments.filter(
    (a) => a.status === "cancelled"
  ).length;

  document.getElementById("confirmedTabCount").textContent = confirmedCount;
  document.getElementById("modifiedTabCount").textContent = modifiedCount;
  document.getElementById("cancelledTabCount").textContent = cancelledCount;
}

// Render confirmed appointments
function renderConfirmedAppointments() {
  const container = document.getElementById("confirmedAppointmentsList");
  const appointments = getData("confirmedAppointments") || [];
  const confirmedAppointments = appointments
    .filter((a) => a.status === "confirmed")
    .sort(
      (a, b) =>
        new Date(a.date + " " + a.time) - new Date(b.date + " " + b.time)
    );

  if (confirmedAppointments.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 60px; color: var(--text-muted);">
        <i class="fas fa-calendar-check" style="font-size: 4rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p style="font-size: 1.1rem;">No confirmed appointments</p>
      </div>
    `;
    return;
  }

  container.innerHTML = confirmedAppointments
    .map((apt) => {
      const timeFormatted = formatTime(apt.time);
      const isPast = new Date(apt.date) < new Date(new Date().toDateString());

      return `
      <div class="request-card ${isPast ? "past" : ""}" data-id="${apt.id}">
        <img src="${apt.patientImage}" alt="${apt.patientName}" class="patient-avatar" />
        <div class="request-info">
          <h4>${apt.patientName}</h4>
          <p class="meta">${apt.doctorName}</p>
          <div class="request-details">
            <span><i class="fas fa-calendar"></i> ${formatDate(apt.date)}</span>
            <span><i class="fas fa-clock"></i> ${timeFormatted.time} ${timeFormatted.period}</span>
            <span><i class="fas fa-stethoscope"></i> ${apt.type}</span>
            <span><i class="fas fa-phone"></i> ${apt.patientPhone}</span>
          </div>
        </div>
        <div class="request-actions">
          <div class="appointment-actions-inline">
            <span class="appointment-status confirmed">Confirmed</span>
            <button class="btn btn-sm btn-outline" onclick="openModifyModal('${apt.id}')">
              <i class="fas fa-edit"></i> Modify
            </button>
            <button class="btn btn-sm btn-danger" onclick="confirmCancelAppointment('${apt.id}')">
              <i class="fas fa-times"></i> Cancel
            </button>
          </div>
        </div>
      </div>
    `;
    })
    .join("");
}

// Render modified appointments
function renderModifiedAppointments() {
  const container = document.getElementById("modifiedAppointmentsList");
  const appointments = getData("confirmedAppointments") || [];
  const modifiedAppointments = appointments.filter(
    (a) => a.status === "modified"
  );

  if (modifiedAppointments.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 60px; color: var(--text-muted);">
        <i class="fas fa-edit" style="font-size: 4rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p style="font-size: 1.1rem;">No modified appointments</p>
      </div>
    `;
    return;
  }

  container.innerHTML = modifiedAppointments
    .map((apt) => {
      const timeFormatted = formatTime(apt.time);
      return `
      <div class="request-card" data-id="${apt.id}">
        <img src="${apt.patientImage}" alt="${apt.patientName}" class="patient-avatar" />
        <div class="request-info">
          <h4>${apt.patientName}</h4>
          <p class="meta">${apt.doctorName}</p>
          <div class="request-details">
            <span><i class="fas fa-calendar"></i> ${formatDate(apt.date)}</span>
            <span><i class="fas fa-clock"></i> ${timeFormatted.time} ${timeFormatted.period}</span>
            <span><i class="fas fa-stethoscope"></i> ${apt.type}</span>
          </div>
          ${apt.modifiedReason ? `<p class="meta" style="margin-top: 8px;"><i class="fas fa-info-circle"></i> Reason: ${apt.modifiedReason}</p>` : ""}
        </div>
        <div class="request-actions">
          <span class="appointment-status modified">Modified</span>
          <button class="btn btn-sm btn-outline" onclick="openModifyModal('${apt.id}')">
            <i class="fas fa-edit"></i> Edit Again
          </button>
        </div>
      </div>
    `;
    })
    .join("");
}

// Render cancelled appointments
function renderCancelledAppointments() {
  const container = document.getElementById("cancelledAppointmentsList");
  const appointments = getData("confirmedAppointments") || [];
  const cancelledAppointments = appointments.filter(
    (a) => a.status === "cancelled"
  );

  if (cancelledAppointments.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 60px; color: var(--text-muted);">
        <i class="fas fa-times-circle" style="font-size: 4rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p style="font-size: 1.1rem;">No cancelled appointments</p>
      </div>
    `;
    return;
  }

  container.innerHTML = cancelledAppointments
    .map((apt) => {
      const timeFormatted = formatTime(apt.time);
      return `
      <div class="request-card" data-id="${apt.id}" style="opacity: 0.7;">
        <img src="${apt.patientImage}" alt="${apt.patientName}" class="patient-avatar" />
        <div class="request-info">
          <h4>${apt.patientName}</h4>
          <p class="meta">${apt.doctorName}</p>
          <div class="request-details">
            <span><i class="fas fa-calendar"></i> ${formatDate(apt.date)}</span>
            <span><i class="fas fa-clock"></i> ${timeFormatted.time} ${timeFormatted.period}</span>
            <span><i class="fas fa-stethoscope"></i> ${apt.type}</span>
          </div>
          ${apt.cancelReason ? `<p class="meta" style="margin-top: 8px;"><i class="fas fa-info-circle"></i> Reason: ${apt.cancelReason}</p>` : ""}
        </div>
        <div class="request-actions">
          <span class="appointment-status cancelled">Cancelled</span>
        </div>
      </div>
    `;
    })
    .join("");
}

// Open modify modal
function openModifyModal(appointmentId) {
  const appointments = getData("confirmedAppointments") || [];
  const appointment = appointments.find((a) => a.id === appointmentId);

  if (!appointment) {
    showToast("Appointment not found", "error");
    return;
  }

  modifyingAppointmentId = appointmentId;

  // Populate modal
  document.getElementById("modifyPatientName").textContent =
    appointment.patientName;
  const timeFormatted = formatTime(appointment.time);
  document.getElementById("modifyCurrentDateTime").textContent =
    `${formatDate(appointment.date)} at ${timeFormatted.time} ${timeFormatted.period}`;
  document.getElementById("newDate").value = appointment.date;
  document.getElementById("modifyReason").value = "";

  // Populate time slots
  populateTimeSlots(appointment.date);

  // Show modal
  document.getElementById("modifyModal").style.display = "flex";
}

// Close modify modal
function closeModifyModal() {
  document.getElementById("modifyModal").style.display = "none";
  modifyingAppointmentId = null;

  // Clear URL param if present
  const url = new URL(window.location);
  url.searchParams.delete("modify");
  window.history.replaceState({}, "", url);
}

// Setup date change listener
function setupDateChangeListener() {
  document.getElementById("newDate").addEventListener("change", (e) => {
    populateTimeSlots(e.target.value);
  });
}

// Populate time slots for selected date
function populateTimeSlots(date) {
  const select = document.getElementById("newTime");
  const slots = getData("availabilitySlots") || {};
  const doctorSlots = slots["doc_001"] || {};
  const daySlots = doctorSlots[date] || [];

  // If no slots for this date, generate default slots
  const availableSlots =
    daySlots.length > 0
      ? daySlots.filter((s) => s.status === "available")
      : generateDefaultSlots();

  select.innerHTML =
    '<option value="">Select time</option>' +
    availableSlots
      .map((slot) => {
        const timeFormatted = formatTime(slot.time);
        return `<option value="${slot.time}">${timeFormatted.time} ${timeFormatted.period}</option>`;
      })
      .join("");
}

// Generate default time slots
function generateDefaultSlots() {
  const slots = [];
  for (let hour = 9; hour <= 16; hour++) {
    for (let min = 0; min < 60; min += 30) {
      if (hour === 16 && min > 0) break;
      const time = `${hour.toString().padStart(2, "0")}:${min.toString().padStart(2, "0")}`;
      slots.push({ time, status: "available" });
    }
  }
  return slots;
}

// Handle save modification
function handleSaveModification() {
  const newDate = document.getElementById("newDate").value;
  const newTime = document.getElementById("newTime").value;
  const reason = document.getElementById("modifyReason").value.trim();

  if (!newDate) {
    showToast("Please select a new date", "error");
    return;
  }

  if (!newTime) {
    showToast("Please select a new time", "error");
    return;
  }

  if (modifyAppointment(modifyingAppointmentId, newDate, newTime, reason)) {
    closeModifyModal();
    renderCurrentTab();
    updateTabCounts();
  }
}

// Handle cancel appointment
function handleCancelAppointment() {
  const reason =
    document.getElementById("modifyReason").value.trim() ||
    "Cancelled by assistant";

  if (confirm("Are you sure you want to cancel this appointment?")) {
    if (cancelConfirmedAppointment(modifyingAppointmentId, reason)) {
      closeModifyModal();
      renderCurrentTab();
      updateTabCounts();
    }
  }
}

// Quick cancel from appointment card
function confirmCancelAppointment(appointmentId) {
  const appointments = getData("confirmedAppointments") || [];
  const appointment = appointments.find((a) => a.id === appointmentId);

  if (!appointment) {
    showToast("Appointment not found", "error");
    return;
  }

  if (
    confirm(
      `Cancel appointment for ${appointment.patientName} on ${formatDate(appointment.date)}?`
    )
  ) {
    const reason = "Cancelled by assistant";
    if (cancelConfirmedAppointment(appointmentId, reason)) {
      renderCurrentTab();
      updateTabCounts();
    }
  }
}

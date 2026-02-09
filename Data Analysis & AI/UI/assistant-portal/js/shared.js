/* ================================
   Shared JavaScript for Assistant Portal
   Handles sidebar, notifications, and common functionality
================================ */

// Demo data for assistant
const demoAssistant = {
  id: "asst_001",
  name: "Layla Mohamed",
  role: "Clinic Assistant",
  email: "layla@mamacare.com",
  phone: "+20 100 123 4567",
  image:
    "https://img.freepik.com/free-photo/female-doctor-hospital-with-stethoscope_23-2148827776.jpg?w=740",
};

// Demo linked doctors
const demoLinkedDoctors = [
  {
    id: "doc_001",
    name: "Dr. Ahmed Hassan",
    specialty: "Gynecologist & Obstetrician",
    image:
      "https://img.freepik.com/free-photo/doctor-with-his-arms-crossed-white-background_1368-5790.jpg?w=740",
    clinicName: "MamaCare Women's Clinic",
    status: "active",
  },
];

// Demo pending clinic requests (from doctors)
const demoPendingClinicRequests = [
  {
    id: "req_001",
    doctorId: "doc_002",
    doctorName: "Dr. Fatima Nour",
    specialty: "Obstetrician",
    image:
      "https://img.freepik.com/free-photo/woman-doctor-wearing-lab-coat-with-stethoscope-isolated_1303-29791.jpg?w=740",
    clinicName: "Cairo Medical Center",
    requestedAt: "2026-01-20T10:00:00Z",
    status: "pending",
  },
];

// Demo appointment requests (from patients)
const demoAppointmentRequests = [
  {
    id: "aptreq_001",
    patientId: "pat_001",
    patientName: "Sarah Ahmed",
    patientImage:
      "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
    patientPhone: "+20 111 222 3333",
    doctorId: "doc_001",
    doctorName: "Dr. Ahmed Hassan",
    requestedDate: "2026-01-25",
    requestedTime: "10:00",
    type: "Regular Checkup",
    reason: "Week 25 checkup",
    status: "pending",
    createdAt: "2026-01-21T14:30:00Z",
  },
  {
    id: "aptreq_002",
    patientId: "pat_003",
    patientName: "Mona Ibrahim",
    patientImage:
      "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
    patientPhone: "+20 111 444 5555",
    doctorId: "doc_001",
    doctorName: "Dr. Ahmed Hassan",
    requestedDate: "2026-01-24",
    requestedTime: "14:00",
    type: "Ultrasound",
    reason: "Scheduled ultrasound - high risk pregnancy",
    urgent: true,
    status: "pending",
    createdAt: "2026-01-21T09:15:00Z",
  },
];

// Demo confirmed appointments
const demoConfirmedAppointments = [
  {
    id: "apt_001",
    patientId: "pat_001",
    patientName: "Sarah Ahmed",
    patientImage:
      "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
    patientPhone: "+20 111 222 3333",
    doctorId: "doc_001",
    doctorName: "Dr. Ahmed Hassan",
    date: "2026-01-22",
    time: "09:00",
    type: "Regular Checkup",
    status: "confirmed",
  },
  {
    id: "apt_002",
    patientId: "pat_003",
    patientName: "Mona Ibrahim",
    patientImage:
      "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
    patientPhone: "+20 111 444 5555",
    doctorId: "doc_001",
    doctorName: "Dr. Ahmed Hassan",
    date: "2026-01-22",
    time: "10:30",
    type: "Ultrasound",
    status: "confirmed",
  },
  {
    id: "apt_003",
    patientId: "pat_002",
    patientName: "Fatima Ali",
    patientImage:
      "https://img.freepik.com/free-photo/portrait-beautiful-young-woman-with-curly-hair-brown-hat_1142-42780.jpg?w=740",
    patientPhone: "+20 111 333 4444",
    doctorId: "doc_001",
    doctorName: "Dr. Ahmed Hassan",
    date: "2026-01-22",
    time: "14:00",
    type: "Follow-up",
    status: "confirmed",
  },
];

// Demo availability slots
const demoAvailabilitySlots = {
  doc_001: {
    "2026-01-22": [
      { time: "09:00", status: "booked" },
      { time: "09:30", status: "available" },
      { time: "10:00", status: "available" },
      { time: "10:30", status: "booked" },
      { time: "11:00", status: "available" },
      { time: "11:30", status: "available" },
      { time: "14:00", status: "booked" },
      { time: "14:30", status: "available" },
      { time: "15:00", status: "available" },
      { time: "15:30", status: "blocked" },
      { time: "16:00", status: "available" },
    ],
    "2026-01-23": [
      { time: "09:00", status: "available" },
      { time: "09:30", status: "available" },
      { time: "10:00", status: "available" },
      { time: "10:30", status: "available" },
      { time: "11:00", status: "booked" },
      { time: "11:30", status: "available" },
      { time: "14:00", status: "available" },
      { time: "14:30", status: "available" },
      { time: "15:00", status: "available" },
      { time: "15:30", status: "available" },
      { time: "16:00", status: "available" },
    ],
  },
};

// Demo notifications for assistant
const demoAssistantNotifications = [
  {
    id: "notif_001",
    type: "appointment",
    title: "New appointment request",
    message: "Sarah Ahmed requested an appointment for Jan 25",
    time: "5 min ago",
    read: false,
    icon: "fas fa-calendar-plus",
    iconBg: "#e3f2fd",
    iconColor: "#1976d2",
  },
  {
    id: "notif_002",
    type: "urgent",
    title: "Urgent request",
    message: "Mona Ibrahim (high-risk) needs ultrasound appointment",
    time: "2 hours ago",
    read: false,
    icon: "fas fa-exclamation-circle",
    iconBg: "#ffebee",
    iconColor: "#c62828",
  },
  {
    id: "notif_003",
    type: "clinic",
    title: "New clinic invitation",
    message: "Dr. Fatima Nour invited you to join Cairo Medical Center",
    time: "1 day ago",
    read: true,
    icon: "fas fa-user-md",
    iconBg: "#f3e5f5",
    iconColor: "#7b1fa2",
  },
];

// Initialize data in localStorage if not present
function initDemoData() {
  if (!localStorage.getItem("assistantPortal_assistant")) {
    localStorage.setItem(
      "assistantPortal_assistant",
      JSON.stringify(demoAssistant),
    );
  }
  if (!localStorage.getItem("assistantPortal_linkedDoctors")) {
    localStorage.setItem(
      "assistantPortal_linkedDoctors",
      JSON.stringify(demoLinkedDoctors),
    );
  }
  if (!localStorage.getItem("assistantPortal_clinicRequests")) {
    localStorage.setItem(
      "assistantPortal_clinicRequests",
      JSON.stringify(demoPendingClinicRequests),
    );
  }
  if (!localStorage.getItem("assistantPortal_appointmentRequests")) {
    localStorage.setItem(
      "assistantPortal_appointmentRequests",
      JSON.stringify(demoAppointmentRequests),
    );
  }
  if (!localStorage.getItem("assistantPortal_confirmedAppointments")) {
    localStorage.setItem(
      "assistantPortal_confirmedAppointments",
      JSON.stringify(demoConfirmedAppointments),
    );
  }
  if (!localStorage.getItem("assistantPortal_availabilitySlots")) {
    localStorage.setItem(
      "assistantPortal_availabilitySlots",
      JSON.stringify(demoAvailabilitySlots),
    );
  }
  if (!localStorage.getItem("assistantPortal_notifications")) {
    localStorage.setItem(
      "assistantPortal_notifications",
      JSON.stringify(demoAssistantNotifications),
    );
  }
}

// Get data from localStorage
function getData(key) {
  const data = localStorage.getItem(`assistantPortal_${key}`);
  return data ? JSON.parse(data) : null;
}

// Save data to localStorage
function saveData(key, data) {
  localStorage.setItem(`assistantPortal_${key}`, JSON.stringify(data));
}

// Format time for display
function formatTime(time24) {
  const [hours, minutes] = time24.split(":");
  const hour = parseInt(hours);
  const ampm = hour >= 12 ? "PM" : "AM";
  const hour12 = hour % 12 || 12;
  return { time: `${hour12}:${minutes}`, period: ampm };
}

// Format date for display
function formatDate(dateStr) {
  const date = new Date(dateStr);
  const options = { weekday: "short", month: "short", day: "numeric" };
  return date.toLocaleDateString("en-US", options);
}

// Check if date is today
function isToday(dateStr) {
  const today = new Date();
  const date = new Date(dateStr);
  return date.toDateString() === today.toDateString();
}

// Get unread notifications count
function getUnreadNotificationsCount() {
  const notifications = getData("notifications") || [];
  return notifications.filter((n) => !n.read).length;
}

// Update notification badge
function updateNotificationBadge() {
  const badge = document.querySelector(".notification .badge");
  const count = getUnreadNotificationsCount();
  if (badge) {
    badge.textContent = count;
    badge.style.display = count > 0 ? "flex" : "none";
  }
}

// Show notification toast
function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.className = `toast toast-${type}`;
  toast.innerHTML = `
    <i class="fas fa-${type === "success" ? "check-circle" : type === "error" ? "times-circle" : "info-circle"}"></i>
    <span>${message}</span>
  `;

  // Add toast styles if not present
  if (!document.querySelector("#toast-styles")) {
    const styles = document.createElement("style");
    styles.id = "toast-styles";
    styles.textContent = `
      .toast {
        position: fixed;
        bottom: 24px;
        right: 24px;
        padding: 16px 24px;
        background: var(--bg-white);
        border-radius: var(--radius-md);
        box-shadow: var(--shadow-lg);
        display: flex;
        align-items: center;
        gap: 12px;
        z-index: 1000;
        animation: slideIn 0.3s ease;
      }
      .toast-success { border-left: 4px solid var(--success); }
      .toast-success i { color: var(--success); }
      .toast-error { border-left: 4px solid var(--danger); }
      .toast-error i { color: var(--danger); }
      .toast-info { border-left: 4px solid var(--info); }
      .toast-info i { color: var(--info); }
      @keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
      }
    `;
    document.head.appendChild(styles);
  }

  document.body.appendChild(toast);
  setTimeout(() => {
    toast.style.animation = "slideIn 0.3s ease reverse";
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

// Sidebar functionality
function initSidebar() {
  const sidebar = document.querySelector(".sidebar");
  const sidebarToggle = document.querySelector(".sidebar-toggle");
  const sidebarClose = document.querySelector(".sidebar-close");
  const sidebarOverlay = document.querySelector(".sidebar-overlay");
  const mainContent = document.querySelector(".main-content");

  // Skip if sidebar already initialized by shared.js
  if (!sidebar || sidebar.dataset.sidebarInitialized === "true") return;
  sidebar.dataset.sidebarInitialized = "true";

  // Check saved state - use same key as shared.js
  const isCollapsed = localStorage.getItem("sidebarCollapsed") === "true";
  if (isCollapsed && window.innerWidth > 768) {
    sidebar.classList.add("collapsed");
    document.body.classList.add("sidebar-collapsed");
    if (sidebarToggle) {
      sidebarToggle.classList.add("collapsed");
      const icon = sidebarToggle.querySelector("i");
      if (icon) icon.className = "fas fa-chevron-right";
    }
    if (mainContent) {
      mainContent.style.marginLeft = "80px";
      mainContent.style.maxWidth = "calc(100% - 80px)";
    }
  }

  // Toggle sidebar
  if (sidebarToggle) {
    sidebarToggle.addEventListener("click", () => {
      if (window.innerWidth <= 768) {
        sidebar.classList.toggle("open");
        sidebarOverlay.classList.toggle("active");
      } else {
        sidebar.classList.toggle("collapsed");
        document.body.classList.toggle("sidebar-collapsed");
        sidebarToggle.classList.toggle("collapsed");

        const icon = sidebarToggle.querySelector("i");
        if (sidebar.classList.contains("collapsed")) {
          if (icon) icon.className = "fas fa-chevron-right";
          localStorage.setItem("sidebarCollapsed", "true");
          if (mainContent) {
            mainContent.style.marginLeft = "80px";
            mainContent.style.maxWidth = "calc(100% - 80px)";
          }
        } else {
          if (icon) icon.className = "fas fa-chevron-left";
          localStorage.setItem("sidebarCollapsed", "false");
          if (mainContent) {
            mainContent.style.marginLeft = "260px";
            mainContent.style.maxWidth = "calc(100% - 260px)";
          }
        }
      }
    });
  }

  // Close sidebar on mobile
  if (sidebarClose) {
    sidebarClose.addEventListener("click", () => {
      sidebar.classList.remove("open");
      sidebarOverlay.classList.remove("active");
    });
  }

  if (sidebarOverlay) {
    sidebarOverlay.addEventListener("click", () => {
      sidebar.classList.remove("open");
      sidebarOverlay.classList.remove("active");
    });
  }

  // Add tooltips for collapsed state
  const navItems = document.querySelectorAll(".nav-item");
  navItems.forEach((item) => {
    const tooltip = item.getAttribute("data-tooltip");
    if (tooltip) {
      item.setAttribute("title", tooltip);
    }
  });
}

// Notifications panel functionality
function initNotificationsPanel() {
  const notificationBtn = document.querySelector(".notification");
  const notificationsPanel = document.querySelector(".notifications-panel");
  const closeNotifications = document.querySelector(".close-notifications");

  if (notificationBtn && notificationsPanel) {
    notificationBtn.addEventListener("click", () => {
      notificationsPanel.classList.toggle("open");
      renderNotifications();
    });

    if (closeNotifications) {
      closeNotifications.addEventListener("click", () => {
        notificationsPanel.classList.remove("open");
      });
    }

    // Close on outside click
    document.addEventListener("click", (e) => {
      if (
        !notificationsPanel.contains(e.target) &&
        !notificationBtn.contains(e.target)
      ) {
        notificationsPanel.classList.remove("open");
      }
    });
  }
}

// Render notifications in panel
function renderNotifications() {
  const container = document.querySelector(".notifications-body");
  if (!container) return;

  const notifications = getData("notifications") || [];

  if (notifications.length === 0) {
    container.innerHTML = `
      <div class="empty-state">
        <i class="fas fa-bell-slash"></i>
        <p>No notifications</p>
      </div>
    `;
    return;
  }

  container.innerHTML = notifications
    .map(
      (notif) => `
    <div class="notification-item ${notif.read ? "" : "unread"}" data-id="${notif.id}">
      <div class="notif-icon" style="background: ${notif.iconBg}">
        <i class="${notif.icon}" style="color: ${notif.iconColor}"></i>
      </div>
      <div class="notif-content">
        <h4>${notif.title}</h4>
        <p>${notif.message}</p>
      </div>
      <span class="notif-time">${notif.time}</span>
    </div>
  `,
    )
    .join("");

  // Mark as read on click
  container.querySelectorAll(".notification-item").forEach((item) => {
    item.addEventListener("click", () => {
      const id = item.dataset.id;
      markNotificationRead(id);
      item.classList.remove("unread");
    });
  });
}

// Mark notification as read
function markNotificationRead(id) {
  const notifications = getData("notifications") || [];
  const index = notifications.findIndex((n) => n.id === id);
  if (index !== -1) {
    notifications[index].read = true;
    saveData("notifications", notifications);
    updateNotificationBadge();
  }
}

// Approve clinic request
function approveClinicRequest(requestId) {
  const requests = getData("clinicRequests") || [];
  const requestIndex = requests.findIndex((r) => r.id === requestId);

  if (requestIndex !== -1) {
    const request = requests[requestIndex];

    // Add doctor to linked doctors
    const linkedDoctors = getData("linkedDoctors") || [];
    linkedDoctors.push({
      id: request.doctorId,
      name: request.doctorName,
      specialty: request.specialty,
      image: request.image,
      clinicName: request.clinicName,
      status: "active",
    });
    saveData("linkedDoctors", linkedDoctors);

    // Remove from pending requests
    requests.splice(requestIndex, 1);
    saveData("clinicRequests", requests);

    showToast(`You are now linked with ${request.doctorName}`, "success");
    return true;
  }
  return false;
}

// Reject clinic request
function rejectClinicRequest(requestId) {
  const requests = getData("clinicRequests") || [];
  const requestIndex = requests.findIndex((r) => r.id === requestId);

  if (requestIndex !== -1) {
    const request = requests[requestIndex];
    requests.splice(requestIndex, 1);
    saveData("clinicRequests", requests);

    showToast(`Declined invitation from ${request.doctorName}`, "info");
    return true;
  }
  return false;
}

// Confirm appointment request
function confirmAppointmentRequest(requestId) {
  const requests = getData("appointmentRequests") || [];
  const requestIndex = requests.findIndex((r) => r.id === requestId);

  if (requestIndex !== -1) {
    const request = requests[requestIndex];

    // Add to confirmed appointments
    const confirmedAppointments = getData("confirmedAppointments") || [];
    confirmedAppointments.push({
      id: "apt_" + Date.now(),
      patientId: request.patientId,
      patientName: request.patientName,
      patientImage: request.patientImage,
      patientPhone: request.patientPhone,
      doctorId: request.doctorId,
      doctorName: request.doctorName,
      date: request.requestedDate,
      time: request.requestedTime,
      type: request.type,
      status: "confirmed",
    });
    saveData("confirmedAppointments", confirmedAppointments);

    // Update slot to booked
    const slots = getData("availabilitySlots") || {};
    if (
      slots[request.doctorId] &&
      slots[request.doctorId][request.requestedDate]
    ) {
      const slotIndex = slots[request.doctorId][
        request.requestedDate
      ].findIndex((s) => s.time === request.requestedTime);
      if (slotIndex !== -1) {
        slots[request.doctorId][request.requestedDate][slotIndex].status =
          "booked";
        saveData("availabilitySlots", slots);
      }
    }

    // Remove from pending requests
    requests.splice(requestIndex, 1);
    saveData("appointmentRequests", requests);

    showToast(`Appointment confirmed for ${request.patientName}`, "success");
    return true;
  }
  return false;
}

// Cancel/Reject appointment request
function cancelAppointmentRequest(requestId, reason = "") {
  const requests = getData("appointmentRequests") || [];
  const requestIndex = requests.findIndex((r) => r.id === requestId);

  if (requestIndex !== -1) {
    const request = requests[requestIndex];
    requests.splice(requestIndex, 1);
    saveData("appointmentRequests", requests);

    showToast(`Appointment request cancelled`, "info");
    return true;
  }
  return false;
}

// Modify confirmed appointment
function modifyAppointment(appointmentId, newDate, newTime, reason = "") {
  const appointments = getData("confirmedAppointments") || [];
  const index = appointments.findIndex((a) => a.id === appointmentId);

  if (index !== -1) {
    const oldDate = appointments[index].date;
    const oldTime = appointments[index].time;
    const doctorId = appointments[index].doctorId;

    // Update availability slots
    const slots = getData("availabilitySlots") || {};

    // Free old slot
    if (slots[doctorId] && slots[doctorId][oldDate]) {
      const oldSlotIndex = slots[doctorId][oldDate].findIndex(
        (s) => s.time === oldTime,
      );
      if (oldSlotIndex !== -1) {
        slots[doctorId][oldDate][oldSlotIndex].status = "available";
      }
    }

    // Book new slot
    if (slots[doctorId] && slots[doctorId][newDate]) {
      const newSlotIndex = slots[doctorId][newDate].findIndex(
        (s) => s.time === newTime,
      );
      if (newSlotIndex !== -1) {
        slots[doctorId][newDate][newSlotIndex].status = "booked";
      }
    }
    saveData("availabilitySlots", slots);

    // Update appointment
    appointments[index].date = newDate;
    appointments[index].time = newTime;
    appointments[index].status = "modified";
    appointments[index].modifiedReason = reason;
    appointments[index].modifiedAt = new Date().toISOString();
    saveData("confirmedAppointments", appointments);

    showToast(
      `Appointment rescheduled to ${formatDate(newDate)} at ${formatTime(newTime).time} ${formatTime(newTime).period}`,
      "success",
    );
    return true;
  }
  return false;
}

// Cancel confirmed appointment
function cancelConfirmedAppointment(appointmentId, reason = "") {
  const appointments = getData("confirmedAppointments") || [];
  const index = appointments.findIndex((a) => a.id === appointmentId);

  if (index !== -1) {
    const appointment = appointments[index];
    const doctorId = appointment.doctorId;

    // Free the slot
    const slots = getData("availabilitySlots") || {};
    if (slots[doctorId] && slots[doctorId][appointment.date]) {
      const slotIndex = slots[doctorId][appointment.date].findIndex(
        (s) => s.time === appointment.time,
      );
      if (slotIndex !== -1) {
        slots[doctorId][appointment.date][slotIndex].status = "available";
      }
    }
    saveData("availabilitySlots", slots);

    // Mark as cancelled (or remove)
    appointments[index].status = "cancelled";
    appointments[index].cancelReason = reason;
    appointments[index].cancelledAt = new Date().toISOString();
    saveData("confirmedAppointments", appointments);

    showToast(`Appointment cancelled for ${appointment.patientName}`, "info");
    return true;
  }
  return false;
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", () => {
  initDemoData();
  initSidebar();
  initNotificationsPanel();
  updateNotificationBadge();
});

/* ================================
   Shared JavaScript for Doctor Portal
   Handles sidebar, notifications, and common functionality
================================ */

// Demo data for doctors
const demoDoctor = {
  id: "doc_001",
  name: "Dr. Ahmed Hassan",
  specialty: "Gynecologist & Obstetrician",
  email: "dr.ahmed@mamacare.com",
  phone: "+20 123 456 7890",
  image:
    "https://img.freepik.com/free-photo/doctor-with-his-arms-crossed-white-background_1368-5790.jpg?w=740",
  clinicName: "MamaCare Women's Clinic",
  experience: "15 years",
  rating: 4.9,
};

// Demo patients data
const demoPatients = [
  {
    id: "pat_001",
    name: "Sarah Ahmed",
    image:
      "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
    gestationalAge: 24,
    dueDate: "2026-08-15",
    riskLevel: "low",
    lastVisit: "2026-01-15",
    nextAppointment: "2026-01-25",
    nextAppointmentTime: "9:00 AM",
    unreadMessages: 2,
    phone: "+20 111 222 3333",
    bloodType: "A+",
    age: 28,
    preWeight: 58,
    height: 165,
    gravidaPara: "G2 P1",
    allergies: "None reported",
    vitals: {
      bloodPressure: [
        { value: "118/76", date: "2026-01-20", notes: "Normal" },
        { value: "120/78", date: "2026-01-15", notes: "Normal" },
        { value: "115/75", date: "2026-01-08", notes: "Normal" },
      ],
      bloodSugar: [
        { value: 95, date: "2026-01-20", type: "Fasting", notes: "Normal" },
        { value: 110, date: "2026-01-15", type: "Random", notes: "Normal" },
      ],
      weight: [
        { value: 65, date: "2026-01-20" },
        { value: 64.5, date: "2026-01-15" },
        { value: 64, date: "2026-01-08" },
      ],
    },
    tests: [
      {
        name: "Complete Blood Count",
        date: "2026-01-18",
        status: "completed",
        result: "Normal",
      },
      {
        name: "Glucose Tolerance Test",
        date: "2026-01-10",
        status: "completed",
        result: "Normal",
      },
      {
        name: "Ultrasound - Anatomy Scan",
        date: "2026-01-25",
        status: "scheduled",
        result: null,
      },
    ],
    appointments: [
      {
        date: "2026-01-25",
        type: "Ultrasound Scan",
        time: "9:00 AM",
        status: "upcoming",
      },
      {
        date: "2026-01-15",
        type: "Routine Checkup",
        time: "10:00 AM",
        status: "completed",
        notes: "All vitals normal. Baby developing well.",
      },
      {
        date: "2026-01-08",
        type: "Blood Work",
        time: "8:30 AM",
        status: "completed",
      },
      {
        date: "2025-12-20",
        type: "First Trimester Screening",
        time: "11:00 AM",
        status: "completed",
        notes: "Low risk results",
      },
    ],
    notes: [
      {
        date: "2026-01-20",
        author: "Dr. Ahmed Hassan",
        content:
          "Patient doing well. No complaints. Continue current prenatal vitamins.",
        type: "visit",
      },
      {
        date: "2026-01-15",
        author: "Dr. Ahmed Hassan",
        content: "Week 23 checkup. Fetal heart rate normal at 145 bpm.",
        type: "visit",
      },
    ],
    riskFactors: [],
  },
  {
    id: "pat_002",
    name: "Fatima Ali",
    image:
      "https://img.freepik.com/free-photo/portrait-beautiful-young-woman-with-curly-hair-brown-hat_1142-42780.jpg?w=740",
    gestationalAge: 32,
    dueDate: "2026-03-19",
    riskLevel: "medium",
    lastVisit: "2026-01-18",
    nextAppointment: "2026-01-24",
    nextAppointmentTime: "10:00 AM",
    unreadMessages: 0,
    phone: "+20 111 333 4444",
    bloodType: "O+",
    age: 31,
    preWeight: 62,
    height: 160,
    gravidaPara: "G3 P2",
    allergies: "Penicillin",
    vitals: {
      bloodPressure: [
        {
          value: "130/85",
          date: "2026-01-20",
          notes: "Slightly elevated - monitoring",
        },
        { value: "128/82", date: "2026-01-15", notes: "Monitor closely" },
        { value: "125/80", date: "2026-01-08", notes: "Normal range" },
      ],
      bloodSugar: [
        {
          value: 105,
          date: "2026-01-20",
          type: "Fasting",
          notes: "Borderline",
        },
        { value: 125, date: "2026-01-15", type: "Random", notes: "Monitor" },
      ],
      weight: [
        { value: 72, date: "2026-01-20" },
        { value: 71.5, date: "2026-01-15" },
        { value: 71, date: "2026-01-08" },
      ],
    },
    tests: [
      {
        name: "Complete Blood Count",
        date: "2026-01-18",
        status: "completed",
        result: "Normal",
      },
      {
        name: "Gestational Diabetes Screen",
        date: "2026-01-10",
        status: "completed",
        result: "Borderline - Retest needed",
      },
      {
        name: "Non-Stress Test",
        date: "2026-01-24",
        status: "scheduled",
        result: null,
      },
    ],
    appointments: [
      {
        date: "2026-01-24",
        type: "Non-Stress Test",
        time: "10:00 AM",
        status: "upcoming",
      },
      {
        date: "2026-01-18",
        type: "Routine Checkup",
        time: "11:00 AM",
        status: "completed",
        notes: "Blood pressure monitoring. Discussed diet changes.",
      },
      {
        date: "2026-01-10",
        type: "Glucose Screening",
        time: "9:00 AM",
        status: "completed",
      },
      {
        date: "2025-12-28",
        type: "Ultrasound",
        time: "2:00 PM",
        status: "completed",
        notes: "Baby in breech position. Will monitor.",
      },
    ],
    notes: [
      {
        date: "2026-01-20",
        author: "Dr. Ahmed Hassan",
        content:
          "Blood pressure slightly elevated. Recommended dietary changes and increased monitoring.",
        type: "visit",
      },
      {
        date: "2026-01-15",
        author: "Dr. Ahmed Hassan",
        content:
          "Week 31 checkup. Baby measuring on track. Discussed birth plan.",
        type: "visit",
      },
    ],
    riskFactors: [
      { description: "Gestational hypertension risk", severity: "medium" },
      { description: "Previous cesarean delivery", severity: "low" },
    ],
  },
  {
    id: "pat_003",
    name: "Mona Ibrahim",
    image:
      "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
    gestationalAge: 12,
    dueDate: "2026-07-22",
    riskLevel: "high",
    lastVisit: "2026-01-20",
    nextAppointment: "2026-01-23",
    nextAppointmentTime: "11:00 AM",
    needsAttention: true,
    unreadMessages: 5,
    phone: "+20 111 444 5555",
    bloodType: "B-",
    age: 35,
    preWeight: 55,
    height: 158,
    gravidaPara: "G1 P0",
    allergies: "Sulfa drugs",
    vitals: {
      bloodPressure: [
        {
          value: "142/92",
          date: "2026-01-20",
          notes: "High - Requires attention",
        },
        { value: "138/88", date: "2026-01-15", notes: "Elevated" },
        { value: "135/85", date: "2026-01-08", notes: "Borderline high" },
      ],
      bloodSugar: [
        { value: 88, date: "2026-01-20", type: "Fasting", notes: "Normal" },
        { value: 102, date: "2026-01-15", type: "Random", notes: "Normal" },
      ],
      weight: [
        { value: 57, date: "2026-01-20" },
        { value: 56.5, date: "2026-01-15" },
        { value: 56, date: "2026-01-08" },
      ],
    },
    tests: [
      {
        name: "NIPT Screening",
        date: "2026-01-18",
        status: "completed",
        result: "Low risk",
      },
      {
        name: "First Trimester Screening",
        date: "2026-01-10",
        status: "completed",
        result: "Normal",
      },
      {
        name: "Blood Pressure Monitoring",
        date: "2026-01-23",
        status: "scheduled",
        result: null,
      },
    ],
    appointments: [
      {
        date: "2026-01-23",
        type: "BP Monitoring",
        time: "11:00 AM",
        status: "upcoming",
        notes: "Urgent follow-up for hypertension",
      },
      {
        date: "2026-01-20",
        type: "Emergency Consult",
        time: "2:00 PM",
        status: "completed",
        notes: "Started methyldopa treatment",
      },
      {
        date: "2026-01-15",
        type: "Routine Checkup",
        time: "10:00 AM",
        status: "completed",
      },
      {
        date: "2026-01-10",
        type: "First Trimester Screening",
        time: "9:00 AM",
        status: "completed",
        notes: "Low risk results",
      },
    ],
    notes: [
      {
        date: "2026-01-20",
        author: "Dr. Ahmed Hassan",
        content:
          "BP elevated. Started on methyldopa. Close monitoring required. Weekly visits recommended.",
        type: "urgent",
      },
      {
        date: "2026-01-15",
        author: "Dr. Ahmed Hassan",
        content:
          "First trimester screening normal. Discussed elevated BP concerns.",
        type: "visit",
      },
    ],
    riskFactors: [
      { description: "Chronic hypertension", severity: "high" },
      { description: "Advanced maternal age (35+)", severity: "medium" },
      { description: "First pregnancy", severity: "low" },
      { description: "Rh negative blood type", severity: "medium" },
    ],
  },
  {
    id: "pat_004",
    name: "Nour Hassan",
    image:
      "https://img.freepik.com/free-photo/portrait-young-businesswoman-holding-eyeglasses-hand-against-gray-backdrop_23-2148029483.jpg?w=740",
    gestationalAge: 28,
    dueDate: "2026-04-16",
    riskLevel: "low",
    lastVisit: "2026-01-10",
    nextAppointment: "2026-01-28",
    nextAppointmentTime: "2:30 PM",
    unreadMessages: 1,
    phone: "+20 111 555 6666",
    bloodType: "AB+",
    age: 26,
    preWeight: 54,
    height: 162,
    gravidaPara: "G1 P0",
    allergies: "None reported",
    vitals: {
      bloodPressure: [
        { value: "110/70", date: "2026-01-18", notes: "Excellent" },
        { value: "112/72", date: "2026-01-10", notes: "Normal" },
      ],
      bloodSugar: [
        { value: 90, date: "2026-01-18", type: "Fasting", notes: "Normal" },
        { value: 105, date: "2026-01-10", type: "Random", notes: "Normal" },
      ],
      weight: [
        { value: 62, date: "2026-01-18" },
        { value: 61, date: "2026-01-10" },
      ],
    },
    tests: [
      {
        name: "Complete Blood Count",
        date: "2026-01-15",
        status: "completed",
        result: "Normal",
      },
      {
        name: "Glucose Challenge Test",
        date: "2026-01-28",
        status: "scheduled",
        result: null,
      },
    ],
    appointments: [
      {
        date: "2026-01-28",
        type: "Glucose Challenge Test",
        time: "8:00 AM",
        status: "upcoming",
      },
      {
        date: "2026-01-18",
        type: "Routine Checkup",
        time: "3:00 PM",
        status: "completed",
        notes: "Week 27 checkup. All normal.",
      },
      {
        date: "2026-01-05",
        type: "Ultrasound",
        time: "10:00 AM",
        status: "completed",
        notes: "Baby growth on track",
      },
      {
        date: "2025-12-15",
        type: "Anatomy Scan",
        time: "11:00 AM",
        status: "completed",
        notes: "All measurements normal",
      },
    ],
    notes: [
      {
        date: "2026-01-18",
        author: "Dr. Ahmed Hassan",
        content:
          "Week 27 checkup. All vitals normal. Baby active. No concerns.",
        type: "visit",
      },
    ],
    riskFactors: [],
  },
];

// Demo appointments data
const demoAppointments = [
  {
    id: "apt_001",
    patientId: "pat_001",
    patientName: "Sarah Ahmed",
    patientImage:
      "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
    date: "2026-01-22",
    time: "09:00",
    type: "Regular Checkup",
    status: "confirmed",
    notes: "Week 24 checkup",
  },
  {
    id: "apt_002",
    patientId: "pat_003",
    patientName: "Mona Ibrahim",
    patientImage:
      "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
    date: "2026-01-22",
    time: "10:30",
    type: "Ultrasound",
    status: "confirmed",
    notes: "High risk - monitor closely",
  },
  {
    id: "apt_003",
    patientId: "pat_002",
    patientName: "Fatima Ali",
    patientImage:
      "https://img.freepik.com/free-photo/portrait-beautiful-young-woman-with-curly-hair-brown-hat_1142-42780.jpg?w=740",
    date: "2026-01-22",
    time: "14:00",
    type: "Follow-up",
    status: "confirmed",
    notes: "Review test results",
  },
  {
    id: "apt_004",
    patientId: "pat_004",
    patientName: "Nour Hassan",
    patientImage:
      "https://img.freepik.com/free-photo/portrait-young-businesswoman-holding-eyeglasses-hand-against-gray-backdrop_23-2148029483.jpg?w=740",
    date: "2026-01-23",
    time: "11:00",
    type: "Regular Checkup",
    status: "confirmed",
    notes: "",
  },
];

// Demo notifications
const demoNotifications = [
  {
    id: "notif_001",
    type: "message",
    title: "New message from Sarah Ahmed",
    message: "I have a question about my medication...",
    time: "5 min ago",
    read: false,
    icon: "fas fa-comment",
    iconBg: "#e3f2fd",
    iconColor: "#1976d2",
  },
  {
    id: "notif_002",
    type: "test",
    title: "Test results uploaded",
    message: "Mona Ibrahim uploaded CBC test results",
    time: "1 hour ago",
    read: false,
    icon: "fas fa-flask",
    iconBg: "#f3e5f5",
    iconColor: "#7b1fa2",
  },
  {
    id: "notif_003",
    type: "appointment",
    title: "Appointment reminder",
    message: "You have 3 appointments today",
    time: "2 hours ago",
    read: true,
    icon: "fas fa-calendar-check",
    iconBg: "#e8f5e9",
    iconColor: "#388e3c",
  },
  {
    id: "notif_004",
    type: "alert",
    title: "High-risk patient alert",
    message: "Mona Ibrahim - elevated blood pressure reading",
    time: "3 hours ago",
    read: false,
    icon: "fas fa-exclamation-triangle",
    iconBg: "#fff3e0",
    iconColor: "#f57c00",
  },
];

// Demo clinic team (assistants)
const demoClinicTeam = [
  {
    id: "asst_001",
    name: "Layla Mohamed",
    role: "Clinic Assistant",
    image:
      "https://img.freepik.com/free-photo/female-doctor-hospital-with-stethoscope_23-2148827776.jpg?w=740",
    status: "online",
    email: "layla@mamacare.com",
  },
];

// Demo pending assistant requests
const demoPendingRequests = [];

// Get data from localStorage
function getData(key) {
  const data = localStorage.getItem(`doctorPortal_${key}`);
  return data ? JSON.parse(data) : null;
}

// Save data to localStorage
function saveData(key, data) {
  localStorage.setItem(`doctorPortal_${key}`, JSON.stringify(data));
}

// Reset demo data (clears old data)
function resetDemoData() {
  localStorage.removeItem("doctorPortal_doctor");
  localStorage.removeItem("doctorPortal_patients");
  localStorage.removeItem("doctorPortal_appointments");
  localStorage.removeItem("doctorPortal_notifications");
  localStorage.removeItem("doctorPortal_clinicTeam");
  localStorage.removeItem("doctorPortal_pendingRequests");
  localStorage.removeItem("doctorPortal_conversations");
}

// Auto-reset if data has old format (migration)
function checkDataVersion() {
  const patients = getData("patients");
  if (
    patients &&
    patients.length > 0 &&
    (patients[0].pregnancyWeek !== undefined ||
      patients[0].vitals === undefined ||
      patients[0].appointments === undefined)
  ) {
    // Old format detected or missing vitals/appointments, reset to new format
    resetDemoData();
  }
}

// Initialize data in localStorage if not present
function initDemoData() {
  // Check for old data format and reset if needed
  checkDataVersion();

  if (!localStorage.getItem("doctorPortal_doctor")) {
    localStorage.setItem("doctorPortal_doctor", JSON.stringify(demoDoctor));
  }
  if (!localStorage.getItem("doctorPortal_patients")) {
    localStorage.setItem("doctorPortal_patients", JSON.stringify(demoPatients));
  }
  if (!localStorage.getItem("doctorPortal_appointments")) {
    localStorage.setItem(
      "doctorPortal_appointments",
      JSON.stringify(demoAppointments),
    );
  }
  if (!localStorage.getItem("doctorPortal_notifications")) {
    localStorage.setItem(
      "doctorPortal_notifications",
      JSON.stringify(demoNotifications),
    );
  }
  if (!localStorage.getItem("doctorPortal_clinicTeam")) {
    localStorage.setItem(
      "doctorPortal_clinicTeam",
      JSON.stringify(demoClinicTeam),
    );
  }
  if (!localStorage.getItem("doctorPortal_pendingRequests")) {
    localStorage.setItem(
      "doctorPortal_pendingRequests",
      JSON.stringify(demoPendingRequests),
    );
  }
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

  // Skip if sidebar already initialized by shared.js
  if (!sidebar || sidebar.dataset.sidebarInitialized === "true") return;
  sidebar.dataset.sidebarInitialized = "true";

  const sidebarToggle = document.querySelector("#menuToggle");
  const sidebarToggleBtn = document.querySelector(".sidebar-toggle");
  const sidebarClose = document.querySelector("#sidebarClose");
  const sidebarOverlay =
    document.querySelector("#sidebarOverlay") ||
    document.querySelector(".sidebar-overlay");
  const mainContent = document.querySelector(".main-content");

  // Function to collapse sidebar
  function collapseSidebar() {
    sidebar.classList.add("collapsed");
    document.body.classList.add("sidebar-collapsed");
    sidebar.style.width = "80px";
    if (sidebarToggleBtn) {
      sidebarToggleBtn.classList.add("collapsed");
      const icon = sidebarToggleBtn.querySelector("i");
      if (icon) icon.className = "fas fa-chevron-right";
    }
    if (mainContent) {
      mainContent.style.marginLeft = "80px";
      mainContent.style.maxWidth = "calc(100% - 80px)";
    }
  }

  // Function to expand sidebar
  function expandSidebar() {
    sidebar.classList.remove("collapsed");
    document.body.classList.remove("sidebar-collapsed");
    sidebar.style.width = "260px";
    if (sidebarToggleBtn) {
      sidebarToggleBtn.classList.remove("collapsed");
      const icon = sidebarToggleBtn.querySelector("i");
      if (icon) icon.className = "fas fa-chevron-left";
    }
    if (mainContent) {
      mainContent.style.marginLeft = "260px";
      mainContent.style.maxWidth = "calc(100% - 260px)";
    }
  }

  // Check saved state - use consistent key
  const isCollapsed = localStorage.getItem("sidebarCollapsed") === "true";
  if (isCollapsed && window.innerWidth > 768) {
    collapseSidebar();
  }

  // Toggle sidebar (chevron button)
  if (sidebarToggleBtn) {
    sidebarToggleBtn.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();

      if (sidebar.classList.contains("collapsed")) {
        expandSidebar();
        localStorage.setItem("sidebarCollapsed", "false");
      } else {
        collapseSidebar();
        localStorage.setItem("sidebarCollapsed", "true");
      }
    });
  }

  // Toggle sidebar (hamburger menu on mobile)
  if (sidebarToggle) {
    sidebarToggle.addEventListener("click", () => {
      if (window.innerWidth <= 768) {
        sidebar.classList.toggle("open");
        if (sidebarOverlay) sidebarOverlay.classList.toggle("active");
      } else {
        if (sidebar.classList.contains("collapsed")) {
          expandSidebar();
          localStorage.setItem("sidebarCollapsed", "false");
        } else {
          collapseSidebar();
          localStorage.setItem("sidebarCollapsed", "true");
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
  const notificationBtn =
    document.querySelector(".notification") ||
    document.querySelector("#notificationBtn") ||
    document.querySelector(".notification-btn");
  const notificationsPanel =
    document.querySelector(".notifications-panel") ||
    document.querySelector("#notificationsPanel");
  const closeNotifications =
    document.querySelector(".close-notifications") ||
    document.querySelector(".mark-all-read");

  if (notificationBtn && notificationsPanel) {
    notificationBtn.addEventListener("click", (e) => {
      e.stopPropagation();
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

// Alias for initNotificationsPanel (for backwards compatibility)
function initNotifications() {
  initNotificationsPanel();
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", () => {
  // Force reset demo data to ensure latest structure
  resetDemoData();
  initSidebar();
  initNotificationsPanel();
  updateNotificationBadge();
});

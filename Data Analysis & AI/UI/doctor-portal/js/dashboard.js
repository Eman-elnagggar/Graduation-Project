/* ================================
   Doctor Dashboard JavaScript
================================ */

document.addEventListener("DOMContentLoaded", () => {
  initDemoData();
  ensureTodayScheduleData();
  renderTodaySchedule();
  renderPriorityPatients();
  renderRecentMessages();
  updateStats();
});

function ensureTodayScheduleData() {
  const today = new Date().toISOString().split("T")[0];
  const storedAppointments = getData("appointments") || [];
  const hasTodayAppointments = storedAppointments.some(
    (apt) => apt.date === today && apt.status === "confirmed",
  );

  if (hasTodayAppointments) {
    return;
  }

  const demoTodayAppointments = [
    {
      id: `apt_demo_${Date.now()}_1`,
      patientId: "pat_001",
      patientName: "Sarah Ahmed",
      patientImage:
        "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
      date: today,
      time: "09:00",
      type: "Regular Checkup",
      status: "confirmed",
      notes: "Week 26 checkup",
    },
    {
      id: `apt_demo_${Date.now()}_2`,
      patientId: "pat_003",
      patientName: "Mona Ibrahim",
      patientImage:
        "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
      date: today,
      time: "10:30",
      type: "Ultrasound",
      status: "confirmed",
      notes: "High risk - monitor closely",
    },
    {
      id: `apt_demo_${Date.now()}_3`,
      patientId: "pat_002",
      patientName: "Fatima Ali",
      patientImage:
        "https://img.freepik.com/free-photo/portrait-beautiful-young-woman-with-curly-hair-brown-hat_1142-42780.jpg?w=740",
      date: today,
      time: "14:00",
      type: "Follow-up",
      status: "confirmed",
      notes: "Review test results",
    },
  ];

  saveData("appointments", [...storedAppointments, ...demoTodayAppointments]);
}

const hardcodedPatients = [
  {
    id: "pat_001",
    name: "Sarah Ahmed",
    image:
      "https://img.freepik.com/free-psd/portrait-woman-wearing-hijab_23-2150945115.jpg?w=740",
    gestationalAge: 24,
    riskLevel: "low",
    lastVisit: "2026-01-15",
    unreadMessages: 2,
  },
  {
    id: "pat_002",
    name: "Fatima Ali",
    image:
      "https://img.freepik.com/free-photo/portrait-beautiful-young-woman-with-curly-hair-brown-hat_1142-42780.jpg?w=740",
    gestationalAge: 32,
    riskLevel: "medium",
    lastVisit: "2026-01-18",
    unreadMessages: 0,
  },
  {
    id: "pat_003",
    name: "Mona Ibrahim",
    image:
      "https://img.freepik.com/free-photo/young-beautiful-woman-pink-warm-sweater-natural-look-smiling-portrait-isolated-long-hair_285396-896.jpg?w=740",
    gestationalAge: 12,
    riskLevel: "high",
    lastVisit: "2026-01-20",
    unreadMessages: 5,
  },
  {
    id: "pat_004",
    name: "Nour Hassan",
    image:
      "https://img.freepik.com/free-photo/portrait-young-businesswoman-holding-eyeglasses-hand-against-gray-backdrop_23-2148029483.jpg?w=740",
    gestationalAge: 28,
    riskLevel: "low",
    lastVisit: "2026-01-10",
    unreadMessages: 1,
  },
];

const hardcodedAppointments = [
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
];

// Update dashboard stats
function updateStats() {
  const storedPatients = getData("patients") || [];
  const storedAppointments = getData("appointments") || [];
  const patients =
    storedPatients.length > 0 ? storedPatients : hardcodedPatients;
  const appointments =
    storedAppointments.length > 0 ? storedAppointments : hardcodedAppointments;
  const today = new Date().toISOString().split("T")[0];

  // Today's appointments count
  const todayAppointments = appointments.filter((apt) => apt.date === today);
  document.getElementById("todayAppointments").textContent =
    todayAppointments.length;

  // Total patients
  document.getElementById("totalPatients").textContent = patients.length;

  // High risk patients
  const highRisk = patients.filter((p) => p.riskLevel === "high");
  document.getElementById("highRiskCount").textContent = highRisk.length;

  // Unread messages
  const totalUnread = patients.reduce(
    (sum, p) => sum + (p.unreadMessages || 0),
    0,
  );
  document.getElementById("unreadMessages").textContent = totalUnread;

  // Next patient
  if (todayAppointments.length > 0) {
    const sortedAppointments = todayAppointments.sort((a, b) =>
      a.time.localeCompare(b.time),
    );
    const now = new Date();
    const currentTime = `${now.getHours().toString().padStart(2, "0")}:${now.getMinutes().toString().padStart(2, "0")}`;

    const nextApt =
      sortedAppointments.find((apt) => apt.time >= currentTime) ||
      sortedAppointments[0];
    const timeFormatted = formatTime(nextApt.time);
    document.getElementById("nextPatient").textContent =
      `${nextApt.patientName} at ${timeFormatted.time} ${timeFormatted.period}`;
  }

  // Today's date
  const dateOptions = {
    weekday: "long",
    year: "numeric",
    month: "long",
    day: "numeric",
  };
  document.getElementById("todayDate").textContent =
    new Date().toLocaleDateString("en-US", dateOptions);
}

// Render today's schedule
function renderTodaySchedule() {
  const container = document.getElementById("todaySchedule");
  const storedAppointments = getData("appointments") || [];
  const appointments =
    storedAppointments.length > 0 ? storedAppointments : hardcodedAppointments;
  const today = new Date().toISOString().split("T")[0];

  const todayAppointments = appointments
    .filter((apt) => apt.date === today && apt.status === "confirmed")
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

  const now = new Date();
  const currentTime = `${now.getHours().toString().padStart(2, "0")}:${now.getMinutes().toString().padStart(2, "0")}`;

  container.innerHTML = todayAppointments
    .map((apt, index) => {
      const timeFormatted = formatTime(apt.time);
      let status = "upcoming";
      if (apt.time < currentTime) status = "completed";
      else if (
        index === todayAppointments.findIndex((a) => a.time >= currentTime)
      )
        status = "current";

      return `
      <div class="timeline-item ${status}">
        <div class="appointment-card">
          <div class="appointment-time">
            <span class="time">${timeFormatted.time}</span>
            <span class="period">${timeFormatted.period}</span>
          </div>
          <div class="appointment-details">
            <h4>${apt.patientName}</h4>
            <p class="type">${apt.type}</p>
            <div class="patient-preview">
              <img src="${apt.patientImage}" alt="${apt.patientName}" />
              <span>${apt.notes || "Regular appointment"}</span>
            </div>
          </div>
          <div class="appointment-actions">
            <a href="patient-details.html?id=${apt.patientId}" class="btn btn-sm btn-outline">
              <i class="fas fa-user"></i> View
            </a>
            <a href="chat.html?patient=${apt.patientId}" class="btn btn-sm btn-primary">
              <i class="fas fa-comment"></i> Chat
            </a>
          </div>
        </div>
      </div>
    `;
    })
    .join("");
}

// Render priority patients (high risk + unread messages)
function renderPriorityPatients() {
  const container = document.getElementById("priorityPatients");
  const storedPatients = getData("patients") || [];
  const patients =
    storedPatients.length > 0 ? storedPatients : hardcodedPatients;

  // Sort by risk level and unread messages
  const priorityPatients = patients
    .filter((p) => p.riskLevel === "high" || p.unreadMessages > 0)
    .sort((a, b) => {
      if (a.riskLevel === "high" && b.riskLevel !== "high") return -1;
      if (b.riskLevel === "high" && a.riskLevel !== "high") return 1;
      return (b.unreadMessages || 0) - (a.unreadMessages || 0);
    })
    .slice(0, 4);

  if (priorityPatients.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted);">
        <i class="fas fa-check-circle" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>All patients are doing well!</p>
      </div>
    `;
    return;
  }

  container.innerHTML = priorityPatients
    .map(
      (patient) => `
    <div class="patient-item" onclick="location.href='patient-details.html?id=${patient.id}'">
      <img src="${patient.image}" alt="${patient.name}" class="patient-avatar" />
      <div class="patient-info">
        <h4>${patient.name}</h4>
        <p>Week ${patient.gestationalAge ?? patient.pregnancyWeek} • Last visit: ${formatDate(patient.lastVisit)}</p>
      </div>
      <div class="patient-meta">
        <span class="patient-week">Week ${patient.gestationalAge ?? patient.pregnancyWeek}</span>
        <span class="risk-badge ${patient.riskLevel}">${patient.riskLevel} risk</span>
        ${patient.unreadMessages > 0 ? `<span class="status-badge pending">${patient.unreadMessages} messages</span>` : ""}
      </div>
    </div>
  `,
    )
    .join("");
}

// Render recent messages
function renderRecentMessages() {
  const container = document.getElementById("recentMessages");
  const storedPatients = getData("patients") || [];
  const patients =
    storedPatients.length > 0 ? storedPatients : hardcodedPatients;

  // Get patients with unread messages
  const patientsWithMessages = patients
    .filter((p) => p.unreadMessages > 0)
    .sort((a, b) => (b.unreadMessages || 0) - (a.unreadMessages || 0))
    .slice(0, 4);

  if (patientsWithMessages.length === 0) {
    container.innerHTML = `
      <div class="empty-state" style="text-align: center; padding: 40px; color: var(--text-muted);">
        <i class="fas fa-comments" style="font-size: 3rem; margin-bottom: 16px; opacity: 0.5;"></i>
        <p>No unread messages</p>
      </div>
    `;
    return;
  }

  container.innerHTML = patientsWithMessages
    .map(
      (patient) => `
    <div class="chat-item unread" onclick="location.href='chat.html?patient=${patient.id}'">
      <div class="avatar">
        <img src="${patient.image}" alt="${patient.name}" />
        <span class="unread-indicator"></span>
      </div>
      <div class="chat-info">
        <h4>${patient.name}</h4>
        <p>Week ${patient.gestationalAge ?? patient.pregnancyWeek} • Click to view messages</p>
      </div>
      <div class="chat-meta">
        <span>Recent</span>
        <span class="unread-count">${patient.unreadMessages}</span>
      </div>
    </div>
  `,
    )
    .join("");
}

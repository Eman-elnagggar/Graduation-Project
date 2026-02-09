// Schedule JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initDemoData();
  initSidebar();
  initNotifications();
  initCalendar();
  initScheduleData();
});

let currentDate = new Date();
let selectedDate = new Date();
let scheduleData = {};

// Initialize calendar
function initCalendar() {
  renderCalendar();

  document.getElementById("prevMonth").addEventListener("click", () => {
    currentDate.setMonth(currentDate.getMonth() - 1);
    renderCalendar();
  });

  document.getElementById("nextMonth").addEventListener("click", () => {
    currentDate.setMonth(currentDate.getMonth() + 1);
    renderCalendar();
  });
}

// Initialize schedule data
function initScheduleData() {
  const today = new Date();
  const appointments = getData("appointments") || [];

  scheduleData = getData("scheduleData") || {};

  // Generate demo schedule if empty
  if (Object.keys(scheduleData).length === 0) {
    // Add today's appointments
    const todayKey = formatDateKey(today);
    scheduleData[todayKey] = [
      {
        time: "09:00",
        duration: 30,
        patient: "Sarah Johnson",
        type: "Routine Checkup",
        status: "confirmed",
      },
      {
        time: "10:00",
        duration: 45,
        patient: "Emily Chen",
        type: "Ultrasound",
        status: "confirmed",
      },
      {
        time: "11:30",
        duration: 30,
        patient: "Maria Garcia",
        type: "Follow-up",
        status: "confirmed",
      },
      {
        time: "14:00",
        duration: 30,
        patient: "Anna Williams",
        type: "Consultation",
        status: "pending",
      },
    ];

    // Add tomorrow's appointments
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    const tomorrowKey = formatDateKey(tomorrow);
    scheduleData[tomorrowKey] = [
      {
        time: "09:30",
        duration: 30,
        patient: "Jessica Brown",
        type: "Lab Review",
        status: "confirmed",
      },
      {
        time: "11:00",
        duration: 60,
        patient: "Rachel Miller",
        type: "New Patient",
        status: "confirmed",
      },
      {
        time: "15:00",
        duration: 30,
        patient: "Lisa Anderson",
        type: "Routine Checkup",
        status: "confirmed",
      },
    ];

    // Add some future appointments
    for (let i = 2; i < 14; i++) {
      const futureDate = new Date(today);
      futureDate.setDate(futureDate.getDate() + i);
      if (
        futureDate.getDay() !== 0 &&
        futureDate.getDay() !== 6 &&
        Math.random() > 0.4
      ) {
        const key = formatDateKey(futureDate);
        const numAppts = Math.floor(Math.random() * 4) + 1;
        scheduleData[key] = generateRandomAppointments(numAppts);
      }
    }
  }

  saveData("scheduleData", scheduleData);

  renderDaySchedule(selectedDate);
}

function generateRandomAppointments(count) {
  const patients = [
    "Sarah J.",
    "Emily C.",
    "Maria G.",
    "Anna W.",
    "Jessica B.",
    "Rachel M.",
    "Lisa A.",
    "Karen D.",
  ];
  const types = [
    "Routine Checkup",
    "Ultrasound",
    "Follow-up",
    "Consultation",
    "Lab Review",
  ];
  const times = [
    "09:00",
    "09:30",
    "10:00",
    "10:30",
    "11:00",
    "11:30",
    "14:00",
    "14:30",
    "15:00",
    "15:30",
    "16:00",
  ];

  const appointments = [];
  const usedTimes = new Set();

  for (let i = 0; i < count; i++) {
    let time;
    do {
      time = times[Math.floor(Math.random() * times.length)];
    } while (usedTimes.has(time));
    usedTimes.add(time);

    appointments.push({
      time: time,
      duration: Math.random() > 0.5 ? 30 : 45,
      patient: patients[Math.floor(Math.random() * patients.length)],
      type: types[Math.floor(Math.random() * types.length)],
      status: Math.random() > 0.2 ? "confirmed" : "pending",
    });
  }

  return appointments.sort((a, b) => a.time.localeCompare(b.time));
}

// Render calendar
function renderCalendar() {
  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();

  document.getElementById("currentMonthYear").textContent =
    currentDate.toLocaleDateString("en-US", { month: "long", year: "numeric" });

  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const startPadding = firstDay.getDay();

  const container = document.getElementById("calendarDays");
  container.innerHTML = "";

  // Previous month days
  const prevMonth = new Date(year, month, 0);
  for (let i = startPadding - 1; i >= 0; i--) {
    const day = document.createElement("div");
    day.className = "calendar-day other-month";
    day.textContent = prevMonth.getDate() - i;
    container.appendChild(day);
  }

  // Current month days
  const today = new Date();
  for (let i = 1; i <= lastDay.getDate(); i++) {
    const day = document.createElement("div");
    day.className = "calendar-day";
    day.textContent = i;

    const dateObj = new Date(year, month, i);
    const dateKey = formatDateKey(dateObj);

    // Check if it's today
    if (dateObj.toDateString() === today.toDateString()) {
      day.classList.add("today");
    }

    // Check if selected
    if (dateObj.toDateString() === selectedDate.toDateString()) {
      day.classList.add("selected");
    }

    // Check if has appointments
    if (scheduleData[dateKey] && scheduleData[dateKey].length > 0) {
      day.classList.add("has-appointments");
      const dot = document.createElement("span");
      dot.className = "appointment-dot";
      day.appendChild(dot);
    }

    day.addEventListener("click", () => selectDate(dateObj));
    container.appendChild(day);
  }

  // Next month days - only fill to complete 5 rows (35 cells) if possible
  const totalCells = startPadding + lastDay.getDate();
  const rowsNeeded = Math.ceil(totalCells / 7);
  const targetCells = rowsNeeded * 7;
  const nextMonthDays = targetCells - totalCells;
  for (let i = 1; i <= nextMonthDays; i++) {
    const day = document.createElement("div");
    day.className = "calendar-day other-month";
    day.textContent = i;
    container.appendChild(day);
  }
}

// Select date
function selectDate(date) {
  selectedDate = date;
  renderCalendar();
  renderDaySchedule(date);
}

// Render day schedule
function renderDaySchedule(date) {
  const dateKey = formatDateKey(date);
  const appointments = scheduleData[dateKey] || [];
  const today = new Date();

  // Update header
  let titleText;
  if (date.toDateString() === today.toDateString()) {
    titleText = "Today's Schedule";
  } else if (
    date.toDateString() === new Date(today.getTime() + 86400000).toDateString()
  ) {
    titleText = "Tomorrow's Schedule";
  } else {
    titleText = date.toLocaleDateString("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
    });
  }

  document.getElementById("selectedDateTitle").textContent = titleText;
  document.getElementById("appointmentCount").textContent =
    `${appointments.length} appointment${appointments.length !== 1 ? "s" : ""}`;

  const container = document.getElementById("scheduleTimeline");

  if (appointments.length === 0) {
    container.innerHTML = `
            <div class="no-appointments">
                <i class="fas fa-calendar-check"></i>
                <p>No appointments scheduled</p>
                <span>Enjoy your free day!</span>
            </div>
        `;
    return;
  }

  container.innerHTML = appointments
    .map(
      (appt) => `
        <div class="schedule-item ${appt.status}">
            <div class="schedule-time">
                <span class="time">${appt.time}</span>
                <span class="duration">${appt.duration} min</span>
            </div>
            <div class="schedule-content">
                <h4>${appt.patient}</h4>
                <p>${appt.type}</p>
            </div>
            <div class="schedule-status">
                <span class="status-tag ${appt.status}">${capitalize(appt.status)}</span>
                <div class="schedule-actions">
                    <button class="action-btn" title="View Patient" onclick="viewPatient()">
                        <i class="fas fa-user"></i>
                    </button>
                    <button class="action-btn" title="Message" onclick="messagePatient()">
                        <i class="fas fa-comment"></i>
                    </button>
            ${
              appt.status !== "completed"
                ? `<button class="action-btn" title="Mark Completed" onclick="completeAppointment('${dateKey}', '${appt.time}')">
                <i class="fas fa-check"></i>
                </button>`
                : ""
            }
                </div>
            </div>
        </div>
    `,
    )
    .join("");
}

// Helper functions
function formatDateKey(date) {
  return date.toISOString().split("T")[0];
}

function capitalize(str) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

function viewPatient() {
  window.location.href = "patients.html";
}

function messagePatient() {
  window.location.href = "chat.html";
}

function completeAppointment(dateKey, time) {
  if (!scheduleData[dateKey]) return;
  const appt = scheduleData[dateKey].find((a) => a.time === time);
  if (!appt || appt.status === "completed") return;

  const confirmed = confirm(
    `Mark appointment for ${appt.patient} at ${appt.time} as completed?`,
  );
  if (!confirmed) return;

  appt.status = "completed";
  saveData("scheduleData", scheduleData);
  renderDaySchedule(selectedDate);
  renderCalendar();
}

// Sync calendar button
document
  .getElementById("syncCalendarBtn")
  ?.addEventListener("click", function () {
    showToast("Calendar sync feature coming soon", "info");
  });

/* ================================
   Availability Management JavaScript (Assistant Portal)
================================ */

let currentDate = new Date();
let selectedDate = getTodayDateString();
let selectedDoctor = "doc_001";

document.addEventListener("DOMContentLoaded", () => {
  renderDoctorSelector();
  renderCalendar();
  renderTimeSlots();
});

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
  renderCalendar();
  renderTimeSlots();
}

// Render calendar
function renderCalendar() {
  const grid = document.getElementById("calendarGrid");
  const monthDisplay = document.getElementById("currentMonth");

  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();

  // Update month display
  const monthNames = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];
  monthDisplay.textContent = `${monthNames[month]} ${year}`;

  // Get first day of month and total days
  const firstDay = new Date(year, month, 1).getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const daysInPrevMonth = new Date(year, month, 0).getDate();

  // Day headers
  const dayHeaders = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
  let html = dayHeaders
    .map((day) => `<div class="calendar-day-header">${day}</div>`)
    .join("");

  // Get slots data
  const slots = getData("availabilitySlots") || {};
  const doctorSlots = slots[selectedDoctor] || {};

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  // Previous month days
  for (let i = firstDay - 1; i >= 0; i--) {
    const day = daysInPrevMonth - i;
    html += `<div class="calendar-day other-month"><span class="day-number">${day}</span></div>`;
  }

  // Current month days
  for (let day = 1; day <= daysInMonth; day++) {
    const dateStr = `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    const date = new Date(year, month, day);
    const isToday = date.getTime() === today.getTime();
    const isSelected = dateStr === selectedDate;
    const isPast = date < today;

    // Get slot status for this day
    const daySlots = doctorSlots[dateStr] || [];
    const availableCount = daySlots.filter(
      (s) => s.status === "available"
    ).length;
    const bookedCount = daySlots.filter((s) => s.status === "booked").length;
    const totalSlots = daySlots.length;

    let dotColor = "";
    if (totalSlots > 0) {
      if (bookedCount === totalSlots) {
        dotColor = "var(--danger)";
      } else if (bookedCount > 0) {
        dotColor = "var(--warning)";
      } else if (availableCount > 0) {
        dotColor = "var(--assistant-primary)";
      }
    }

    html += `
      <div class="calendar-day ${isToday ? "today" : ""} ${isSelected ? "selected" : ""} ${isPast ? "past" : ""}" 
           onclick="${isPast ? "" : `selectCalendarDate('${dateStr}')`}"
           style="${isPast ? "cursor: not-allowed; opacity: 0.5;" : ""}">
        <span class="day-number">${day}</span>
        ${
          dotColor
            ? `
          <div class="appointment-dots">
            <span class="appointment-dot" style="background: ${dotColor};"></span>
          </div>
        `
            : ""
        }
      </div>
    `;
  }

  // Next month days
  const totalCells = firstDay + daysInMonth;
  const remainingCells = 7 - (totalCells % 7);
  if (remainingCells < 7) {
    for (let i = 1; i <= remainingCells; i++) {
      html += `<div class="calendar-day other-month"><span class="day-number">${i}</span></div>`;
    }
  }

  grid.innerHTML = html;
}

function getTodayDateString() {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

// Select date from calendar
function selectCalendarDate(dateStr) {
  selectedDate = dateStr;
  renderCalendar();
  renderTimeSlots();
}

// Navigate to previous month
function previousMonth() {
  currentDate.setMonth(currentDate.getMonth() - 1);
  renderCalendar();
}

// Navigate to next month
function nextMonth() {
  currentDate.setMonth(currentDate.getMonth() + 1);
  renderCalendar();
}

// Go to today
function goToToday() {
  currentDate = new Date();
  selectedDate = getTodayDateString();
  renderCalendar();
  renderTimeSlots();
}

// Render time slots for selected date
function renderTimeSlots() {
  const container = document.getElementById("timeSlotsContainer");
  const dateDisplay = document.getElementById("selectedDateDisplay");

  dateDisplay.textContent = formatDate(selectedDate);

  const slots = getData("availabilitySlots") || {};
  const doctorSlots = slots[selectedDoctor] || {};
  let daySlots = doctorSlots[selectedDate];

  // If no slots exist for this date, generate default slots
  if (!daySlots || daySlots.length === 0) {
    daySlots = generateDefaultSlots();
  }

  container.innerHTML = daySlots
    .map((slot, index) => {
      const timeFormatted = formatTime(slot.time);
      return `
      <div class="time-slot ${slot.status}" 
           onclick="${slot.status === "booked" ? "" : `toggleSlot(${index})`}"
           style="${slot.status === "booked" ? "cursor: not-allowed;" : ""}">
        <span class="time">${timeFormatted.time}</span>
        <span class="period">${timeFormatted.period}</span>
        <span class="status">${slot.status === "available" ? "Open" : slot.status === "booked" ? "Booked" : "Blocked"}</span>
      </div>
    `;
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

// Toggle slot status
function toggleSlot(index) {
  const slots = getData("availabilitySlots") || {};
  if (!slots[selectedDoctor]) slots[selectedDoctor] = {};
  if (!slots[selectedDoctor][selectedDate]) {
    slots[selectedDoctor][selectedDate] = generateDefaultSlots();
  }

  const currentStatus = slots[selectedDoctor][selectedDate][index].status;

  // Toggle between available and blocked (can't toggle booked)
  if (currentStatus === "available") {
    slots[selectedDoctor][selectedDate][index].status = "blocked";
  } else if (currentStatus === "blocked") {
    slots[selectedDoctor][selectedDate][index].status = "available";
  }

  saveData("availabilitySlots", slots);
  renderTimeSlots();
  renderCalendar();
}

// Set all slots as available
function setAllAvailable() {
  const slots = getData("availabilitySlots") || {};
  if (!slots[selectedDoctor]) slots[selectedDoctor] = {};
  if (!slots[selectedDoctor][selectedDate]) {
    slots[selectedDoctor][selectedDate] = generateDefaultSlots();
  }

  slots[selectedDoctor][selectedDate] = slots[selectedDoctor][selectedDate].map(
    (slot) => ({
      ...slot,
      status: slot.status === "booked" ? "booked" : "available",
    })
  );

  saveData("availabilitySlots", slots);
  renderTimeSlots();
  renderCalendar();
  showToast("All slots set to available", "success");
}

// Block all slots
function blockAllSlots() {
  const slots = getData("availabilitySlots") || {};
  if (!slots[selectedDoctor]) slots[selectedDoctor] = {};
  if (!slots[selectedDoctor][selectedDate]) {
    slots[selectedDoctor][selectedDate] = generateDefaultSlots();
  }

  slots[selectedDoctor][selectedDate] = slots[selectedDoctor][selectedDate].map(
    (slot) => ({
      ...slot,
      status: slot.status === "booked" ? "booked" : "blocked",
    })
  );

  saveData("availabilitySlots", slots);
  renderTimeSlots();
  renderCalendar();
  showToast("All slots blocked", "info");
}

// Apply quick setup
function applyQuickSetup() {
  const startTime = document.getElementById("startTime").value;
  const endTime = document.getElementById("endTime").value;
  const slotDuration = parseInt(document.getElementById("slotDuration").value);
  const weeksAhead = parseInt(document.getElementById("weeksAhead").value);

  // Get selected days
  const selectedDays = [];
  for (let i = 0; i <= 6; i++) {
    const checkbox = document.querySelector(`input[value="${i}"]`);
    if (checkbox && checkbox.checked) {
      selectedDays.push(i);
    }
  }

  if (selectedDays.length === 0) {
    showToast("Please select at least one working day", "error");
    return;
  }

  // Generate slots
  const slots = getData("availabilitySlots") || {};
  if (!slots[selectedDoctor]) slots[selectedDoctor] = {};

  const startDate = new Date();
  const endDate = new Date();
  endDate.setDate(endDate.getDate() + weeksAhead * 7);

  let daysUpdated = 0;

  for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
    const dayOfWeek = d.getDay();

    if (selectedDays.includes(dayOfWeek)) {
      const dateStr = d.toISOString().split("T")[0];
      const existingSlots = slots[selectedDoctor][dateStr] || [];
      const bookedSlots = existingSlots.filter((s) => s.status === "booked");

      // Generate new slots
      const newSlots = [];
      const [startHour, startMin] = startTime.split(":").map(Number);
      const [endHour, endMin] = endTime.split(":").map(Number);

      let currentTime = startHour * 60 + startMin;
      const endTimeMinutes = endHour * 60 + endMin;

      while (currentTime < endTimeMinutes) {
        const hour = Math.floor(currentTime / 60);
        const min = currentTime % 60;
        const timeStr = `${hour.toString().padStart(2, "0")}:${min.toString().padStart(2, "0")}`;

        // Check if this slot is already booked
        const isBooked = bookedSlots.some((s) => s.time === timeStr);

        newSlots.push({
          time: timeStr,
          status: isBooked ? "booked" : "available",
        });

        currentTime += slotDuration;
      }

      slots[selectedDoctor][dateStr] = newSlots;
      daysUpdated++;
    }
  }

  saveData("availabilitySlots", slots);
  renderCalendar();
  renderTimeSlots();
  showToast(`Schedule applied for ${daysUpdated} days`, "success");
}

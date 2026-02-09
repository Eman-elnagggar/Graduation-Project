// Patient Details JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initDemoData();
  initSidebar();
  initNotifications();
  initTabs();
  loadPatientData();
  initNoteModal();
});

let currentPatient = null;

// Hardcoded demo patient data
const hardcodedPatient = {
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
        time: "09:30 AM",
        notes: "Slightly elevated - monitoring",
      },
      {
        value: "128/82",
        date: "2026-01-15",
        time: "10:00 AM",
        notes: "Monitor closely",
      },
      {
        value: "125/80",
        date: "2026-01-08",
        time: "09:15 AM",
        notes: "Normal range",
      },
      {
        value: "122/78",
        date: "2026-01-01",
        time: "11:00 AM",
        notes: "Normal",
      },
      {
        value: "120/76",
        date: "2025-12-25",
        time: "09:45 AM",
        notes: "Normal",
      },
    ],
    bloodSugar: [
      { value: 105, date: "2026-01-20", type: "Fasting", notes: "Borderline" },
      { value: 125, date: "2026-01-15", type: "Random", notes: "Monitor" },
      { value: 98, date: "2026-01-08", type: "Fasting", notes: "Normal" },
    ],
    weight: [
      { value: 72, date: "2026-01-20" },
      { value: 71.5, date: "2026-01-15" },
      { value: 71, date: "2026-01-08" },
      { value: 70.5, date: "2026-01-01" },
    ],
  },
  tests: [
    {
      id: "t1",
      name: "Complete Blood Count",
      date: "2026-01-18",
      status: "completed",
      result: "Normal - All values within range",
    },
    {
      id: "t2",
      name: "Gestational Diabetes Screen",
      date: "2026-01-10",
      status: "completed",
      result: "Borderline - Retest recommended",
    },
    {
      id: "t3",
      name: "Non-Stress Test",
      date: "2026-01-24",
      status: "scheduled",
      result: null,
    },
    {
      id: "t4",
      name: "Group B Strep Test",
      date: "2026-01-28",
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
      notes: "Bring previous reports",
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
      notes: "Fasting test performed",
    },
    {
      date: "2025-12-28",
      type: "Ultrasound",
      time: "2:00 PM",
      status: "completed",
      notes: "Baby in breech position. Will monitor.",
    },
    {
      date: "2025-12-15",
      type: "Routine Checkup",
      time: "10:30 AM",
      status: "completed",
      notes: "All normal",
    },
  ],
  notes: [
    {
      id: "n1",
      date: "2026-01-20",
      author: "Dr. Ahmed Hassan",
      content:
        "Blood pressure slightly elevated at 130/85. Recommended dietary changes including reduced sodium intake and increased monitoring. Patient advised to rest more and avoid stress.",
      type: "visit",
    },
    {
      id: "n2",
      date: "2026-01-18",
      author: "Dr. Ahmed Hassan",
      content:
        "Week 31 checkup completed. Baby measuring on track. Discussed birth plan options. Patient prefers natural delivery if possible.",
      type: "visit",
    },
    {
      id: "n3",
      date: "2026-01-10",
      author: "Dr. Ahmed Hassan",
      content:
        "Glucose screening showed borderline results. Scheduled follow-up GTT. Discussed dietary modifications.",
      type: "followup",
    },
    {
      id: "n4",
      date: "2025-12-28",
      author: "Dr. Ahmed Hassan",
      content:
        "Baby in breech position at 28 weeks. Will continue monitoring. Discussed potential for version procedure if position doesn't change.",
      type: "alert",
    },
  ],
  riskFactors: [
    { description: "Gestational hypertension risk", severity: "medium" },
    { description: "Previous cesarean delivery", severity: "low" },
    { description: "Borderline gestational diabetes", severity: "medium" },
  ],
};

// Get patient ID from URL
function getPatientId() {
  const params = new URLSearchParams(window.location.search);
  return params.get("id");
}

// Load patient data
function loadPatientData() {
  const patientId = getPatientId();
  const patients = getData("patients") || [];
  currentPatient = patients.find((p) => p.id === patientId);

  if (!currentPatient) {
    // Default to first patient for demo
    currentPatient = patients[0];
  }

  if (!currentPatient) {
    // Use hardcoded patient as fallback
    currentPatient = hardcodedPatient;
  }

  if (!currentPatient) {
    showToast("Patient not found", "error");
    return;
  }

  renderPatientHeader();
  renderQuickStats();
  renderOverview();
  renderVitals();
  renderTests();
  renderAppointments();
  renderNotes();
  renderPrescriptions();
}

// Initialize tabs
function initTabs() {
  const tabs = document.querySelectorAll(".detail-tab");
  tabs.forEach((tab) => {
    tab.addEventListener("click", function () {
      tabs.forEach((t) => t.classList.remove("active"));
      this.classList.add("active");

      const tabId = this.dataset.tab;
      document.querySelectorAll(".tab-pane").forEach((pane) => {
        pane.classList.remove("active");
      });
      document.getElementById(`${tabId}Tab`).classList.add("active");
    });
  });
}

// Render patient header
function renderPatientHeader() {
  const avatarUrl =
    currentPatient.avatar ||
    `https://ui-avatars.com/api/?name=${encodeURIComponent(currentPatient.name)}&background=e91e8c&color=fff`;

  document.getElementById("patientAvatar").src = avatarUrl;
  document.getElementById("patientName").textContent = currentPatient.name;
  document.getElementById("patientNameBreadcrumb").textContent =
    currentPatient.name;
  document.getElementById("patientAge").textContent =
    currentPatient.age || "28";
  document.getElementById("gestationalAge").textContent =
    currentPatient.gestationalAge || "--";

  // Calculate EDD
  const weeksRemaining = 40 - (currentPatient.gestationalAge || 0);
  const edd = new Date();
  edd.setDate(edd.getDate() + weeksRemaining * 7);
  document.getElementById("eddDate").textContent = formatDateLong(edd);

  // Trimester tag
  const weeks = currentPatient.gestationalAge || 0;
  let trimester = "1st";
  if (weeks > 12 && weeks <= 28) trimester = "2nd";
  else if (weeks > 28) trimester = "3rd";
  document.getElementById("trimesterTag").textContent =
    `${trimester} Trimester`;

  // Risk level
  const riskTag = document.getElementById("riskTag");
  riskTag.textContent = `${capitalize(currentPatient.riskLevel)} Risk`;
  riskTag.className = `tag risk-${currentPatient.riskLevel}`;

  // Risk indicator
  const riskIndicator = document.getElementById("riskIndicator");
  riskIndicator.className = `risk-indicator risk-${currentPatient.riskLevel}`;
}

// Render quick stats
function renderQuickStats() {
  const patient = currentPatient || hardcodedPatient;

  // Blood Pressure
  const latestBP =
    patient.vitals?.bloodPressure?.[0] ||
    hardcodedPatient.vitals.bloodPressure[0];
  document.getElementById("lastBP").textContent = latestBP.value;
  document.getElementById("bpDate").textContent = formatDateShort(
    latestBP.date
  );

  // Blood Sugar
  const latestSugar =
    patient.vitals?.bloodSugar?.[0] || hardcodedPatient.vitals.bloodSugar[0];
  document.getElementById("lastSugar").textContent =
    `${latestSugar.value} mg/dL`;
  document.getElementById("sugarDate").textContent = formatDateShort(
    latestSugar.date
  );

  // Weight
  const latestWeight =
    patient.vitals?.weight?.[0] || hardcodedPatient.vitals.weight[0];
  document.getElementById("lastWeight").textContent =
    `${latestWeight.value} kg`;
  document.getElementById("weightDate").textContent = formatDateShort(
    latestWeight.date
  );

  // Next Appointment
  const nextAppt = patient.nextAppointment || hardcodedPatient.nextAppointment;
  const apptDate = new Date(nextAppt);
  document.getElementById("nextApptDate").textContent =
    formatDateShort(apptDate);
  document.getElementById("nextApptTime").textContent =
    patient.nextAppointmentTime || hardcodedPatient.nextAppointmentTime;
}

// Render overview tab
function renderOverview() {
  const patient = currentPatient || hardcodedPatient;

  // Pregnancy progress
  const weeks = patient.gestationalAge || hardcodedPatient.gestationalAge;
  const progress = (weeks / 40) * 100;
  document.getElementById("progressMarker").style.left =
    `${Math.min(progress, 100)}%`;
  document.getElementById("currentWeek").textContent = `Week ${weeks}`;

  const daysRemaining = (40 - weeks) * 7;
  document.getElementById("daysRemaining").textContent =
    `${Math.max(daysRemaining, 0)} days to go`;

  // Development info
  const developmentInfo = getDevelopmentInfo(weeks);
  document.getElementById("developmentInfo").textContent = developmentInfo;

  // Medical info
  document.getElementById("bloodType").textContent =
    patient.bloodType || hardcodedPatient.bloodType;
  document.getElementById("preWeight").textContent =
    `${patient.preWeight || hardcodedPatient.preWeight} kg`;
  document.getElementById("height").textContent =
    `${patient.height || hardcodedPatient.height} cm`;
  document.getElementById("gravidaPara").textContent =
    patient.gravidaPara || hardcodedPatient.gravidaPara;
  document.getElementById("allergies").textContent =
    patient.allergies || hardcodedPatient.allergies;

  // Risk factors
  const riskList = document.getElementById("riskList");
  const riskFactors =
    patient.riskFactors?.length > 0
      ? patient.riskFactors
      : hardcodedPatient.riskFactors;

  if (riskFactors && riskFactors.length > 0) {
    riskList.innerHTML = riskFactors
      .map(
        (risk) => `
            <li class="risk-item ${risk.severity}">
                <i class="fas fa-exclamation-circle"></i>
                <span>${risk.description}</span>
            </li>
        `
      )
      .join("");
  } else {
    riskList.innerHTML =
      '<li class="no-risks">No significant risk factors identified</li>';
  }

  // Recent activity
  renderActivityTimeline();
}

// Get development info based on week
function getDevelopmentInfo(weeks) {
  const info = {
    4: "The embryo is about the size of a poppy seed. Major organs are beginning to form.",
    8: "Baby is about the size of a kidney bean. Facial features are developing.",
    12: "Baby is about the size of a lime. Fingernails are forming and the baby can make fists.",
    16: "Baby is about the size of an avocado. Baby can hear sounds and make facial expressions.",
    20: "Baby is about the size of a banana. This is typically when you can learn the baby's sex.",
    24: "Baby is about the size of an ear of corn. Baby's face is almost fully formed.",
    28: "Baby is about the size of an eggplant. Baby can open and close their eyes.",
    32: "Baby is about the size of a squash. Bones are fully developed but still soft.",
    36: "Baby is about the size of a honeydew melon. Baby is getting into position for birth.",
    40: "Baby is about the size of a watermelon. Baby is fully developed and ready for birth!",
  };

  const weekKey = Math.floor(weeks / 4) * 4;
  return info[weekKey] || info[Math.min(Math.max(weekKey, 4), 40)];
}

// Render activity timeline
function renderActivityTimeline() {
  const timeline = document.getElementById("activityTimeline");
  const activities = currentPatient.activities || getDemoActivities();

  timeline.innerHTML = activities
    .slice(0, 5)
    .map(
      (activity) => `
        <div class="activity-item">
            <div class="activity-icon ${activity.type}">
                <i class="fas fa-${getActivityIcon(activity.type)}"></i>
            </div>
            <div class="activity-content">
                <p>${activity.description}</p>
                <span class="activity-time">${formatTimeAgo(activity.date)}</span>
            </div>
        </div>
    `
    )
    .join("");
}

function getDemoActivities() {
  const today = new Date();
  return [
    {
      type: "vitals",
      description: "Blood pressure recorded: 118/76",
      date: new Date(today.getTime() - 2 * 60 * 60 * 1000),
    },
    {
      type: "message",
      description: "Sent message about nutrition guidelines",
      date: new Date(today.getTime() - 24 * 60 * 60 * 1000),
    },
    {
      type: "appointment",
      description: "Completed routine checkup visit",
      date: new Date(today.getTime() - 5 * 24 * 60 * 60 * 1000),
    },
    {
      type: "test",
      description: "Lab results received: Complete Blood Count",
      date: new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000),
    },
    {
      type: "note",
      description: "Added clinical note about iron supplements",
      date: new Date(today.getTime() - 10 * 24 * 60 * 60 * 1000),
    },
  ];
}

function getActivityIcon(type) {
  const icons = {
    vitals: "heartbeat",
    message: "comment-medical",
    appointment: "calendar-check",
    test: "flask",
    note: "sticky-note",
  };
  return icons[type] || "circle";
}

// Helper to determine vital status from notes or value
function getVitalStatus(vital, type) {
  if (vital.status) return vital.status;
  const notes = (vital.notes || "").toLowerCase();
  if (notes.includes("high") || notes.includes("elevated")) return "elevated";
  if (notes.includes("low")) return "low";
  return "normal";
}

// Render vitals tab
function renderVitals() {
  const vitals =
    currentPatient?.vitals?.bloodPressure ||
    hardcodedPatient.vitals.bloodPressure;
  const tbody = document.getElementById("vitalsTableBody");

  tbody.innerHTML = vitals
    .map((vital) => {
      const status = getVitalStatus(vital, "bp");
      return `
        <tr>
            <td>${formatDateShort(vital.date)}</td>
            <td>${vital.time || "09:00 AM"}</td>
            <td>${vital.value}</td>
            <td><span class="status-badge ${status}">${capitalize(status)}</span></td>
            <td>${vital.notes || "-"}</td>
        </tr>
    `;
    })
    .join("");

  // Vital type change handler
  document.getElementById("vitalType")?.addEventListener("change", function () {
    updateVitalsTable(this.value);
  });
}
function getDemoVitals() {
  return [
    { date: new Date(), value: "118/76", status: "normal", time: "09:30 AM" },
    {
      date: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000),
      value: "120/78",
      status: "normal",
      time: "10:00 AM",
    },
    {
      date: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
      value: "122/80",
      status: "normal",
      time: "09:15 AM",
    },
    {
      date: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000),
      value: "125/82",
      status: "elevated",
      time: "11:00 AM",
      notes: "Slight elevation after activity",
    },
    {
      date: new Date(Date.now() - 21 * 24 * 60 * 60 * 1000),
      value: "118/75",
      status: "normal",
      time: "09:45 AM",
    },
  ];
}

function updateVitalsTable(vitalType) {
  // In production, this would update the table based on vital type
  showToast(`Showing ${vitalType} readings`, "info");
}

// Render tests tab
function renderTests() {
  const tests = currentPatient?.tests || hardcodedPatient.tests;
  const container = document.getElementById("testsList");

  if (!tests || tests.length === 0) {
    container.innerHTML = `
      <div class="empty-tests">
        <i class="fas fa-flask"></i>
        <p>No tests recorded yet</p>
      </div>
    `;
    return;
  }

  container.innerHTML = tests
    .map(
      (test, index) => `
        <div class="test-card ${test.status}">
            <div class="test-header">
                <div class="test-info">
                    <h4>${test.name}</h4>
                    <span class="test-date">${formatDateShort(test.date)}</span>
                </div>
                <span class="test-status ${test.status}">${capitalize(test.status)}</span>
            </div>
            <div class="test-body">
                ${
                  test.results && test.results.length > 0
                    ? `
                    <div class="test-results">
                        ${test.results
                          .map(
                            (r) => `
                            <div class="result-item">
                                <span class="result-name">${r.name}</span>
                                <span class="result-value ${r.status || "normal"}">${r.value}</span>
                                <span class="result-range">${r.range || ""}</span>
                            </div>
                        `
                          )
                          .join("")}
                    </div>
                `
                    : test.result
                      ? `<p class="test-result-simple"><strong>Result:</strong> ${test.result}</p>`
                      : '<p class="pending-notice">Results pending...</p>'
                }
            </div>
            ${
              test.status === "completed"
                ? `
                <div class="test-footer">
                    <button class="btn btn-sm btn-outline" onclick="viewFullResults('${test.id || index}')">
                        <i class="fas fa-eye"></i> View Full Results
                    </button>
                    <button class="btn btn-sm btn-outline" onclick="downloadResults('${test.id || index}')">
                        <i class="fas fa-download"></i> Download
                    </button>
                </div>
            `
                : ""
            }
        </div>
    `
    )
    .join("");
}

function getDemoTests() {
  return [
    {
      id: "test1",
      name: "Complete Blood Count (CBC)",
      date: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
      status: "completed",
      results: [
        {
          name: "Hemoglobin",
          value: "11.8 g/dL",
          range: "11.0-14.0",
          status: "normal",
        },
        { name: "Hematocrit", value: "35%", range: "33-44%", status: "normal" },
        {
          name: "White Blood Cells",
          value: "8,500/μL",
          range: "4,500-11,000",
          status: "normal",
        },
        {
          name: "Platelets",
          value: "250,000/μL",
          range: "150,000-400,000",
          status: "normal",
        },
      ],
    },
    {
      id: "test2",
      name: "Glucose Tolerance Test",
      date: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000),
      status: "completed",
      results: [
        { name: "Fasting", value: "85 mg/dL", range: "<95", status: "normal" },
        { name: "1 Hour", value: "168 mg/dL", range: "<180", status: "normal" },
        { name: "2 Hour", value: "142 mg/dL", range: "<155", status: "normal" },
      ],
    },
    {
      id: "test3",
      name: "Ultrasound - Anatomy Scan",
      date: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000),
      status: "pending",
    },
  ];
}

// Render appointments tab
function renderAppointments() {
  const appointments =
    currentPatient?.appointments || hardcodedPatient.appointments;
  const container = document.getElementById("appointmentsList");

  container.innerHTML = appointments
    .map(
      (appt) => `
        <div class="appointment-history-card ${appt.status}">
            <div class="appt-date-col">
                <span class="appt-month">${formatMonth(appt.date)}</span>
                <span class="appt-day">${formatDay(appt.date)}</span>
                <span class="appt-year">${formatYear(appt.date)}</span>
            </div>
            <div class="appt-details-col">
                <h4>${appt.type}</h4>
                <p><i class="fas fa-clock"></i> ${appt.time}</p>
                ${appt.notes ? `<p class="appt-notes">${appt.notes}</p>` : ""}
            </div>
            <div class="appt-status-col">
                <span class="appt-status-tag ${appt.status}">${capitalize(appt.status)}</span>
            </div>
        </div>
    `
    )
    .join("");

  // Filter buttons
  document
    .querySelectorAll(".appointment-filters .filter-btn")
    .forEach((btn) => {
      btn.addEventListener("click", function () {
        document
          .querySelectorAll(".appointment-filters .filter-btn")
          .forEach((b) => b.classList.remove("active"));
        this.classList.add("active");
        filterAppointments(this.dataset.filter);
      });
    });
}

function getDemoAppointmentHistory() {
  return [
    {
      date: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
      type: "Routine Checkup",
      time: "10:00 AM",
      status: "upcoming",
    },
    {
      date: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000),
      type: "Ultrasound",
      time: "11:30 AM",
      status: "completed",
      notes: "Normal development observed",
    },
    {
      date: new Date(Date.now() - 28 * 24 * 60 * 60 * 1000),
      type: "Routine Checkup",
      time: "09:00 AM",
      status: "completed",
      notes: "All vitals normal",
    },
    {
      date: new Date(Date.now() - 42 * 24 * 60 * 60 * 1000),
      type: "Lab Work",
      time: "08:00 AM",
      status: "completed",
    },
  ];
}

function filterAppointments(filter) {
  const cards = document.querySelectorAll(".appointment-history-card");
  cards.forEach((card) => {
    if (filter === "all" || card.classList.contains(filter)) {
      card.style.display = "flex";
    } else {
      card.style.display = "none";
    }
  });
}

// Render notes tab
function renderNotes() {
  const notes = currentPatient?.notes || hardcodedPatient.notes;
  const container = document.getElementById("notesList");

  if (notes.length === 0) {
    container.innerHTML = `
            <div class="empty-notes">
                <i class="fas fa-sticky-note"></i>
                <p>No clinical notes yet</p>
            </div>
        `;
    return;
  }

  container.innerHTML = notes
    .map(
      (note) => `
        <div class="note-card ${note.type}">
            <div class="note-header">
                <span class="note-type ${note.type}">
                    <i class="fas fa-${getNoteIcon(note.type)}"></i>
                    ${formatNoteType(note.type)}
                </span>
                <span class="note-date">${formatDateLong(note.date)}</span>
                ${note.private ? '<span class="private-badge"><i class="fas fa-lock"></i> Private</span>' : ""}
            </div>
            <div class="note-content">
                <p>${note.content}</p>
            </div>
            <div class="note-footer">
                <span class="note-author">By ${note.author || "Dr. Sarah"}</span>
            </div>
        </div>
    `
    )
    .join("");
}

function getDemoNotes() {
  return [
    {
      id: "note1",
      type: "visit",
      date: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000),
      content:
        "Routine checkup completed. Patient reports feeling well with normal energy levels. Discussed nutrition and exercise guidelines for second trimester. Recommended continuing prenatal vitamins.",
      author: "Dr. Sarah",
      private: false,
    },
    {
      id: "note2",
      type: "followup",
      date: new Date(Date.now() - 21 * 24 * 60 * 60 * 1000),
      content:
        "Follow-up needed for iron levels. Schedule blood work in 2 weeks to monitor hemoglobin.",
      author: "Dr. Sarah",
      private: false,
    },
    {
      id: "note3",
      type: "general",
      date: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
      content:
        "Patient expressed concerns about travel during pregnancy. Discussed safety guidelines for flying during second trimester.",
      author: "Dr. Sarah",
      private: true,
    },
  ];
}

function getNoteIcon(type) {
  const icons = {
    general: "sticky-note",
    visit: "stethoscope",
    phone: "phone",
    alert: "exclamation-triangle",
    followup: "clock",
  };
  return icons[type] || "sticky-note";
}

function formatNoteType(type) {
  const labels = {
    general: "General Note",
    visit: "Visit Summary",
    phone: "Phone Consultation",
    alert: "Alert",
    followup: "Follow-up",
  };
  return labels[type] || "Note";
}

// Note modal functions
function initNoteModal() {
  document
    .getElementById("addNoteBtn")
    ?.addEventListener("click", openNoteModal);
  document
    .getElementById("addNoteBtn2")
    ?.addEventListener("click", openNoteModal);
}

function openNoteModal() {
  document.getElementById("noteModal").classList.add("active");
}

function closeNoteModal() {
  document.getElementById("noteModal").classList.remove("active");
  document.getElementById("noteForm").reset();
}

function saveNote() {
  const type = document.getElementById("noteType").value;
  const content = document.getElementById("noteContent").value;
  const isPrivate = document.getElementById("notePrivate").checked;

  if (!content.trim()) {
    showToast("Please enter note content", "error");
    return;
  }

  const newNote = {
    id: `note_${Date.now()}`,
    type: type,
    date: new Date(),
    content: content,
    author: "Dr. Sarah",
    private: isPrivate,
  };

  // In production, save to backend
  if (!currentPatient.notes) currentPatient.notes = [];
  currentPatient.notes.unshift(newNote);

  closeNoteModal();
  renderNotes();
  showToast("Note added successfully", "success");
}

// Action functions
function openChatWithPatient() {
  window.location.href = `chat.html?patient=${currentPatient?.id}`;
}

function scheduleAppointment() {
  showToast("Schedule feature - would open scheduling modal", "info");
}

function viewFullResults(testId) {
  showToast("Would open full test results view", "info");
}

function downloadResults(testId) {
  showToast("Download feature - would download PDF results", "info");
}

// Utility functions
function capitalize(str) {
  return str?.charAt(0).toUpperCase() + str?.slice(1) || "";
}

function formatDateLong(date) {
  return new Date(date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
}

function formatDateShort(date) {
  return new Date(date).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
  });
}

function formatMonth(date) {
  return new Date(date).toLocaleDateString("en-US", { month: "short" });
}

function formatDay(date) {
  return new Date(date).getDate();
}

function formatYear(date) {
  return new Date(date).getFullYear();
}

function formatTimeAgo(date) {
  const now = new Date();
  const diff = now - new Date(date);
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);

  if (minutes < 60) return `${minutes}m ago`;
  if (hours < 24) return `${hours}h ago`;
  if (days < 7) return `${days}d ago`;
  return formatDateShort(date);
}

// Close modal on overlay click
document.getElementById("noteModal")?.addEventListener("click", function (e) {
  if (e.target === this) closeNoteModal();
});

// ==========================================
// Upload Test for AI Analysis Functions
// ==========================================

let uploadedTestFile = null;
let currentTestResults = null;

function openUploadTestModal() {
  document.getElementById("uploadTestModal").classList.add("active");
  resetUploadTestModal();
}

function closeUploadTestModal() {
  document.getElementById("uploadTestModal").classList.remove("active");
  resetUploadTestModal();
}

function resetUploadTestModal() {
  document.getElementById("testTypeSelect").value = "";
  document.getElementById("testFileInput").value = "";
  document.getElementById("uploadedFileInfo").style.display = "none";
  document.getElementById("aiResultsSection").style.display = "none";
  document.getElementById("analyzeTestBtn").disabled = true;
  document.getElementById("analyzeTestBtn").style.display = "inline-flex";
  document.getElementById("saveTestResultBtn").style.display = "none";
  uploadedTestFile = null;
  currentTestResults = null;
}

// Initialize upload zone event listeners
document.addEventListener("DOMContentLoaded", function () {
  const uploadZone = document.getElementById("uploadZone");
  const fileInput = document.getElementById("testFileInput");

  if (uploadZone && fileInput) {
    // Click to upload
    uploadZone.addEventListener("click", () => fileInput.click());

    // Drag and drop
    uploadZone.addEventListener("dragover", (e) => {
      e.preventDefault();
      uploadZone.classList.add("drag-over");
    });

    uploadZone.addEventListener("dragleave", () => {
      uploadZone.classList.remove("drag-over");
    });

    uploadZone.addEventListener("drop", (e) => {
      e.preventDefault();
      uploadZone.classList.remove("drag-over");
      const files = e.dataTransfer.files;
      if (files.length > 0) {
        handleTestFileUpload(files[0]);
      }
    });

    // File input change
    fileInput.addEventListener("change", (e) => {
      if (e.target.files.length > 0) {
        handleTestFileUpload(e.target.files[0]);
      }
    });
  }

  // Close modals on overlay click
  document
    .getElementById("uploadTestModal")
    ?.addEventListener("click", function (e) {
      if (e.target === this) closeUploadTestModal();
    });

  document
    .getElementById("prescriptionModal")
    ?.addEventListener("click", function (e) {
      if (e.target === this) closePrescriptionModal();
    });

  document
    .getElementById("prescriptionPreviewModal")
    ?.addEventListener("click", function (e) {
      if (e.target === this) closePrescriptionPreview();
    });
});

function handleTestFileUpload(file) {
  // Validate file type
  const validTypes = [
    "application/pdf",
    "image/jpeg",
    "image/jpg",
    "image/png",
    "application/dicom",
  ];
  const validExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".dcm"];
  const fileExtension = "." + file.name.split(".").pop().toLowerCase();

  if (!validExtensions.includes(fileExtension)) {
    showToast(
      "Invalid file type. Please upload PDF, JPG, PNG, or DICOM files.",
      "error"
    );
    return;
  }

  // Validate file size (max 10MB)
  if (file.size > 10 * 1024 * 1024) {
    showToast("File too large. Maximum size is 10MB.", "error");
    return;
  }

  uploadedTestFile = file;
  document.getElementById("uploadedFileName").textContent = file.name;
  document.getElementById("uploadedFileInfo").style.display = "flex";
  document.getElementById("uploadZone").style.display = "none";
  updateAnalyzeButton();
}

function removeUploadedFile() {
  uploadedTestFile = null;
  document.getElementById("testFileInput").value = "";
  document.getElementById("uploadedFileInfo").style.display = "none";
  document.getElementById("uploadZone").style.display = "block";
  updateAnalyzeButton();
}

function updateAnalyzeButton() {
  const testType = document.getElementById("testTypeSelect").value;
  document.getElementById("analyzeTestBtn").disabled =
    !uploadedTestFile || !testType;
}

// Add event listener for test type change
document.addEventListener("DOMContentLoaded", function () {
  document
    .getElementById("testTypeSelect")
    ?.addEventListener("change", updateAnalyzeButton);
});

function analyzeTest() {
  const testType = document.getElementById("testTypeSelect").value;

  if (!uploadedTestFile || !testType) {
    showToast("Please select test type and upload a file", "error");
    return;
  }

  // Show loading state
  const analyzeBtn = document.getElementById("analyzeTestBtn");
  analyzeBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Analyzing...';
  analyzeBtn.disabled = true;

  // Simulate AI analysis (in production, this would call your AI backend)
  setTimeout(() => {
    currentTestResults = generateMockAIResults(testType);
    displayAIResults(currentTestResults);

    analyzeBtn.style.display = "none";
    document.getElementById("saveTestResultBtn").style.display = "inline-flex";
  }, 2000);
}

function generateMockAIResults(testType) {
  const resultsMap = {
    cbc: {
      summary:
        "Complete Blood Count analysis complete. Most values within normal range with minor variations.",
      recommendations: [
        "Hemoglobin levels are slightly low - consider iron supplementation",
        "Monitor for signs of mild anemia",
        "Retest in 4 weeks to confirm values",
      ],
      results: [
        {
          variable: "Hemoglobin",
          value: "11.2 g/dL",
          range: "12.0-15.5 g/dL",
          status: "low",
        },
        {
          variable: "Hematocrit",
          value: "34%",
          range: "36-44%",
          status: "low",
        },
        {
          variable: "WBC Count",
          value: "7,500/µL",
          range: "4,500-11,000/µL",
          status: "normal",
        },
        {
          variable: "Platelet Count",
          value: "250,000/µL",
          range: "150,000-400,000/µL",
          status: "normal",
        },
        {
          variable: "RBC Count",
          value: "4.2 M/µL",
          range: "4.0-5.5 M/µL",
          status: "normal",
        },
        {
          variable: "MCV",
          value: "82 fL",
          range: "80-100 fL",
          status: "normal",
        },
      ],
    },
    lipid: {
      summary:
        "Lipid Panel analysis complete. Cholesterol levels need attention.",
      recommendations: [
        "LDL cholesterol elevated - dietary modifications recommended",
        "Increase physical activity",
        "Consider statin therapy if lifestyle changes insufficient",
        "Recheck lipids in 3 months",
      ],
      results: [
        {
          variable: "Total Cholesterol",
          value: "235 mg/dL",
          range: "<200 mg/dL",
          status: "high",
        },
        {
          variable: "LDL Cholesterol",
          value: "155 mg/dL",
          range: "<100 mg/dL",
          status: "high",
        },
        {
          variable: "HDL Cholesterol",
          value: "48 mg/dL",
          range: ">60 mg/dL",
          status: "low",
        },
        {
          variable: "Triglycerides",
          value: "160 mg/dL",
          range: "<150 mg/dL",
          status: "high",
        },
        {
          variable: "VLDL",
          value: "32 mg/dL",
          range: "5-40 mg/dL",
          status: "normal",
        },
      ],
    },
    glucose: {
      summary:
        "Blood Glucose analysis complete. Results indicate prediabetic range.",
      recommendations: [
        "HbA1c in prediabetic range - lifestyle intervention recommended",
        "Implement low glycemic index diet",
        "Regular exercise (150 min/week)",
        "Monitor blood glucose regularly",
        "Schedule follow-up in 3 months",
      ],
      results: [
        {
          variable: "Fasting Glucose",
          value: "118 mg/dL",
          range: "<100 mg/dL",
          status: "high",
        },
        { variable: "HbA1c", value: "6.1%", range: "<5.7%", status: "high" },
        {
          variable: "Random Glucose",
          value: "145 mg/dL",
          range: "<140 mg/dL",
          status: "high",
        },
        {
          variable: "Insulin (Fasting)",
          value: "15 µU/mL",
          range: "2-25 µU/mL",
          status: "normal",
        },
      ],
    },
    thyroid: {
      summary:
        "Thyroid Function analysis complete. Mild hypothyroidism detected.",
      recommendations: [
        "TSH slightly elevated suggesting subclinical hypothyroidism",
        "Consider thyroid hormone supplementation",
        "Repeat test in 6-8 weeks",
        "Screen for thyroid antibodies",
      ],
      results: [
        {
          variable: "TSH",
          value: "5.8 mIU/L",
          range: "0.4-4.0 mIU/L",
          status: "high",
        },
        {
          variable: "Free T4",
          value: "0.9 ng/dL",
          range: "0.8-1.8 ng/dL",
          status: "normal",
        },
        {
          variable: "Free T3",
          value: "2.5 pg/mL",
          range: "2.3-4.2 pg/mL",
          status: "normal",
        },
        {
          variable: "T4 Total",
          value: "6.5 µg/dL",
          range: "4.5-12.0 µg/dL",
          status: "normal",
        },
      ],
    },
  };

  // Default results for other test types
  const defaultResults = {
    summary:
      "Test analysis complete. Results have been processed by AI for interpretation.",
    recommendations: [
      "Results within expected parameters",
      "Continue current management plan",
      "Schedule follow-up as appropriate",
    ],
    results: [
      {
        variable: "Parameter 1",
        value: "Normal",
        range: "Reference Range",
        status: "normal",
      },
      {
        variable: "Parameter 2",
        value: "Normal",
        range: "Reference Range",
        status: "normal",
      },
      {
        variable: "Parameter 3",
        value: "Normal",
        range: "Reference Range",
        status: "normal",
      },
    ],
  };

  return resultsMap[testType] || defaultResults;
}

function displayAIResults(results) {
  const aiResultsSection = document.getElementById("aiResultsSection");
  const aiSummary = document.getElementById("aiSummary");
  const resultsTableBody = document.getElementById("resultsTableBody");
  const aiRecommendations = document.getElementById("aiRecommendations");

  // Display summary
  aiSummary.innerHTML = `
    <div class="ai-summary-content">
      <i class="fas fa-info-circle"></i>
      <p>${results.summary}</p>
    </div>
  `;

  // Display results table
  resultsTableBody.innerHTML = results.results
    .map(
      (r) => `
    <tr>
      <td>${r.variable}</td>
      <td><strong>${r.value}</strong></td>
      <td>${r.range}</td>
      <td><span class="status-badge ${r.status}">${capitalize(r.status)}</span></td>
    </tr>
  `
    )
    .join("");

  // Display recommendations
  aiRecommendations.innerHTML = `
    <h4><i class="fas fa-lightbulb"></i> AI Recommendations</h4>
    <ul>
      ${results.recommendations.map((rec) => `<li>${rec}</li>`).join("")}
    </ul>
  `;

  aiResultsSection.style.display = "block";
  showToast("AI analysis complete!", "success");
}

function saveTestToPatient() {
  if (!currentTestResults || !currentPatient) {
    showToast("No results to save", "error");
    return;
  }

  const testType = document.getElementById("testTypeSelect");
  const testName = testType.options[testType.selectedIndex].text;

  const newTest = {
    id: `t_${Date.now()}`,
    name: testName,
    date: new Date().toISOString().split("T")[0],
    status: "completed",
    result: currentTestResults.summary,
    aiAnalysis: currentTestResults,
    uploadedBy: "Doctor",
    fileName: uploadedTestFile?.name,
  };

  // Add to patient's tests
  if (!currentPatient.tests) currentPatient.tests = [];
  currentPatient.tests.unshift(newTest);

  closeUploadTestModal();
  renderTests();
  showToast("Test results saved to patient record", "success");
}

// ==========================================
// Prescription Writing Functions
// ==========================================

let medicationCount = 1;

function openPrescriptionModal() {
  document.getElementById("prescriptionModal").classList.add("active");
  document.getElementById("rxDate").textContent = new Date().toLocaleDateString(
    "en-US",
    {
      year: "numeric",
      month: "long",
      day: "numeric",
    }
  );
  document.getElementById("rxPatientName").textContent =
    currentPatient?.name || "Patient";
  resetPrescriptionForm();
}

function closePrescriptionModal() {
  document.getElementById("prescriptionModal").classList.remove("active");
  resetPrescriptionForm();
}

function resetPrescriptionForm() {
  document.getElementById("prescriptionForm").reset();
  medicationCount = 1;

  // Reset to single medication entry
  const medicationsList = document.getElementById("medicationsList");
  medicationsList.innerHTML = getMedicationEntryHTML(0);
}

function getMedicationEntryHTML(index) {
  return `
    <div class="medication-entry" data-index="${index}">
      <div class="medication-header">
        <h4>Medication #${index + 1}</h4>
        <button type="button" class="btn-icon remove-medication" onclick="removeMedication(${index})" style="${index === 0 ? "display: none;" : ""}">
          <i class="fas fa-trash"></i>
        </button>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>Medication Name *</label>
          <input type="text" class="medication-name" placeholder="e.g., Metformin" required />
        </div>
        <div class="form-group">
          <label>Dosage *</label>
          <input type="text" class="medication-dosage" placeholder="e.g., 500mg" required />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>Frequency *</label>
          <select class="medication-frequency" required>
            <option value="">Select frequency...</option>
            <option value="once-daily">Once daily</option>
            <option value="twice-daily">Twice daily</option>
            <option value="three-daily">Three times daily</option>
            <option value="four-daily">Four times daily</option>
            <option value="as-needed">As needed (PRN)</option>
            <option value="every-4-hours">Every 4 hours</option>
            <option value="every-6-hours">Every 6 hours</option>
            <option value="every-8-hours">Every 8 hours</option>
            <option value="weekly">Weekly</option>
            <option value="other">Other</option>
          </select>
        </div>
        <div class="form-group">
          <label>Duration *</label>
          <input type="text" class="medication-duration" placeholder="e.g., 30 days, 2 weeks" required />
        </div>
      </div>
      <div class="form-group">
        <label>Route of Administration</label>
        <select class="medication-route">
          <option value="oral">Oral</option>
          <option value="topical">Topical</option>
          <option value="injection">Injection</option>
          <option value="inhalation">Inhalation</option>
          <option value="sublingual">Sublingual</option>
          <option value="rectal">Rectal</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div class="form-group">
        <label>Special Instructions</label>
        <textarea class="medication-instructions" rows="2" placeholder="e.g., Take with food, Avoid alcohol..."></textarea>
      </div>
    </div>
  `;
}

function addMedication() {
  const medicationsList = document.getElementById("medicationsList");
  medicationsList.insertAdjacentHTML(
    "beforeend",
    getMedicationEntryHTML(medicationCount)
  );
  medicationCount++;

  // Show all remove buttons except the first one
  updateRemoveButtons();
}

function removeMedication(index) {
  const entries = document.querySelectorAll(".medication-entry");
  if (entries.length > 1) {
    entries.forEach((entry, i) => {
      if (parseInt(entry.dataset.index) === index) {
        entry.remove();
      }
    });
    updateMedicationNumbers();
  }
}

function updateMedicationNumbers() {
  const entries = document.querySelectorAll(".medication-entry");
  entries.forEach((entry, index) => {
    entry.dataset.index = index;
    entry.querySelector("h4").textContent = `Medication #${index + 1}`;
    const removeBtn = entry.querySelector(".remove-medication");
    if (removeBtn) {
      removeBtn.onclick = () => removeMedication(index);
      removeBtn.style.display =
        index === 0 && entries.length === 1 ? "none" : "";
    }
  });
  medicationCount = entries.length;
}

function updateRemoveButtons() {
  const entries = document.querySelectorAll(".medication-entry");
  entries.forEach((entry, index) => {
    const removeBtn = entry.querySelector(".remove-medication");
    if (removeBtn) {
      removeBtn.style.display = entries.length > 1 ? "" : "none";
    }
  });
}

function collectPrescriptionData() {
  const medications = [];
  const entries = document.querySelectorAll(".medication-entry");

  entries.forEach((entry) => {
    const name = entry.querySelector(".medication-name").value.trim();
    const dosage = entry.querySelector(".medication-dosage").value.trim();
    const frequency = entry.querySelector(".medication-frequency").value;
    const duration = entry.querySelector(".medication-duration").value.trim();
    const route = entry.querySelector(".medication-route").value;
    const instructions = entry
      .querySelector(".medication-instructions")
      .value.trim();

    if (name && dosage && frequency && duration) {
      medications.push({
        name,
        dosage,
        frequency: getFrequencyText(frequency),
        duration,
        route: capitalize(route),
        instructions,
      });
    }
  });

  return {
    patientName: currentPatient?.name || "Patient",
    date: new Date().toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    }),
    medications,
    generalNotes: document.getElementById("rxGeneralNotes").value.trim(),
    refills: document.getElementById("rxRefills").value,
  };
}

function getFrequencyText(value) {
  const frequencyMap = {
    "once-daily": "Once daily",
    "twice-daily": "Twice daily",
    "three-daily": "Three times daily",
    "four-daily": "Four times daily",
    "as-needed": "As needed (PRN)",
    "every-4-hours": "Every 4 hours",
    "every-6-hours": "Every 6 hours",
    "every-8-hours": "Every 8 hours",
    weekly: "Weekly",
    other: "Other",
  };
  return frequencyMap[value] || value;
}

function previewPrescription() {
  const data = collectPrescriptionData();

  if (data.medications.length === 0) {
    showToast("Please add at least one medication", "error");
    return;
  }

  const previewContent = document.getElementById("prescriptionPreviewContent");
  previewContent.innerHTML = `
    <div class="rx-preview">
      <div class="rx-header">
        <div class="rx-logo">
          <i class="fas fa-hospital"></i>
          <h2>MaternaCare Clinic</h2>
        </div>
        <div class="rx-symbol">℞</div>
      </div>
      
      <div class="rx-info">
        <p><strong>Patient:</strong> ${data.patientName}</p>
        <p><strong>Date:</strong> ${data.date}</p>
        <p><strong>Prescriber:</strong> Dr. Ahmed Hassan</p>
      </div>
      
      <div class="rx-medications">
        ${data.medications
          .map(
            (med, i) => `
          <div class="rx-medication-item">
            <p class="rx-med-name"><strong>${i + 1}. ${med.name}</strong> - ${med.dosage}</p>
            <p class="rx-med-details">
              <span><i class="fas fa-clock"></i> ${med.frequency}</span>
              <span><i class="fas fa-calendar"></i> ${med.duration}</span>
              <span><i class="fas fa-route"></i> ${med.route}</span>
            </p>
            ${med.instructions ? `<p class="rx-med-instructions"><i class="fas fa-info-circle"></i> ${med.instructions}</p>` : ""}
          </div>
        `
          )
          .join("")}
      </div>
      
      ${
        data.generalNotes
          ? `
        <div class="rx-notes">
          <p><strong>Instructions:</strong> ${data.generalNotes}</p>
        </div>
      `
          : ""
      }
      
      <div class="rx-footer">
        <p><strong>Refills:</strong> ${data.refills === "0" ? "No refills" : data.refills + " refill(s)"}</p>
        <div class="rx-signature">
          <div class="signature-line"></div>
          <p>Physician Signature</p>
        </div>
      </div>
    </div>
  `;

  document.getElementById("prescriptionPreviewModal").classList.add("active");
}

function closePrescriptionPreview() {
  document
    .getElementById("prescriptionPreviewModal")
    .classList.remove("active");
}

function savePrescription() {
  const data = collectPrescriptionData();

  if (data.medications.length === 0) {
    showToast("Please add at least one medication", "error");
    return;
  }

  const prescription = {
    id: `rx_${Date.now()}`,
    date: new Date().toISOString(),
    medications: data.medications,
    generalNotes: data.generalNotes,
    refills: data.refills,
    prescriber: "Dr. Ahmed Hassan",
    status: "active",
  };

  // Save to patient's prescriptions
  if (!currentPatient.prescriptions) currentPatient.prescriptions = [];
  currentPatient.prescriptions.unshift(prescription);

  closePrescriptionModal();
  showToast("Prescription saved successfully", "success");

  // Optionally add to notes as well
  const prescriptionNote = {
    id: `note_${Date.now()}`,
    type: "general",
    date: new Date(),
    content: `New prescription issued: ${data.medications.map((m) => `${m.name} ${m.dosage} (${m.frequency})`).join(", ")}`,
    author: "Dr. Ahmed Hassan",
    private: false,
  };

  if (!currentPatient.notes) currentPatient.notes = [];
  currentPatient.notes.unshift(prescriptionNote);
  renderNotes();
}

function printPrescription() {
  const previewContent = document.getElementById(
    "prescriptionPreviewContent"
  ).innerHTML;
  const printWindow = window.open("", "_blank");
  printWindow.document.write(`
    <!DOCTYPE html>
    <html>
    <head>
      <title>Prescription - ${currentPatient?.name || "Patient"}</title>
      <style>
        body { font-family: Arial, sans-serif; padding: 40px; max-width: 800px; margin: 0 auto; }
        .rx-preview { border: 2px solid #333; padding: 30px; border-radius: 8px; }
        .rx-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 20px; }
        .rx-logo h2 { margin: 0; color: #0077b6; }
        .rx-symbol { font-size: 48px; color: #0077b6; font-weight: bold; }
        .rx-info { margin-bottom: 20px; }
        .rx-info p { margin: 5px 0; }
        .rx-medications { margin: 20px 0; padding: 20px; background: #f5f5f5; border-radius: 8px; }
        .rx-medication-item { margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #ddd; }
        .rx-medication-item:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }
        .rx-med-name { margin: 0 0 5px 0; font-size: 16px; }
        .rx-med-details { margin: 5px 0; color: #666; }
        .rx-med-details span { margin-right: 20px; }
        .rx-med-instructions { margin: 5px 0; color: #444; font-style: italic; }
        .rx-notes { margin: 20px 0; padding: 15px; background: #fff3cd; border-radius: 8px; }
        .rx-footer { margin-top: 30px; display: flex; justify-content: space-between; align-items: flex-end; }
        .signature-line { width: 200px; border-bottom: 1px solid #333; margin-bottom: 5px; }
        .rx-signature p { margin: 0; font-size: 12px; color: #666; }
        @media print { body { padding: 20px; } }
      </style>
    </head>
    <body>
      ${previewContent}
      <script>window.onload = function() { window.print(); }</script>
    </body>
    </html>
  `);
  printWindow.document.close();
}

// ==========================================
// Saved Prescriptions Functions
// ==========================================

// Demo saved prescriptions data
const savedPrescriptions = {
  rx_1: {
    id: "rx_1",
    date: "January 18, 2026",
    status: "active",
    prescriber: "Dr. Ahmed Hassan",
    refills: 3,
    medications: [
      {
        name: "Prenatal Vitamins",
        dosage: "1 tablet",
        frequency: "Once daily",
        duration: "30 days",
        route: "Oral",
        instructions: "Take with food",
      },
      {
        name: "Iron Supplement (Ferrous Sulfate)",
        dosage: "325mg",
        frequency: "Once daily",
        duration: "30 days",
        route: "Oral",
        instructions:
          "Take on empty stomach with vitamin C for better absorption",
      },
    ],
    generalNotes:
      "Continue taking until delivery. May cause mild constipation.",
  },
  rx_2: {
    id: "rx_2",
    date: "January 10, 2026",
    status: "active",
    prescriber: "Dr. Ahmed Hassan",
    refills: 2,
    medications: [
      {
        name: "Metformin",
        dosage: "500mg",
        frequency: "Twice daily",
        duration: "30 days",
        route: "Oral",
        instructions: "Take with meals to reduce GI side effects",
      },
    ],
    generalNotes:
      "For gestational diabetes management. Monitor blood sugar regularly.",
  },
  rx_3: {
    id: "rx_3",
    date: "December 15, 2025",
    status: "completed",
    prescriber: "Dr. Ahmed Hassan",
    refills: 0,
    medications: [
      {
        name: "Ondansetron (Zofran)",
        dosage: "4mg",
        frequency: "As needed",
        duration: "14 days",
        route: "Oral",
        instructions: "For nausea relief. Do not exceed 3 doses per day.",
      },
    ],
    generalNotes: "For morning sickness. Discontinue if symptoms resolve.",
  },
};

function viewPrescription(prescriptionId) {
  const prescription =
    savedPrescriptions[prescriptionId] ||
    currentPatient?.prescriptions?.find((p) => p.id === prescriptionId);

  if (!prescription) {
    showToast("Prescription not found", "error");
    return;
  }

  const previewContent = document.getElementById("prescriptionPreviewContent");
  previewContent.innerHTML = generatePrescriptionPreviewHTML(prescription);
  document.getElementById("prescriptionPreviewModal").classList.add("active");
}

function generatePrescriptionPreviewHTML(prescription) {
  const medications = prescription.medications || [];

  return `
    <div class="rx-preview">
      <div class="rx-header">
        <div class="rx-logo">
          <i class="fas fa-hospital"></i>
          <h2>MaternaCare Clinic</h2>
        </div>
        <div class="rx-symbol">℞</div>
      </div>
      
      <div class="rx-info">
        <p><strong>Patient:</strong> ${currentPatient?.name || "Fatima Ali"}</p>
        <p><strong>Date:</strong> ${prescription.date || formatDateLong(prescription.date)}</p>
        <p><strong>Prescriber:</strong> ${prescription.prescriber || "Dr. Ahmed Hassan"}</p>
      </div>
      
      <div class="rx-medications">
        ${medications
          .map(
            (med, i) => `
          <div class="rx-medication-item">
            <p class="rx-med-name"><strong>${i + 1}. ${med.name}</strong> - ${med.dosage}</p>
            <p class="rx-med-details">
              <span><i class="fas fa-clock"></i> ${med.frequency}</span>
              <span><i class="fas fa-calendar"></i> ${med.duration}</span>
              <span><i class="fas fa-route"></i> ${med.route || "Oral"}</span>
            </p>
            ${med.instructions ? `<p class="rx-med-instructions"><i class="fas fa-info-circle"></i> ${med.instructions}</p>` : ""}
          </div>
        `
          )
          .join("")}
      </div>
      
      ${
        prescription.generalNotes
          ? `
        <div class="rx-notes">
          <p><strong>Instructions:</strong> ${prescription.generalNotes}</p>
        </div>
      `
          : ""
      }
      
      <div class="rx-footer">
        <p><strong>Refills:</strong> ${prescription.refills === 0 ? "No refills" : prescription.refills + " refill(s)"}</p>
        <div class="rx-signature">
          <div class="signature-line"></div>
          <p>Physician Signature</p>
        </div>
      </div>
    </div>
  `;
}

function printSavedPrescription(prescriptionId) {
  const prescription =
    savedPrescriptions[prescriptionId] ||
    currentPatient?.prescriptions?.find((p) => p.id === prescriptionId);

  if (!prescription) {
    showToast("Prescription not found", "error");
    return;
  }

  const previewHTML = generatePrescriptionPreviewHTML(prescription);
  const printWindow = window.open("", "_blank");
  printWindow.document.write(`
    <!DOCTYPE html>
    <html>
    <head>
      <title>Prescription - ${currentPatient?.name || "Patient"}</title>
      <style>
        body { font-family: Arial, sans-serif; padding: 40px; max-width: 800px; margin: 0 auto; }
        .rx-preview { border: 2px solid #333; padding: 30px; border-radius: 8px; }
        .rx-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 20px; }
        .rx-logo { display: flex; align-items: center; gap: 12px; }
        .rx-logo i { font-size: 24px; color: #0077b6; }
        .rx-logo h2 { margin: 0; color: #0077b6; }
        .rx-symbol { font-size: 48px; color: #0077b6; font-weight: bold; }
        .rx-info { margin-bottom: 20px; }
        .rx-info p { margin: 5px 0; }
        .rx-medications { margin: 20px 0; padding: 20px; background: #f5f5f5; border-radius: 8px; }
        .rx-medication-item { margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #ddd; }
        .rx-medication-item:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }
        .rx-med-name { margin: 0 0 5px 0; font-size: 16px; }
        .rx-med-details { margin: 5px 0; color: #666; }
        .rx-med-details span { margin-right: 20px; }
        .rx-med-instructions { margin: 5px 0; color: #444; font-style: italic; }
        .rx-notes { margin: 20px 0; padding: 15px; background: #fff3cd; border-radius: 8px; }
        .rx-notes p { margin: 0; }
        .rx-footer { margin-top: 30px; display: flex; justify-content: space-between; align-items: flex-end; }
        .signature-line { width: 200px; border-bottom: 1px solid #333; margin-bottom: 5px; }
        .rx-signature { text-align: center; }
        .rx-signature p { margin: 0; font-size: 12px; color: #666; }
        i { display: none; }
        @media print { body { padding: 20px; } }
      </style>
    </head>
    <body>
      ${previewHTML}
      <script>window.onload = function() { window.print(); }</script>
    </body>
    </html>
  `);
  printWindow.document.close();
}

// Render prescriptions tab content
function renderPrescriptions() {
  const prescriptionsList = document.getElementById("prescriptionsList");
  if (!prescriptionsList) return;

  // Combine saved prescriptions with any dynamically added ones
  const allPrescriptions = [...(currentPatient?.prescriptions || [])];

  // If no prescriptions, show the demo ones (already in HTML)
  if (allPrescriptions.length === 0) return;

  // Add dynamically created prescriptions to the list
  const dynamicHTML = allPrescriptions
    .map(
      (rx) => `
    <div class="prescription-card ${rx.status === "completed" ? "completed" : ""}">
      <div class="prescription-header">
        <div class="prescription-info">
          <span class="prescription-date"><i class="fas fa-calendar"></i> ${typeof rx.date === "string" ? rx.date : formatDateLong(rx.date)}</span>
          <span class="prescription-status ${rx.status}">
            <i class="fas fa-${rx.status === "active" ? "check-circle" : "check-double"}"></i> 
            ${capitalize(rx.status)}
          </span>
        </div>
        <div class="prescription-actions">
          <button class="btn-icon" onclick="viewPrescription('${rx.id}')" title="View">
            <i class="fas fa-eye"></i>
          </button>
          <button class="btn-icon" onclick="printSavedPrescription('${rx.id}')" title="Print">
            <i class="fas fa-print"></i>
          </button>
        </div>
      </div>
      <div class="prescription-medications">
        ${rx.medications
          .map(
            (med) => `
          <div class="medication-item">
            <span class="med-name">${med.name}</span>
            <span class="med-details">${med.dosage} • ${med.frequency} • ${med.duration}</span>
          </div>
        `
          )
          .join("")}
      </div>
      <div class="prescription-footer">
        <span class="prescription-prescriber"><i class="fas fa-user-md"></i> ${rx.prescriber}</span>
        <span class="prescription-refills"><i class="fas fa-redo"></i> ${rx.refills === 0 ? "No refills" : rx.refills + " refills remaining"}</span>
      </div>
    </div>
  `
    )
    .join("");

  // Prepend dynamic prescriptions before demo ones
  if (dynamicHTML) {
    prescriptionsList.insertAdjacentHTML("afterbegin", dynamicHTML);
  }
}

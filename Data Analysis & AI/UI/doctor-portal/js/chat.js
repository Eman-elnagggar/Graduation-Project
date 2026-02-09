// Doctor Chat JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initDemoData();
  initDemoChatData();
  initSidebar();
  initNotifications();
  initConversationFilters();
  initSearch();
  initQuickReplies();
  initMessageInput();
  renderConversations();
  checkUrlParams();
});

let currentConversation = null;
let conversations = [];
let currentFilter = "all";
let searchTerm = "";

// Initialize demo chat data
function initDemoChatData() {
  const existing = localStorage.getItem("doctorPortal_conversations");
  if (!existing) {
    const patients = getData("patients") || [];
    conversations = patients.map((patient, index) => ({
      id: `conv_${patient.id}`,
      patientId: patient.id,
      patientName: patient.name,
      patientAvatar:
        patient.image ||
        patient.avatar ||
        `https://ui-avatars.com/api/?name=${encodeURIComponent(patient.name)}&background=e91e8c&color=fff`,
      gestationalAge: patient.gestationalAge,
      riskLevel: patient.riskLevel,
      nextAppointment: patient.nextAppointment,
      lastMessage: getDemoLastMessage(index),
      lastMessageTime: getDemoLastTime(index),
      unreadCount: index === 0 ? 2 : index === 2 ? 1 : 0,
      isPriority: index === 1,
      isOnline: index < 2,
      messages: getDemoMessages(patient, index),
    }));
    localStorage.setItem(
      "doctorPortal_conversations",
      JSON.stringify(conversations)
    );
  } else {
    conversations = JSON.parse(existing);
  }
}

function getDemoLastMessage(index) {
  const messages = [
    "I've been feeling some discomfort. Is this normal?",
    "Thank you doctor! The medication is helping.",
    "When should I come in for my next checkup?",
    "I completed the blood test you ordered.",
  ];
  return messages[index % messages.length];
}

function getDemoLastTime(index) {
  const now = new Date();
  const offsets = [5, 45, 180, 1440]; // minutes ago
  return new Date(
    now.getTime() - offsets[index % offsets.length] * 60 * 1000
  ).toISOString();
}

function getDemoMessages(patient, index) {
  const now = new Date();
  const baseMessages = [
    {
      id: "msg1",
      sender: "patient",
      content: "Good morning Dr. Sarah! I have a question about my symptoms.",
      timestamp: new Date(now.getTime() - 2 * 60 * 60 * 1000).toISOString(),
      read: true,
    },
    {
      id: "msg2",
      sender: "doctor",
      content:
        "Good morning! Of course, I'm here to help. What symptoms are you experiencing?",
      timestamp: new Date(now.getTime() - 1.5 * 60 * 60 * 1000).toISOString(),
      read: true,
    },
    {
      id: "msg3",
      sender: "patient",
      content:
        "I've been having some mild cramping and back pain. It's not severe but I wanted to check if it's normal.",
      timestamp: new Date(now.getTime() - 1 * 60 * 60 * 1000).toISOString(),
      read: true,
    },
  ];

  if (index === 0) {
    baseMessages.push({
      id: "msg4",
      sender: "patient",
      content: "I've been feeling some discomfort. Is this normal?",
      timestamp: new Date(now.getTime() - 5 * 60 * 1000).toISOString(),
      read: false,
    });
  }

  return baseMessages;
}

// Initialize conversation filters
function initConversationFilters() {
  document.querySelectorAll(".filter-chip").forEach((chip) => {
    chip.addEventListener("click", function () {
      document
        .querySelectorAll(".filter-chip")
        .forEach((c) => c.classList.remove("active"));
      this.classList.add("active");
      currentFilter = this.dataset.filter;
      renderConversations();
    });
  });
}

// Initialize search
function initSearch() {
  document
    .getElementById("searchConversations")
    .addEventListener("input", function () {
      searchTerm = this.value.toLowerCase();
      renderConversations();
    });
}

// Initialize quick replies
function initQuickReplies() {
  document.querySelectorAll(".quick-reply-chip").forEach((chip) => {
    chip.addEventListener("click", function () {
      const message = this.dataset.message;
      document.getElementById("messageInput").value = message;
      document.getElementById("messageInput").focus();
      autoResizeTextarea();
    });
  });
}

// Initialize message input
function initMessageInput() {
  const textarea = document.getElementById("messageInput");
  textarea.addEventListener("input", autoResizeTextarea);
  textarea.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  });

  // Back to list button for mobile
  document.getElementById("backToList")?.addEventListener("click", function () {
    document.getElementById("activeChat").style.display = "none";
    document.getElementById("chatEmptyState").style.display = "flex";
    document
      .querySelector(".conversations-panel")
      .classList.remove("hidden-mobile");
  });
}

function autoResizeTextarea() {
  const textarea = document.getElementById("messageInput");
  textarea.style.height = "auto";
  textarea.style.height = Math.min(textarea.scrollHeight, 120) + "px";
}

// Filter conversations
function getFilteredConversations() {
  let filtered = [...conversations];

  // Apply search
  if (searchTerm) {
    filtered = filtered.filter(
      (c) =>
        c.patientName.toLowerCase().includes(searchTerm) ||
        c.lastMessage.toLowerCase().includes(searchTerm)
    );
  }

  // Apply filter
  if (currentFilter === "unread") {
    filtered = filtered.filter((c) => c.unreadCount > 0);
  } else if (currentFilter === "priority") {
    filtered = filtered.filter((c) => c.isPriority);
  }

  // Sort by last message time (most recent first)
  filtered.sort(
    (a, b) => new Date(b.lastMessageTime) - new Date(a.lastMessageTime)
  );

  return filtered;
}

// Render conversations list
function renderConversations() {
  const container = document.getElementById("conversationsList");
  const filtered = getFilteredConversations();

  if (filtered.length === 0) {
    container.innerHTML = `
            <div class="no-conversations">
                <i class="fas fa-inbox"></i>
                <p>${searchTerm ? "No conversations found" : "No conversations yet"}</p>
            </div>
        `;
    return;
  }

  container.innerHTML = filtered
    .map(
      (conv) => `
        <div class="conversation-item ${conv.id === currentConversation?.id ? "active" : ""} ${conv.unreadCount > 0 ? "unread" : ""}"
             onclick="openConversation('${conv.id}')">
            <div class="conversation-avatar">
                <img src="${conv.patientAvatar}" alt="${conv.patientName}" onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(conv.patientName)}&background=e91e8c&color=fff'">
                ${conv.isOnline ? '<span class="online-indicator"></span>' : ""}
            </div>
            <div class="conversation-info">
                <div class="conversation-header">
                    <h4>${conv.patientName}</h4>
                    <span class="conversation-time">${formatConversationTime(conv.lastMessageTime)}</span>
                </div>
                <div class="conversation-preview">
                    <p>${truncateMessage(conv.lastMessage)}</p>
                    <div class="conversation-badges">
                        ${conv.isPriority ? '<span class="priority-badge"><i class="fas fa-flag"></i></span>' : ""}
                        ${conv.unreadCount > 0 ? `<span class="unread-badge">${conv.unreadCount}</span>` : ""}
                    </div>
                </div>
            </div>
        </div>
    `
    )
    .join("");
}

// Open conversation
function openConversation(conversationId) {
  currentConversation = conversations.find((c) => c.id === conversationId);
  if (!currentConversation) return;

  // Mark as read
  currentConversation.unreadCount = 0;
  currentConversation.messages.forEach((m) => (m.read = true));
  saveConversations();

  // Update UI
  document.getElementById("chatEmptyState").style.display = "none";
  document.getElementById("activeChat").style.display = "flex";

  // Hide conversations panel on mobile
  document.querySelector(".conversations-panel").classList.add("hidden-mobile");

  // Populate chat header
  document.getElementById("chatPatientAvatar").src =
    currentConversation.patientAvatar;
  document.getElementById("chatPatientName").textContent =
    currentConversation.patientName;
  document.getElementById("statusText").textContent =
    currentConversation.isOnline ? "Online" : "Offline";
  document
    .querySelector(".patient-status")
    .classList.toggle("online", currentConversation.isOnline);

  // Quick info bar
  document.getElementById("quickGestationalAge").textContent =
    `${currentConversation.gestationalAge || "--"} weeks`;
  document.getElementById("quickRiskLevel").textContent =
    `${capitalize(currentConversation.riskLevel || "low")} risk`;
  document.getElementById("quickNextAppt").textContent =
    currentConversation.nextAppointment
      ? formatDateShort(currentConversation.nextAppointment)
      : "Not scheduled";

  // Priority button state
  document
    .getElementById("priorityBtn")
    .classList.toggle("active", currentConversation.isPriority);

  renderMessages();
  renderConversations();

  // Scroll to bottom
  setTimeout(scrollToBottom, 100);
}

// Render messages
function renderMessages() {
  const container = document.getElementById("messagesArea");
  const messages = currentConversation.messages;

  let lastDate = null;
  let html = "";

  messages.forEach((msg) => {
    const msgDate = new Date(msg.timestamp).toDateString();

    // Date separator
    if (msgDate !== lastDate) {
      html += `<div class="date-separator">${formatMessageDate(msg.timestamp)}</div>`;
      lastDate = msgDate;
    }

    const isDoctor = msg.sender === "doctor";
    html += `
            <div class="message ${isDoctor ? "sent" : "received"}">
                ${!isDoctor ? `<img src="${currentConversation.patientAvatar}" alt="Patient" class="message-avatar">` : ""}
                <div class="message-content">
                    <p>${msg.content}</p>
                    <span class="message-time">${formatMessageTime(msg.timestamp)}</span>
                </div>
            </div>
        `;
  });

  container.innerHTML = html;
}

// Send message
function sendMessage() {
  const input = document.getElementById("messageInput");
  const content = input.value.trim();

  if (!content || !currentConversation) return;

  const newMessage = {
    id: `msg_${Date.now()}`,
    sender: "doctor",
    content: content,
    timestamp: new Date().toISOString(),
    read: true,
  };

  currentConversation.messages.push(newMessage);
  currentConversation.lastMessage = content;
  currentConversation.lastMessageTime = newMessage.timestamp;

  saveConversations();
  renderMessages();
  renderConversations();

  input.value = "";
  input.style.height = "auto";
  scrollToBottom();

  // Simulate patient typing response (for demo)
  setTimeout(() => {
    simulatePatientResponse();
  }, 3000);
}

// Simulate patient response (demo only)
function simulatePatientResponse() {
  if (!currentConversation) return;

  const responses = [
    "Thank you for your quick response, doctor!",
    "I understand. I'll follow your advice.",
    "That's very helpful. I appreciate it!",
    "I'll keep you updated on how I'm feeling.",
  ];

  const newMessage = {
    id: `msg_${Date.now()}`,
    sender: "patient",
    content: responses[Math.floor(Math.random() * responses.length)],
    timestamp: new Date().toISOString(),
    read: true,
  };

  currentConversation.messages.push(newMessage);
  currentConversation.lastMessage = newMessage.content;
  currentConversation.lastMessageTime = newMessage.timestamp;

  saveConversations();

  if (currentConversation) {
    renderMessages();
    renderConversations();
    scrollToBottom();
  }
}

// Toggle priority
function togglePriority() {
  if (!currentConversation) return;

  currentConversation.isPriority = !currentConversation.isPriority;
  document
    .getElementById("priorityBtn")
    .classList.toggle("active", currentConversation.isPriority);

  saveConversations();
  renderConversations();

  showToast(
    currentConversation.isPriority
      ? "Marked as priority"
      : "Removed from priority",
    "success"
  );
}

// View patient details
function viewPatientDetails() {
  if (!currentConversation) return;
  window.location.href = `patient-details.html?id=${currentConversation.patientId}`;
}

// Schedule visit
function scheduleVisit() {
  showToast("Schedule feature - would open scheduling modal", "info");
}

// Show templates modal
function showTemplates() {
  const templates = [
    {
      title: "Appointment Reminder",
      content:
        "This is a reminder about your upcoming appointment. Please arrive 15 minutes early and bring any recent test results.",
    },
    {
      title: "Normal Results",
      content:
        "I've reviewed your recent test results and everything looks normal. Please continue with your current routine and let me know if you have any questions.",
    },
    {
      title: "Follow-up Needed",
      content:
        "Based on your recent visit, I'd like to schedule a follow-up appointment to monitor your progress. Please contact our office to book a time.",
    },
    {
      title: "Medication Instructions",
      content:
        "Please take the prescribed medication as directed. If you experience any side effects, discontinue use and contact me immediately.",
    },
    {
      title: "General Wellness",
      content:
        "Remember to stay hydrated, get adequate rest, and maintain a balanced diet. These are especially important during this stage of pregnancy.",
    },
  ];

  const container = document.getElementById("templatesList");
  container.innerHTML = templates
    .map(
      (t) => `
        <div class="template-card" onclick="useTemplate('${escapeHtml(t.content)}')">
            <h4>${t.title}</h4>
            <p>${t.content}</p>
        </div>
    `
    )
    .join("");

  document.getElementById("templatesModal").classList.add("active");
}

function closeTemplatesModal() {
  document.getElementById("templatesModal").classList.remove("active");
}

function useTemplate(content) {
  document.getElementById("messageInput").value = content;
  closeTemplatesModal();
  document.getElementById("messageInput").focus();
  autoResizeTextarea();
}

function showAttachmentOptions() {
  showToast("Attachment feature coming soon", "info");
}

// Helper functions
function saveConversations() {
  localStorage.setItem(
    "doctorPortal_conversations",
    JSON.stringify(conversations)
  );
}

function scrollToBottom() {
  const container = document.getElementById("messagesArea");
  container.scrollTop = container.scrollHeight;
}

function truncateMessage(message) {
  return message.length > 50 ? message.substring(0, 50) + "..." : message;
}

function formatConversationTime(timestamp) {
  const date = new Date(timestamp);
  const now = new Date();
  const diff = now - date;
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);

  if (minutes < 60) return `${minutes}m`;
  if (hours < 24) return `${hours}h`;
  if (hours < 48) return "Yesterday";

  return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

function formatMessageDate(timestamp) {
  const date = new Date(timestamp);
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);

  if (date.toDateString() === today.toDateString()) return "Today";
  if (date.toDateString() === yesterday.toDateString()) return "Yesterday";

  return date.toLocaleDateString("en-US", {
    weekday: "long",
    month: "long",
    day: "numeric",
  });
}

function formatMessageTime(timestamp) {
  return new Date(timestamp).toLocaleTimeString("en-US", {
    hour: "numeric",
    minute: "2-digit",
  });
}

function formatDateShort(dateStr) {
  return new Date(dateStr).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
  });
}

function capitalize(str) {
  return str?.charAt(0).toUpperCase() + str?.slice(1) || "";
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML.replace(/'/g, "\\'");
}

// Check URL params for direct conversation opening
function checkUrlParams() {
  const params = new URLSearchParams(window.location.search);
  const patientId = params.get("patient");

  if (patientId) {
    const conv = conversations.find((c) => c.patientId === patientId);
    if (conv) {
      openConversation(conv.id);
    }
  }
}

// Close modal on overlay click
document
  .getElementById("templatesModal")
  ?.addEventListener("click", function (e) {
    if (e.target === this) closeTemplatesModal();
  });

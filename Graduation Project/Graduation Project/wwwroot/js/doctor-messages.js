/**
 * Doctor Messages page
 * SignalR integration over existing UI rendering/layout.
 */

const config = window.__doctorMessagesConfig || {};
const CURRENT_USER_ID = String(config.currentUserId || "");
const DOCTOR_ID = String(config.doctorId || "");
const CONVERSATION_ENDPOINT_TEMPLATE = String(config.conversationMessagesEndpointTemplate || "");

const state = {
  currentConversation: null,
  conversations: [],
  messages: {},
  filter: "all",
  searchQuery: ""
};

let connection = null;
const SIGNALR_CDNS = [
  "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js",
  "https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js",
  "https://unpkg.com/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js"
];

document.addEventListener("DOMContentLoaded", async () => {
  initializeState();
  renderConversations();
  updateFilterCounts();
  setupEventListeners();

  await ensureSignalRLoaded();
  await setupSignalRConnection();

  const urlPatient = new URLSearchParams(location.search).get("patient");
  if (urlPatient) {
    const target = state.conversations.find(c => String(c.participantType) === "Patient" && String(c.participantId) === String(urlPatient));
    if (target) {
      selectConversation(target.id);
    }
  }

  const urlAssistant = new URLSearchParams(location.search).get("assistant");
  if (urlAssistant) {
    const target = state.conversations.find(c => String(c.participantType) === "Assistant" && String(c.participantId) === String(urlAssistant));
    if (target) {
      selectConversation(target.id);
    }
  }
});

function initializeState() {
  const initial = Array.isArray(config.conversations) ? config.conversations : [];
  state.conversations = initial.map((c, idx) => ({
    id: String(c.id ?? idx + 1),
    participantId: String(c.participantId ?? c.id ?? ""),
    participantType: String(c.participantType ?? "Patient"),
    receiverUserId: String(c.receiverUserId ?? ""),
    name: c.name || "User",
    avatar: c.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(c.name || "U")}&background=14967f&color=fff&size=80`,
    status: c.status || "online",
    lastMessage: c.lastMessage || "Start a conversation",
    lastMessageTime: c.lastMessageTime ? new Date(c.lastMessageTime) : null,
    unreadCount: Number(c.unreadCount || 0),
    isUrgent: Boolean(c.isUrgent)
  }));
}

async function ensureSignalRLoaded() {
  if (window.signalR) return true;

  for (const url of SIGNALR_CDNS) {
    const loaded = await loadScript(url);
    if (loaded && window.signalR) {
      console.info("SignalR client loaded.", url);
      return true;
    }
  }

  showToast("error", "Connection", "SignalR script could not be loaded.");
  return false;
}

function loadScript(src) {
  return new Promise(resolve => {
    const script = document.createElement("script");
    script.src = src;
    script.async = true;
    script.onload = () => resolve(true);
    script.onerror = () => resolve(false);
    document.head.appendChild(script);
  });
}

async function setupSignalRConnection() {
  if (!window.signalR) {
    console.error("SignalR client script is missing.");
    showToast("error", "Connection", "SignalR library failed to load.");
    return;
  }

  // Integration point: create hub connection to backend ChatHub endpoint.
  connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Integration point: receive message event from ChatHub.
  connection.on("ReceiveMessage", (senderId, message, sentAtUtc) => {
    handleIncomingMessage(String(senderId ?? ""), String(message ?? ""), sentAtUtc);
  });

  connection.onreconnecting((error) => {
    console.warn("Chat reconnecting...", error);
  });

  connection.onreconnected((id) => {
    console.info("Chat reconnected.", id);
    showToast("success", "Connection", "Chat connection restored.");
  });

  connection.onclose((error) => {
    console.error("Chat disconnected.", error);
    showToast("error", "Connection", "Chat disconnected. Retrying when possible.");
  });

  try {
    await connection.start();
    console.info("Connected to /chatHub successfully.");
    showToast("success", "Connection", "Connected to chat server.");
  } catch (error) {
    // Integration point: explicit start error handling.
    console.error("Failed to connect to /chatHub.", error);
    showToast("error", "Connection", "Unable to connect to chat server.");
  }
}

function setupEventListeners() {
  const searchInput = document.getElementById("conversationSearch");
  const searchClear = document.getElementById("searchClear");
  const messageInput = document.getElementById("messageInput");
  const sendBtn = document.getElementById("sendBtn");

  searchInput?.addEventListener("input", (e) => {
    state.searchQuery = (e.target.value || "").toLowerCase();
    searchClear.style.display = state.searchQuery ? "block" : "none";
    renderConversations();
  });

  searchClear?.addEventListener("click", () => {
    searchInput.value = "";
    state.searchQuery = "";
    searchClear.style.display = "none";
    renderConversations();
  });

  document.querySelectorAll(".filter-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".filter-btn").forEach(b => b.classList.remove("active"));
      btn.classList.add("active");
      state.filter = btn.dataset.filter || "all";
      renderConversations();
    });
  });

  messageInput?.addEventListener("input", () => {
    sendBtn.disabled = !messageInput.value.trim();
    autoResizeTextarea(messageInput);
  });

  messageInput?.addEventListener("keydown", (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  });

  sendBtn?.addEventListener("click", sendMessage);

  document.getElementById("viewProfileBtn")?.addEventListener("click", () => {
    if (!state.currentConversation) return;
    if (state.currentConversation.participantType !== "Patient") return;
    window.location.href = `/Doctor/PatientDetails/${DOCTOR_ID}/${state.currentConversation.participantId}`;
  });

  document.getElementById("mobileMenuBtn")?.addEventListener("click", () => toggleSidebar(true));
  document.getElementById("sidebarClose")?.addEventListener("click", () => toggleSidebar(false));
  document.getElementById("sidebarOverlay")?.addEventListener("click", () => toggleSidebar(false));
}

function renderConversations() {
  const container = document.getElementById("conversationsList");
  if (!container) return;

  let filtered = [...state.conversations];

  if (state.searchQuery) {
    filtered = filtered.filter(c =>
      c.name.toLowerCase().includes(state.searchQuery) ||
      (c.lastMessage || "").toLowerCase().includes(state.searchQuery)
    );
  }

  if (state.filter === "unread") {
    filtered = filtered.filter(c => c.unreadCount > 0);
  } else if (state.filter === "urgent") {
    filtered = filtered.filter(c => c.isUrgent);
  }

  filtered.sort((a, b) => getConversationTimestamp(b) - getConversationTimestamp(a));

  if (!filtered.length) {
    container.innerHTML = `<div style="text-align:center;padding:2rem;color:#8896ab;">No conversations found</div>`;
    return;
  }

  container.innerHTML = filtered.map(conv => `
    <div class="conversation-item conversation-${getParticipantTypeClass(conv.participantType)} ${conv.unreadCount > 0 ? "unread" : ""} ${state.currentConversation?.id === conv.id ? "active" : ""}" data-id="${conv.id}">
      <div class="conversation-avatar">
        <img src="${conv.avatar}" alt="${escapeHtml(conv.name)}" />
      </div>
      <div class="conversation-content">
        <div class="conversation-header">
          <div class="conversation-name-wrap">
            <span class="conversation-name">${escapeHtml(conv.name)}</span>
            <span class="conversation-role-badge ${getParticipantTypeClass(conv.participantType)}">${escapeHtml(getParticipantTypeLabel(conv.participantType))}</span>
          </div>
          <span class="conversation-time">${formatTime(conv.lastMessageTime)}</span>
        </div>
        <div style="display:flex;justify-content:space-between;gap:8px;align-items:center;">
          <span class="conversation-message">${escapeHtml(conv.lastMessage || "")}</span>
          ${conv.unreadCount > 0 ? `<span class="unread-badge">${conv.unreadCount}</span>` : ""}
        </div>
      </div>
    </div>
  `).join("");

  container.querySelectorAll(".conversation-item").forEach(item => {
    item.addEventListener("click", () => {
      selectConversation(String(item.dataset.id));
    });
  });
}

function selectConversation(id) {
  const conversation = state.conversations.find(c => c.id === id);
  if (!conversation) return;

  state.currentConversation = conversation;
  conversation.unreadCount = 0;

  const viewProfileBtn = document.getElementById("viewProfileBtn");
  if (viewProfileBtn) {
    viewProfileBtn.style.display = conversation.participantType === "Patient" ? "inline-flex" : "none";
  }

  document.getElementById("chatUserAvatar").src = conversation.avatar;
  document.getElementById("chatUserAvatar").alt = conversation.name;
  document.getElementById("chatUserName").textContent = conversation.name;
  const chatUserRole = document.getElementById("chatUserRole");
  if (chatUserRole) {
    chatUserRole.textContent = getParticipantTypeLabel(conversation.participantType);
  }

  document.getElementById("chatEmpty").style.display = "none";
  document.getElementById("chatContainer").style.display = "flex";

  renderConversations();
  updateFilterCounts();
  loadConversationMessages(conversation);
  toggleSidebar(false);

  document.getElementById("messageInput")?.focus();
}

async function loadConversationMessages(conversation) {
  if (!conversation || !CONVERSATION_ENDPOINT_TEMPLATE) {
    renderMessages();
    return;
  }

  const endpoint = CONVERSATION_ENDPOINT_TEMPLATE
    .replace("__DOCTOR_ID__", encodeURIComponent(DOCTOR_ID))
    .replace("__USER_ID__", encodeURIComponent(conversation.receiverUserId));

  try {
    const response = await fetch(endpoint, { credentials: "same-origin" });
    if (!response.ok) {
      console.error("Failed to load conversation messages.", response.status);
      renderMessages();
      return;
    }

    const payload = await response.json();
    state.messages[conversation.id] = Array.isArray(payload)
      ? payload.map(m => ({
        id: m.id,
        sender: String(m.senderId) === CURRENT_USER_ID ? "me" : "other",
        content: String(m.content || ""),
        timestamp: m.timestamp ? new Date(m.timestamp) : new Date()
      }))
      : [];

    renderMessages();
    renderConversations();
    updateFilterCounts();
  } catch (error) {
    console.error("Error loading conversation messages.", error);
    renderMessages();
  }
}

function renderMessages() {
  const container = document.getElementById("chatMessages");
  if (!container || !state.currentConversation) return;

  const messages = state.messages[state.currentConversation.id] || [];
  const grouped = groupMessagesByDate(messages);

  let html = "";
  Object.entries(grouped).forEach(([date, msgs]) => {
    html += `<div class="date-separator"><span>${date}</span></div>`;
    msgs.forEach(msg => {
      html += renderMessage(msg);
    });
  });

  container.innerHTML = html;
  container.scrollTop = container.scrollHeight;
}

function renderMessage(msg) {
  const wrapperClass = `message-wrapper ${msg.sender === "me" ? "sent" : "received"}`;
  return `
    <div class="${wrapperClass}">
      <div class="message-bubble"><p class="message-text">${formatMessageContent(msg.content || "")}</p></div>
      <div class="message-meta">
        <span class="message-time">${formatMessageTime(msg.timestamp)}</span>
      </div>
    </div>
  `;
}

async function sendMessage() {
  const input = document.getElementById("messageInput");
  const text = input?.value.trim();

  if (!text || !state.currentConversation) return;

  if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
    console.warn("Send blocked because connection state is not connected.", connection?.state);
    showToast("error", "Connection", "Chat is not connected.");
    return;
  }

  try {
    // Integration point: send through hub method SendMessage(receiverId, message).
    await connection.invoke("SendMessage", state.currentConversation.receiverUserId, text);
    input.value = "";
    input.style.height = "auto";
    document.getElementById("sendBtn").disabled = true;
  } catch (error) {
    console.error("Failed to send message.", error);
    showToast("error", "Send failed", "Message could not be sent.");
  }
}

function handleIncomingMessage(senderId, message, sentAtUtc) {
  const isSentByCurrentUser = senderId === CURRENT_USER_ID;

  let conversation = null;
  if (isSentByCurrentUser) {
    conversation = state.currentConversation;
  } else {
    conversation = state.conversations.find(c => String(c.receiverUserId) === senderId);
  }

  if (!conversation) {
    console.warn("Incoming message ignored because conversation was not found.", { senderId });
    return;
  }

  if (!state.messages[conversation.id]) {
    state.messages[conversation.id] = [];
  }

  state.messages[conversation.id].push({
    id: Date.now() + Math.random(),
    sender: isSentByCurrentUser ? "me" : "other",
    content: message,
    timestamp: sentAtUtc ? new Date(sentAtUtc) : new Date(),
    status: "delivered"
  });

  conversation.lastMessage = message;
  conversation.lastMessageTime = sentAtUtc ? new Date(sentAtUtc) : new Date();

  if (!state.currentConversation || state.currentConversation.id !== conversation.id) {
    conversation.unreadCount = (conversation.unreadCount || 0) + (isSentByCurrentUser ? 0 : 1);
  }

  renderConversations();
  updateFilterCounts();

  if (state.currentConversation && state.currentConversation.id === conversation.id) {
    renderMessages();
  }
}

function updateFilterCounts() {
  document.getElementById("countAll").textContent = String(state.conversations.length);
  document.getElementById("countUnread").textContent = String(state.conversations.filter(c => c.unreadCount > 0).length);
  document.getElementById("countUrgent").textContent = String(state.conversations.filter(c => c.isUrgent).length);
}

function autoResizeTextarea(textarea) {
  textarea.style.height = "auto";
  textarea.style.height = `${Math.min(textarea.scrollHeight, 120)}px`;
}

function formatTime(date) {
  if (!date) return "";
  const d = new Date(date);
  return d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit", hour12: true });
}

function getConversationTimestamp(conversation) {
  if (!conversation?.lastMessageTime) return 0;
  const time = new Date(conversation.lastMessageTime).getTime();
  return Number.isFinite(time) ? time : 0;
}

function formatMessageTime(date) {
  return new Date(date).toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit", hour12: true });
}

function formatMessageContent(content) {
  const escaped = escapeHtml(content).replace(/\n/g, "<br>");
  return escaped.replace(/(https?:\/\/[^\s]+)/g, '<a href="$1" target="_blank" rel="noopener">$1</a>');
}

function groupMessagesByDate(messages) {
  const groups = {};
  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86400000).toDateString();

  messages.forEach(msg => {
    const date = new Date(msg.timestamp);
    const dateStr = date.toDateString();
    let label = date.toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric" });
    if (dateStr === today) label = "Today";
    if (dateStr === yesterday) label = "Yesterday";

    if (!groups[label]) groups[label] = [];
    groups[label].push(msg);
  });

  return groups;
}

function toggleSidebar(show) {
  const sidebar = document.getElementById("chatSidebar");
  const overlay = document.getElementById("sidebarOverlay");
  if (!sidebar || !overlay) return;

  if (show) {
    sidebar.classList.add("show");
    overlay.classList.add("show");
  } else {
    sidebar.classList.remove("show");
    overlay.classList.remove("show");
  }
}

function showToast(type, title, message) {
  const container = document.getElementById("toastContainer");
  if (!container) return;

  const toast = document.createElement("div");
  toast.className = `toast ${type}`;
  toast.innerHTML = `<div style="font-weight:700;font-size:.86rem;">${escapeHtml(title)}</div><div style="font-size:.8rem;color:#64748b;">${escapeHtml(message)}</div>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.remove();
  }, 3500);
}

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function getParticipantTypeLabel(type) {
  return String(type || "Patient").toLowerCase() === "assistant" ? "Assistant" : "Patient";
}

function getParticipantTypeClass(type) {
  return String(type || "Patient").toLowerCase() === "assistant" ? "assistant" : "patient";
}

const config = window.__doctorMessagesConfig || {};
const CURRENT_USER_ID = String(config.currentUserId || "");
const DOCTOR_ID = String(config.doctorId || "");
const CONVERSATION_ENDPOINT_TEMPLATE = String(config.conversationMessagesEndpointTemplate || "");
const UPLOAD_ENDPOINT_TEMPLATE = String(config.uploadChatFileEndpointTemplate || "");
const ANTIFORGERY_TOKEN = String(config.antiForgeryToken || "");

const state = {
  currentConversation: null,
  conversations: [],
  messages: {},
  filter: "all",
  searchQuery: "",
  pendingFile: null
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
    if (target) selectConversation(target.id);
  }

  const urlAssistant = new URLSearchParams(location.search).get("assistant");
  if (urlAssistant) {
    const target = state.conversations.find(c => String(c.participantType) === "Assistant" && String(c.participantId) === String(urlAssistant));
    if (target) selectConversation(target.id);
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
    if (loaded && window.signalR) return true;
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
    showToast("error", "Connection", "SignalR library failed to load.");
    return;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.on("ReceiveMessage", (senderId, message, sentAtUtc, attachmentUrl, attachmentType, attachmentName) => {
    handleIncomingMessage(String(senderId ?? ""), String(message ?? ""), sentAtUtc, attachmentUrl || null, attachmentType || null, attachmentName || null);
  });

  connection.onreconnected(() => {
    showToast("success", "Connection", "Chat connection restored.");
  });

  connection.onclose(() => {
    showToast("error", "Connection", "Chat disconnected. Retrying when possible.");
  });

  try {
    await connection.start();
    showToast("success", "Connection", "Connected to chat server.");
    await requestNotificationPermission();
  } catch {
    showToast("error", "Connection", "Unable to connect to chat server.");
  }
}

async function requestNotificationPermission() {
  if (!("Notification" in window)) return;
  if (Notification.permission === "default") {
    await Notification.requestPermission();
  }
}

function showBrowserNotification(senderName, body) {
  if (!("Notification" in window) || Notification.permission !== "granted") return;
  if (document.visibilityState === "visible") return;

  const notif = new Notification(`💬 ${senderName}`, {
    body: body || "Sent you a message",
    icon: "/images/logo.png",
    tag: `chat-${senderName}`
  });
  notif.onclick = () => { window.focus(); notif.close(); };
  setTimeout(() => notif.close(), 6000);
}

function setupEventListeners() {
  const searchInput = document.getElementById("conversationSearch");
  const searchClear = document.getElementById("searchClear");
  const messageInput = document.getElementById("messageInput");
  const sendBtn = document.getElementById("sendBtn");
  const attachBtn = document.getElementById("attachBtn");
  const fileInput = document.getElementById("chatFileInput");
  const removeFileBtn = document.getElementById("removeFileBtn");

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
    updateSendBtnState();
    autoResizeTextarea(messageInput);
  });

  messageInput?.addEventListener("keydown", (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  });

  sendBtn?.addEventListener("click", sendMessage);

  attachBtn?.addEventListener("click", () => {
    if (state.currentConversation) fileInput?.click();
  });

  fileInput?.addEventListener("change", () => {
    const file = fileInput.files?.[0];
    if (!file) return;

    const allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf"];
    if (!allowedTypes.includes(file.type)) {
      showToast("error", "Invalid file", "Only images (JPG, PNG, GIF, WebP) and PDFs are allowed.");
      fileInput.value = "";
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      showToast("error", "File too large", "Maximum file size is 10 MB.");
      fileInput.value = "";
      return;
    }

    state.pendingFile = file;
    showFilePreview(file);
    updateSendBtnState();
    fileInput.value = "";
  });

  removeFileBtn?.addEventListener("click", () => {
    clearPendingFile();
  });

  document.getElementById("viewProfileBtn")?.addEventListener("click", () => {
    if (!state.currentConversation) return;
    if (state.currentConversation.participantType !== "Patient") return;
    window.location.href = `/Doctor/PatientDetails/${DOCTOR_ID}?patientId=${state.currentConversation.participantId}`;
  });

  document.getElementById("mobileMenuBtn")?.addEventListener("click", () => toggleSidebar(true));
  document.getElementById("sidebarClose")?.addEventListener("click", () => toggleSidebar(false));
  document.getElementById("sidebarOverlay")?.addEventListener("click", () => toggleSidebar(false));
}

function showFilePreview(file) {
  const preview = document.getElementById("filePreview");
  const previewName = document.getElementById("filePreviewName");
  const previewImg = document.getElementById("filePreviewImg");
  if (!preview) return;

  previewName.textContent = file.name;

  if (file.type.startsWith("image/")) {
    const reader = new FileReader();
    reader.onload = (e) => {
      previewImg.src = e.target.result;
      previewImg.style.display = "block";
    };
    reader.readAsDataURL(file);
  } else {
    previewImg.style.display = "none";
    previewImg.src = "";
  }

  preview.style.display = "flex";
}

function clearPendingFile() {
  state.pendingFile = null;
  const preview = document.getElementById("filePreview");
  if (preview) preview.style.display = "none";
  const previewImg = document.getElementById("filePreviewImg");
  if (previewImg) { previewImg.src = ""; previewImg.style.display = "none"; }
  updateSendBtnState();
}

function updateSendBtnState() {
  const input = document.getElementById("messageInput");
  const sendBtn = document.getElementById("sendBtn");
  if (sendBtn) sendBtn.disabled = !state.pendingFile && !(input?.value.trim());
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
  if (chatUserRole) chatUserRole.textContent = getParticipantTypeLabel(conversation.participantType);

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
      renderMessages();
      return;
    }

    const payload = await response.json();
    state.messages[conversation.id] = Array.isArray(payload)
      ? payload.map(m => ({
        id: m.id,
        sender: String(m.senderId) === CURRENT_USER_ID ? "me" : "other",
        content: String(m.content || ""),
        timestamp: m.timestamp ? new Date(m.timestamp) : new Date(),
        attachmentUrl: m.attachmentUrl || null,
        attachmentType: m.attachmentType || null,
        attachmentName: m.attachmentName || null
      }))
      : [];

    renderMessages();
    renderConversations();
    updateFilterCounts();
    if (typeof window.refreshSidebarMsgBadge === "function") window.refreshSidebarMsgBadge();
  } catch {
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
  let attachmentHtml = "";

  if (msg.attachmentUrl) {
    if (msg.attachmentType === "image") {
      attachmentHtml = `<div class="msg-attachment msg-attachment-image"><a href="${escapeHtml(msg.attachmentUrl)}" target="_blank" rel="noopener"><img src="${escapeHtml(msg.attachmentUrl)}" alt="${escapeHtml(msg.attachmentName || "image")}" class="chat-img-preview" /></a></div>`;
    } else {
      attachmentHtml = `<div class="msg-attachment msg-attachment-file"><a href="${escapeHtml(msg.attachmentUrl)}" target="_blank" rel="noopener" download="${escapeHtml(msg.attachmentName || "file")}">📄 ${escapeHtml(msg.attachmentName || "Download file")}</a></div>`;
    }
  }

  const textHtml = msg.content ? `<p class="message-text">${formatMessageContent(msg.content)}</p>` : "";

  return `
    <div class="${wrapperClass}">
      <div class="message-bubble">${attachmentHtml}${textHtml}</div>
      <div class="message-meta">
        <span class="message-time">${formatMessageTime(msg.timestamp)}</span>
      </div>
    </div>
  `;
}

async function sendMessage() {
  const input = document.getElementById("messageInput");
  const text = input?.value.trim() || "";

  if (!text && !state.pendingFile) return;
  if (!state.currentConversation) return;

  if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
    showToast("error", "Connection", "Chat is not connected.");
    return;
  }

  if (state.pendingFile) {
    await sendFileMessage(text);
  } else {
    try {
      await connection.invoke("SendMessage", state.currentConversation.receiverUserId, text);
      input.value = "";
      input.style.height = "auto";
      updateSendBtnState();
    } catch {
      showToast("error", "Send failed", "Message could not be sent.");
    }
  }
}

async function sendFileMessage(caption) {
  const uploadEndpoint = UPLOAD_ENDPOINT_TEMPLATE.replace("__DOCTOR_ID__", encodeURIComponent(DOCTOR_ID));
  const formData = new FormData();
  formData.append("file", state.pendingFile);

  try {
    const response = await fetch(uploadEndpoint, {
      method: "POST",
      headers: { "RequestVerificationToken": ANTIFORGERY_TOKEN },
      body: formData,
      credentials: "same-origin"
    });

    if (!response.ok) {
      const err = await response.json().catch(() => ({}));
      showToast("error", "Upload failed", err.error || "Could not upload file.");
      return;
    }

    const result = await response.json();
    clearPendingFile();

    const input = document.getElementById("messageInput");
    input.value = "";
    input.style.height = "auto";

    await connection.invoke("SendFileMessage", state.currentConversation.receiverUserId, caption, result.url, result.type, result.name);
    updateSendBtnState();
  } catch {
    showToast("error", "Send failed", "File could not be sent.");
  }
}

function handleIncomingMessage(senderId, message, sentAtUtc, attachmentUrl, attachmentType, attachmentName) {
  const isSentByCurrentUser = senderId === CURRENT_USER_ID;

  let conversation = null;
  if (isSentByCurrentUser) {
    conversation = state.currentConversation;
  } else {
    conversation = state.conversations.find(c => String(c.receiverUserId) === senderId);
  }

  if (!conversation) return;

  if (!state.messages[conversation.id]) {
    state.messages[conversation.id] = [];
  }

  state.messages[conversation.id].push({
    id: Date.now() + Math.random(),
    sender: isSentByCurrentUser ? "me" : "other",
    content: message,
    timestamp: sentAtUtc ? new Date(sentAtUtc) : new Date(),
    status: "delivered",
    attachmentUrl: attachmentUrl || null,
    attachmentType: attachmentType || null,
    attachmentName: attachmentName || null
  });

  conversation.lastMessage = attachmentUrl ? (message || "📎 Attachment") : message;
  conversation.lastMessageTime = sentAtUtc ? new Date(sentAtUtc) : new Date();

  if (!isSentByCurrentUser) {
    if (!state.currentConversation || state.currentConversation.id !== conversation.id) {
      conversation.unreadCount = (conversation.unreadCount || 0) + 1;
    }
    const body = attachmentUrl ? "📎 Sent an attachment" : message;
    showBrowserNotification(conversation.name, body);
    if (typeof window.refreshSidebarMsgBadge === "function") window.refreshSidebarMsgBadge();
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

  setTimeout(() => { toast.remove(); }, 3500);
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

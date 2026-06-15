/* ================================
   Shared JavaScript for All Portals
   Handles sidebar, notifications, and common functionality
================================ */

// ================================
// Sidebar Toggle Functionality
// ================================
function initSharedSidebar() {
  const sidebar = document.querySelector(".sidebar");

  // Prevent double initialization
  if (!sidebar || sidebar.dataset.sidebarInitialized === "true") return;
  sidebar.dataset.sidebarInitialized = "true";

  // Create toggle button if not exists
  let toggleBtn = document.querySelector(".sidebar-toggle");
  if (!toggleBtn) {
    toggleBtn = document.createElement("button");
    toggleBtn.className = "sidebar-toggle";
    toggleBtn.innerHTML = '<i class="fas fa-chevron-left"></i>';
    toggleBtn.setAttribute("title", "Toggle Sidebar");
    document.body.appendChild(toggleBtn);
  }

  // Create close button for mobile
  let closeBtn = sidebar.querySelector(".sidebar-close");
  if (!closeBtn) {
    closeBtn = document.createElement("button");
    closeBtn.className = "sidebar-close";
    closeBtn.innerHTML = '<i class="fas fa-times"></i>';
    sidebar.insertBefore(closeBtn, sidebar.firstChild);
  }

  // Create overlay for mobile
  let overlay = document.querySelector(".sidebar-overlay");
  if (!overlay) {
    overlay = document.createElement("div");
    overlay.className = "sidebar-overlay";
    document.body.appendChild(overlay);
  }

  // Add tooltips to nav items (read from span text or existing data-tooltip)
  const navItems = sidebar.querySelectorAll(".nav-item");
  navItems.forEach((item) => {
    if (!item.getAttribute("data-tooltip")) {
      const text = item.querySelector("span:not(.nav-icon)")?.textContent?.trim() || "";
      if (text) {
        item.setAttribute("data-tooltip", text);
      }
    }
  });

  // Load saved state from localStorage
  const storageKey = "sidebarCollapsed";
  const savedState = localStorage.getItem(storageKey);
  if (savedState === "true" && window.innerWidth > 768) {
    sidebar.classList.add("collapsed");
    document.body.classList.add("sidebar-collapsed");
    toggleBtn.innerHTML = '<i class="fas fa-chevron-right"></i>';
    toggleBtn.classList.add("collapsed");
  }

  // Toggle button click handler
  toggleBtn.addEventListener("click", function () {
    if (window.innerWidth <= 768) {
      // Mobile: open/close sidebar
      sidebar.classList.toggle("mobile-open");
      sidebar.classList.toggle("open");
      overlay.classList.toggle("active");
    } else {
      // Desktop: collapse/expand sidebar
      sidebar.classList.toggle("collapsed");
      document.body.classList.toggle("sidebar-collapsed");
      toggleBtn.classList.toggle("collapsed");

      // Update icon
      const icon = this.querySelector("i");
      if (sidebar.classList.contains("collapsed")) {
        icon.className = "fas fa-chevron-right";
        localStorage.setItem(storageKey, "true");
      } else {
        icon.className = "fas fa-chevron-left";
        localStorage.setItem(storageKey, "false");
      }
    }
  });

  // Close button click handler (mobile)
  closeBtn.addEventListener("click", function () {
    sidebar.classList.remove("mobile-open");
    sidebar.classList.remove("open");
    overlay.classList.remove("active");
  });

  // Overlay click handler (mobile)
  overlay.addEventListener("click", function () {
    sidebar.classList.remove("mobile-open");
    sidebar.classList.remove("open");
    overlay.classList.remove("active");
  });

  // Handle window resize
  let resizeTimer;
  window.addEventListener("resize", function () {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function () {
      if (window.innerWidth > 768) {
        sidebar.classList.remove("mobile-open");
        sidebar.classList.remove("open");
        overlay.classList.remove("active");

        // Restore collapsed state on desktop
        const savedState = localStorage.getItem(storageKey);
        if (savedState === "true") {
          sidebar.classList.add("collapsed");
          document.body.classList.add("sidebar-collapsed");
          toggleBtn.classList.add("collapsed");
          toggleBtn.querySelector("i").className = "fas fa-chevron-right";
        }
      } else {
        // Remove collapsed state on mobile
        sidebar.classList.remove("collapsed");
        document.body.classList.remove("sidebar-collapsed");
        toggleBtn.classList.remove("collapsed");
        toggleBtn.querySelector("i").className = "fas fa-bars";
      }
    }, 250);
  });

  // Update toggle icon based on screen size
  if (window.innerWidth <= 768) {
    toggleBtn.querySelector("i").className = "fas fa-bars";
  }

  // Close sidebar when clicking a nav item on mobile
  navItems.forEach((item) => {
    item.addEventListener("click", function () {
      if (window.innerWidth <= 768) {
        sidebar.classList.remove("mobile-open");
        sidebar.classList.remove("open");
        overlay.classList.remove("active");
      }
    });
  });

  // Keyboard shortcut (Ctrl + B) to toggle sidebar
  document.addEventListener("keydown", function (e) {
    if (e.ctrlKey && e.key === "b") {
      e.preventDefault();
      toggleBtn.click();
    }
  });

  // Add subtle hover ripple effect to nav items
  navItems.forEach((item) => {
    item.addEventListener("mouseenter", function () {
      this.style.transition = "all 0.2s ease";
    });
  });
}

// ================================
// Toast Notifications
// ================================
function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.className = `toast toast-${type}`;
  toast.innerHTML = `
        <i class="fas fa-${type === "success" ? "check-circle" : type === "error" ? "times-circle" : "info-circle"}"></i>
        <span>${message}</span>
    `;

  // Add toast styles if not present
  if (!document.querySelector("#shared-toast-styles")) {
    const styles = document.createElement("style");
    styles.id = "shared-toast-styles";
    styles.textContent = `
            .toast {
                position: fixed;
                bottom: 24px;
                right: 24px;
                padding: 16px 24px;
                background: var(--bg-white, #ffffff);
                border-radius: var(--radius-md, 10px);
                box-shadow: var(--shadow-lg, 0 10px 15px rgba(0, 0, 0, 0.1));
                display: flex;
                align-items: center;
                gap: 12px;
                z-index: 10000;
                animation: toastSlideIn 0.3s ease;
            }
            .toast-success { border-left: 4px solid var(--success, #4caf50); }
            .toast-success i { color: var(--success, #4caf50); }
            .toast-error { border-left: 4px solid var(--danger, #f44336); }
            .toast-error i { color: var(--danger, #f44336); }
            .toast-info { border-left: 4px solid var(--info, #2196f3); }
            .toast-info i { color: var(--info, #2196f3); }
            .toast-warning { border-left: 4px solid var(--warning, #ff9800); }
            .toast-warning i { color: var(--warning, #ff9800); }
            @keyframes toastSlideIn {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
        `;
    document.head.appendChild(styles);
  }

  document.body.appendChild(toast);
  setTimeout(() => {
    toast.style.animation = "toastSlideIn 0.3s ease reverse";
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

// ================================
// Utility Functions
// ================================

// Format time for display (24h to 12h)
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

// Format relative time
function formatRelativeTime(dateStr) {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now - date;
  const diffMins = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffMins < 1) return "Just now";
  if (diffMins < 60) return `${diffMins} min ago`;
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
  if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? "s" : ""} ago`;
  return formatDate(dateStr);
}

// ================================
// Modal Functionality
// ================================
function openModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.add("active");
    document.body.style.overflow = "hidden";
  }
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.remove("active");
    document.body.style.overflow = "";
  }
}

// Close modal on overlay click
document.addEventListener("click", function (e) {
  if (
    e.target.classList.contains("modal-overlay") ||
    e.target.classList.contains("modal")
  ) {
    const modal = e.target.closest(".modal") || e.target;
    if (modal.id) {
      closeModal(modal.id);
    }
  }
});

// Close modal on escape key
document.addEventListener("keydown", function (e) {
  if (e.key === "Escape") {
    const activeModal = document.querySelector(".modal.active");
    if (activeModal && activeModal.id) {
      closeModal(activeModal.id);
    }
  }
});

// ================================
// File Upload Helpers
// ================================
function initFileUpload(
  uploadZoneId,
  fileInputId,
  uploadedFileId,
  fileNameId,
  removeFileId,
) {
  const uploadZone = document.getElementById(uploadZoneId);
  const fileInput = document.getElementById(fileInputId);
  const uploadedFile = document.getElementById(uploadedFileId);
  const fileName = document.getElementById(fileNameId);
  const removeFile = document.getElementById(removeFileId);

  if (!uploadZone || !fileInput) return;

  // Click to upload
  uploadZone.addEventListener("click", function () {
    fileInput.click();
  });

  // Drag and drop
  uploadZone.addEventListener("dragover", function (e) {
    e.preventDefault();
    uploadZone.style.borderColor = "var(--primary, #1BAEBE)";
    uploadZone.style.background = "rgba(27, 174, 190, 0.1)";
  });

  uploadZone.addEventListener("dragleave", function (e) {
    e.preventDefault();
    uploadZone.style.borderColor = "var(--border-color, #E2E8F0)";
    uploadZone.style.background = "transparent";
  });

  uploadZone.addEventListener("drop", function (e) {
    e.preventDefault();
    uploadZone.style.borderColor = "var(--border-color, #E2E8F0)";
    uploadZone.style.background = "transparent";

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileSelection(files[0], uploadZone, uploadedFile, fileName);
    }
  });

  // File input change
  fileInput.addEventListener("change", function () {
    if (fileInput.files.length > 0) {
      handleFileSelection(
        fileInput.files[0],
        uploadZone,
        uploadedFile,
        fileName,
      );
    }
  });

  // Remove file
  if (removeFile) {
    removeFile.addEventListener("click", function () {
      if (uploadedFile) uploadedFile.style.display = "none";
      if (uploadZone) uploadZone.style.display = "block";
      fileInput.value = "";
    });
  }
}

function handleFileSelection(file, uploadZone, uploadedFile, fileNameEl) {
  if (fileNameEl) fileNameEl.textContent = file.name;
  if (uploadZone) uploadZone.style.display = "none";
  if (uploadedFile) uploadedFile.style.display = "flex";
}

// ================================
// Top-Bar Notifications Panel
// ================================
// ================================
// Top-Bar Notifications Panel
// ================================

const _notifIconMap = {
  danger:  { icon: "fa-exclamation-circle", color: "#ef4444" },
  warning: { icon: "fa-exclamation-triangle", color: "#f59e0b" },
  success: { icon: "fa-check-circle",        color: "#10b981" },
  info:    { icon: "fa-info-circle",          color: "#3b82f6" },
};

function _notifRelativeTime(isoStr) {
  const diff = Date.now() - new Date(isoStr).getTime();
  const m = Math.floor(diff / 60000);
  if (m < 1)  return "Just now";
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

function _setBadge(badge, count) {
  if (!badge) return;
  if (count > 0) {
    badge.textContent = count > 99 ? "99+" : String(count);
    badge.hidden = false;
    badge.style.display = "inline-flex";
  } else {
    badge.hidden = true;
    badge.style.display = "none";
  }
}

function _renderNotifications(items, listEl, assistantId, badge, sidebarBadge) {
  if (!listEl) return;

  if (!items || items.length === 0) {
    listEl.innerHTML = `
      <div style="text-align:center;padding:40px 16px;color:var(--text-muted,#64748b)">
        <i class="fas fa-bell-slash" style="font-size:2rem;opacity:.4;display:block;margin-bottom:8px"></i>
        <span style="font-size:.9rem">No notifications</span>
      </div>`;
    return;
  }

  const alertsUrl = `/Assistant/Alerts/${assistantId}`;
  const unread = items.filter(n => !n.isRead).length;

  listEl.innerHTML = `
    <div style="display:flex;align-items:center;justify-content:space-between;padding:0 4px 12px;border-bottom:1px solid var(--border-color,#e2e8f0);margin-bottom:8px">
      <span style="font-size:.8rem;color:var(--text-muted,#64748b)">${unread} unread</span>
      <div style="display:flex;gap:8px;align-items:center">
        ${unread > 0 ? `<button id="markAllReadBtn" type="button" style="font-size:.78rem;color:var(--assistant-primary,#7c3aed);background:none;border:none;cursor:pointer;padding:0">Mark all read</button>` : ""}
        <a href="${alertsUrl}" style="font-size:.78rem;color:var(--assistant-primary,#7c3aed);text-decoration:none">View all</a>
      </div>
    </div>
    ${items.map(n => {
      const style = _notifIconMap[(n.alertType || "info").toLowerCase()] || _notifIconMap.info;
      return `
        <div class="notification-item ${n.isRead ? "" : "unread"}" data-alert-id="${n.alertId}" style="cursor:pointer">
          <div class="notif-icon" style="color:${style.color};font-size:1.1rem;flex-shrink:0;padding-top:2px">
            <i class="fas ${style.icon}"></i>
          </div>
          <div class="notif-content" style="flex:1;min-width:0">
            <h4 style="margin:0 0 3px;font-size:.88rem;font-weight:600;color:var(--text-strong,#1e293b);white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${_escHtml(n.title)}</h4>
            <p style="margin:0 0 4px;font-size:.8rem;color:var(--text-body,#475569);display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden">${_escHtml(n.message || "")}</p>
            <span style="font-size:.73rem;color:var(--text-muted,#94a3b8)">${_escHtml(n.patientName)} &middot; ${_notifRelativeTime(n.dateCreated)}</span>
          </div>
        </div>`;
    }).join("")}`;

  // Mark all read
  const markAllBtn = listEl.querySelector("#markAllReadBtn");
  if (markAllBtn) {
    markAllBtn.addEventListener("click", async (e) => {
      e.stopPropagation();
      try {
        const tok = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
        const r = await fetch(`/Assistant/MarkAllAlertsRead/${assistantId}`, {
          method: "POST",
          headers: { "RequestVerificationToken": tok, "Content-Type": "application/x-www-form-urlencoded" },
          body: `__RequestVerificationToken=${encodeURIComponent(tok)}`
        });
        if (r.ok) {
          items.forEach(n => n.isRead = true);
          _renderNotifications(items, listEl, assistantId, badge, sidebarBadge);
          _setBadge(badge, 0);
          _setBadge(sidebarBadge, 0);
        }
      } catch { /* no-op */ }
    });
  }

  // Mark single read on click
  listEl.querySelectorAll(".notification-item[data-alert-id]").forEach(el => {
    el.addEventListener("click", async () => {
      const alertId = Number(el.dataset.alertId);
      const item = items.find(n => n.alertId === alertId);
      if (!item || item.isRead) return;
      try {
        const tok = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
        await fetch(`/Assistant/MarkAlertRead/${assistantId}?alertId=${alertId}`, {
          method: "POST",
          headers: { "RequestVerificationToken": tok, "Content-Type": "application/x-www-form-urlencoded" },
          body: `__RequestVerificationToken=${encodeURIComponent(tok)}&alertId=${alertId}`
        });
        item.isRead = true;
        el.classList.remove("unread");
        const newUnread = items.filter(n => !n.isRead).length;
        _setBadge(badge, newUnread);
        _setBadge(sidebarBadge, newUnread);
        // Re-render header row
        _renderNotifications(items, listEl, assistantId, badge, sidebarBadge);
      } catch { /* no-op */ }
    });
  });
}

function _escHtml(str) {
  return String(str || "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function initTopbarNotifications() {
  const notificationBtn   = document.getElementById("notificationBtn");
  const notificationsPanel = document.getElementById("notificationsPanel");
  const closeNotifications = document.getElementById("closeNotifications");
  const notificationBadge  = document.getElementById("notificationBadge");
  const notificationsList  = document.getElementById("notificationsList");

  if (!notificationBtn || !notificationsPanel) return;
  if (notificationBtn.dataset.notificationsInitialized === "true") return;
  notificationBtn.dataset.notificationsInitialized = "true";

  const assistantId = Number(document.body?.dataset?.assistantId || 0);
  const sidebarAlertsBadge = document.getElementById("sidebarAlertsBadge");
  let loaded = false;
  let cachedItems = null;

  async function loadNotifications() {
    if (!assistantId) return;
    if (notificationsList) {
      notificationsList.innerHTML = `<div style="text-align:center;padding:32px 16px;color:var(--text-muted,#64748b)"><i class="fas fa-spinner fa-spin"></i></div>`;
    }
    try {
      const r = await fetch(`/Assistant/GetNotificationsJson?id=${assistantId}`);
      if (!r.ok) throw new Error(r.status);
      cachedItems = await r.json();
      loaded = true;
      _renderNotifications(cachedItems, notificationsList, assistantId, notificationBadge, sidebarAlertsBadge);
      const unread = cachedItems.filter(n => !n.isRead).length;
      _setBadge(notificationBadge, unread);
      _setBadge(sidebarAlertsBadge, unread);
    } catch {
      if (notificationsList) {
        notificationsList.innerHTML = `<div style="text-align:center;padding:32px 16px;color:var(--danger,#ef4444);font-size:.85rem"><i class="fas fa-exclamation-circle"></i> Failed to load</div>`;
      }
    }
  }

  notificationBtn.addEventListener("click", function (e) {
    e.stopPropagation();
    const isOpening = !notificationsPanel.classList.contains("open");
    notificationsPanel.classList.toggle("open");
    if (isOpening && !loaded) {
      loadNotifications();
    }
  });

  if (closeNotifications) {
    closeNotifications.addEventListener("click", function () {
      notificationsPanel.classList.remove("open");
    });
  }

  document.addEventListener("click", function (e) {
    if (
      notificationsPanel.classList.contains("open") &&
      !notificationsPanel.contains(e.target) &&
      !notificationBtn.contains(e.target)
    ) {
      notificationsPanel.classList.remove("open");
    }
  });

  // Load badge count immediately (without opening panel)
  if (assistantId) {
    fetch(`/Assistant/GetUnreadAlertsCount?id=${assistantId}`)
      .then(r => r.ok ? r.json() : null)
      .then(data => {
        if (data) {
          _setBadge(notificationBadge, Number(data.unreadCount || 0));
          _setBadge(sidebarAlertsBadge, Number(data.unreadCount || 0));
        }
      })
      .catch(() => {});
  }
}

async function initAssistantTopbarBadge() {
  // Badge is now handled inside initTopbarNotifications; this is a no-op kept for compatibility.
}

// ================================
// Sidebar Unread Message Badge
// ================================
async function _fetchAndUpdateMsgBadge() {
  const badge = document.getElementById("sidebarMsgBadge");
  if (!badge) return;

  try {
    const resp = await fetch("/Chat/UnreadCount", { credentials: "same-origin" });
    if (!resp.ok) return;
    const data = await resp.json();
    const count = Number(data?.count || 0);
    if (count > 0) {
      badge.textContent = count > 99 ? "99+" : String(count);
      badge.style.display = "inline-flex";
    } else {
      badge.style.display = "none";
    }
  } catch {
    // no-op
  }
}

window.refreshSidebarMsgBadge = _fetchAndUpdateMsgBadge;

function initSidebarMessageBadge() {
  const badge = document.getElementById("sidebarMsgBadge");
  if (!badge) return;

  // Inject badge CSS once
  if (!document.getElementById("nav-msg-badge-style")) {
    const style = document.createElement("style");
    style.id = "nav-msg-badge-style";
    style.textContent = `
      .nav-msg-badge {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        margin-left: auto;
        min-width: 18px;
        height: 18px;
        padding: 0 5px;
        border-radius: 9px;
        background: #e53e3e;
        color: #fff;
        font-size: 0.7rem;
        font-weight: 700;
        line-height: 1;
        flex-shrink: 0;
        animation: badgePop .25s ease;
      }
      @keyframes badgePop {
        0%   { transform: scale(0.5); opacity: 0; }
        70%  { transform: scale(1.15); }
        100% { transform: scale(1);   opacity: 1; }
      }
      .sidebar.collapsed .nav-msg-badge { display: none !important; }
    `;
    document.head.appendChild(style);
  }

  _fetchAndUpdateMsgBadge();
  setInterval(_fetchAndUpdateMsgBadge, 60000);
}

// ================================
// Top-Bar Search
// ================================
function initTopbarSearch() {
  const btn = document.getElementById('topbarSearchBtn');
  if (!btn || btn.dataset.searchInitialized === 'true') return;
  btn.dataset.searchInitialized = 'true';

  // Inject overlay HTML once
  const overlay = document.createElement('div');
  overlay.id = 'topbarSearchOverlay';
  overlay.setAttribute('role', 'dialog');
  overlay.setAttribute('aria-modal', 'true');
  overlay.setAttribute('aria-label', 'Search');
  overlay.innerHTML = `
    <div id="topbarSearchBox">
      <i class="fas fa-search" style="color:var(--primary,#1baebe);font-size:.95rem;flex-shrink:0"></i>
      <input id="topbarSearchInput" type="search" placeholder="Search on this page…" autocomplete="off" spellcheck="false" />
      <span id="topbarSearchCount" style="font-size:.75rem;color:var(--text-muted,#64748b);white-space:nowrap"></span>
      <button id="topbarSearchClose" type="button" aria-label="Close search"><i class="fas fa-times"></i></button>
    </div>`;

  // Inject styles once
  if (!document.getElementById('topbarSearchStyles')) {
    const s = document.createElement('style');
    s.id = 'topbarSearchStyles';
    s.textContent = `
      #topbarSearchOverlay{position:fixed;inset:0;z-index:9900;background:rgba(15,23,42,.45);backdrop-filter:blur(2px);display:none;align-items:flex-start;justify-content:center;padding-top:100px}
      #topbarSearchOverlay.open{display:flex}
      #topbarSearchBox{display:flex;align-items:center;gap:10px;width:min(600px,90vw);background:#fff;border-radius:14px;padding:12px 16px;box-shadow:0 20px 60px rgba(15,23,42,.22);border:1px solid var(--border-subtle,#e2e8f0)}
      #topbarSearchInput{flex:1;border:none;outline:none;font-size:1rem;font-family:inherit;color:var(--text-strong,#1e293b);background:transparent}
      #topbarSearchInput::placeholder{color:var(--text-muted,#94a3b8)}
      #topbarSearchClose{background:none;border:none;cursor:pointer;color:var(--text-muted,#64748b);font-size:.9rem;padding:2px 4px;border-radius:4px;transition:color .15s}
      #topbarSearchClose:hover{color:var(--danger,#f44336)}
      mark.topbar-highlight{background:rgba(27,174,190,.25);color:inherit;border-radius:2px;padding:0 1px}
      mark.topbar-highlight-current{background:rgba(27,174,190,.6);outline:2px solid var(--primary,#1baebe);border-radius:2px}`;
    document.head.appendChild(s);
  }

  document.body.appendChild(overlay);

  const input   = document.getElementById('topbarSearchInput');
  const counter = document.getElementById('topbarSearchCount');
  const closeBtn= document.getElementById('topbarSearchClose');
  let marks = [], currentIdx = 0;

  function open() {
    overlay.classList.add('open');
    document.body.style.overflow = 'hidden';
    input.value = '';
    counter.textContent = '';
    clearMarks();
    setTimeout(() => input.focus(), 50);
  }

  function close() {
    overlay.classList.remove('open');
    document.body.style.overflow = '';
    clearMarks();
  }

  function clearMarks() {
    marks.forEach(m => {
      const parent = m.parentNode;
      if (parent) { parent.replaceChild(document.createTextNode(m.textContent), m); parent.normalize(); }
    });
    marks = []; currentIdx = 0;
  }

  function doSearch(term) {
    clearMarks();
    if (!term.trim()) { counter.textContent = ''; return; }
    const walker = document.createTreeWalker(
      document.body, NodeFilter.SHOW_TEXT,
      { acceptNode: n => (n.parentElement.closest('#topbarSearchOverlay,script,style,noscript') ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT) }
    );
    const regex = new RegExp(term.replace(/[.*+?^${}()|[\]\\]/g,'\\$&'), 'gi');
    const nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    nodes.forEach(node => {
      let m, last = 0; const text = node.textContent, frag = document.createDocumentFragment();
      while ((m = regex.exec(text)) !== null) {
        frag.appendChild(document.createTextNode(text.slice(last, m.index)));
        const mark = document.createElement('mark');
        mark.className = 'topbar-highlight'; mark.textContent = m[0];
        frag.appendChild(mark); marks.push(mark); last = regex.lastIndex;
      }
      if (marks.length && last < text.length) { frag.appendChild(document.createTextNode(text.slice(last))); node.parentNode.replaceChild(frag, node); }
    });
    if (marks.length) { marks[0].className = 'topbar-highlight topbar-highlight-current'; marks[0].scrollIntoView({block:'center',behavior:'smooth'}); }
    counter.textContent = marks.length ? `1 / ${marks.length}` : 'No results';
  }

  input.addEventListener('input', () => doSearch(input.value));
  input.addEventListener('keydown', e => {
    if (e.key === 'Enter' && marks.length) {
      marks[currentIdx].className = 'topbar-highlight';
      currentIdx = (currentIdx + 1) % marks.length;
      marks[currentIdx].className = 'topbar-highlight topbar-highlight-current';
      marks[currentIdx].scrollIntoView({block:'center',behavior:'smooth'});
      counter.textContent = `${currentIdx + 1} / ${marks.length}`;
    }
  });
  overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
  closeBtn.addEventListener('click', close);
  btn.addEventListener('click', open);
  document.addEventListener('keydown', e => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); overlay.classList.contains('open') ? close() : open(); }
    if (e.key === 'Escape' && overlay.classList.contains('open')) close();
  });
}

// ================================
// Initialize on DOM Ready
// ================================
document.addEventListener("DOMContentLoaded", function () {
  initSharedSidebar();
  initTopbarNotifications();
  initAssistantTopbarBadge();
  initSidebarMessageBadge();
  initTopbarSearch();
});

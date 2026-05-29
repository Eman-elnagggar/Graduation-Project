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
function initTopbarNotifications() {
  const notificationBtn = document.getElementById("notificationBtn");
  const notificationsPanel = document.getElementById("notificationsPanel");
  const closeNotifications = document.getElementById("closeNotifications");
  const notificationBadge = document.getElementById("notificationBadge");
  const notificationsList = document.getElementById("notificationsList");

  if (!notificationBtn || !notificationsPanel) return;
  if (notificationBtn.dataset.notificationsInitialized === "true") return;
  notificationBtn.dataset.notificationsInitialized = "true";

  const unreadCount = notificationsList
    ? notificationsList.querySelectorAll(".unread, [data-unread='true']").length
    : 0;

  if (notificationBadge) {
    notificationBadge.textContent = unreadCount > 0 ? String(unreadCount) : "";
    notificationBadge.style.display = unreadCount > 0 ? "block" : "none";
  }

  notificationBtn.addEventListener("click", function (e) {
    e.stopPropagation();
    notificationsPanel.classList.toggle("open");
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
}

async function initAssistantTopbarBadge() {
  const notificationBadge = document.getElementById("notificationBadge");
  if (!notificationBadge) return;

  const assistantId = Number(document.body?.dataset?.assistantId || 0);

  try {
    const response = await fetch(`/Assistant/GetUnreadAlertsCount?id=${assistantId}`);
    if (!response.ok) return;

    const data = await response.json();
    const count = Number(data?.unreadCount || 0);

    if (count > 0) {
      notificationBadge.textContent = count > 99 ? "99+" : String(count);
      notificationBadge.style.display = "inline-flex";
    } else {
      notificationBadge.textContent = "0";
      notificationBadge.style.display = "none";
    }
  } catch {
    // no-op
  }
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
  initTopbarSearch();
});

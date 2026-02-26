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
// Initialize on DOM Ready
// ================================
document.addEventListener("DOMContentLoaded", function () {
  initSharedSidebar();
});

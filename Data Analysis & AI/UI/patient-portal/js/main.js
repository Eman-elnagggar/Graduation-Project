// ================================
// Mother Portal - Main JavaScript
// ================================

document.addEventListener("DOMContentLoaded", function () {
  // ================================
  // Sidebar Toggle Functionality
  // ================================
  initSidebar();

  function initSidebar() {
    const sidebar = document.querySelector(".sidebar");
    const mainContent = document.querySelector(".main-content");

    // Skip if sidebar already initialized by shared.js
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

    // Add tooltips to nav items
    const navItems = sidebar.querySelectorAll(".nav-item");
    navItems.forEach((item) => {
      const text = item.querySelector("span")?.textContent || "";
      item.setAttribute("data-tooltip", text);
    });

    // Load saved state from localStorage
    const savedState = localStorage.getItem("sidebarCollapsed");
    if (savedState === "true" && window.innerWidth > 768) {
      sidebar.classList.add("collapsed");
      document.body.classList.add("sidebar-collapsed");
      toggleBtn.innerHTML = '<i class="fas fa-chevron-right"></i>';
      if (mainContent) {
        mainContent.style.marginLeft = "80px";
        mainContent.style.maxWidth = "calc(100% - 80px)";
      }
    }

    // Toggle button click handler
    toggleBtn.addEventListener("click", function () {
      if (window.innerWidth <= 768) {
        // Mobile: open/close sidebar
        sidebar.classList.toggle("mobile-open");
        overlay.classList.toggle("active");
      } else {
        // Desktop: collapse/expand sidebar
        sidebar.classList.toggle("collapsed");
        document.body.classList.toggle("sidebar-collapsed");

        // Update icon and main content
        const icon = this.querySelector("i");
        if (sidebar.classList.contains("collapsed")) {
          icon.className = "fas fa-chevron-right";
          localStorage.setItem("sidebarCollapsed", "true");
          if (mainContent) {
            mainContent.style.marginLeft = "80px";
            mainContent.style.maxWidth = "calc(100% - 80px)";
          }
        } else {
          icon.className = "fas fa-chevron-left";
          localStorage.setItem("sidebarCollapsed", "false");
          if (mainContent) {
            mainContent.style.marginLeft = "260px";
            mainContent.style.maxWidth = "calc(100% - 260px)";
          }
        }
      }
    });

    // Close button click handler (mobile)
    closeBtn.addEventListener("click", function () {
      sidebar.classList.remove("mobile-open");
      overlay.classList.remove("active");
    });

    // Overlay click handler (mobile)
    overlay.addEventListener("click", function () {
      sidebar.classList.remove("mobile-open");
      overlay.classList.remove("active");
    });

    // Handle window resize
    let resizeTimer;
    window.addEventListener("resize", function () {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(function () {
        if (window.innerWidth > 768) {
          sidebar.classList.remove("mobile-open");
          overlay.classList.remove("active");

          // Restore collapsed state on desktop
          const savedState = localStorage.getItem("sidebarCollapsed");
          if (savedState === "true") {
            sidebar.classList.add("collapsed");
            document.body.classList.add("sidebar-collapsed");
            toggleBtn.querySelector("i").className = "fas fa-chevron-right";
          }
        } else {
          // Remove collapsed state on mobile
          sidebar.classList.remove("collapsed");
          document.body.classList.remove("sidebar-collapsed");
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
  }

  // ================================
  // File Upload Functionality
  // ================================
  const uploadZone = document.getElementById("uploadZone");
  const fileInput = document.getElementById("fileInput");
  const uploadedFile = document.getElementById("uploadedFile");
  const fileName = document.getElementById("fileName");
  const removeFile = document.getElementById("removeFile");

  if (uploadZone && fileInput) {
    // Click to upload
    uploadZone.addEventListener("click", function () {
      fileInput.click();
    });

    // Drag and drop
    uploadZone.addEventListener("dragover", function (e) {
      e.preventDefault();
      uploadZone.style.borderColor = "#1BAEBE";
      uploadZone.style.background = "rgba(27, 174, 190, 0.1)";
    });

    uploadZone.addEventListener("dragleave", function (e) {
      e.preventDefault();
      uploadZone.style.borderColor = "#E2E8F0";
      uploadZone.style.background = "transparent";
    });

    uploadZone.addEventListener("drop", function (e) {
      e.preventDefault();
      uploadZone.style.borderColor = "#E2E8F0";
      uploadZone.style.background = "transparent";

      const files = e.dataTransfer.files;
      if (files.length > 0) {
        handleFileUpload(files[0]);
      }
    });

    // File input change
    fileInput.addEventListener("change", function () {
      if (fileInput.files.length > 0) {
        handleFileUpload(fileInput.files[0]);
      }
    });

    // Remove file
    if (removeFile) {
      removeFile.addEventListener("click", function () {
        uploadedFile.style.display = "none";
        uploadZone.style.display = "block";
        fileInput.value = "";
      });
    }
  }

  function handleFileUpload(file) {
    if (fileName) {
      fileName.textContent = file.name;
    }
    if (uploadedFile) {
      uploadedFile.style.display = "flex";
    }
    if (uploadZone) {
      uploadZone.style.display = "none";
    }
  }

  // ================================
  // Chatbot Functionality
  // ================================
  const chatInput = document.getElementById("chatInput");
  const sendBtn = document.getElementById("sendBtn");
  const chatMessages = document.getElementById("chatMessages");
  const suggestionChips = document.querySelectorAll(".suggestion-chip");

  if (sendBtn && chatInput) {
    sendBtn.addEventListener("click", sendMessage);

    chatInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") {
        sendMessage();
      }
    });
  }

  // Suggestion chips
  suggestionChips.forEach((chip) => {
    chip.addEventListener("click", function () {
      const text = this.textContent;
      if (chatInput) {
        chatInput.value = text;
        sendMessage();
      }
    });
  });

  function sendMessage() {
    const message = chatInput.value.trim();
    if (message === "") return;

    // Add user message
    addMessage(message, "user");
    chatInput.value = "";

    // Simulate bot response
    setTimeout(function () {
      const botResponse = getBotResponse(message);
      addMessage(botResponse, "bot");
    }, 1000);
  }

  function addMessage(content, type) {
    if (!chatMessages) return;

    const messageDiv = document.createElement("div");
    messageDiv.className = `message ${type}`;

    const time = new Date().toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    });

    if (type === "bot") {
      messageDiv.innerHTML = `
                <div class="message-avatar">
                    <i class="fas fa-robot"></i>
                </div>
                <div class="message-content">
                    <p>${content}</p>
                </div>
                <span class="message-time">${time}</span>
            `;
    } else {
      messageDiv.innerHTML = `
                <div class="message-content">
                    <p>${content}</p>
                </div>
                <span class="message-time">${time}</span>
            `;
    }

    chatMessages.appendChild(messageDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;
  }

  function getBotResponse(message) {
    const lowerMessage = message.toLowerCase();

    if (lowerMessage.includes("week") || lowerMessage.includes("tips")) {
      return "At week 24, your baby is about the size of a cantaloupe! 🍈 Make sure to stay hydrated, get plenty of rest, and keep up with your prenatal vitamins.";
    } else if (
      lowerMessage.includes("snack") ||
      lowerMessage.includes("food")
    ) {
      return "Great healthy snacks for pregnancy include: fruits, nuts, yogurt, cheese, and whole grain crackers. Avoid raw fish and unpasteurized dairy! 🍎";
    } else if (
      lowerMessage.includes("sleep") ||
      lowerMessage.includes("position")
    ) {
      return "Sleeping on your left side is recommended during pregnancy as it improves blood flow to your baby. Use pillows for support! 😴";
    } else if (
      lowerMessage.includes("exercise") ||
      lowerMessage.includes("workout")
    ) {
      return "Safe exercises during pregnancy include walking, swimming, prenatal yoga, and light stretching. Always consult your doctor before starting any exercise routine! 🏃‍♀️";
    } else {
      return "Thank you for your question! I'm here to help with pregnancy-related queries. Could you please provide more details so I can assist you better? 💕";
    }
  }

  // ================================
  // Image Upload Preview (Places Page)
  // ================================
  const imageUploadZones = document.querySelectorAll(".image-upload-zone");

  imageUploadZones.forEach((zone) => {
    const input = zone.querySelector('input[type="file"]');

    zone.addEventListener("click", function () {
      if (input) input.click();
    });

    if (input) {
      input.addEventListener("change", function () {
        if (input.files.length > 0) {
          const file = input.files[0];
          zone.innerHTML = `
                        <img src="${URL.createObjectURL(file)}" style="max-width: 100%; max-height: 150px; border-radius: 8px;">
                        <p style="margin-top: 10px; color: #4CAF50;">✓ Image selected</p>
                    `;
        }
      });
    }
  });

  // ================================
  // Notification Badge Animation
  // ================================
  const notifications = document.querySelectorAll(".notification");

  notifications.forEach((notification) => {
    notification.addEventListener("click", function () {
      const badge = this.querySelector(".badge");
      if (badge) {
        badge.style.display = "none";
      }
      showNotification("Notifications panel coming soon!", "info");
    });
  });

  // ================================
  // Button Hover Effects
  // ================================
  const buttons = document.querySelectorAll(".btn");

  buttons.forEach((btn) => {
    btn.addEventListener("mouseenter", function () {
      this.style.transform = "translateY(-2px)";
    });

    btn.addEventListener("mouseleave", function () {
      this.style.transform = "translateY(0)";
    });
  });

  // ================================
  // Form Validation Placeholder
  // ================================
  const forms = document.querySelectorAll("form");

  forms.forEach((form) => {
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      showNotification("Form submitted successfully!", "success");
    });
  });

  // ================================
  // Post Like Toggle
  // ================================
  const likeButtons = document.querySelectorAll(".post-btn:first-child");

  likeButtons.forEach((btn) => {
    btn.addEventListener("click", function () {
      this.classList.toggle("active");
      const icon = this.querySelector("i");
      if (this.classList.contains("active")) {
        icon.className = "fas fa-heart";
        this.innerHTML = '<i class="fas fa-heart"></i> Liked';
      } else {
        icon.className = "far fa-heart";
        this.innerHTML = '<i class="far fa-heart"></i> Like';
      }
    });
  });

  // ================================
  // Global Notification Function
  // ================================
  window.showNotification = function (message, type = "info") {
    // Remove existing notification
    const existing = document.querySelector(".global-notification");
    if (existing) existing.remove();

    const notification = document.createElement("div");
    notification.className = `global-notification ${type}`;

    let icon = "fa-info-circle";
    if (type === "success") icon = "fa-check-circle";
    if (type === "error") icon = "fa-exclamation-circle";
    if (type === "warning") icon = "fa-exclamation-triangle";

    notification.innerHTML = `
            <i class="fas ${icon}"></i>
            <span>${message}</span>
            <button class="notification-close"><i class="fas fa-times"></i></button>
        `;

    document.body.appendChild(notification);

    // Close button
    notification
      .querySelector(".notification-close")
      .addEventListener("click", () => {
        notification.remove();
      });

    // Auto-remove after 4 seconds
    setTimeout(() => {
      if (notification.parentNode) {
        notification.style.animation = "slideOutRight 0.3s ease forwards";
        setTimeout(() => notification.remove(), 300);
      }
    }, 4000);
  };

  // Add global notification styles if not already added
  if (!document.getElementById("global-notification-styles")) {
    const style = document.createElement("style");
    style.id = "global-notification-styles";
    style.textContent = `
            .global-notification {
                position: fixed;
                top: 24px;
                right: 24px;
                padding: 16px 20px;
                border-radius: 12px;
                display: flex;
                align-items: center;
                gap: 12px;
                font-weight: 500;
                z-index: 9999;
                animation: slideInRight 0.3s ease;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                max-width: 400px;
            }
            .global-notification.success {
                background: #E8F5E9;
                color: #2E7D32;
                border: 1px solid #A5D6A7;
            }
            .global-notification.error {
                background: #FFEBEE;
                color: #C62828;
                border: 1px solid #EF9A9A;
            }
            .global-notification.warning {
                background: #FFF3E0;
                color: #E65100;
                border: 1px solid #FFCC80;
            }
            .global-notification.info {
                background: #E3F2FD;
                color: #1565C0;
                border: 1px solid #90CAF9;
            }
            .global-notification span {
                flex: 1;
            }
            .global-notification .notification-close {
                background: none;
                border: none;
                cursor: pointer;
                color: inherit;
                opacity: 0.7;
                padding: 4px;
            }
            .global-notification .notification-close:hover {
                opacity: 1;
            }
            @keyframes slideInRight {
                from {
                    opacity: 0;
                    transform: translateX(100px);
                }
                to {
                    opacity: 1;
                    transform: translateX(0);
                }
            }
            @keyframes slideOutRight {
                from {
                    opacity: 1;
                    transform: translateX(0);
                }
                to {
                    opacity: 0;
                    transform: translateX(100px);
                }
            }
        `;
    document.head.appendChild(style);
  }

  console.log("MamaCare Portal initialized successfully! 🤰✨");
  console.log("Tip: Press Ctrl+B to toggle the sidebar!");
});

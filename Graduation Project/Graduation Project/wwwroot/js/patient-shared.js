// ================================
// MamaCare Patient Portal - Shared JS
// ================================

document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // Sidebar Toggle
    // ================================
    const sidebar = document.querySelector(".sidebar");

    if (sidebar && sidebar.dataset.sidebarInitialized !== "true") {
        sidebar.dataset.sidebarInitialized = "true";

        let toggleBtn = document.querySelector(".sidebar-toggle");
        let closeBtn = sidebar.querySelector(".sidebar-close");
        let overlay = document.querySelector(".sidebar-overlay");

        // Load saved state
        const savedState = localStorage.getItem("sidebarCollapsed");
        if (savedState === "true" && window.innerWidth > 768) {
            sidebar.classList.add("collapsed");
            document.body.classList.add("sidebar-collapsed");
            if (toggleBtn) toggleBtn.innerHTML = '<i class="fas fa-chevron-right"></i>';
        }

        // Toggle button
        if (toggleBtn) {
            toggleBtn.addEventListener("click", function () {
                if (window.innerWidth <= 768) {
                    sidebar.classList.toggle("mobile-open");
                    if (overlay) overlay.classList.toggle("active");
                } else {
                    sidebar.classList.toggle("collapsed");
                    document.body.classList.toggle("sidebar-collapsed");
                    const icon = this.querySelector("i");
                    if (sidebar.classList.contains("collapsed")) {
                        if (icon) icon.className = "fas fa-chevron-right";
                        localStorage.setItem("sidebarCollapsed", "true");
                    } else {
                        if (icon) icon.className = "fas fa-chevron-left";
                        localStorage.setItem("sidebarCollapsed", "false");
                    }
                }
            });
        }

        if (closeBtn) {
            closeBtn.addEventListener("click", function () {
                sidebar.classList.remove("mobile-open");
                if (overlay) overlay.classList.remove("active");
            });
        }

        if (overlay) {
            overlay.addEventListener("click", function () {
                sidebar.classList.remove("mobile-open");
                overlay.classList.remove("active");
            });
        }

        // Resize handler
        let resizeTimer;
        window.addEventListener("resize", function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                if (window.innerWidth > 768) {
                    sidebar.classList.remove("mobile-open");
                    if (overlay) overlay.classList.remove("active");
                    if (localStorage.getItem("sidebarCollapsed") === "true") {
                        sidebar.classList.add("collapsed");
                        document.body.classList.add("sidebar-collapsed");
                        if (toggleBtn) toggleBtn.querySelector("i").className = "fas fa-chevron-right";
                    }
                } else {
                    sidebar.classList.remove("collapsed");
                    document.body.classList.remove("sidebar-collapsed");
                    if (toggleBtn) toggleBtn.querySelector("i").className = "fas fa-bars";
                }
            }, 250);
        });

        if (window.innerWidth <= 768 && toggleBtn) {
            toggleBtn.querySelector("i").className = "fas fa-bars";
        }

        // Close on nav click (mobile)
        sidebar.querySelectorAll(".nav-item").forEach(item => {
            item.addEventListener("click", function () {
                if (window.innerWidth <= 768) {
                    sidebar.classList.remove("mobile-open");
                    if (overlay) overlay.classList.remove("active");
                }
            });
        });

        // Ctrl+B shortcut
        document.addEventListener("keydown", function (e) {
            if (e.ctrlKey && e.key === "b") {
                e.preventDefault();
                if (toggleBtn) toggleBtn.click();
            }
        });
    }

    // ================================
    // Global Notification
    // ================================
    window.showNotification = function (message, type = "info") {
        const existing = document.querySelector(".global-notification");
        if (existing) existing.remove();

        const notification = document.createElement("div");
        notification.className = `global-notification ${type}`;

        const icons = { success: "fa-check-circle", error: "fa-exclamation-circle", warning: "fa-exclamation-triangle", info: "fa-info-circle" };
        notification.innerHTML = `
            <i class="fas ${icons[type] || icons.info}"></i>
            <span>${message}</span>
            <button class="notification-close"><i class="fas fa-times"></i></button>
        `;
        document.body.appendChild(notification);

        notification.querySelector(".notification-close").addEventListener("click", () => notification.remove());
        setTimeout(() => {
            if (notification.parentNode) {
                notification.style.animation = "slideOutRight 0.3s ease forwards";
                setTimeout(() => notification.remove(), 300);
            }
        }, 4000);
    };

    // ================================
    // Button Hover Effects
    // ================================
    document.querySelectorAll(".btn").forEach(btn => {
        btn.addEventListener("mouseenter", () => btn.style.transform = "translateY(-2px)");
        btn.addEventListener("mouseleave", () => btn.style.transform = "translateY(0)");
    });

    // ================================
    // Notification Bell
    // ================================
    document.querySelectorAll(".notification").forEach(n => {
        n.addEventListener("click", function () {
            const badge = this.querySelector(".badge");
            if (badge) badge.style.display = "none";
            window.showNotification("Notifications panel coming soon!", "info");
        });
    });
});
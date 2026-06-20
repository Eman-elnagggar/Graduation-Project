document.addEventListener("DOMContentLoaded", () => {
  const patientId = Number(document.body?.dataset?.patientId || 0);
  const toggleBtn = document.getElementById("patientNotificationToggle");
  const closeBtn = document.getElementById("patientNotificationClose");
  const overlay = document.getElementById("patientNotificationOverlay");
  const panel = document.getElementById("patientNotificationPanel");
  const list = document.getElementById("patientNotificationList");
  const badge = document.getElementById("patientNotificationBadge");
  const topbarDate = document.getElementById("ppTopbarDateLabel");
  const topbarUserName = document.getElementById("ppTopbarUserName");
  const antiForgery = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
  let currentUnreadCount = 0;

  if (!toggleBtn || !closeBtn || !overlay || !panel || !list || !badge) return;

  if (!patientId) {
    toggleBtn.style.display = "none";
    return;
  }

  const openPanel = () => {
    document.body.classList.add("pp-notif-open");
    panel.setAttribute("aria-hidden", "false");
  };

  const closePanel = () => {
    document.body.classList.remove("pp-notif-open");
    panel.setAttribute("aria-hidden", "true");
  };

  const toRelativeTime = (dateValue) => {
    const date = new Date(dateValue);
    const now = new Date();
    const diffMs = now - date;
    const diffMin = Math.floor(diffMs / 60000);
    const diffHour = Math.floor(diffMin / 60);
    const diffDay = Math.floor(diffHour / 24);

    if (diffMin < 1) return "Just now";
    if (diffMin < 60) return `${diffMin} min ago`;
    if (diffHour < 24) return `${diffHour} hour${diffHour > 1 ? "s" : ""} ago`;
    if (diffDay < 7) return `${diffDay} day${diffDay > 1 ? "s" : ""} ago`;
    return date.toLocaleDateString();
  };

  const escapeHtml = (value) => {
    if (!value) return "";
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  };

  const getRiskType = (alertType) => {
    const t = String(alertType || "").toLowerCase();
    if (t === "danger" || t === "critical") return "critical";
    if (t === "warning") return "warning";
    return "info";
  };

  const riskIcon = (riskType) => {
    switch (riskType) {
      case "critical": return "exclamation-triangle";
      case "warning": return "exclamation-circle";
      default: return "info-circle";
    }
  };

  const riskColor = (riskType) => {
    switch (riskType) {
      case "critical": return "#e53e3e";
      case "warning": return "#f59e0b";
      default: return "#3182ce";
    }
  };

  const renderAlerts = (alerts) => {
    if (!alerts || alerts.length === 0) {
      list.innerHTML = '<div class="pp-notif-empty">No alerts found.</div>';
      return;
    }

    const unread = alerts.filter((a) => !a.isRead).length;

    const itemsHtml = alerts
      .map((alert) => {
        const title = alert.title || "Alert";
        const message = alert.message || "";
        const isUnread = !alert.isRead;
        const time = toRelativeTime(alert.dateCreated);
        const riskType = getRiskType(alert.alertType);
        const icon = riskIcon(riskType);
        const color = riskColor(riskType);

        return `
          <div class="pp-notif-item ${isUnread ? "unread" : ""}" data-alert-id="${alert.alertId}">
            <div class="pp-notif-icon" style="background:${color}22;">
              <i class="fas fa-${icon}" style="color:${color};"></i>
            </div>
            <div class="pp-notif-body">
              <div class="pp-notif-item-title">${escapeHtml(title)}</div>
              <div class="pp-notif-item-message">${escapeHtml(message)}</div>
              <div class="pp-notif-item-time">${time}</div>
            </div>
            ${isUnread ? '<span class="pp-notif-dot"></span>' : ""}
          </div>
        `;
      })
      .join("");

    const headerHtml = unread > 0
      ? '<div class="pp-notif-markall"><button type="button" class="pp-markall-btn" id="ppMarkAllReadBtn">Mark all as read</button></div>'
      : "";

    list.innerHTML = headerHtml + itemsHtml;
  };

  const updateBadge = (unreadCount) => {
    if (!unreadCount || unreadCount <= 0) {
      badge.hidden = true;
      badge.textContent = "0";
      return;
    }

    badge.hidden = false;
    badge.textContent = unreadCount > 99 ? "99+" : String(unreadCount);
  };

  const loadAlerts = async () => {
    try {
      const response = await fetch(`/PatientNotifications/GetNotifications?patientId=${patientId}`);
      if (!response.ok) throw new Error("Failed to load notifications.");

      const data = await response.json();
      if (!data.success) throw new Error(data.message || "Failed to load notifications.");

      renderAlerts(data.alerts || []);
      currentUnreadCount = data.unreadCount || 0;
      updateBadge(currentUnreadCount);
      if (topbarUserName && data.userName) {
        topbarUserName.textContent = data.userName;
      }
    } catch {
      list.innerHTML = '<div class="pp-notif-error">Unable to load alerts right now.</div>';
    }
  };

  const updateTopbarDate = () => {
    if (!topbarDate) return;
    const now = new Date();
    const formatted = now.toLocaleDateString("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
      year: "numeric",
    });
    topbarDate.textContent = formatted;
  };

  const markAlertRead = async (card) => {
    const alertId = Number(card?.dataset?.alertId || 0);
    if (!alertId || !antiForgery) return;
    if (!card.classList.contains("unread")) return;

    // Optimistic UI update
    card.classList.remove("unread");
    card.querySelector(".pp-notif-dot")?.remove();
    currentUnreadCount = Math.max(0, currentUnreadCount - 1);
    updateBadge(currentUnreadCount);
    if (currentUnreadCount === 0) {
      document.getElementById("ppMarkAllReadBtn")?.closest(".pp-notif-markall")?.remove();
    }

    try {
      const body = new URLSearchParams({
        alertId: String(alertId),
        patientId: String(patientId),
        __RequestVerificationToken: antiForgery,
      });

      await fetch("/PatientNotifications/MarkAlertRead", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: body.toString(),
        keepalive: true,
      });
    } catch {
      /* silently ignore — UI already updated */
    }
  };

  const markAllRead = async () => {
    if (!antiForgery) return;

    list.querySelectorAll(".pp-notif-item.unread").forEach((card) => {
      card.classList.remove("unread");
      card.querySelector(".pp-notif-dot")?.remove();
    });
    document.getElementById("ppMarkAllReadBtn")?.closest(".pp-notif-markall")?.remove();
    currentUnreadCount = 0;
    updateBadge(0);

    try {
      const body = new URLSearchParams({
        patientId: String(patientId),
        __RequestVerificationToken: antiForgery,
      });

      await fetch("/PatientNotifications/MarkAllAlertsRead", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: body.toString(),
        keepalive: true,
      });
    } catch {
      /* silently ignore — UI already updated */
    }
  };

  list.addEventListener("click", async (e) => {
    const markAllBtn = e.target.closest("#ppMarkAllReadBtn");
    if (markAllBtn) {
      e.stopPropagation();
      await markAllRead();
      return;
    }

    const card = e.target.closest(".pp-notif-item");
    if (!card) return;
    await markAlertRead(card);
  });

  toggleBtn.addEventListener("click", async () => {
    await loadAlerts();
    openPanel();
  });

  closeBtn.addEventListener("click", closePanel);
  overlay.addEventListener("click", closePanel);

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && document.body.classList.contains("pp-notif-open")) {
      closePanel();
    }
  });

  updateTopbarDate();
  loadAlerts();
});

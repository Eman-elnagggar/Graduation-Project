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

  const renderAlerts = (alerts) => {
    if (!alerts || alerts.length === 0) {
      list.innerHTML = '<div class="pp-notif-empty">No alerts found.</div>';
      return;
    }

    list.innerHTML = alerts
      .map((alert) => {
        const title = alert.title || "Alert";
        const message = alert.message || "";
        const isUnread = !alert.isRead;
        const time = toRelativeTime(alert.dateCreated);
        const riskType = getRiskType(alert.alertType);
        const riskLabel = riskType.toUpperCase();

        return `
          <article class="pp-notif-item pp-notif-${riskType} ${isUnread ? "unread" : ""}" data-alert-id="${alert.alertId}">
            <div class="pp-notif-item-top">
              <div class="pp-notif-item-title-wrap">
                <span class="pp-risk-pill pp-risk-${riskType}">${riskLabel}</span>
                <h4 class="pp-notif-item-title">${escapeHtml(title)}</h4>
              </div>
              <span class="pp-notif-item-time">${time}</span>
            </div>
            <p class="pp-notif-item-message">${escapeHtml(message)}</p>
            <div class="pp-notif-item-footer">
              ${isUnread
                ? '<button type="button" class="pp-mark-read-btn">Mark as read</button>'
                : '<span class="pp-read-pill"><i class="fas fa-check-circle"></i> Read</span>'}
            </div>
          </article>
        `;
      })
      .join("");
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
      const response = await fetch(`/PatientAlerts/GetNotifications?patientId=${patientId}`);
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

  const markAlertRead = async (card, button) => {
    const alertId = Number(card?.dataset?.alertId || 0);
    if (!alertId || !antiForgery) return;

    button.disabled = true;
    button.textContent = "Marking...";

    try {
      const body = new URLSearchParams({
        alertId: String(alertId),
        patientId: String(patientId),
        __RequestVerificationToken: antiForgery,
      });

      const response = await fetch("/PatientAlerts/MarkAlertRead", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: body.toString(),
      });

      if (!response.ok) throw new Error("Request failed");
      const data = await response.json();
      if (!data.success) throw new Error("Request failed");

      card.classList.remove("unread");
      button.outerHTML = '<span class="pp-read-pill"><i class="fas fa-check-circle"></i> Read</span>';
      currentUnreadCount = Math.max(0, currentUnreadCount - 1);
      updateBadge(currentUnreadCount);
    } catch {
      button.disabled = false;
      button.textContent = "Mark as read";
    }
  };

  let notificationConnection = null;

  const startRealtimeNotifications = async () => {
    if (!window.signalR || !patientId) return;

    notificationConnection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/notifications")
      .withAutomaticReconnect()
      .build();

    notificationConnection.on("AlertCreated", async (alert) => {
      // safest: reload canonical data from DB
      await loadAlerts();
    });

    notificationConnection.onreconnected(async () => {
      await loadAlerts();
    });

    try {
      await notificationConnection.start();
    } catch {
      // optional: silent fail; polling/open-panel load still works
    }
  };

  list.addEventListener("click", async (e) => {
    const btn = e.target.closest(".pp-mark-read-btn");
    if (!btn) return;
    const card = btn.closest(".pp-notif-item");
    await markAlertRead(card, btn);
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
  startRealtimeNotifications();
});

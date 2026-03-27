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

  let notificationConnection = null;
  const seenAlertIds = new Set();
  let hasLoadedOnce = false;
  let isConnecting = false;
  let reconnectTimer = null;
  let pollTimer = null;

  const showFallbackToast = (text, kind = "info") => {
    const old = document.getElementById("ppRealtimeToast");
    if (old) old.remove();

    const toast = document.createElement("div");
    toast.id = "ppRealtimeToast";
    toast.textContent = text;
    toast.style.position = "fixed";
    toast.style.right = "20px";
    toast.style.bottom = "20px";
    toast.style.zIndex = "99999";
    toast.style.padding = "12px 16px";
    toast.style.borderRadius = "10px";
    toast.style.color = "#fff";
    toast.style.maxWidth = "360px";
    toast.style.boxShadow = "0 8px 24px rgba(0,0,0,.2)";
    toast.style.background = kind === "error" ? "#dc2626" : kind === "warning" ? "#d97706" : "#2563eb";

    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 10000);
  };

  const showRealtimePopup = (alert) => {
    if (!alert) return;

    const risk = getRiskType(alert.alertType);
    const title = alert.title || "New alert";
    const message = alert.message || "";
    const popupText = `${title}: ${message}`;

    if (typeof showToast === "function") {
      const toastType = risk === "critical" ? "error" : risk === "warning" ? "warning" : "info";
      showToast(popupText, toastType, 10000);
    } else {
      const fallbackType = risk === "critical" ? "error" : risk === "warning" ? "warning" : "info";
      showFallbackToast(popupText, fallbackType);
    }
  };

  const rememberAlertIds = (alerts) => {
    (alerts || []).forEach((a) => {
      const id = Number(a?.alertId || 0);
      if (id > 0) seenAlertIds.add(id);
    });
  };

  const loadAlerts = async () => {
    try {
      const response = await fetch(`/PatientAlerts/GetNotifications?patientId=${patientId}`);
      if (!response.ok) throw new Error("Failed to load notifications.");

      const data = await response.json();
      if (!data.success) throw new Error(data.message || "Failed to load notifications.");

      const alerts = data.alerts || [];

      // show popup for new alerts even if SignalR missed
      const newAlerts = hasLoadedOnce
        ? alerts.filter((a) => {
            const id = Number(a?.alertId || 0);
            return id > 0 && !seenAlertIds.has(id);
          })
        : [];

      renderAlerts(alerts);
      rememberAlertIds(alerts);

      if (hasLoadedOnce && newAlerts.length > 0) {
        newAlerts.slice().reverse().forEach(showRealtimePopup);
      }

      hasLoadedOnce = true;
      currentUnreadCount = data.unreadCount || 0;
      updateBadge(currentUnreadCount);

      if (topbarUserName && data.userName) {
        topbarUserName.textContent = data.userName;
      }
    } catch {
      list.innerHTML = '<div class="pp-notif-error">Unable to load alerts right now.</div>';
    }
  };

  const startPollingFallback = () => {
    if (pollTimer) return;
    pollTimer = setInterval(async () => {
      await loadAlerts();
    }, 15000);
  };

  const startRealtimeNotifications = async () => {
    if (!window.signalR || !patientId) return;
    if (notificationConnection) return;

    notificationConnection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/notifications")
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    notificationConnection.on("AlertCreated", async (alert) => {
      const id = Number(alert?.alertId || 0);
      if (id > 0 && seenAlertIds.has(id)) return;
      if (id > 0) seenAlertIds.add(id);

      showRealtimePopup(alert);
      await loadAlerts();
    });

    notificationConnection.onreconnected(async () => {
      await loadAlerts();
    });

    notificationConnection.onclose(() => {
      if (reconnectTimer) return;
      reconnectTimer = setTimeout(async () => {
        reconnectTimer = null;
        await connectWithRetry();
      }, 5000);
    });

    const connectWithRetry = async () => {
      if (isConnecting) return;
      isConnecting = true;
      try {
        await notificationConnection.start();
        console.log("[Notifications] SignalR connected");
      } catch (err) {
        console.error("[Notifications] SignalR start failed, retry in 5s", err);
        setTimeout(connectWithRetry, 5000);
      } finally {
        isConnecting = false;
      }
    };

    await connectWithRetry();
  };

  list.addEventListener("click", async (e) => {
    const btn = e.target.closest(".pp-mark-read-btn");
    if (!btn) return;
    const card = btn.closest(".pp-notif-item");
    await markAlertRead(card, btn);
  });

  toggleBtn.addEventListener("click", async () => {
    if ("Notification" in window && Notification.permission === "default") {
      try { await Notification.requestPermission(); } catch { /* ignore */ }
    }

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
  startPollingFallback();
});

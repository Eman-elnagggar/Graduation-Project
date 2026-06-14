/* ================================
   Doctor Notifications Panel
   Fetches from /Doctor/GetNotifications and renders into the shared panel
================================ */

(function () {
  var _pollTimer = null;
  var _antiforgery = null;

  function getAntiforgeryToken() {
    if (_antiforgery) return _antiforgery;
    var el = document.querySelector('[name="__RequestVerificationToken"]');
    _antiforgery = el ? el.value : "";
    return _antiforgery;
  }

  function typeIcon(type) {
    switch (type) {
      case "patient_risk":     return "exclamation-triangle";
      case "invitation_accepted": return "user-check";
      case "admin_approved":   return "check-circle";
      default:                 return "bell";
    }
  }

  function typeColor(type) {
    switch (type) {
      case "patient_risk":     return "#e53e3e";
      case "invitation_accepted": return "#38a169";
      case "admin_approved":   return "#38a169";
      default:                 return "#3182ce";
    }
  }

  function timeAgo(dateStr) {
    var date = new Date(dateStr);
    var now = new Date();
    var diffMs = now - date;
    var diffMins = Math.floor(diffMs / 60000);
    var diffHours = Math.floor(diffMs / 3600000);
    var diffDays = Math.floor(diffMs / 86400000);
    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return diffMins + " min ago";
    if (diffHours < 24) return diffHours + " hour" + (diffHours > 1 ? "s" : "") + " ago";
    if (diffDays < 7) return diffDays + " day" + (diffDays > 1 ? "s" : "") + " ago";
    return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
  }

  function renderNotification(n) {
    var icon = typeIcon(n.notificationType);
    var color = typeColor(n.notificationType);
    var time = timeAgo(n.dateCreated);
    var unreadClass = n.isRead ? "" : " notif-unread";
    var url = n.actionUrl || "#";

    return (
      '<div class="notif-item' + unreadClass + '" data-id="' + n.id + '" style="' +
        'display:flex;align-items:flex-start;gap:12px;padding:14px 16px;' +
        'border-bottom:1px solid var(--border-color,#e2e8f0);cursor:pointer;' +
        'background:' + (n.isRead ? "transparent" : "rgba(27,174,190,0.04)") + ';' +
        'transition:background 0.2s;">' +
        '<div style="width:36px;height:36px;border-radius:50%;background:' + color + '22;' +
          'display:flex;align-items:center;justify-content:center;flex-shrink:0;">' +
          '<i class="fas fa-' + icon + '" style="color:' + color + ';font-size:14px;"></i>' +
        '</div>' +
        '<div style="flex:1;min-width:0;">' +
          '<div style="font-size:0.82rem;font-weight:' + (n.isRead ? "500" : "600") + ';' +
            'color:var(--text-primary,#1a202c);margin-bottom:2px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">' +
            escapeHtml(n.title) +
          '</div>' +
          '<div style="font-size:0.75rem;color:var(--text-secondary,#718096);line-height:1.4;' +
            'display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;">' +
            escapeHtml(n.message) +
          '</div>' +
          '<div style="font-size:0.7rem;color:var(--text-muted,#a0aec0);margin-top:4px;">' +
            time +
          '</div>' +
        '</div>' +
        (n.isRead ? "" : '<div style="width:8px;height:8px;border-radius:50%;background:#1BAEBE;flex-shrink:0;margin-top:4px;"></div>') +
      '</div>'
    );
  }

  function escapeHtml(str) {
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function markRead(notificationId, itemEl) {
    // keepalive: true keeps the request alive even if the page navigates away
    fetch("/Doctor/MarkNotificationRead", {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "RequestVerificationToken": getAntiforgeryToken()
      },
      body: "notificationId=" + notificationId,
      keepalive: true
    });
    if (itemEl) {
      itemEl.style.background = "transparent";
      itemEl.classList.remove("notif-unread");
      var dot = itemEl.querySelector('div[style*="8px;height:8px"]');
      if (dot) dot.remove();
    }
  }

  function markAllRead() {
    fetch("/Doctor/MarkAllNotificationsRead", {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "RequestVerificationToken": getAntiforgeryToken()
      },
      body: ""
    });
  }

  function updateBadge(count) {
    var badge = document.getElementById("notificationBadge");
    if (!badge) return;
    if (count > 0) {
      badge.textContent = count > 99 ? "99+" : String(count);
      badge.style.display = "flex";
    } else {
      badge.style.display = "none";
    }
  }

  function loadNotifications() {
    var list = document.getElementById("notificationsList");
    if (!list) return;

    fetch("/Doctor/GetNotifications", { credentials: "same-origin" })
      .then(function (r) { return r.ok ? r.json() : []; })
      .then(function (data) {
        var unread = data.filter(function (n) { return !n.isRead; }).length;
        updateBadge(unread);

        if (data.length === 0) {
          list.innerHTML =
            '<div style="text-align:center;padding:32px 16px;color:var(--text-secondary,#718096);">' +
            '<i class="fas fa-bell-slash" style="font-size:2rem;margin-bottom:8px;opacity:0.4;display:block;"></i>' +
            '<div style="font-size:0.85rem;">No notifications yet</div></div>';
          return;
        }

        var html = data.map(renderNotification).join("");

        // "Mark all read" header button if there are unread items
        if (unread > 0) {
          html = '<div style="padding:8px 16px;border-bottom:1px solid var(--border-color,#e2e8f0);' +
            'display:flex;justify-content:flex-end;">' +
            '<button id="markAllReadBtn" style="font-size:0.75rem;color:var(--primary,#1BAEBE);' +
            'background:none;border:none;cursor:pointer;padding:4px 8px;">Mark all as read</button></div>' +
            html;
        }

        list.innerHTML = html;

        // Attach click handlers
        list.querySelectorAll(".notif-item").forEach(function (item) {
          item.addEventListener("click", function () {
            var id = parseInt(item.dataset.id);
            var wasUnread = item.classList.contains("notif-unread");

            // Always mark as read on click (keepalive survives page navigation)
            markRead(id, item);
            if (wasUnread) {
              var badge = document.getElementById("notificationBadge");
              var prev = parseInt(badge?.textContent) || 0;
              updateBadge(Math.max(0, prev - 1));
            }

            // Navigate to action URL
            var notification = data.find(function (n) { return n.id === id; });
            if (notification && notification.actionUrl && notification.actionUrl !== "#") {
              window.location.href = notification.actionUrl;
            }
          });
        });

        var markAllBtn = document.getElementById("markAllReadBtn");
        if (markAllBtn) {
          markAllBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            markAllRead();
            list.querySelectorAll(".notif-item").forEach(function (item) {
              item.style.background = "transparent";
              item.classList.remove("notif-unread");
              var dot = item.querySelector('div[style*="8px;height:8px"]');
              if (dot) dot.remove();
            });
            markAllBtn.parentElement.remove();
            updateBadge(0);
          });
        }
      })
      .catch(function () { /* silently ignore network errors */ });
  }

  function initDoctorNotifications() {
    var notificationBtn = document.getElementById("notificationBtn");
    var panel = document.getElementById("notificationsPanel");
    if (!notificationBtn || !panel) return;

    // Load on first open
    var loaded = false;
    notificationBtn.addEventListener("click", function () {
      if (!loaded || panel.classList.contains("open")) {
        loadNotifications();
        loaded = true;
      }
    });

    // Initial badge count (lightweight)
    fetch("/Doctor/GetUnreadNotificationsCount", { credentials: "same-origin" })
      .then(function (r) { return r.ok ? r.json() : { unreadCount: 0 }; })
      .then(function (d) { updateBadge(d.unreadCount || 0); })
      .catch(function () {});

    // Poll for new notifications every 60 seconds
    _pollTimer = setInterval(function () {
      fetch("/Doctor/GetUnreadNotificationsCount", { credentials: "same-origin" })
        .then(function (r) { return r.ok ? r.json() : { unreadCount: 0 }; })
        .then(function (d) {
          var count = d.unreadCount || 0;
          var prevCount = parseInt(document.getElementById("notificationBadge")?.textContent) || 0;
          updateBadge(count);
          // Refresh panel content if it's open and count changed
          if (count !== prevCount && panel.classList.contains("open")) {
            loadNotifications();
          }
        })
        .catch(function () {});
    }, 60000);
  }

  document.addEventListener("DOMContentLoaded", initDoctorNotifications);
})();

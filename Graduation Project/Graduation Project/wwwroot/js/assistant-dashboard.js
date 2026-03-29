(function () {
  const getMeta = (name) => document.querySelector(`meta[name="${name}"]`)?.content ?? "";

  const assistantId = Number(getMeta("assistant-id"));
  const baseStatsUrl = getMeta("assistant-stats-url");
  const baseScheduleUrl = getMeta("assistant-schedule-url");
  const baseIndexUrl = getMeta("assistant-index-url");

  if (!assistantId || !baseStatsUrl || !baseScheduleUrl || !baseIndexUrl) {
    return;
  }

  const notificationBtn = document.getElementById("notificationBtn") || document.querySelector(".notification");
  const notificationsPanel = document.querySelector(".notifications-panel");
  const closeNotifications = document.querySelector(".close-notifications");

  if (notificationBtn && notificationsPanel) {
    notificationBtn.addEventListener("click", () => {
      notificationsPanel.classList.toggle("open");
    });

    if (closeNotifications) {
      closeNotifications.addEventListener("click", () => {
        notificationsPanel.classList.remove("open");
      });
    }

    document.addEventListener("click", (e) => {
      if (!notificationsPanel.contains(e.target) && !notificationBtn.contains(e.target)) {
        notificationsPanel.classList.remove("open");
      }
    });
  }

  const todayDateEl = document.getElementById("todayDate");
  const datePicker = document.getElementById("dashboardDatePicker");
  const prevDateBtn = document.getElementById("prevDateBtn");
  const nextDateBtn = document.getElementById("nextDateBtn");
  const todayBtn = document.getElementById("todayBtn");
  const scheduleContainer = document.getElementById("schedule-container");

  const formatDisplayDate = (isoDate) => {
    const [y, m, d] = isoDate.split("-").map(Number);
    const dt = new Date(y, m - 1, d);
    return dt.toLocaleDateString("en-US", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const shiftDate = (isoDate, days) => {
    const [y, m, d] = isoDate.split("-").map(Number);
    const dt = new Date(y, m - 1, d);
    dt.setDate(dt.getDate() + days);
    return `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, "0")}-${String(dt.getDate()).padStart(2, "0")}`;
  };

  const selectedDoctorMeta = getMeta("selected-doctor-id");
  let currentDoctorId = selectedDoctorMeta ? Number(selectedDoctorMeta) : null;
  let currentDate = getMeta("selected-date") || new Date().toISOString().slice(0, 10);
  let currentScheduleStatus = getMeta("selected-schedule-status") || "Booked";

  if (datePicker) {
    datePicker.value = currentDate;
  }

  if (todayDateEl) {
    todayDateEl.textContent = formatDisplayDate(currentDate);
  }

  const doctorNames = {};
  document.querySelectorAll(".doctor-filter-tab[data-doctor-id][data-doctor-name]").forEach((tab) => {
    const id = tab.dataset.doctorId;
    if (!id) {
      return;
    }

    const doctorId = Number(id);
    if (Number.isNaN(doctorId)) {
      return;
    }

    doctorNames[doctorId] = tab.dataset.doctorName;
  });

  const setTxt = (id, val) => {
    const el = document.getElementById(id);
    if (el) {
      el.textContent = val;
    }
  };

  function updateScheduleStatusButtons() {
    document.querySelectorAll(".schedule-status-btn").forEach((btn) => {
      const isActive = btn.dataset.status === currentScheduleStatus;
      btn.classList.toggle("btn-primary", isActive);
      btn.classList.toggle("btn-outline", !isActive);
    });
  }

  function updateTabStyles(activeDoctorId) {
    document.querySelectorAll(".doctor-filter-tab").forEach((tab) => {
      const tabId = tab.dataset.doctorId;
      const isAllTab = tabId === "" || tabId === undefined;
      const isActive = isAllTab ? activeDoctorId === null : Number(tabId) === activeDoctorId;
      tab.classList.toggle("active", isActive);
    });
  }

  function loadDashboardData(doctorId, selectedDate, pushState, scheduleStatus) {
    currentDoctorId = doctorId;
    currentDate = selectedDate;
    currentScheduleStatus = scheduleStatus || currentScheduleStatus;

    if (datePicker) {
      datePicker.value = currentDate;
    }

    if (pushState !== false) {
      const newUrl =
        `${baseIndexUrl}?id=${assistantId}&date=${currentDate}` +
        `&status=${encodeURIComponent(currentScheduleStatus)}` +
        (doctorId !== null ? `&doctorId=${doctorId}` : "");

      history.pushState(
        {
          assistantId,
          doctorId,
          date: currentDate,
          status: currentScheduleStatus,
        },
        "",
        newUrl,
      );
    }

    updateTabStyles(doctorId);
    updateScheduleStatusButtons();

    const label = document.getElementById("schedule-doctor-label");
    const nameSpan = document.getElementById("schedule-doctor-name");
    if (label && nameSpan) {
      if (doctorId !== null && doctorNames[doctorId]) {
        nameSpan.textContent = doctorNames[doctorId];
        label.classList.remove("is-hidden");
      } else {
        label.classList.add("is-hidden");
      }
    }

    if (scheduleContainer) {
      scheduleContainer.innerHTML =
        '<div class="loading-state"><i class="fas fa-spinner fa-spin"></i><p>Loading schedule...</p></div>';
    }

    const qs =
      `?id=${assistantId}&date=${encodeURIComponent(currentDate)}` +
      (doctorId !== null ? `&doctorId=${doctorId}` : "");

    fetch(baseStatsUrl + qs)
      .then((r) => {
        if (!r.ok) {
          throw new Error(r.status);
        }

        return r.json();
      })
      .then((data) => {
        if (todayDateEl) {
          todayDateEl.textContent = data.selectedDateLabel || formatDisplayDate(currentDate);
        }

        setTxt("stat-today-appointments", data.todayAppointmentsCount);
        setTxt("stat-total-patients", data.totalPatients);
        setTxt("hero-appointment-count", data.todayAppointmentsCount);

        const assistantHeaderBadge = document.getElementById("alert-badge");
        const topbarBadge = document.getElementById("notificationBadge");
        const nextBadgeText = data.pendingAlertsCount > 99 ? "99+" : String(data.pendingAlertsCount);

        if (assistantHeaderBadge) {
          if (data.pendingAlertsCount > 0) {
            assistantHeaderBadge.textContent = nextBadgeText;
            assistantHeaderBadge.hidden = false;
          } else {
            assistantHeaderBadge.hidden = true;
          }
        }

        if (topbarBadge) {
          if (data.pendingAlertsCount > 0) {
            topbarBadge.textContent = nextBadgeText;
            topbarBadge.hidden = false;
          } else {
            topbarBadge.textContent = "0";
            topbarBadge.hidden = true;
          }
        }

        let totalAppts = 0;
        if (data.doctorCounts) {
          data.doctorCounts.forEach((dc) => {
            totalAppts += dc.todayAppointments;
            setTxt(`doctor-tab-count-${dc.doctorId}`, dc.todayAppointments);
            setTxt(`doctor-today-${dc.doctorId}`, dc.todayAppointments);
            setTxt(`doctor-patients-${dc.doctorId}`, dc.totalPatients);
          });
        }

        setTxt("all-doctors-tab-count", totalAppts);
      })
      .catch((err) => {
        console.error("Failed to load stats:", err);
        ["stat-today-appointments", "stat-total-patients", "hero-appointment-count"].forEach((id) => setTxt(id, "!"));
      });

    fetch(`${baseScheduleUrl}${qs}&status=${encodeURIComponent(currentScheduleStatus)}`)
      .then((r) => {
        if (!r.ok) {
          throw new Error(r.status);
        }

        return r.text();
      })
      .then((html) => {
        if (scheduleContainer) {
          scheduleContainer.innerHTML = html;
        }
      })
      .catch((err) => {
        console.error("Failed to load schedule:", err);
        if (scheduleContainer) {
          scheduleContainer.innerHTML =
            '<div class="privacy-notice privacy-notice-danger"><i class="fas fa-exclamation-circle text-danger"></i><p>Failed to load schedule. Please refresh the page.</p></div>';
        }
      });
  }

  const tabContainer = document.querySelector(".doctor-filter-tabs");
  if (tabContainer) {
    tabContainer.addEventListener("click", (e) => {
      const tab = e.target.closest(".doctor-filter-tab");
      if (!tab) {
        return;
      }

      e.preventDefault();
      const tabId = tab.dataset.doctorId;
      const newDoctorId = tabId === "" || tabId === undefined ? null : Number(tabId);
      if (newDoctorId === currentDoctorId) {
        return;
      }

      loadDashboardData(newDoctorId, currentDate);
    });

    tabContainer.addEventListener(
      "mouseenter",
      (e) => {
        const tab = e.target.closest(".doctor-filter-tab:not(.active)");
        if (tab) {
          tab.classList.add("hovered");
        }
      },
      true,
    );

    tabContainer.addEventListener(
      "mouseleave",
      (e) => {
        const tab = e.target.closest(".doctor-filter-tab:not(.active)");
        if (tab) {
          tab.classList.remove("hovered");
        }
      },
      true,
    );
  }

  document.querySelectorAll(".team-card[data-doctor-id]").forEach((card) => {
    card.addEventListener("click", () => {
      const docId = Number(card.dataset.doctorId);
      if (docId !== currentDoctorId) {
        loadDashboardData(docId, currentDate);
      }
    });
  });

  document.querySelectorAll(".schedule-status-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const selectedStatus = btn.dataset.status || "Booked";
      if (selectedStatus === currentScheduleStatus) {
        return;
      }

      loadDashboardData(currentDoctorId, currentDate, true, selectedStatus);
    });
  });

  window.addEventListener("popstate", (e) => {
    if (e.state && e.state.assistantId) {
      loadDashboardData(e.state.doctorId, e.state.date || currentDate, false, e.state.status || "Booked");
    }
  });

  if (datePicker) {
    datePicker.addEventListener("change", () => {
      if (!datePicker.value) {
        return;
      }

      loadDashboardData(currentDoctorId, datePicker.value);
    });
  }

  prevDateBtn?.addEventListener("click", () => {
    loadDashboardData(currentDoctorId, shiftDate(currentDate, -1));
  });

  nextDateBtn?.addEventListener("click", () => {
    loadDashboardData(currentDoctorId, shiftDate(currentDate, 1));
  });

  todayBtn?.addEventListener("click", () => {
    const todayIso = new Date().toISOString().slice(0, 10);
    loadDashboardData(currentDoctorId, todayIso);
  });

  history.replaceState(
    {
      assistantId,
      doctorId: currentDoctorId,
      date: currentDate,
      status: currentScheduleStatus,
    },
    "",
    location.href,
  );

  loadDashboardData(currentDoctorId, currentDate, false, currentScheduleStatus);
})();

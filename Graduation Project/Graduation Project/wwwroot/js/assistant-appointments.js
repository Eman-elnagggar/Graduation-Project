const assistantAppointmentsConfig = window.assistantAppointmentsConfig || {};

const assistantId = Number(assistantAppointmentsConfig.assistantId || 0);
const selectedDoctorId = assistantAppointmentsConfig.selectedDoctorId ?? null;
const urls = assistantAppointmentsConfig.urls || {};

const ALL_STATUSES = ["Confirmed", "Modified", "Cancelled", "Missed"];
const PAGE_SIZE = 20;
const AUTO_REFRESH_MS = 60_000;
let currentDate = assistantAppointmentsConfig.selectedDate || new Date().toISOString().slice(0, 10);
const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

let currentTab = "All";
let modifyingAppointmentId = null;
let modifyingDoctorId = null;
let autoRefreshTimer = null;
let isBusy = false;
let currentSearch = "";
let searchDebounce = null;
const paginationState = {
    All: { page: 1, totalPages: 1, total: 0 },
    Confirmed: { page: 1, totalPages: 1, total: 0 },
    Modified: { page: 1, totalPages: 1, total: 0 },
    Cancelled: { page: 1, totalPages: 1, total: 0 },
    Missed: { page: 1, totalPages: 1, total: 0 }
};

const cache = {};

function formatTime(time24) {
    if (!time24) return { time: "--", period: "" };
    const parts = time24.split(":");
    if (parts.length < 2) return { time: time24, period: "" };
    const hour = parseInt(parts[0], 10);
    if (isNaN(hour)) return { time: time24, period: "" };
    const ampm = hour >= 12 ? "PM" : "AM";
    const hour12 = hour % 12 || 12;
    return { time: `${hour12}:${parts[1]}`, period: ampm };
}

function formatDate(dateStr) {
    if (!dateStr) return "–";
    const [y, m, d] = dateStr.split("-").map(Number);
    if (!y || !m || !d) return dateStr;
    const date = new Date(y, m - 1, d);
    return date.toLocaleDateString("en-US", { weekday: "short", month: "short", day: "numeric" });
}

function todayISO() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function isPast(dateStr) { return dateStr < todayISO(); }

function getStatusClass(status) {
    switch (status?.toLowerCase()) {
        case "confirmed": return "confirmed";
        case "modified": return "reviewed";
        case "cancelled": return "danger";
        case "missed": return "missed";
        default: return "";
    }
}

function escapeHtml(str) {
    if (!str) return "";
    const el = document.createElement("span");
    el.textContent = str;
    return el.innerHTML;
}

function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value;
}

function setButtonLoading(btn, loading) {
    if (!btn) return;
    if (loading) {
        btn.disabled = true;
        btn.dataset.prevHtml = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Wait…';
    } else {
        btn.disabled = false;
        if (btn.dataset.prevHtml) {
            btn.innerHTML = btn.dataset.prevHtml;
            delete btn.dataset.prevHtml;
        }
    }
}

async function api(url, options = {}) {
    let resp;
    try {
        resp = await fetch(url, options);
    } catch {
        showToast("Network error — check your connection.", "error");
        throw new Error("network");
    }

    if (resp.status === 401 || resp.status === 403) {
        showToast("Session expired. Redirecting to login…", "error");
        setTimeout(() => location.reload(), 2000);
        throw new Error("auth");
    }

    if (resp.status === 404) {
        showToast("The requested resource was not found.", "error");
        throw new Error("not-found");
    }

    if (!resp.ok) {
        throw new Error(`server-${resp.status}`);
    }

    return resp.json();
}

function buildParams(extra = {}) {
    const p = new URLSearchParams({ id: assistantId, ...extra });
    if (selectedDoctorId !== null) p.append("doctorId", selectedDoctorId);
    p.append("date", currentDate);
    return p;
}

function formatLongDate(dateStr) {
    if (!dateStr) return "–";
    const [y, m, d] = dateStr.split("-").map(Number);
    if (!y || !m || !d) return dateStr;
    const date = new Date(y, m - 1, d);
    return date.toLocaleDateString("en-US", {
        weekday: "long",
        month: "long",
        day: "numeric",
        year: "numeric"
    });
}

function updateSelectedDateUI() {
    const heroDate = document.getElementById("appointmentsHeroDate");
    const headerDate = document.getElementById("appointmentsHeaderDate");
    const picker = document.getElementById("appointmentsDatePicker");
    if (heroDate) heroDate.textContent = formatLongDate(currentDate);
    if (headerDate) {
        const short = formatDate(currentDate);
        headerDate.innerHTML = `<i class="fas fa-calendar-day"></i> ${short}`;
    }
    if (picker && picker.value !== currentDate) picker.value = currentDate;
}

function setCurrentDate(newDate) {
    if (!newDate || newDate === currentDate) return;
    currentDate = newDate;
    updateSelectedDateUI();
    syncUrlState();
    updateDoctorFilterLinks();
    reloadAfterMutation();
}

function syncUrlState() {
    const url = new URL(window.location.href);
    url.searchParams.set("id", assistantId);
    url.searchParams.set("date", currentDate);
    if (selectedDoctorId !== null) {
        url.searchParams.set("doctorId", selectedDoctorId);
    } else {
        url.searchParams.delete("doctorId");
    }
    history.replaceState({}, "", url);
}

function updateDoctorFilterLinks() {
    document.querySelectorAll(".doctor-pill[data-doctor-id]").forEach(link => {
        const doctorId = link.dataset.doctorId;
        const href = new URL(link.getAttribute("href"), window.location.origin);
        href.searchParams.set("id", assistantId);
        href.searchParams.set("date", currentDate);
        if (doctorId) {
            href.searchParams.set("doctorId", doctorId);
        } else {
            href.searchParams.delete("doctorId");
        }
        link.setAttribute("href", `${href.pathname}${href.search}`);
    });
}

function renderLoading(status) {
    const el = document.getElementById(`${status}AppointmentsList`);
    if (el) el.innerHTML = '<div class="empty-state"><i class="fas fa-spinner fa-spin"></i><p>Loading…</p></div>';
}

function renderError(status, message) {
    const el = document.getElementById(`${status}AppointmentsList`);
    if (!el) return;
    el.innerHTML = `
        <div class="empty-state">
            <i class="fas fa-exclamation-triangle"></i>
            <p>${escapeHtml(message || "Failed to load appointments")}</p>
            <button class="btn btn-outline btn-sm mt-12" onclick="loadTab('${status}')">
                <i class="fas fa-redo"></i> Retry
            </button>
        </div>`;
}

function renderEmpty(status) {
    const icons = {
        All: "fa-calendar-day",
        Confirmed: "fa-calendar-check",
        Modified: "fa-edit",
        Cancelled: "fa-calendar-times",
        Missed: "fa-user-clock"
    };
    const messages = {
        All: "No appointments on this date",
        Confirmed: "No confirmed appointments found",
        Modified: "No modified appointments at the moment",
        Cancelled: "No cancelled appointments",
        Missed: "No missed appointments"
    };
    const subtitles = {
        All: "Pick another date or add a new appointment to see it here",
        Missed: "Appointments where the patient did not check in will appear here"
    };
    const el = document.getElementById(`${status}AppointmentsList`);
    if (!el) return;
    el.innerHTML = `
        <div class="empty-state">
            <i class="fas ${icons[status] || "fa-calendar"}"></i>
            <p>${messages[status] || "No " + status.toLowerCase() + " appointments"}</p>
            <p class="appointment-muted-note">${subtitles[status] || "Appointments will appear here when their status changes"}</p>
        </div>`;
}

function getStripeClass(status) {
    switch (status?.toLowerCase()) {
        case "confirmed": return "stripe-confirmed";
        case "modified": return "stripe-modified";
        case "cancelled": return "stripe-cancelled";
        case "missed": return "stripe-missed";
        default: return "";
    }
}

function renderAppointments(appointments, status) {
    const list = document.getElementById(`${status}AppointmentsList`);
    if (!list) return;
    if (!appointments?.length) {
        renderEmpty(status);
        return;
    }

    const AVATAR_PALETTE = ["#7c3aed", "#2563eb", "#059669", "#d97706", "#dc2626", "#0891b2", "#6366f1"];
    const STATUS_MAP = {
        confirmed: { bg: "#dcfce7", fg: "#15803d", dot: "#10b981" },
        modified:  { bg: "#dbeafe", fg: "#1d4ed8", dot: "#2563eb" },
        cancelled: { bg: "#fee2e2", fg: "#b91c1c", dot: "#ef4444" },
        missed:    { bg: "#fff3cd", fg: "#92400e", dot: "#f59e0b" }
    };

    list.innerHTML = appointments.map(a => {
        const fmt       = formatTime(a.time);
        const isApt     = a.isToday;
        const isPastApt = !isApt && isPast(a.date);
        const rowStatus = (a.status || "").toLowerCase();
        const s         = STATUS_MAP[rowStatus] || STATUS_MAP.confirmed;

        // Capabilities are driven by each appointment's own status so the
        // "All" tab shows the right actions per row.
        const canModify    = rowStatus === "confirmed";
        const canCancel    = rowStatus !== "cancelled";
        const canReinstate = rowStatus === "cancelled" || rowStatus === "modified";

        const initials  = (a.patientName || "?").trim().split(/\s+/).map(w => w[0]).slice(0, 2).join("").toUpperCase();
        const avatarBg  = AVATAR_PALETTE[(a.patientName?.charCodeAt(0) || 0) % AVATAR_PALETTE.length];

        const todayChip   = isApt     ? `<span class="appt-row-chip appt-row-chip--today">Today</span>` : "";
        const pastChip    = isPastApt  ? `<span class="appt-row-chip appt-row-chip--past">Past</span>`  : "";
        const checkinChip = a.isCheckedIn
            ? `<span class="appt-row-chip appt-row-chip--checkin"><i class="fas fa-check-double"></i>${a.checkedInAt ? " " + escapeHtml(a.checkedInAt) : " In"}</span>`
            : "";

        const modifyBtn = canModify && !isPastApt
            ? `<button class="appt-icon-btn appt-icon-btn--blue" title="Modify" data-action="modify-${a.appointmentId}" onclick="openModifyModal(${a.appointmentId})"><i class="fas fa-edit"></i></button>`
            : "";

        const checkInBtn = canModify && !isPastApt && !a.isCheckedIn
            ? `<button class="appt-icon-btn appt-icon-btn--green" title="Check In" data-action="checkin-${a.appointmentId}" onclick="handleCheckIn(${a.appointmentId})"><i class="fas fa-user-check"></i></button>`
            : "";

        const reinstateBtn = canReinstate && !isPastApt
            ? `<button class="appt-icon-btn appt-icon-btn--teal" title="Reinstate" data-action="reinstate-${a.appointmentId}" onclick="handleReinstateAppointment(${a.appointmentId})"><i class="fas fa-undo"></i></button>`
            : "";

        const cancelBtn = canCancel && !isPastApt
            ? `<button class="appt-icon-btn appt-icon-btn--red" title="Cancel" data-action="cancel-${a.appointmentId}" onclick="quickCancelAppointment(${a.appointmentId})"><i class="fas fa-times"></i></button>`
            : "";

        const phoneHtml = a.patientPhone
            ? `<div class="appt-row-phone"><i class="fas fa-phone"></i> ${escapeHtml(a.patientPhone)}</div>`
            : `<div class="appt-row-phone appt-row-phone--nil">—</div>`;

        // Show the appointment's date whenever it isn't the selected day
        // (e.g. the Missed tab, which spans past dates).
        const showDate = status === "Missed" || a.date !== currentDate;
        const dateLine = showDate
            ? `<span class="appt-row-date"><i class="fas fa-calendar-day"></i> ${escapeHtml(formatDate(a.date))}</span>`
            : "";

        return `
<div class="appt-row" data-appointment-id="${a.appointmentId}"
     data-search="${escapeHtml(((a.patientName || "") + " " + (a.doctorName || "") + " " + (a.patientPhone || "")).toLowerCase())}">
  <div class="appt-row-avatar" style="background:${avatarBg}">${initials}</div>
  <div class="appt-row-patient">
    <span class="appt-row-pname">${escapeHtml(a.patientName)}</span>
    <span class="appt-row-pdoc">${escapeHtml(a.doctorName)}${a.doctorSpecialization ? " · " + escapeHtml(a.doctorSpecialization) : ""}</span>
  </div>
  <div class="appt-row-time">
    <span class="appt-row-time-main"><i class="fas fa-clock"></i> ${fmt.time} <small>${fmt.period}</small></span>
    ${dateLine}
  </div>
  ${phoneHtml}
  <div class="appt-row-status">
    <span class="appt-row-sbadge" style="background:${s.bg};color:${s.fg}">
      <span class="appt-row-sdot" style="background:${s.dot}"></span>${escapeHtml(a.status)}
    </span>
    ${todayChip}${pastChip}${checkinChip}
  </div>
  <div class="appt-row-actions">${modifyBtn}${checkInBtn}${reinstateBtn}${cancelBtn}</div>
</div>`;
    }).join("");
}

function renderPager(status) {
    const pager = document.getElementById("appointmentsPager");
    if (!pager) return;

    const state = paginationState[status] || { page: 1, totalPages: 1, total: 0 };
    if (state.totalPages <= 1) {
        pager.innerHTML = state.total > 0 ? `<span class="pager-summary">${state.total} result(s)</span>` : "";
        return;
    }

    pager.innerHTML = `
        <span class="pager-summary">${state.total} result(s)</span>
        <div class="pager-controls">
            <button class="btn btn-outline btn-sm" ${state.page <= 1 ? "disabled" : ""} onclick="changePage(-1)">
                <i class="fas fa-chevron-left"></i> Prev
            </button>
            <span class="pager-page">Page ${state.page} / ${state.totalPages}</span>
            <button class="btn btn-outline btn-sm" ${state.page >= state.totalPages ? "disabled" : ""} onclick="changePage(1)">
                Next <i class="fas fa-chevron-right"></i>
            </button>
        </div>`;
}

async function loadTab(status, page = 1, useCache = false) {
    const cacheKey = `${status}|${page}|${currentSearch}`;
    if (useCache && cache[cacheKey]) {
        const cached = cache[cacheKey];
        paginationState[status] = {
            page: cached.page,
            totalPages: cached.totalPages,
            total: cached.total
        };
        renderAppointments(cached.items, status);
        renderPager(status);
        return cached.items.length;
    }

    renderLoading(status);
    try {
        const data = await api(`${urls.getAppointments}?${buildParams({ status, page, pageSize: PAGE_SIZE, search: currentSearch })}`);
        cache[cacheKey] = data;
        paginationState[status] = {
            page: data.page || 1,
            totalPages: data.totalPages || 1,
            total: data.total || 0
        };
        renderAppointments(data.items || [], status);
        renderPager(status);
        return (data.items || []).length;
    } catch {
        renderError(status);
        renderPager(status);
        return 0;
    }
}

async function loadAllData() {
    await refreshCounts();
    await loadTab(currentTab, 1);
}

async function refreshCounts() {
    try {
        const counts = await api(`${urls.getAppointmentCounts}?${buildParams()}`);
        applyCountsToUI({
            Confirmed: counts.confirmed,
            Modified: counts.modified,
            Cancelled: counts.cancelled,
            Missed: counts.missed
        });
    } catch { }
}

function applyCountsToUI(counts) {
    const total = (counts.Confirmed || 0) + (counts.Modified || 0) + (counts.Cancelled || 0) + (counts.Missed || 0);
    // "All" reflects every appointment on the selected date (active bookings, any status).
    setText("allTabCount", (counts.Confirmed || 0) + (counts.Modified || 0) + (counts.Cancelled || 0));
    setText("confirmedTabCount", counts.Confirmed);
    setText("modifiedTabCount", counts.Modified);
    setText("cancelledTabCount", counts.Cancelled);
    setText("missedTabCount", counts.Missed);
    setText("heroConfirmedCount", counts.Confirmed);
    setText("heroModifiedCount", counts.Modified);
    setText("heroCancelledCount", counts.Cancelled);
    setText("heroMissedCount", counts.Missed);
    setText("statConfirmed", counts.Confirmed);
    setText("statModified", counts.Modified);
    setText("statCancelled", counts.Cancelled);
    setText("statMissed", counts.Missed);
    setText("statTotal", total);
}

function reloadAfterMutation() {
    Object.keys(cache).forEach(k => delete cache[k]);
    Object.keys(paginationState).forEach(s => paginationState[s] = { page: 1, totalPages: 1, total: 0 });
    loadAllData();
}

function switchTab(tab) {
    if (tab === currentTab) return;
    currentTab = tab;

    document.querySelectorAll(".tab-btn").forEach(btn =>
        btn.classList.toggle("active", btn.dataset.tab === tab));

    document.querySelectorAll(".tab-content").forEach(c => c.classList.add("is-hidden"));
    const pane = document.getElementById(`${tab}-tab`);
    if (pane) pane.classList.remove("is-hidden");

    const page = paginationState[tab]?.page || 1;
    loadTab(tab, page, true);
}

function changePage(delta) {
    const state = paginationState[currentTab] || { page: 1, totalPages: 1 };
    const next = Math.min(Math.max(1, state.page + delta), state.totalPages);
    if (next === state.page) return;
    loadTab(currentTab, next);
}

function handleSearchInput(value) {
    currentSearch = (value || "").trim();
    if (searchDebounce) clearTimeout(searchDebounce);
    searchDebounce = setTimeout(() => {
        Object.keys(cache).forEach(k => delete cache[k]);
        Object.keys(paginationState).forEach(s => paginationState[s].page = 1);
        loadTab(currentTab, 1);
    }, 300);
}

function getModifyModal() {
    return document.getElementById("modifyModal");
}

function isModifyModalOpen() {
    const modal = getModifyModal();
    return !!modal && !modal.classList.contains("is-hidden");
}

async function openModifyModal(appointmentId) {
    const btn = document.querySelector(`[data-action="modify-${appointmentId}"]`);
    setButtonLoading(btn, true);
    try {
        const appt = await api(`${urls.getAppointmentDetail}?${buildParams({ appointmentId })}`);

        const fmt = formatTime(appt.time);
        setText("modifyPatientName", appt.patientName || "–");
        setText("modifyCurrentDateTime", `${formatDate(appt.date)} at ${fmt.time} ${fmt.period}`);

        const dateInput = document.getElementById("newDate");
        const timeInput = document.getElementById("newTime");
        dateInput.value = appt.date || "";
        dateInput.min = todayISO();
        timeInput.value = appt.time || "";
        document.getElementById("modifyReason").value = "";

        modifyingAppointmentId = appointmentId;
        modifyingDoctorId = appt.doctorId;
        getModifyModal()?.classList.remove("is-hidden");
    } catch {
        showToast("Failed to load appointment details.", "error");
    } finally {
        setButtonLoading(btn, false);
    }
}

function closeModifyModal() {
    getModifyModal()?.classList.add("is-hidden");
    modifyingAppointmentId = null;
    modifyingDoctorId = null;

    ["modalSaveBtn", "modalCancelBtn"].forEach(id => {
        const b = document.getElementById(id);
        if (b) setButtonLoading(b, false);
    });

    const url = new URL(location);
    url.searchParams.delete("modify");
    history.replaceState({}, "", url);
}

async function handleSaveModification() {
    const newDate = document.getElementById("newDate").value;
    const newTime = document.getElementById("newTime").value;
    const reason = document.getElementById("modifyReason").value.trim();

    if (!newDate) { showToast("Please select a new date.", "error"); return; }
    if (!newTime) { showToast("Please select a new time.", "error"); return; }
    if (isPast(newDate)) { showToast("Cannot schedule in the past.", "error"); return; }

    const btn = document.getElementById("modalSaveBtn");
    if (isBusy) return;
    isBusy = true;
    setButtonLoading(btn, true);

    try {
        const result = await api(urls.modifyAppointment, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                id: assistantId,
                appointmentId: modifyingAppointmentId,
                newDate,
                newTime,
                reason,
                __RequestVerificationToken: antiForgeryToken
            })
        });

        if (result.success) {
            showToast(result.message, "success");
            closeModifyModal();
            reloadAfterMutation();
        } else {
            showToast(result.message || "Failed to modify appointment.", "error");
        }
    } catch {
        showToast("An error occurred. Please try again.", "error");
    } finally {
        isBusy = false;
        setButtonLoading(btn, false);
    }
}

async function handleCancelAppointment() {
    if (!confirm("Are you sure you want to cancel this appointment?")) return;
    const reason = document.getElementById("modifyReason").value.trim();
    const btn = document.getElementById("modalCancelBtn");

    if (isBusy) return;
    isBusy = true;
    setButtonLoading(btn, true);

    try {
        await doCancelAppointment(modifyingAppointmentId, reason);
        closeModifyModal();
    } finally {
        isBusy = false;
        setButtonLoading(btn, false);
    }
}

async function quickCancelAppointment(appointmentId) {
    if (!confirm("Are you sure you want to cancel this appointment?")) return;
    const btn = document.querySelector(`[data-action="cancel-${appointmentId}"]`);

    if (isBusy) return;
    isBusy = true;
    setButtonLoading(btn, true);

    try {
        await doCancelAppointment(appointmentId, "");
    } finally {
        isBusy = false;
        setButtonLoading(btn, false);
    }
}

async function doCancelAppointment(appointmentId, reason) {
    try {
        const result = await api(urls.cancelAppointment, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                id: assistantId,
                appointmentId,
                reason,
                __RequestVerificationToken: antiForgeryToken
            })
        });

        if (result.success) {
            showToast(result.message, "success");
            reloadAfterMutation();
        } else {
            showToast(result.message || "Failed to cancel appointment.", "error");
        }
    } catch {
        showToast("An error occurred. Please try again.", "error");
    }
}

async function handleReinstateAppointment(appointmentId) {
    if (!confirm("Reinstate this appointment to Confirmed status?")) return;
    const btn = document.querySelector(`[data-action="reinstate-${appointmentId}"]`);

    if (isBusy) return;
    isBusy = true;
    setButtonLoading(btn, true);

    try {
        const result = await api(urls.reinstateAppointment, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                id: assistantId,
                appointmentId,
                __RequestVerificationToken: antiForgeryToken
            })
        });
        if (result.success) {
            showToast(result.message, "success");
            reloadAfterMutation();
        } else {
            showToast(result.message || "Failed to reinstate appointment.", "error");
        }
    } catch {
        showToast("An error occurred. Please try again.", "error");
    } finally {
        isBusy = false;
        setButtonLoading(btn, false);
    }
}

async function handleCheckIn(appointmentId) {
    const btn = document.querySelector(`[data-action="checkin-${appointmentId}"]`);

    if (isBusy) return;
    isBusy = true;
    setButtonLoading(btn, true);

    try {
        const result = await api(urls.checkInPatient, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                id: assistantId,
                appointmentId,
                __RequestVerificationToken: antiForgeryToken
            })
        });
        if (result.success) {
            showToast(result.message + (result.checkedInAt ? ` at ${result.checkedInAt}` : ""), "success");
            reloadAfterMutation();
        } else {
            showToast(result.message || "Failed to check in patient.", "error");
        }
    } catch {
        showToast("An error occurred. Please try again.", "error");
    } finally {
        isBusy = false;
        setButtonLoading(btn, false);
    }
}

function startAutoRefresh() {
    stopAutoRefresh();
    autoRefreshTimer = setInterval(() => {
        if (!document.hidden && !isModifyModalOpen() && !isBusy) {
            refreshCounts();
            const page = paginationState[currentTab]?.page || 1;
            loadTab(currentTab, page);
        }
    }, AUTO_REFRESH_MS);
}

function stopAutoRefresh() {
    if (autoRefreshTimer) {
        clearInterval(autoRefreshTimer);
        autoRefreshTimer = null;
    }
}

document.addEventListener("visibilitychange", () => {
    if (document.hidden) {
        stopAutoRefresh();
    } else {
        reloadAfterMutation();
        startAutoRefresh();
    }
});

document.addEventListener("keydown", e => {
    if (e.key === "Escape") {
        if (isModifyModalOpen()) closeModifyModal();
    }
});

function exportAppointmentsCsv() {
    const params = buildParams({ status: currentTab, search: currentSearch });
    if (urls.exportAppointmentsCsv) {
        window.location.href = `${urls.exportAppointmentsCsv}?${params.toString()}`;
    }
}

document.addEventListener("DOMContentLoaded", () => {
    if (!assistantId || !urls.getAppointments || !urls.getAppointmentCounts) return;

    updateSelectedDateUI();
    updateDoctorFilterLinks();
    syncUrlState();
    loadAllData();
    startAutoRefresh();

    const picker = document.getElementById("appointmentsDatePicker");
    const prevBtn = document.getElementById("prevAppointmentsDateBtn");
    const nextBtn = document.getElementById("nextAppointmentsDateBtn");
    const todayBtn = document.getElementById("todayAppointmentsDateBtn");

    picker?.addEventListener("change", e => {
        const value = e.target?.value;
        if (value) setCurrentDate(value);
    });

    prevBtn?.addEventListener("click", () => {
        const d = new Date(currentDate + "T00:00:00");
        d.setDate(d.getDate() - 1);
        setCurrentDate(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`);
    });

    nextBtn?.addEventListener("click", () => {
        const d = new Date(currentDate + "T00:00:00");
        d.setDate(d.getDate() + 1);
        setCurrentDate(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`);
    });

    todayBtn?.addEventListener("click", () => setCurrentDate(todayISO()));

    document.getElementById("modifyModal")?.addEventListener("click", e => {
        if (e.target.id === "modifyModal") closeModifyModal();
    });

    const modifyId = new URLSearchParams(location.search).get("modify");
    if (modifyId) openModifyModal(parseInt(modifyId, 10));
});

window.addEventListener("beforeunload", stopAutoRefresh);

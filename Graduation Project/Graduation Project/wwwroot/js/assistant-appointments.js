const assistantAppointmentsConfig = window.assistantAppointmentsConfig || {};

const assistantId = Number(assistantAppointmentsConfig.assistantId || 0);
const selectedDoctorId = assistantAppointmentsConfig.selectedDoctorId ?? null;
const urls = assistantAppointmentsConfig.urls || {};

const ALL_STATUSES = ["Confirmed", "Modified", "Cancelled"];
const PAGE_SIZE = 20;
const AUTO_REFRESH_MS = 60_000;
let currentDate = assistantAppointmentsConfig.selectedDate || new Date().toISOString().slice(0, 10);
const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

let currentTab = "Confirmed";
let modifyingAppointmentId = null;
let modifyingDoctorId = null;
let autoRefreshTimer = null;
let isBusy = false;
let currentSearch = "";
let searchDebounce = null;
const paginationState = {
    Confirmed: { page: 1, totalPages: 1, total: 0 },
    Modified: { page: 1, totalPages: 1, total: 0 },
    Cancelled: { page: 1, totalPages: 1, total: 0 }
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
    const icons = { Confirmed: "fa-calendar-check", Modified: "fa-edit", Cancelled: "fa-calendar-times" };
    const messages = {
        Confirmed: "No confirmed appointments found",
        Modified: "No modified appointments at the moment",
        Cancelled: "No cancelled appointments"
    };
    const el = document.getElementById(`${status}AppointmentsList`);
    if (!el) return;
    el.innerHTML = `
        <div class="empty-state">
            <i class="fas ${icons[status] || "fa-calendar"}"></i>
            <p>${messages[status] || "No " + status.toLowerCase() + " appointments"}</p>
            <p class="appointment-muted-note">Appointments will appear here when their status changes</p>
        </div>`;
}

function getStripeClass(status) {
    switch (status?.toLowerCase()) {
        case "confirmed": return "stripe-confirmed";
        case "modified": return "stripe-modified";
        case "cancelled": return "stripe-cancelled";
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

    const canModify = status === "Confirmed";
    const canCancel = status !== "Cancelled";

    list.innerHTML = appointments.map(a => {
        const fmt = formatTime(a.time);
        const dateStr = formatDate(a.date);
        const isApt = a.isToday;
        const isPastApt = !isApt && isPast(a.date);

        const badges = [
            isApt ? '<span class="status-badge status-badge--today"><i class="fas fa-sun"></i>Today</span>' : "",
            isPastApt ? '<span class="status-badge status-badge--past"><i class="fas fa-history"></i>Past</span>' : "",
            `<span class="status-badge ${getStatusClass(a.status)}">${escapeHtml(a.status)}</span>`
        ].join("");

        const details = [
            `<div class="detail-item"><i class="fas fa-calendar"></i><span>${dateStr}</span></div>`,
            `<div class="detail-item"><i class="fas fa-clock"></i><span>${fmt.time} ${fmt.period}</span></div>`,
            a.reason ? `<div class="detail-item"><i class="fas fa-info-circle"></i><span>${escapeHtml(a.reason)}</span></div>` : "",
            a.notes ? `<div class="detail-item"><i class="fas fa-sticky-note"></i><span>${escapeHtml(a.notes)}</span></div>` : "",
            a.patientPhone ? `<div class="detail-item"><i class="fas fa-phone"></i><span>${escapeHtml(a.patientPhone)}</span></div>` : ""
        ].join("");

        const modifyBtn = canModify && !isPastApt
            ? `<button class="btn btn-outline btn-sm" data-action="modify-${a.appointmentId}" onclick="openModifyModal(${a.appointmentId})"><i class="fas fa-edit"></i> Modify</button>`
            : "";

        const cancelBtn = canCancel && !isPastApt
            ? `<button class="btn btn-danger btn-sm" data-action="cancel-${a.appointmentId}" onclick="quickCancelAppointment(${a.appointmentId})"><i class="fas fa-times"></i> Cancel</button>`
            : "";

        return `
            <div class="request-item" data-appointment-id="${a.appointmentId}" data-search="${escapeHtml(((a.patientName || "") + " " + (a.doctorName || "") + " " + (a.patientPhone || "")).toLowerCase())}">
                <div class="appt-stripe ${getStripeClass(a.status)}"></div>
                <div class="appt-body">
                    <div class="request-header">
                        <div class="patient-info">
                            <div class="patient-avatar"><i class="fas fa-user"></i></div>
                            <div>
                                <h4>${escapeHtml(a.patientName)}</h4>
                                <p class="appointment-secondary-text">
                                    <i class="fas fa-user-md"></i> ${escapeHtml(a.doctorName)}
                                    ${a.doctorSpecialization ? `&nbsp;·&nbsp;${escapeHtml(a.doctorSpecialization)}` : ""}
                                </p>
                            </div>
                        </div>
                        <div class="request-meta">${badges}</div>
                    </div>
                    <div class="request-details">${details}</div>
                    <div class="request-actions">${modifyBtn}${cancelBtn}</div>
                </div>
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
            Cancelled: counts.cancelled
        });
    } catch { }
}

function applyCountsToUI(counts) {
    const total = (counts.Confirmed || 0) + (counts.Modified || 0) + (counts.Cancelled || 0);
    setText("confirmedTabCount", counts.Confirmed);
    setText("modifiedTabCount", counts.Modified);
    setText("cancelledTabCount", counts.Cancelled);
    setText("heroConfirmedCount", counts.Confirmed);
    setText("heroModifiedCount", counts.Modified);
    setText("heroCancelledCount", counts.Cancelled);
    setText("statConfirmed", counts.Confirmed);
    setText("statModified", counts.Modified);
    setText("statCancelled", counts.Cancelled);
    setText("statTotal", total);
}

function reloadAfterMutation() {
    Object.keys(cache).forEach(k => delete cache[k]);
    ALL_STATUSES.forEach(s => paginationState[s] = { page: 1, totalPages: 1, total: 0 });
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
        ALL_STATUSES.forEach(s => paginationState[s].page = 1);
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

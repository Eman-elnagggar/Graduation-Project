const assistantAvailabilityConfig = window.assistantAvailabilityConfig || {};

const assistantId = Number(assistantAvailabilityConfig.assistantId || 0);
let currentDoctorId = assistantAvailabilityConfig.selectedDoctorId ?? null;
const clinicName = assistantAvailabilityConfig.clinicName || "Clinic";
const doctorsCount = Number(assistantAvailabilityConfig.doctorsCount || 0);
const availabilityUrls = assistantAvailabilityConfig.urls || {};
const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

let SLOT_START = "08:00";
let SLOT_END = "18:00";
let SLOT_DURATION = 30;

let currentCalDate = new Date();
let selectedDate = todayISO();

function todayISO() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function formatDateLabel(dateStr) {
    const [y, m, d] = dateStr.split("-").map(Number);
    return new Date(y, m - 1, d).toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric", year: "numeric" });
}

function formatTime12(time24) {
    const [h, m] = time24.split(":").map(Number);
    const ampm = h >= 12 ? "PM" : "AM";
    const h12 = h % 12 || 12;
    return `${h12}:${String(m).padStart(2, "0")} ${ampm}`;
}

function generateTimeGrid(startStr, endStr, stepMin) {
    const times = [];
    let [h, mi] = startStr.split(":").map(Number);
    const [eh, em] = endStr.split(":").map(Number);

    while (h < eh || (h === eh && mi < em)) {
        times.push(`${String(h).padStart(2, "0")}:${String(mi).padStart(2, "0")}`);
        mi += stepMin;
        if (mi >= 60) {
            h += Math.floor(mi / 60);
            mi %= 60;
        }
    }

    return times;
}

function escapeHtml(str) {
    if (!str) return "";
    const el = document.createElement("span");
    el.textContent = str;
    return el.innerHTML;
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

function updateStats(available, booked, blocked) {
    ["heroAvailable", "statAvailable"].forEach(id => document.getElementById(id).textContent = available);
    ["heroBooked", "statBooked"].forEach(id => document.getElementById(id).textContent = booked);
    ["heroBlocked", "statBlocked"].forEach(id => document.getElementById(id).textContent = blocked);
}

async function api(url, options = {}) {
    const method = (options.method || "GET").toUpperCase();
    const headers = new Headers(options.headers || {});

    if (method !== "GET" && method !== "HEAD" && antiForgeryToken && !headers.has("RequestVerificationToken")) {
        headers.set("RequestVerificationToken", antiForgeryToken);
    }

    options.headers = headers;

    let resp;
    try {
        resp = await fetch(url, options);
    } catch {
        showToast("Network error — check your connection.", "error");
        throw new Error("network");
    }

    if (!resp.ok) {
        showToast("Server error. Please try again.", "error");
        throw new Error(`server-${resp.status}`);
    }

    return resp.json();
}

function renderCalendar() {
    const grid = document.getElementById("calendarGrid");
    const display = document.getElementById("currentMonth");
    const year = currentCalDate.getFullYear();
    const month = currentCalDate.getMonth();

    const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    display.textContent = `${monthNames[month]} ${year}`;

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMon = new Date(year, month, 0).getDate();

    const headers = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    let html = headers.map(d => `<div class="calendar-day-header">${d}</div>`).join("");

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    for (let i = firstDay - 1; i >= 0; i--) {
        html += `<div class="calendar-day other-month"><span class="day-number">${daysInPrevMon - i}</span></div>`;
    }

    for (let day = 1; day <= daysInMonth; day++) {
        const dateStr = `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
        const date = new Date(year, month, day);
        const isToday = date.getTime() === today.getTime();
        const isSelected = dateStr === selectedDate;
        const isPast = date < today;

        html += `
            <div class="calendar-day ${isToday ? "today" : ""} ${isSelected ? "selected" : ""} ${isPast ? "past" : ""}" ${!isPast ? `onclick="selectCalendarDate('${dateStr}')"` : ""} title="${isPast ? "Past date" : dateStr}">
                <span class="day-number">${day}</span>
            </div>`;
    }

    const total = firstDay + daysInMonth;
    const remaining = 7 - (total % 7);
    if (remaining < 7) {
        for (let i = 1; i <= remaining; i++) {
            html += `<div class="calendar-day other-month"><span class="day-number">${i}</span></div>`;
        }
    }

    grid.innerHTML = html;
}

function selectCalendarDate(dateStr) {
    selectedDate = dateStr;
    renderCalendar();
    const label = formatDateLabel(dateStr);
    document.getElementById("selectedDateDisplay").textContent = label;
    document.getElementById("heroDateLabel").textContent = label;
    const dp = document.getElementById("slotDatePicker");
    if (dp) dp.value = dateStr;

    if (currentDoctorId !== null) loadSlots();
    else showNoDoctorMessage();
}

function previousMonth() {
    currentCalDate.setMonth(currentCalDate.getMonth() - 1);
    renderCalendar();
}

function nextMonth() {
    currentCalDate.setMonth(currentCalDate.getMonth() + 1);
    renderCalendar();
}

function goToToday() {
    currentCalDate = new Date();
    selectedDate = todayISO();
    renderCalendar();
    const label = formatDateLabel(selectedDate);
    document.getElementById("selectedDateDisplay").textContent = label;
    document.getElementById("heroDateLabel").textContent = label;
    const dp = document.getElementById("slotDatePicker");
    if (dp) dp.value = selectedDate;
    if (currentDoctorId !== null) loadSlots(); else showNoDoctorMessage();
}

function onSlotDateChange(dateStr) {
    const [y, m, d] = dateStr.split("-").map(Number);
    currentCalDate = new Date(y, m - 1, d);
    selectCalendarDate(dateStr);
}

function onSlotDurationPickerChange(value) {
    const parsed = parseInt(value, 10);
    if (!Number.isFinite(parsed) || parsed <= 0) return;

    SLOT_DURATION = parsed;
    if (currentDoctorId !== null) loadSlots();
}

function syncAvailabilityUrl() {
    const url = new URL(window.location.href);
    url.searchParams.set("id", assistantId);
    if (currentDoctorId !== null) {
        url.searchParams.set("doctorId", currentDoctorId);
    } else {
        url.searchParams.delete("doctorId");
    }
    history.replaceState({}, "", url);
}

function updateDoctorPillStyles() {
    document.querySelectorAll(".doctor-filter-pills .doctor-pill[data-doctor-id]").forEach(link => {
        const doctorId = parseInt(link.dataset.doctorId, 10);
        const isActive = currentDoctorId !== null && doctorId === currentDoctorId;
        link.classList.toggle("active-blue", isActive);
    });
}

function showNoDoctorMessage() {
    document.getElementById("timeSlotsContainer").innerHTML =
        '<div class="empty-state"><i class="fas fa-user-md"></i><p>Select a doctor above to manage slots</p></div>';
    updateStats("—", "—", "—");
}

async function loadSlots() {
    const container = document.getElementById("timeSlotsContainer");
    container.innerHTML = '<div class="empty-state"><i class="fas fa-spinner fa-spin"></i><p>Loading slots…</p></div>';

    let data = {};
    try {
        data = await api(`${availabilityUrls.getAvailabilitySlots}?id=${assistantId}&doctorId=${currentDoctorId}&date=${selectedDate}`);
    } catch {
        container.innerHTML = '<div class="empty-state"><i class="fas fa-exclamation-triangle"></i><p>Failed to load slots</p></div>';
        return;
    }

    const existing = data.slots || [];
    const otherClinic = data.otherClinicSlots || [];

    const slotMap = {};
    existing.forEach(s => { slotMap[s.time] = s; });

    const otherClinicMap = {};
    otherClinic.forEach(s => { otherClinicMap[s.time] = s; });

    const grid = generateTimeGrid(SLOT_START, SLOT_END, SLOT_DURATION);
    const isPastDate = selectedDate < todayISO();
    const isToday = selectedDate === todayISO();
    const nowTime = (() => {
        const n = new Date();
        return `${String(n.getHours()).padStart(2, "0")}:${String(n.getMinutes()).padStart(2, "0")}`;
    })();

    function isSlotPast(time) {
        return isPastDate || (isToday && time <= nowTime);
    }

    let available = 0;
    let booked = 0;
    let blocked = 0;
    const amTimes = [];
    const pmTimes = [];

    const gridSet = new Set(grid);

    grid.forEach(time => {
        if (isSlotPast(time)) return;
        const h = parseInt(time.split(":")[0]);
        const slot = slotMap[time];
        const other = otherClinicMap[time];
        if (!slot) {
            if (other) booked++;
            else blocked++;
        } else if (slot.isBooked) booked++;
        else available++;

        (h < 12 ? amTimes : pmTimes).push(time);
    });

    Object.keys(slotMap).forEach(time => {
        if (gridSet.has(time) || isSlotPast(time)) return;
        const h = parseInt(time.split(":")[0]);
        if (slotMap[time].isBooked) booked++;
        else available++;
        (h < 12 ? amTimes : pmTimes).push(time);
    });

    amTimes.sort();
    pmTimes.sort();

    if (!amTimes.length && !pmTimes.length) {
        container.innerHTML = '<div class="empty-state"><p>No time slots configured</p></div>';
        updateStats(0, 0, 0);
        return;
    }

    updateStats(available, booked, blocked);

    function buildSlotCard(time) {
        const slot = slotMap[time];
        const other = otherClinicMap[time];
        const label = formatTime12(time);
        const id = `slot-${time.replace(":", "")}`;

        if (!slot) {
            if (other) {
                return `
                    <div class="slot-item booked" id="${id}">
                        <div class="slot-icon-col">
                            <span class="slot-icon booked-icon"><i class="fas fa-user"></i></span>
                        </div>
                        <div class="slot-body">
                            <span class="slot-time">${label}</span>
                            <span class="slot-status booked-text">${other.isBooked ? "Booked" : "Reserved"}</span>
                            <span class="slot-location"><i class="fas fa-map-marker-alt"></i> ${escapeHtml(other.clinicName)}</span>
                        </div>
                    </div>`;
            }

            return `
                <div class="slot-item blocked" id="${id}">
                    <div class="slot-icon-col">
                        <span class="slot-icon blocked-icon"><i class="fas fa-minus-circle"></i></span>
                    </div>
                    <div class="slot-body">
                        <span class="slot-time">${label}</span>
                        <span class="slot-status blocked-text">Blocked</span>
                    </div>
                    <div class="slot-action">
                        <button class="btn btn-sm btn-outline" onclick="createSlot('${time}')">
                            <i class="fas fa-plus"></i> Enable
                        </button>
                    </div>
                </div>`;
        }

        if (slot.isBooked) {
            return `
                <div class="slot-item booked" id="${id}">
                    <div class="slot-icon-col">
                        <span class="slot-icon booked-icon"><i class="fas fa-user"></i></span>
                    </div>
                    <div class="slot-body">
                        <span class="slot-time">${label}</span>
                        <span class="slot-status booked-text">${slot.patientName ? escapeHtml(slot.patientName) : "Booked"}</span>
                        <span class="slot-location"><i class="fas fa-map-marker-alt"></i> ${escapeHtml(clinicName)}</span>
                    </div>
                </div>`;
        }

        return `
            <div class="slot-item available" id="${id}">
                <div class="slot-icon-col">
                    <span class="slot-icon available-icon"><i class="fas fa-check"></i></span>
                </div>
                <div class="slot-body">
                    <span class="slot-time">${label}</span>
                    <span class="slot-status available-text">Available</span>
                    <span class="slot-location"><i class="fas fa-map-marker-alt"></i> ${escapeHtml(clinicName)}</span>
                </div>
                <div class="slot-action">
                    <button class="btn btn-sm btn-outline danger" onclick="deleteSlot(${slot.appointmentId})">
                        <i class="fas fa-ban"></i> Block
                    </button>
                </div>
            </div>`;
    }

    function buildGroup(times, label, iconClass) {
        if (!times.length) return "";
        return `
            <div class="slot-group">
                <div class="slot-group-header">
                    <i class="${iconClass}"></i> ${label}
                    <span class="slot-group-count">${times.length} slots</span>
                </div>
                <div class="slots-grid">${times.map(buildSlotCard).join("")}</div>
            </div>`;
    }

    container.innerHTML = buildGroup(amTimes, "Morning", "fas fa-sun") + buildGroup(pmTimes, "Afternoon", "fas fa-moon");
}

async function createSlot(time) {
    const btn = event.target.closest("button");
    setButtonLoading(btn, true);
    try {
        const data = await api(`${availabilityUrls.createAvailabilitySlot}?id=${assistantId}&doctorId=${currentDoctorId}&date=${selectedDate}&time=${time}`, { method: "POST" });
        if (data.success) {
            showToast("Slot enabled.", "success");
            loadSlots();
        } else {
            showToast(data.message, "error");
            setButtonLoading(btn, false);
        }
    } catch {
        setButtonLoading(btn, false);
    }
}

async function deleteSlot(appointmentId) {
    const btn = event.target.closest("button");
    setButtonLoading(btn, true);
    try {
        const data = await api(`${availabilityUrls.deleteAvailabilitySlot}?id=${assistantId}&appointmentId=${appointmentId}`, { method: "POST" });
        if (data.success) {
            showToast("Slot blocked.", "success");
            loadSlots();
        } else {
            showToast(data.message, "error");
            setButtonLoading(btn, false);
        }
    } catch {
        setButtonLoading(btn, false);
    }
}

async function setAllAvailable() {
    if (!currentDoctorId) { showToast("Select a doctor first.", "error"); return; }
    if (selectedDate < todayISO()) { showToast("Cannot modify past dates.", "error"); return; }

    const start = document.getElementById("startTime").value;
    const end = document.getElementById("endTime").value;
    const dur = document.getElementById("slotDuration").value;

    try {
        const data = await api(`${availabilityUrls.setAllSlotsAvailable}?id=${assistantId}&doctorId=${currentDoctorId}&date=${selectedDate}&startTime=${start}&endTime=${end}&slotDuration=${dur}`, { method: "POST" });
        if (data.success) {
            showToast(data.message, "success");
            loadSlots();
        } else {
            showToast(data.message, "error");
        }
    } catch { }
}

async function blockAllSlots() {
    if (!currentDoctorId) { showToast("Select a doctor first.", "error"); return; }
    if (selectedDate < todayISO()) { showToast("Cannot modify past dates.", "error"); return; }

    try {
        const data = await api(`${availabilityUrls.blockAllAvailabilitySlots}?id=${assistantId}&doctorId=${currentDoctorId}&date=${selectedDate}`, { method: "POST" });
        if (data.success) {
            showToast(data.message, "success");
            loadSlots();
        } else {
            showToast(data.message, "error");
        }
    } catch { }
}

async function applyQuickSetup() {
    if (!currentDoctorId) { showToast("Select a doctor first.", "error"); return; }

    const workingDays = [];
    for (let i = 0; i <= 6; i++) {
        const cb = document.querySelector(`input[value="${i}"]`);
        if (cb && cb.checked) workingDays.push(i);
    }

    if (!workingDays.length) {
        showToast("Please select at least one working day.", "error");
        return;
    }

    const slotDurationRaw = parseInt(document.getElementById("slotDuration").value, 10);
    if (!Number.isFinite(slotDurationRaw) || slotDurationRaw <= 0) {
        showToast("Please enter a valid slot duration in minutes.", "error");
        return;
    }

    const request = {
        doctorId: currentDoctorId,
        workingDays,
        startTime: document.getElementById("startTime").value,
        endTime: document.getElementById("endTime").value,
        slotDuration: slotDurationRaw,
        weeksAhead: parseInt(document.getElementById("weeksAhead").value)
    };

    const btn = document.getElementById("applySetupBtn");
    setButtonLoading(btn, true);

    try {
        const data = await api(`${availabilityUrls.applyQuickSetupSchedule}?id=${assistantId}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request)
        });

        if (data.success) {
            showToast(data.message, "success");
            loadSlots();
        } else {
            showToast(data.message, "error");
        }
    } catch { }

    setButtonLoading(btn, false);
}

function toggleCustomSlotForm() {
    if (!currentDoctorId) { showToast("Select a doctor first.", "error"); return; }
    if (selectedDate < todayISO()) { showToast("Cannot add slots in the past.", "error"); return; }

    const form = document.getElementById("customSlotForm");
    const visible = form.style.display !== "none";
    form.style.display = visible ? "none" : "flex";
    if (!visible) document.getElementById("customSlotTime").focus();
}

async function createCustomSlot() {
    const timeInput = document.getElementById("customSlotTime");
    const time = timeInput.value;
    if (!time) { showToast("Please enter a valid time.", "error"); return; }

    const btn = document.getElementById("addCustomSlotBtn");
    setButtonLoading(btn, true);

    try {
        const data = await api(`${availabilityUrls.createAvailabilitySlot}?id=${assistantId}&doctorId=${currentDoctorId}&date=${selectedDate}&time=${time}`, { method: "POST" });
        if (data.success) {
            showToast("Custom slot added.", "success");
            timeInput.value = "";
            document.getElementById("customSlotForm").style.display = "none";
            loadSlots();
        } else {
            showToast(data.message, "error");
            setButtonLoading(btn, false);
        }
    } catch {
        setButtonLoading(btn, false);
    }
}

function updateQsSummary() {
    const dayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    const selected = [];
    const selectedDayIndexes = [];

    for (let i = 0; i <= 6; i++) {
        const cb = document.querySelector(`.qs-days input[value="${i}"]`);
        if (cb && cb.checked) {
            selected.push(dayNames[i]);
            selectedDayIndexes.push(i);
        }
    }

    const badge = document.getElementById("qsDayCount");
    if (badge) badge.textContent = selected.length + " selected";

    const start = document.getElementById("startTime")?.value || "09:00";
    const end = document.getElementById("endTime")?.value || "17:00";
    const durRaw = parseInt(document.getElementById("slotDuration")?.value || "30", 10);
    const dur = Number.isFinite(durRaw) && durRaw > 0 ? durRaw : 30;
    const weeks = parseInt(document.getElementById("weeksAhead")?.value || "2");

    const [sh, sm] = start.split(":").map(Number);
    const [eh, em] = end.split(":").map(Number);
    const startMinutes = (sh * 60 + sm);
    const endMinutes = (eh * 60 + em);
    const minsPerDay = endMinutes - startMinutes;
    const slotsPerDay = minsPerDay > 0 ? Math.floor(minsPerDay / dur) : 0;
    const totalSelectedDaysSlots = slotsPerDay * selected.length * weeks;

    // Backend behavior: today's remaining slots are also included by quick setup
    // even when today is not selected in working days.
    const todayDayIndex = new Date().getDay();
    const todayAlreadySelected = selectedDayIndexes.includes(todayDayIndex);
    let todayRemainingSlots = 0;

    if (!todayAlreadySelected && minsPerDay > 0) {
        const now = new Date();
        const nowMinutes = (now.getHours() * 60) + now.getMinutes();

        for (let current = startMinutes; current < endMinutes; current += dur) {
            if (current <= nowMinutes) continue;
            todayRemainingSlots++;
        }
    }

    const totalSlots = totalSelectedDaysSlots + todayRemainingSlots;

    const pill = document.getElementById("qsSummaryText");
    if (pill) {
        if (selected.length === 0) {
            pill.textContent = "No days selected";
        } else {
            const fmt = t => {
                const [h, m] = t.split(":");
                const a = +h >= 12 ? "PM" : "AM";
                const h12 = +h % 12 || 12;
                return `${h12}:${m.padStart(2, "0")} ${a}`;
            };
            const todayExtra = todayRemainingSlots > 0 ? ` + today(${todayRemainingSlots})` : "";
            pill.textContent = `${selected.join(", ")} · ${fmt(start)}–${fmt(end)} · ${weeks}w${todayExtra}`;
        }
    }

    const est = document.getElementById("qsSlotCount");
    if (est) est.textContent = totalSlots > 0 ? totalSlots.toLocaleString() : "—";
}

document.addEventListener("DOMContentLoaded", () => {
    if (!assistantId || !availabilityUrls.getAvailabilitySlots) return;

    const doctorPillsContainer = document.querySelector(".doctor-filter-pills");
    if (doctorPillsContainer) {
        doctorPillsContainer.addEventListener("click", e => {
            const pill = e.target.closest(".doctor-pill[data-doctor-id]");
            if (!pill) return;
            e.preventDefault();
            const newDoctorId = parseInt(pill.dataset.doctorId, 10);
            if (Number.isNaN(newDoctorId) || newDoctorId === currentDoctorId) return;
            currentDoctorId = newDoctorId;
            updateDoctorPillStyles();
            syncAvailabilityUrl();
            loadSlots();
        });
    }

    renderCalendar();
    const label = formatDateLabel(selectedDate);
    document.getElementById("selectedDateDisplay").textContent = label;
    document.getElementById("heroDateLabel").textContent = label;

    const dp = document.getElementById("slotDatePicker");
    if (dp) dp.value = selectedDate;

    syncAvailabilityUrl();
    if (currentDoctorId !== null) loadSlots();
    else if (doctorsCount === 0) { }
    else showNoDoctorMessage();

    updateQsSummary();
});

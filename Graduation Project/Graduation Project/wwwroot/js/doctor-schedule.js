(function () {
  'use strict';

  const bootstrap = window.scheduleBootstrap || {};
  const DOCTOR_ID = bootstrap.doctorId || 0;

  const state = {
    currentDate: new Date(),
    selectedDate: new Date(),
    currentView: 'day',
    currentFilter: 'all',
    searchQuery: '',
    appointments: []
  };

  function formatDateKey(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  function formatTime(timeStr) {
    const [hours, minutes] = timeStr.split(':');
    const hour = parseInt(hours, 10);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 || 12;
    return { main: `${hour12}:${minutes}`, sub: ampm };
  }

  function formatDateDisplay(date) {
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);

    if (date.toDateString() === today.toDateString()) {
      return "Today's Schedule";
    }
    if (date.toDateString() === tomorrow.toDateString()) {
      return "Tomorrow's Schedule";
    }
    return date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' });
  }

  function getStartOfWeek(date) {
    const d = new Date(date);
    const day = d.getDay();
    d.setDate(d.getDate() - day);
    return d;
  }

  function getAvatarUrl(name) {
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=14967f&color=fff&size=96`;
  }

  function normalizeStatus(status) {
    const s = (status || '').toLowerCase();
    if (s === 'completed' || s === 'urgent' || s === 'pending' || s === 'cancelled' || s === 'confirmed' || s === 'modified' || s === 'missed') {
      return s;
    }
    if (s === 'booked') {
      return 'confirmed';
    }
    if (s === 'canceled') {
      return 'cancelled';
    }
    return 'pending';
  }

  function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  async function updateAppointmentStatus(apptId, status) {
    const token = getAntiForgeryToken();
    const body = new URLSearchParams({
      appointmentId: String(apptId),
      status,
      __RequestVerificationToken: token
    });

    const response = await fetch(`/Doctor/UpdateAppointmentStatus/${DOCTOR_ID}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
      },
      body: body.toString()
    });

    if (!response.ok) {
      throw new Error('Failed to update appointment status.');
    }

    return response.json();
  }

  function loadAppointments() {
    const rawAppointments = Array.isArray(bootstrap.appointments) ? bootstrap.appointments : [];
    state.appointments = rawAppointments.map(a => ({
      id: a.id,
      date: a.date,
      time: a.time,
      duration: a.duration || 30,
      patient: {
        id: a.patient?.id || 0,
        name: a.patient?.name || 'Patient',
        week: a.patient?.week || 0,
        trimester: a.patient?.trimester || 1,
        risk: a.patient?.risk || 'low'
      },
      type: a.type || 'Consultation',
      status: normalizeStatus(a.status),
      notes: a.notes || ''
    }));

    updateStats();
    renderUpcoming();
  }

  function getAppointmentsForDate(date) {
    const dateKey = formatDateKey(date);
    let appointments = state.appointments.filter(a => a.date === dateKey);

    if (state.currentFilter !== 'all') {
      appointments = appointments.filter(a => a.status === state.currentFilter);
    }

    if (state.searchQuery) {
      const query = state.searchQuery.toLowerCase();
      appointments = appointments.filter(a =>
        a.patient.name.toLowerCase().includes(query) ||
        a.type.toLowerCase().includes(query)
      );
    }

    return appointments.sort((a, b) => a.time.localeCompare(b.time));
  }

  function getAppointmentsForWeek(startDate) {
    const appointments = {};
    for (let i = 0; i < 7; i++) {
      const date = new Date(startDate);
      date.setDate(date.getDate() + i);
      const dateKey = formatDateKey(date);
      appointments[dateKey] = getAppointmentsForDate(date);
    }
    return appointments;
  }

  function getAppointmentsForMonth(year, month) {
    const appointments = {};
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    for (let d = new Date(firstDay); d <= lastDay; d.setDate(d.getDate() + 1)) {
      const dateKey = formatDateKey(new Date(d));
      const dayAppts = state.appointments.filter(a => a.date === dateKey);
      if (dayAppts.length > 0) {
        appointments[dateKey] = dayAppts;
      }
    }
    return appointments;
  }

  function hasAppointments(date) {
    const dateKey = formatDateKey(date);
    return state.appointments.some(a => a.date === dateKey);
  }

  function updateStats() {
    const today = formatDateKey(new Date());
    const todayAppts = state.appointments.filter(a => a.date === today);
    const pendingAppts = state.appointments.filter(a => a.status === 'pending');
    const completedToday = todayAppts.filter(a => a.status === 'completed');

    const startOfWeek = getStartOfWeek(new Date());
    let weekCount = 0;
    for (let i = 0; i < 7; i++) {
      const d = new Date(startOfWeek);
      d.setDate(d.getDate() + i);
      const dateKey = formatDateKey(d);
      weekCount += state.appointments.filter(a => a.date === dateKey).length;
    }

    document.getElementById('heroDate').textContent = new Date().toLocaleDateString('en-US', {
      weekday: 'long', month: 'long', day: 'numeric', year: 'numeric'
    });
    document.getElementById('heroTodayCount').textContent = todayAppts.length;
    document.getElementById('heroWeekCount').textContent = weekCount;

    const now = new Date();
    const currentTime = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
    const nextAppt = todayAppts.find(a => a.time > currentTime && a.status !== 'completed' && a.status !== 'cancelled');
    document.getElementById('heroNextTime').textContent = nextAppt ? `${formatTime(nextAppt.time).main} ${formatTime(nextAppt.time).sub}` : '—';

    document.getElementById('kpiToday').textContent = todayAppts.length;
    document.getElementById('kpiWeek').textContent = weekCount;
    document.getElementById('kpiPending').textContent = pendingAppts.length;
    document.getElementById('kpiCompleted').textContent = completedToday.length;

    const trendToday = document.getElementById('kpiTodayTrend');
    if (trendToday) {
      trendToday.querySelector('span').textContent = todayAppts.length > 0 ? '+' + todayAppts.length : '0';
    }
  }

  function renderCalendar() {
    const year = state.currentDate.getFullYear();
    const month = state.currentDate.getMonth();

    document.getElementById('calendarMonthLabel').textContent =
      state.currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const today = new Date();

    const container = document.getElementById('calendarDays');
    container.innerHTML = '';

    for (let i = firstDay.getDay() - 1; i >= 0; i--) {
      const day = new Date(year, month, 0).getDate() - i;
      const div = document.createElement('div');
      div.className = 'sch-cal-day other-month';
      div.textContent = day;
      container.appendChild(div);
    }

    for (let i = 1; i <= lastDay.getDate(); i++) {
      const date = new Date(year, month, i);
      const div = document.createElement('div');
      div.className = 'sch-cal-day';
      div.textContent = i;

      if (date.toDateString() === today.toDateString()) {
        div.classList.add('today');
      }
      if (date.toDateString() === state.selectedDate.toDateString()) {
        div.classList.add('selected');
      }
      if (hasAppointments(date)) {
        div.classList.add('has-appointments');
      }

      div.addEventListener('click', () => selectDate(date));
      container.appendChild(div);
    }

    const totalCells = Math.ceil((firstDay.getDay() + lastDay.getDate()) / 7) * 7;
    const remaining = totalCells - (firstDay.getDay() + lastDay.getDate());
    for (let i = 1; i <= remaining; i++) {
      const div = document.createElement('div');
      div.className = 'sch-cal-day other-month';
      div.textContent = i;
      container.appendChild(div);
    }
  }

  function renderUpcoming() {
    const container = document.getElementById('upcomingList');
    const today = new Date();
    const currentTime = `${today.getHours().toString().padStart(2, '0')}:${today.getMinutes().toString().padStart(2, '0')}`;

    const upcoming = state.appointments
      .filter(a => {
        if (a.status === 'completed' || a.status === 'cancelled') return false;
        if (a.date > formatDateKey(new Date())) return true;
        if (a.date === formatDateKey(new Date()) && a.time > currentTime) return true;
        return false;
      })
      .sort((a, b) => {
        if (a.date !== b.date) return a.date.localeCompare(b.date);
        return a.time.localeCompare(b.time);
      })
      .slice(0, 5);

    if (upcoming.length === 0) {
      container.innerHTML = `
        <div class="sch-upcoming-empty">
          <i class="fas fa-calendar-check"></i>
          <p>No upcoming appointments</p>
        </div>
      `;
      return;
    }

    container.innerHTML = upcoming.map(appt => {
      const time = formatTime(appt.time);
      return `
        <div class="sch-upcoming-item" data-appt-id="${appt.id}">
          <div class="sch-upcoming-time">
            <span class="sch-upcoming-time-main">${time.main}</span>
            <span class="sch-upcoming-time-sub">${time.sub}</span>
          </div>
          <div class="sch-upcoming-line ${appt.status}"></div>
          <div class="sch-upcoming-body">
            <div class="sch-upcoming-name">${appt.patient.name}</div>
            <div class="sch-upcoming-type">${appt.type}</div>
          </div>
        </div>
      `;
    }).join('');

    container.querySelectorAll('.sch-upcoming-item').forEach(item => {
      item.addEventListener('click', () => {
        const apptId = parseInt(item.dataset.apptId, 10);
        const appt = state.appointments.find(a => a.id === apptId);
        if (appt) {
          const date = new Date(appt.date + 'T00:00:00');
          selectDate(date);
        }
      });
    });
  }

  function renderDayView() {
    const appointments = getAppointmentsForDate(state.selectedDate);

    document.getElementById('dayViewTitle').textContent = formatDateDisplay(state.selectedDate);
    document.getElementById('dayViewSub').textContent = `${appointments.length} appointment${appointments.length !== 1 ? 's' : ''}`;
    document.getElementById('dayLabel').textContent = state.selectedDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });

    const container = document.getElementById('dayContent');

    if (appointments.length === 0) {
      container.innerHTML = `
        <div class="sch-empty-state">
          <i class="fas fa-calendar-check"></i>
          <h3>No Appointments</h3>
          <p>No appointments scheduled for this day. Enjoy your free time!</p>
        </div>
      `;
      return;
    }

    const activeAppointments = appointments.filter(a => a.status !== 'completed' && a.status !== 'missed' && a.status !== 'cancelled');
    const completedAppointments = appointments.filter(a => a.status === 'completed');
    const missedAppointments = appointments.filter(a => a.status === 'missed');
    const cancelledAppointments = appointments.filter(a => a.status === 'cancelled');

    const renderAppointment = (appt) => {
      const time = formatTime(appt.time);
      const riskClass = `risk-${appt.patient.risk}`;

      return `
        <div class="sch-appt-item sch-status-${appt.status}" data-appt-id="${appt.id}">
          <div class="sch-appt-time">
            <span class="sch-appt-time-main">${time.main}</span>
            <span class="sch-appt-time-sub">${time.sub}</span>
            <span class="sch-appt-duration">${appt.duration} min</span>
          </div>
          <div class="sch-appt-line ${appt.status}"></div>
          <div class="sch-appt-body">
            <div class="sch-appt-header">
              <div class="sch-appt-patient">
                <img class="sch-appt-avatar" src="${getAvatarUrl(appt.patient.name)}" alt="${appt.patient.name}">
                <div class="sch-appt-info">
                  <h4>${appt.patient.name}</h4>
                  <span class="sch-appt-type">${appt.type}</span>
                </div>
              </div>
              <span class="sch-appt-status ${appt.status}">${appt.status}</span>
            </div>
            <div class="sch-appt-meta">
              <span class="sch-appt-tag trimester"><i class="fas fa-baby"></i> ${getOrdinal(appt.patient.trimester)} Trimester</span>
              <span class="sch-appt-tag week"><i class="fas fa-calendar-week"></i> Week ${appt.patient.week}</span>
              <span class="sch-appt-tag ${riskClass}"><i class="fas fa-shield-alt"></i> ${capitalize(appt.patient.risk)} Risk</span>
            </div>
            ${appt.notes ? `
              <div class="sch-appt-notes">
                <i class="fas fa-sticky-note"></i>
                <span>${appt.notes}</span>
              </div>
            ` : ''}
            <div class="sch-appt-actions">
              <button class="sch-appt-btn primary" onclick="viewPatient(${appt.patient.id})">
                <i class="fas fa-user"></i> View Patient
              </button>
              <button class="sch-appt-btn" onclick="messagePatient(${appt.patient.id})">
                <i class="fas fa-comment"></i> Message
              </button>
              ${appt.status !== 'completed' && appt.status !== 'cancelled' ? `
                <button class="sch-appt-btn success" onclick="completeAppointment(${appt.id})">
                  <i class="fas fa-check"></i> Complete
                </button>
              ` : ''}
            </div>
          </div>
        </div>
      `;
    };

    const renderStatusSection = (title, statusClass, items) => {
      if (!items.length) {
        return '';
      }

      return `
        <div class="sch-status-section ${statusClass}">
          <div class="sch-status-section-header">
            <h4>${title}</h4>
            <span>${items.length}</span>
          </div>
          <div class="sch-status-section-list">
            ${items.map(renderAppointment).join('')}
          </div>
        </div>
      `;
    };

    let html = '';

    if (activeAppointments.length) {
      html += `
        <div class="sch-status-group today-group">
          <div class="sch-status-group-header">
            <h4>Today's Schedule</h4>
            <span>${activeAppointments.length}</span>
          </div>
          <div class="sch-status-group-body">
            ${activeAppointments.map(renderAppointment).join('')}
          </div>
        </div>
      `;
    }

    const otherSectionsHtml =
      renderStatusSection('Completed Appointments', 'completed', completedAppointments) +
      renderStatusSection('Missed Appointments', 'missed', missedAppointments) +
      renderStatusSection('Cancelled Appointments', 'cancelled', cancelledAppointments);

    const otherAppointmentsCount = completedAppointments.length + missedAppointments.length + cancelledAppointments.length;
    if (otherAppointmentsCount > 0) {
      html += `
        <div class="sch-status-group others-group">
          <div class="sch-status-group-header">
            <h4>Other Appointments</h4>
            <span>${otherAppointmentsCount}</span>
          </div>
          <div class="sch-status-group-body">
            ${otherSectionsHtml}
          </div>
        </div>
      `;
    }

    container.innerHTML = html;
  }

  function renderWeekView() {
    const startOfWeek = getStartOfWeek(state.selectedDate);
    const appointments = getAppointmentsForWeek(startOfWeek);
    const today = new Date();

    const endOfWeek = new Date(startOfWeek);
    endOfWeek.setDate(endOfWeek.getDate() + 6);
    document.getElementById('weekViewTitle').textContent = 'Weekly Schedule';
    document.getElementById('weekViewSub').textContent = `${startOfWeek.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${endOfWeek.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}`;
    document.getElementById('weekLabel').textContent = `${startOfWeek.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}`;

    const container = document.getElementById('weekContent');

    let html = '<div class="sch-week-grid">';
    html += '<div class="sch-week-header">';
    html += '<div class="sch-week-header-cell"></div>';
    for (let i = 0; i < 7; i++) {
      const date = new Date(startOfWeek);
      date.setDate(date.getDate() + i);
      const isToday = date.toDateString() === today.toDateString();
      html += `
        <div class="sch-week-header-cell ${isToday ? 'today' : ''}">
          <div class="sch-week-day-name">${date.toLocaleDateString('en-US', { weekday: 'short' })}</div>
          <div class="sch-week-day-num">${date.getDate()}</div>
        </div>
      `;
    }
    html += '</div>';

    html += '<div class="sch-week-body">';
    for (let hour = 8; hour <= 18; hour++) {
      html += '<div class="sch-week-row">';
      html += `<div class="sch-week-time">${hour > 12 ? hour - 12 : hour}:00 ${hour >= 12 ? 'PM' : 'AM'}</div>`;

      for (let i = 0; i < 7; i++) {
        const date = new Date(startOfWeek);
        date.setDate(date.getDate() + i);
        const dateKey = formatDateKey(date);
        const isToday = date.toDateString() === today.toDateString();

        const hourAppts = (appointments[dateKey] || []).filter(a => {
          const apptHour = parseInt(a.time.split(':')[0], 10);
          return apptHour === hour;
        });

        html += `<div class="sch-week-cell ${isToday ? 'today' : ''}">`;
        hourAppts.forEach(appt => {
          const time = formatTime(appt.time);
          html += `
            <div class="sch-week-appt ${appt.status}" onclick="selectAppointment(${appt.id})">
              <div class="sch-week-appt-time">${time.main} ${time.sub}</div>
              <div class="sch-week-appt-name">${appt.patient.name}</div>
            </div>
          `;
        });
        html += '</div>';
      }
      html += '</div>';
    }
    html += '</div>';
    html += '</div>';

    container.innerHTML = html;
  }

  function renderMonthView() {
    const year = state.currentDate.getFullYear();
    const month = state.currentDate.getMonth();
    const appointments = getAppointmentsForMonth(year, month);
    const today = new Date();

    document.getElementById('monthViewTitle').textContent = state.currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
    document.getElementById('monthViewSub').textContent = 'Monthly overview';
    document.getElementById('monthLabel').textContent = state.currentDate.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });

    const container = document.getElementById('monthContent');
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    let html = '<div class="sch-month-grid">';

    ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].forEach(day => {
      html += `<div class="sch-month-header-cell">${day}</div>`;
    });

    for (let i = 0; i < firstDay.getDay(); i++) {
      const day = new Date(year, month, 0).getDate() - firstDay.getDay() + i + 1;
      html += `<div class="sch-month-cell other-month"><div class="sch-month-day-num">${day}</div></div>`;
    }

    for (let i = 1; i <= lastDay.getDate(); i++) {
      const date = new Date(year, month, i);
      const dateKey = formatDateKey(date);
      const isToday = date.toDateString() === today.toDateString();
      const dayAppts = appointments[dateKey] || [];

      html += `<div class="sch-month-cell ${isToday ? 'today' : ''}">`;
      html += `<div class="sch-month-day-num">${i}</div>`;

      if (dayAppts.length > 0) {
        html += '<div class="sch-month-appts">';
        const displayAppts = dayAppts.slice(0, 3);
        displayAppts.forEach(appt => {
          const time = formatTime(appt.time);
          html += `
            <div class="sch-month-appt ${appt.status}" onclick="selectDateAndAppt('${dateKey}', ${appt.id})">
              ${time.main} ${appt.patient.name.split(' ')[0]}
            </div>
          `;
        });
        if (dayAppts.length > 3) {
          html += `<div class="sch-month-more" onclick="selectDateFromMonth('${dateKey}')">+${dayAppts.length - 3} more</div>`;
        }
        html += '</div>';
      }
      html += '</div>';
    }

    const totalCells = Math.ceil((firstDay.getDay() + lastDay.getDate()) / 7) * 7;
    const remaining = totalCells - (firstDay.getDay() + lastDay.getDate());
    for (let i = 1; i <= remaining; i++) {
      html += `<div class="sch-month-cell other-month"><div class="sch-month-day-num">${i}</div></div>`;
    }

    html += '</div>';
    container.innerHTML = html;
  }

  function getOrdinal(n) {
    const suffixes = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return n + (suffixes[(v - 20) % 10] || suffixes[v] || suffixes[0]);
  }

  function capitalize(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  function selectDate(date) {
    state.selectedDate = date;
    state.currentDate = new Date(date);
    renderCalendar();
    renderDayView();
    renderWeekView();
    renderMonthView();
  }

  function switchView(view) {
    state.currentView = view;

    document.querySelectorAll('.sch-view-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.view === view);
    });

    document.querySelectorAll('.sch-view-panel').forEach(panel => {
      panel.classList.remove('active');
    });
    document.getElementById(`${view}View`).classList.add('active');
  }

  function applyFilter(filter) {
    state.currentFilter = filter;

    const filterBtn = document.getElementById('filterBtn');
    const filterText = filter === 'all' ? 'All Status' : capitalize(filter);
    filterBtn.querySelector('span').textContent = filterText;

    document.querySelectorAll('.sch-filter-option').forEach(opt => {
      opt.classList.toggle('active', opt.dataset.filter === filter);
    });

    renderDayView();
    renderWeekView();
    renderMonthView();

    document.querySelector('.sch-filter-dropdown').classList.remove('open');

    showToast('Filter Applied', `Showing ${filterText.toLowerCase()} appointments`, 'info');
  }

  function applySearch(query) {
    state.searchQuery = query;
    renderDayView();
    renderWeekView();
    renderMonthView();
  }

  window.viewPatient = function (patientId) {
    if (!patientId) {
      showToast('Unavailable', 'Patient details are not available for this slot', 'warning');
      return;
    }
    window.location.href = `/Doctor/PatientDetails/${DOCTOR_ID}/${patientId}`;
  };

  window.messagePatient = function (patientId) {
    if (!patientId) {
      showToast('Unavailable', 'Patient messaging is not available for this slot', 'warning');
      return;
    }
    window.location.href = `/Doctor/Messages/${DOCTOR_ID}?patient=${patientId}`;
  };

  window.completeAppointment = async function (apptId) {
    const appt = state.appointments.find(a => a.id === apptId);
    if (!appt) {
      return;
    }

    try {
      const result = await updateAppointmentStatus(apptId, 'completed');
      if (!result?.success) {
        showToast('Update Failed', result?.message || 'Could not update appointment status.', 'error');
        return;
      }

      appt.status = normalizeStatus(result.status || 'completed');
      if (Array.isArray(result.autoMissedIds) && result.autoMissedIds.length > 0) {
        result.autoMissedIds.forEach(id => {
          const autoMissed = state.appointments.find(a => a.id === id);
          if (autoMissed) {
            autoMissed.status = 'missed';
          }
        });
      }

      renderDayView();
      renderWeekView();
      renderMonthView();
      renderUpcoming();
      updateStats();
      showToast('Appointment Completed', result.message || `Appointment with ${appt.patient.name} marked as complete`, 'success');
    } catch {
      showToast('Update Failed', 'Could not update appointment status.', 'error');
    }
  };

  window.cancelAppointment = async function (apptId) {
    const appt = state.appointments.find(a => a.id === apptId);
    if (!appt || !confirm(`Are you sure you want to cancel the appointment with ${appt.patient.name}?`)) {
      return;
    }

    try {
      const result = await updateAppointmentStatus(apptId, 'cancelled');
      if (!result?.success) {
        showToast('Update Failed', result?.message || 'Could not update appointment status.', 'error');
        return;
      }

      appt.status = normalizeStatus(result.status || 'cancelled');
      renderDayView();
      renderWeekView();
      renderMonthView();
      renderUpcoming();
      updateStats();
      showToast('Appointment Cancelled', result.message || `Appointment with ${appt.patient.name} has been cancelled`, 'warning');
    } catch {
      showToast('Update Failed', 'Could not update appointment status.', 'error');
    }
  };

  window.selectAppointment = function (apptId) {
    const appt = state.appointments.find(a => a.id === apptId);
    if (appt) {
      const date = new Date(appt.date + 'T00:00:00');
      selectDate(date);
      switchView('day');
    }
  };

  window.selectDateFromMonth = function (dateKey) {
    const date = new Date(dateKey + 'T00:00:00');
    selectDate(date);
    switchView('day');
  };

  window.selectDateAndAppt = function (dateKey) {
    const date = new Date(dateKey + 'T00:00:00');
    selectDate(date);
    switchView('day');
  };

  function showToast(title, message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const icons = {
      success: 'fa-check-circle',
      warning: 'fa-exclamation-triangle',
      error: 'fa-times-circle',
      info: 'fa-info-circle'
    };

    const toast = document.createElement('div');
    toast.className = `sch-toast ${type}`;
    toast.innerHTML = `
      <div class="sch-toast-icon"><i class="fas ${icons[type]}\"></i></div>
      <div class="sch-toast-body">
        <div class="sch-toast-title">${title}</div>
        <div class="sch-toast-message">${message}</div>
      </div>
      <button class="sch-toast-close"><i class="fas fa-times"></i></button>
    `;

    container.appendChild(toast);

    const closeBtn = toast.querySelector('.sch-toast-close');
    closeBtn.addEventListener('click', () => {
      toast.style.animation = 'toastSlideOut 0.3s ease forwards';
      setTimeout(() => toast.remove(), 300);
    });

    setTimeout(() => {
      if (toast.parentNode) {
        toast.style.animation = 'toastSlideOut 0.3s ease forwards';
        setTimeout(() => toast.remove(), 300);
      }
    }, 5000);
  }

  const style = document.createElement('style');
  style.textContent = `
    @keyframes toastSlideOut {
      to {
        transform: translateX(120%);
        opacity: 0;
      }
    }
  `;
  document.head.appendChild(style);

  function closeModal() {
    document.getElementById('modalOverlay').classList.remove('active');
  }

  function init() {
    loadAppointments();
    renderCalendar();
    renderDayView();
    renderWeekView();
    renderMonthView();

    document.querySelectorAll('.sch-view-btn').forEach(btn => {
      btn.addEventListener('click', () => switchView(btn.dataset.view));
    });

    document.getElementById('todayBtn').addEventListener('click', () => {
      selectDate(new Date());
    });

    document.getElementById('prevMonth').addEventListener('click', () => {
      state.currentDate.setMonth(state.currentDate.getMonth() - 1);
      renderCalendar();
    });

    document.getElementById('nextMonth').addEventListener('click', () => {
      state.currentDate.setMonth(state.currentDate.getMonth() + 1);
      renderCalendar();
    });

    document.getElementById('prevDay').addEventListener('click', () => {
      const newDate = new Date(state.selectedDate);
      newDate.setDate(newDate.getDate() - 1);
      selectDate(newDate);
    });

    document.getElementById('nextDay').addEventListener('click', () => {
      const newDate = new Date(state.selectedDate);
      newDate.setDate(newDate.getDate() + 1);
      selectDate(newDate);
    });

    document.getElementById('prevWeek').addEventListener('click', () => {
      const newDate = new Date(state.selectedDate);
      newDate.setDate(newDate.getDate() - 7);
      selectDate(newDate);
    });

    document.getElementById('nextWeek').addEventListener('click', () => {
      const newDate = new Date(state.selectedDate);
      newDate.setDate(newDate.getDate() + 7);
      selectDate(newDate);
    });

    document.getElementById('prevMonthView').addEventListener('click', () => {
      state.currentDate.setMonth(state.currentDate.getMonth() - 1);
      renderMonthView();
    });

    document.getElementById('nextMonthView').addEventListener('click', () => {
      state.currentDate.setMonth(state.currentDate.getMonth() + 1);
      renderMonthView();
    });

    const searchInput = document.getElementById('searchInput');
    let searchTimeout;
    searchInput.addEventListener('input', (e) => {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(() => applySearch(e.target.value), 300);
    });

    const filterBtn = document.getElementById('filterBtn');
    const filterDropdown = document.querySelector('.sch-filter-dropdown');

    filterBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      filterDropdown.classList.toggle('open');
    });

    document.querySelectorAll('.sch-filter-option').forEach(opt => {
      opt.addEventListener('click', () => applyFilter(opt.dataset.filter));
    });

    document.addEventListener('click', () => {
      filterDropdown.classList.remove('open');
    });

    document.getElementById('syncBtn').addEventListener('click', () => {
      showToast('Calendar Sync', 'Syncing with external calendars...', 'info');
    });

    document.getElementById('exportBtn').addEventListener('click', () => {
      showToast('Export', 'Export functionality coming soon', 'info');
    });

    document.getElementById('newApptBtn').addEventListener('click', () => {
      showToast('New Appointment', 'New appointment form coming soon', 'info');
    });

    document.querySelectorAll('.sch-quick-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const action = btn.dataset.action;
        const actions = {
          new: 'New Appointment',
          block: 'Block Time Slot',
          availability: 'Set Availability',
          recurring: 'Recurring Schedule'
        };
        showToast(actions[action], `${actions[action]} feature coming soon`, 'info');
      });
    });

    document.getElementById('closeModal').addEventListener('click', closeModal);
    document.getElementById('modalCancel').addEventListener('click', closeModal);
    document.getElementById('modalOverlay').addEventListener('click', (e) => {
      if (e.target === e.currentTarget) closeModal();
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

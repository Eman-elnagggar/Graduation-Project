(() => {
  'use strict';

  const seedElement = document.getElementById('analyticsSeed');
  const seed = seedElement ? JSON.parse(seedElement.textContent || '{}') : {};

  const labels = Array.isArray(seed.labels) && seed.labels.length ? seed.labels : ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'];
  const appointments = Array.isArray(seed.appointments) && seed.appointments.length ? seed.appointments : [0, 0, 0, 0, 0, 0];
  const scheduled = Array.isArray(seed.scheduled) && seed.scheduled.length ? seed.scheduled : appointments.map(v => Math.round(v * 0.7));
  const patients = Array.isArray(seed.patients) && seed.patients.length ? seed.patients : appointments.map(v => Math.max(0, Math.round(v * 0.45)));

  const weeklyData = Array.isArray(seed.weekly) && seed.weekly.length ? seed.weekly : [0, 0, 0, 0, 0, 0, 0];
  const daysOfWeek = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  function showLocalToast(message) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => toast.remove(), 2500);
  }

  function formatDate() {
    return new Date().toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  function animateCounters() {
    const values = document.querySelectorAll('.kpi-val[data-count]');

    values.forEach((el) => {
      const target = Number(el.dataset.count || 0);
      const suffix = el.dataset.suffix || '';
      const start = performance.now();
      const duration = 1200;

      const tick = (now) => {
        const t = Math.min((now - start) / duration, 1);
        const eased = 1 - Math.pow(1 - t, 3);
        const value = Math.round(target * eased);
        el.textContent = `${value}${suffix}`;

        if (t < 1) {
          requestAnimationFrame(tick);
        }
      };

      requestAnimationFrame(tick);
    });
  }

  function renderTrendBars(type = 'appointments') {
    const chart = document.getElementById('trendsChart');
    if (!chart) return;

    const isPatientsTab = type === 'patients';
    const primary = isPatientsTab ? patients : appointments;
    const secondary = isPatientsTab ? appointments : scheduled;

    const maxPrimary = Math.max(...primary, 1);
    const maxSecondary = Math.max(...secondary, 1);

    chart.innerHTML = labels.map((label, index) => {
      const mainHeight = Math.max((primary[index] / maxPrimary) * 100, 4);
      const secondaryHeight = Math.max((secondary[index] / maxSecondary) * 60, 3);
      const primaryLabel = isPatientsTab ? 'patients' : 'appointments';
      const secondaryLabel = isPatientsTab ? 'completed appointments' : 'scheduled appointments';

      return `
        <div class="bar-group">
          <div class="bar-wrapper">
            <div class="bar secondary" style="height:${secondaryHeight}%">
              <span class="bar-tooltip">${secondary[index]} ${secondaryLabel}</span>
            </div>
            <div class="bar primary" style="height:${mainHeight}%">
              <span class="bar-tooltip">${primary[index]} ${primaryLabel}</span>
            </div>
          </div>
          <span class="bar-label">${label}</span>
        </div>
      `;
    }).join('');

    const primaryLegend = document.getElementById('trendLegendPrimary');
    const secondaryLegend = document.getElementById('trendLegendSecondary');
    if (primaryLegend && secondaryLegend) {
      primaryLegend.textContent = isPatientsTab ? 'Patients' : 'Completed';
      secondaryLegend.textContent = isPatientsTab ? 'Completed' : 'Scheduled';
    }
  }

  function renderWeeklyChart() {
    const chart = document.getElementById('weeklyChart');
    const labelHost = document.getElementById('weeklyLabels');
    if (!chart || !labelHost) return;

    const max = Math.max(...weeklyData, 1);
    const today = new Date().getDay();

    chart.innerHTML = weeklyData.map((value, index) => {
      const height = Math.max((value / max) * 100, 8);
      const todayClass = index === today ? 'today' : '';
      return `<div class="week-bar ${todayClass}" style="height:${height}%" title="${value} appointments"></div>`;
    }).join('');

    labelHost.innerHTML = daysOfWeek.map((label, index) => {
      return `<span class="${index === today ? 'today' : ''}">${label}</span>`;
    }).join('');
  }

  function wireActions() {
    document.getElementById('periodSelect')?.addEventListener('change', (e) => {
      const select = e.currentTarget;
      const text = select.options[select.selectedIndex]?.text || 'selected period';
      showLocalToast(`Showing analytics for ${text}`);
    });

    document.getElementById('exportBtn')?.addEventListener('click', () => {
      showLocalToast('Export feature is ready for backend integration.');
    });

    document.getElementById('printBtn')?.addEventListener('click', () => {
      window.print();
    });

    const tabs = document.querySelectorAll('.chart-tab');
    tabs.forEach((tab) => {
      tab.addEventListener('click', () => {
        tabs.forEach((t) => t.classList.remove('active'));
        tab.classList.add('active');
        renderTrendBars(tab.dataset.tab || 'appointments');
      });
    });
  }

  function init() {
    const date = document.getElementById('currentDate');
    if (date) {
      date.textContent = formatDate();
    }

    renderTrendBars('appointments');
    renderWeeklyChart();
    animateCounters();
    wireActions();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

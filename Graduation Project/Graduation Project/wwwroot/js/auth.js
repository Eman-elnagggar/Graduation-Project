/**
 * MamaCare — Auth Pages Shared JavaScript
 */

function showToast(message, type = 'info') {
  let wrap = document.getElementById('mc-toast-wrap');
  if (!wrap) {
    wrap = document.createElement('div');
    wrap.id = 'mc-toast-wrap';
    wrap.className = 'mc-toast-wrap';
    document.body.appendChild(wrap);
  }
  const icons = { success: 'fa-check-circle', error: 'fa-exclamation-circle', info: 'fa-info-circle' };
  const t = document.createElement('div');
  t.className = 'mc-toast ' + type;
  t.innerHTML = `<i class="fas ${icons[type] || icons.info}"></i><span>${message}</span>`;
  wrap.appendChild(t);
  setTimeout(() => {
    t.style.opacity = '0';
    t.style.transform = 'translateX(30px)';
    t.style.transition = 'all .3s';
    setTimeout(() => t.remove(), 300);
  }, 4000);
}

function initPasswordToggles() {
  document.querySelectorAll('.mc-input-eye').forEach(btn => {
    btn.addEventListener('click', function () {
      const input = this.previousElementSibling;
      if (!input || input.tagName !== 'INPUT') return;
      const show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      this.querySelector('i').className = show ? 'fas fa-eye-slash' : 'fas fa-eye';
    });
  });
}

function initPasswordStrength(inputId, barId, labelId) {
  const input = document.getElementById(inputId);
  const bar = document.getElementById(barId);
  const lbl = document.getElementById(labelId);
  if (!input || !bar || !lbl) return;

  input.addEventListener('input', () => {
    const val = input.value;
    let score = 0;
    if (val.length >= 8) score++;
    if (/[A-Z]/.test(val)) score++;
    if (/[0-9]/.test(val)) score++;
    if (/[^A-Za-z0-9]/.test(val)) score++;

    const levels = [
      { pct: '0%', color: '', label: '' },
      { pct: '25%', color: '#ef4444', label: 'Weak' },
      { pct: '50%', color: '#f59e0b', label: 'Fair' },
      { pct: '75%', color: '#3b82f6', label: 'Good' },
      { pct: '100%', color: '#22c55e', label: 'Strong' }
    ];

    const l = levels[score] || levels[0];
    bar.style.width = l.pct;
    bar.style.background = l.color;
    lbl.textContent = l.label;
    lbl.style.color = l.color;
  });
}

function validateField(input) {
  const val = input.value.trim();
  let error = '';

  if (input.required && !val) {
    error = 'This field is required.';
  } else if (input.type === 'email' && val && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val)) {
    error = 'Please enter a valid email address.';
  } else if (input.dataset.minLength && val.length < parseInt(input.dataset.minLength)) {
    error = `Must be at least ${input.dataset.minLength} characters.`;
  } else if (input.dataset.match) {
    const other = document.getElementById(input.dataset.match);
    if (other && val !== other.value) error = 'Passwords do not match.';
  } else if (input.type === 'tel' && val && !/^\+?[\d\s\-()]{7,}$/.test(val)) {
    error = 'Please enter a valid phone number.';
  }

  setFieldState(input, error);
  return !error;
}

function setFieldState(input, error) {
  input.classList.remove('is-invalid', 'is-valid');
  const wrap = input.closest('.mc-form-group') || input.parentElement;
  const existing = wrap ? wrap.querySelector('.mc-feedback') : null;
  if (existing) existing.remove();

  if (error) {
    input.classList.add('is-invalid');
    if (wrap) {
      const fb = document.createElement('div');
      fb.className = 'mc-feedback invalid';
      fb.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${error}`;
      wrap.appendChild(fb);
    }
  } else if (input.value.trim()) {
    input.classList.add('is-valid');
  }
}

function validateForm(formId) {
  const form = document.getElementById(formId);
  if (!form) return true;
  let valid = true;
  form.querySelectorAll('input[required], select[required]').forEach(input => {
    if (!validateField(input)) valid = false;
  });
  return valid;
}

function initLiveValidation(formId) {
  const form = document.getElementById(formId);
  if (!form) return;
  form.querySelectorAll('input, select').forEach(input => {
    input.addEventListener('blur', () => validateField(input));
    input.addEventListener('input', () => {
      if (input.classList.contains('is-invalid')) validateField(input);
    });
  });
}

function initRadioGroups() {
  document.querySelectorAll('.mc-radio-btn').forEach(btn => {
    const radio = btn.querySelector('input[type=radio]');
    if (!radio) return;
    btn.addEventListener('click', () => {
      const name = radio.name;
      document.querySelectorAll(`.mc-radio-btn input[name="${name}"]`).forEach(r => {
        r.closest('.mc-radio-btn').classList.remove('selected');
      });
      radio.checked = true;
      btn.classList.add('selected');
      btn.dispatchEvent(new Event('change', { bubbles: true }));
    });
  });
}

function initFileUploads() {
  document.querySelectorAll('.mc-file-zone').forEach(zone => {
    const input = zone.querySelector('input[type=file]');
    const nameEl = zone.querySelector('.mc-file-name');
    if (!input) return;

    zone.addEventListener('click', e => {
      if (e.target !== input) input.click();
    });

    zone.addEventListener('dragover', e => {
      e.preventDefault();
      zone.style.borderColor = 'var(--mc-primary)';
    });

    zone.addEventListener('dragleave', () => {
      zone.style.borderColor = '';
    });

    zone.addEventListener('drop', e => {
      e.preventDefault();
      zone.style.borderColor = '';
      if (e.dataTransfer.files.length) {
        const dt = new DataTransfer();
        dt.items.add(e.dataTransfer.files[0]);
        input.files = dt.files;
        if (nameEl) nameEl.textContent = e.dataTransfer.files[0].name;
      }
    });

    input.addEventListener('change', () => {
      if (input.files.length && nameEl) nameEl.textContent = input.files[0].name;
    });
  });
}

document.addEventListener('DOMContentLoaded', () => {
  initPasswordToggles();
  initRadioGroups();
  initFileUploads();
});

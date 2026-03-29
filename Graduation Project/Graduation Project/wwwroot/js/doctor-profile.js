(() => {
  document.addEventListener('DOMContentLoaded', () => {
    initProfileActions();
    initInlineSectionEditing();
    initPasswordVisibility();
    initPasswordStrength();
    initPasswordSubmit();
    initDangerZoneActions();
  });

  function initProfileActions() {
    const editProfileBtn = document.getElementById('editProfileBtn');
    editProfileBtn?.addEventListener('click', () => {
      const doctorId = window.DOCTOR_PROFILE?.doctorId;
      if (doctorId) {
        window.location.href = `/Doctor/EditProfile/${doctorId}`;
      }
    });

  }

  function initInlineSectionEditing() {
    document.querySelectorAll('.section-edit-btn[data-edit-form]').forEach(btn => {
      btn.addEventListener('click', () => {
        const formId = btn.dataset.editForm;
        const actionsId = btn.dataset.actionsId;
        const form = document.getElementById(formId);
        const actions = document.getElementById(actionsId);
        if (!form || !actions) return;

        const editableInputs = getEditableInputs(form);
        const isEditMode = editableInputs.some(i => !i.disabled);

        if (isEditMode) {
          cancelInlineEdit(form, actions, btn);
          return;
        }

        editableInputs.forEach(input => {
          input.dataset.originalValue = input.value;
          input.disabled = false;
        });

        actions.style.display = 'flex';
        btn.innerHTML = '<i class="fas fa-times"></i> Cancel';
      });
    });

    document.querySelectorAll('.cancel-edit-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const form = document.getElementById(btn.dataset.editForm);
        const actions = document.getElementById(btn.dataset.actionsId);
        const editBtn = document.getElementById(btn.dataset.editBtnId);
        if (!form || !actions || !editBtn) return;

        cancelInlineEdit(form, actions, editBtn);
      });
    });

    document.querySelectorAll('.inline-edit-form[data-save-url]').forEach(form => {
      form.addEventListener('submit', async e => {
        e.preventDefault();

        const doctorId = window.DOCTOR_PROFILE?.doctorId;
        if (!doctorId) {
          showToast('Error', 'Doctor id not found.', 'error');
          return;
        }

        const saveUrl = form.dataset.saveUrl;
        const token = getAntiForgeryToken();
        if (!saveUrl || !token) {
          showToast('Error', 'Security token not found.', 'error');
          return;
        }

        const formData = new FormData(form);
        formData.append('doctorId', doctorId);
        formData.append('__RequestVerificationToken', token);

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn?.innerHTML;
        if (submitBtn) {
          submitBtn.disabled = true;
          submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
        }

        try {
          const response = await fetch(saveUrl, { method: 'POST', body: formData });
          const data = await response.json();
          if (!data.success) {
            showToast('Error', data.message || 'Save failed.', 'error');
            return;
          }

          const actions = form.querySelector('.inline-actions');
          const editBtn = findEditButtonForForm(form.id);
          if (actions && editBtn) {
            cancelInlineEdit(form, actions, editBtn, true);
          }

          showToast('Success', data.message || 'Changes saved.', 'success');
        } catch {
          showToast('Error', 'Could not save changes. Please try again.', 'error');
        } finally {
          if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText || '<i class="fas fa-save"></i> Save Changes';
          }
        }
      });
    });
  }

  function getEditableInputs(form) {
    return Array.from(form.querySelectorAll('input[name], select[name], textarea[name]'));
  }

  function findEditButtonForForm(formId) {
    return document.querySelector(`.section-edit-btn[data-edit-form="${formId}"]`);
  }

  function cancelInlineEdit(form, actions, editBtn, keepCurrentValues = false) {
    getEditableInputs(form).forEach(input => {
      if (!keepCurrentValues && input.dataset.originalValue !== undefined) {
        input.value = input.dataset.originalValue;
      }
      input.disabled = true;
    });

    actions.style.display = 'none';
    editBtn.innerHTML = '<i class="fas fa-pen"></i> Edit';
  }

  function initPasswordVisibility() {
    document.querySelectorAll('.password-toggle').forEach(btn => {
      btn.addEventListener('click', () => {
        const input = btn.parentElement?.querySelector('input');
        const icon = btn.querySelector('i');
        if (!input || !icon) return;

        if (input.type === 'password') {
          input.type = 'text';
          icon.className = 'fas fa-eye-slash';
        } else {
          input.type = 'password';
          icon.className = 'fas fa-eye';
        }
      });
    });
  }

  function initPasswordStrength() {
    const newPassword = document.getElementById('newPassword');
    newPassword?.addEventListener('input', e => {
      const password = e.target.value || '';
      updatePasswordRequirements(password);
      updatePasswordStrength(password);
    });
  }

  function updatePasswordRequirements(password) {
    const checks = {
      length: password.length >= 8,
      upper: /[A-Z]/.test(password),
      lower: /[a-z]/.test(password),
      number: /\d/.test(password),
      special: /[!@#$%^&*]/.test(password)
    };

    Object.entries(checks).forEach(([key, valid]) => {
      const item = document.getElementById(`req-${key}`);
      if (!item) return;

      item.classList.toggle('ok', valid);
      const icon = item.querySelector('i');
      if (icon) {
        icon.className = valid ? 'fas fa-check-circle' : 'fas fa-circle';
      }
    });
  }

  function updatePasswordStrength(password) {
    const fill = document.getElementById('pwFill');
    const label = document.getElementById('pwLabel');
    if (!fill || !label) return;

    if (!password) {
      fill.style.width = '0%';
      fill.style.background = '#e2e8f0';
      label.textContent = 'Password strength';
      return;
    }

    let score = 0;
    if (password.length >= 8) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[a-z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[!@#$%^&*]/.test(password)) score++;

    const states = [
      { width: '20%', text: 'Very Weak', color: '#ef4444' },
      { width: '40%', text: 'Weak', color: '#f97316' },
      { width: '60%', text: 'Fair', color: '#f59e0b' },
      { width: '80%', text: 'Good', color: '#22c55e' },
      { width: '100%', text: 'Strong', color: '#16a34a' }
    ];

    const state = states[Math.max(0, score - 1)] || states[0];
    fill.style.width = state.width;
    fill.style.background = state.color;
    label.textContent = state.text;
  }

  function initPasswordSubmit() {
    const form = document.getElementById('passwordForm');
    form?.addEventListener('submit', async e => {
      e.preventDefault();

      const currentPassword = document.getElementById('currentPassword')?.value || '';
      const newPassword = document.getElementById('newPassword')?.value || '';
      const confirmPassword = document.getElementById('confirmPassword')?.value || '';

      if (!currentPassword || !newPassword || !confirmPassword) {
        showToast('Error', 'Please fill in all password fields.', 'error');
        return;
      }

      if (newPassword !== confirmPassword) {
        showToast('Error', 'New passwords do not match.', 'error');
        return;
      }

      if (newPassword.length < 8) {
        showToast('Error', 'Password must be at least 8 characters.', 'error');
        return;
      }

      const token = getAntiForgeryToken();
      if (!token) {
        showToast('Error', 'Security token not found.', 'error');
        return;
      }

      const formData = new FormData();
      formData.append('currentPassword', currentPassword);
      formData.append('newPassword', newPassword);
      formData.append('confirmPassword', confirmPassword);
      formData.append('__RequestVerificationToken', token);

      try {
        const response = await fetch('/Account/ChangePassword', { method: 'POST', body: formData });
        const data = await response.json();
        if (data.success) {
          showToast('Success', 'Password updated successfully.', 'success');
          form.reset();
          updatePasswordRequirements('');
          updatePasswordStrength('');
        } else {
          showToast('Error', data.message || 'Could not update password.', 'error');
        }
      } catch {
        showToast('Error', 'Could not update password. Please try again.', 'error');
      }
    });
  }

  function initDangerZoneActions() {
    document.getElementById('downloadDataBtn')?.addEventListener('click', async function () {
      const token = getAntiForgeryToken();
      if (!token) {
        showToast('Error', 'Security token not found.', 'error');
        return;
      }

      const btn = this;
      const oldHtml = btn.innerHTML;
      btn.disabled = true;
      btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Preparing...';

      try {
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);

        const response = await fetch('/Account/DownloadMyData', { method: 'POST', body: formData });
        if (!response.ok) {
          showToast('Error', 'Could not download data.', 'error');
          return;
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        const cd = response.headers.get('Content-Disposition') || '';
        const match = cd.match(/filename="?([^";]+)"?/i);

        link.href = url;
        link.download = match?.[1] || 'my-data.csv';
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);

        showToast('Success', 'Data downloaded successfully.', 'success');
      } catch {
        showToast('Error', 'Could not download data. Please try again.', 'error');
      } finally {
        btn.disabled = false;
        btn.innerHTML = oldHtml;
      }
    });

    document.getElementById('deleteAccountBtn')?.addEventListener('click', async function () {
      if (!confirm('Are you sure you want to delete your account? This action cannot be undone.')) {
        return;
      }

      const token = getAntiForgeryToken();
      if (!token) {
        showToast('Error', 'Security token not found.', 'error');
        return;
      }

      const btn = this;
      const oldHtml = btn.innerHTML;
      btn.disabled = true;
      btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';

      try {
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);

        const response = await fetch('/Account/DeleteAccount', { method: 'POST', body: formData });
        const data = await response.json();

        if (data.success) {
          showToast('Success', 'Your account has been deleted.', 'info');
          setTimeout(() => {
            window.location.href = data.redirectUrl || '/Account/Login';
          }, 700);
          return;
        }

        showToast('Error', data.message || 'Could not delete account.', 'error');
      } catch {
        showToast('Error', 'Could not delete account. Please try again.', 'error');
      } finally {
        btn.disabled = false;
        btn.innerHTML = oldHtml;
      }
    });
  }

  function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  function showToast(title, message, type = 'info') {
    let container = document.getElementById('toastContainer');
    if (!container) {
      container = document.createElement('div');
      container.id = 'toastContainer';
      container.className = 'toast-container';
      document.body.appendChild(container);
    }

    const icons = {
      success: 'fa-check-circle',
      error: 'fa-times-circle',
      warning: 'fa-exclamation-triangle',
      info: 'fa-info-circle'
    };

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `
      <div class="toast-icon"><i class="fas ${icons[type] || icons.info}"></i></div>
      <div class="toast-content">
        <div class="toast-title">${title}</div>
        <div class="toast-message">${message}</div>
      </div>
      <button class="toast-close" type="button"><i class="fas fa-times"></i></button>
    `;

    container.appendChild(toast);
    toast.querySelector('.toast-close')?.addEventListener('click', () => removeToast(toast));
    setTimeout(() => removeToast(toast), 5000);
  }

  function removeToast(toast) {
    toast.classList.add('hiding');
    setTimeout(() => toast.remove(), 300);
  }
})();

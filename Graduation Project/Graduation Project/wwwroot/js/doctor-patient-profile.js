(function () {
  'use strict';

  const boot = window.patientProfileBootstrap || {};

  function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  function showNotification(message, type = 'info') {
    const existing = document.querySelector('.pp-notification');
    if (existing) {
      existing.remove();
    }

    const colors = {
      success: '#16a34a',
      error: '#e53e3e',
      warning: '#d97706',
      info: '#2563eb'
    };

    const notification = document.createElement('div');
    notification.className = 'pp-notification';
    notification.style.cssText = `position:fixed;top:20px;right:20px;background:#fff;color:#0f1c2e;padding:12px 16px;border-radius:10px;box-shadow:0 8px 22px rgba(11,31,53,.18);border-left:4px solid ${colors[type] || colors.info};z-index:2000;font-size:.86rem;font-weight:600;`;
    notification.textContent = message;
    document.body.appendChild(notification);

    setTimeout(() => notification.remove(), 3000);
  }

  function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;

    if (modalId === 'prescriptionModal') {
      preparePrescriptionModal();
    }

    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
  }

  function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;

    modal.classList.remove('active');
    if (!document.querySelector('.pp-modal.active')) {
      document.body.style.overflow = '';
    }
  }

  function setupModals() {
    const modalOpeners = {
      btnWritePrescription: 'prescriptionModal',
      btnViewAllAlerts: 'alertsModal',
      btnViewAllAppointments: 'appointmentsModal',
      btnViewAllPrescriptions: 'allPrescriptionsModal',
      btnViewFullHistory: 'medicalHistoryModal',
      btnViewAllLabs: 'labTestsModal'
    };

    Object.entries(modalOpeners).forEach(([id, modalId]) => {
      document.getElementById(id)?.addEventListener('click', () => openModal(modalId));
    });

    document.querySelectorAll('[data-open-modal]').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const modalId = e.currentTarget.getAttribute('data-open-modal');
        if (modalId) {
          document.querySelectorAll('.pp-modal.active').forEach(m => m.classList.remove('active'));
          openModal(modalId);
        }
      });
    });

    document.querySelectorAll('.pp-modal-close,[data-modal]').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const modalId = e.currentTarget.getAttribute('data-modal');
        if (modalId) {
          closeModal(modalId);
        }
      });
    });

    document.querySelectorAll('.pp-modal-overlay').forEach(overlay => {
      overlay.addEventListener('click', () => {
        const modal = overlay.closest('.pp-modal');
        if (modal) {
          modal.classList.remove('active');
          if (!document.querySelector('.pp-modal.active')) {
            document.body.style.overflow = '';
          }
        }
      });
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        document.querySelectorAll('.pp-modal.active').forEach(m => m.classList.remove('active'));
        document.body.style.overflow = '';
      }
    });
  }

  function refreshMedicineRowsUI() {
    const rows = Array.from(document.querySelectorAll('#prescriptionMedicinesContainer .pp-rx-medicine-item'));
    rows.forEach((row, idx) => {
      const title = row.querySelector('.pp-rx-medicine-title');
      if (title) {
        title.textContent = `Medicine ${idx + 1}`;
      }

      const removeBtn = row.querySelector('.pp-rx-remove-btn');
      if (removeBtn) {
        removeBtn.style.display = rows.length > 1 ? 'inline-flex' : 'none';
      }
    });
  }

  function bindMedicineRowEvents(row) {
    const removeBtn = row.querySelector('.pp-rx-remove-btn');
    if (!removeBtn) {
      return;
    }

    removeBtn.addEventListener('click', () => {
      row.remove();
      refreshMedicineRowsUI();
    });
  }

  function addMedicineRow() {
    const template = document.getElementById('ppMedicineRowTemplate');
    const container = document.getElementById('prescriptionMedicinesContainer');
    if (!template || !container) {
      return;
    }

    const node = template.content.firstElementChild?.cloneNode(true);
    if (!node) {
      return;
    }

    container.appendChild(node);
    bindMedicineRowEvents(node);
    refreshMedicineRowsUI();
  }

  function resetMedicineRows() {
    const container = document.getElementById('prescriptionMedicinesContainer');
    if (!container) {
      return;
    }

    container.innerHTML = '';
    addMedicineRow();
  }

  function preparePrescriptionModal() {
    const form = document.getElementById('prescriptionForm');
    form?.reset();
    document.getElementById('prescriptionPatientId').value = String(boot.patientId || 0);
    document.getElementById('prescriptionPatientName').value = boot.patientName || 'Patient';
    resetMedicineRows();

    const firstInput = document.querySelector('#prescriptionMedicinesContainer .pp-medicine-name');
    if (firstInput) {
      firstInput.focus();
    }
  }

  async function savePrescription() {
    const patientId = parseInt(document.getElementById('prescriptionPatientId')?.value || String(boot.patientId || 0), 10);
    const notes = document.getElementById('rxNotes')?.value?.trim() || '';

    const medicineRows = Array.from(document.querySelectorAll('#prescriptionMedicinesContainer .pp-rx-medicine-item'));
    const items = medicineRows.map(row => {
      const durationRaw = row.querySelector('.pp-medicine-duration')?.value?.trim() || '';
      return {
        medicineName: row.querySelector('.pp-medicine-name')?.value?.trim() || '',
        dosage: row.querySelector('.pp-medicine-dosage')?.value?.trim() || '',
        frequency: row.querySelector('.pp-medicine-frequency')?.value?.trim() || '',
        durationDays: durationRaw ? Math.max(0, parseInt(durationRaw, 10) || 0) : 0,
        instructions: row.querySelector('.pp-medicine-instructions')?.value?.trim() || ''
      };
    }).filter(item => item.medicineName);

    if (!patientId) {
      showNotification('Patient is required.', 'warning');
      return;
    }

    if (!items.length) {
      showNotification('Please add at least one medicine name.', 'warning');
      return;
    }

    const url = boot.createPrescriptionUrl;
    if (!url || !boot.patientId) {
      showNotification('Prescription endpoint is not available.', 'error');
      return;
    }

    const token = getAntiForgeryToken();
    const body = new URLSearchParams();
    body.append('patientId', String(patientId));
    items.forEach(item => {
      body.append('medicineNames', item.medicineName || '');
      body.append('dosages', item.dosage || '');
      body.append('frequencies', item.frequency || '');
      body.append('durationDays', String(item.durationDays || 0));
      body.append('instructions', item.instructions || '');
    });
    body.append('notes', notes);
    body.append('__RequestVerificationToken', token);

    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
        },
        body: body.toString()
      });

      const result = await response.json();
      if (!response.ok || !result?.success) {
        showNotification(result?.message || 'Failed to save prescription.', 'error');
        return;
      }

      document.getElementById('prescriptionForm')?.reset();
      resetMedicineRows();
      closeModal('prescriptionModal');
      showNotification(result.message || 'Prescription saved successfully.', 'success');

      if (result?.prescriptionId && boot?.doctorId) {
        const printUrl = `/Doctor/PrintPrescription/${boot.doctorId}?prescriptionId=${result.prescriptionId}`;
        window.open(printUrl, '_blank');
      }
    } catch {
      showNotification('Failed to save prescription.', 'error');
    }
  }

  function toggleAddNoteForm(forceOpen) {
    const form = document.getElementById('addNoteForm');
    if (!form) return;

    const shouldOpen = typeof forceOpen === 'boolean'
      ? forceOpen
      : form.style.display === 'none' || form.style.display === '';

    form.style.display = shouldOpen ? 'block' : 'none';

    if (shouldOpen) {
      document.getElementById('noteText')?.focus();
      form.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  function setupAddNoteButtons() {
    ['btnAddNote', 'btnAddNote2'].forEach(id => {
      document.getElementById(id)?.addEventListener('click', () => toggleAddNoteForm(true));
    });

    document.getElementById('cancelNote')?.addEventListener('click', () => toggleAddNoteForm(false));
  }

  function animateTimeline() {
    const progressBar = document.querySelector('.pp-timeline-fill');
    if (!progressBar) return;

    const targetWidth = progressBar.style.width || '0%';
    progressBar.style.width = '0%';
    setTimeout(() => {
      progressBar.style.width = targetWidth;
    }, 80);
  }

  function setupChartBars() {
    const getBloodSugarRisk = (value) => {
      if (value >= 141) return 'high';
      if (value >= 96) return 'medium';
      return 'low';
    };

    const getBloodPressureRisk = (sys, dia) => {
      if (sys >= 140 || dia >= 90) return 'high';
      if (sys >= 130 || dia >= 80) return 'medium';
      return 'low';
    };

    document.querySelectorAll('.pp-chart-bars-sugar .pp-chart-bar.sugar').forEach(bar => {
      const sugar = parseInt(bar.dataset.value || '0', 10) || 0;
      const risk = getBloodSugarRisk(sugar);
      bar.classList.remove('risk-low', 'risk-medium', 'risk-high');
      bar.classList.add(`risk-${risk}`);
      bar.dataset.risk = risk;
    });

    document.querySelectorAll('.pp-chart-bars:not(.pp-chart-bars-sugar) .pp-chart-bar-group').forEach(group => {
      const sysBar = group.querySelector('.pp-chart-bar.systolic');
      const diaBar = group.querySelector('.pp-chart-bar.diastolic');
      if (!sysBar && !diaBar) return;

      const sys = parseInt(sysBar?.dataset.value || '0', 10) || 0;
      const dia = parseInt(diaBar?.dataset.value || '0', 10) || 0;
      const risk = getBloodPressureRisk(sys, dia);

      [sysBar, diaBar].forEach(bar => {
        if (!bar) return;
        bar.classList.remove('risk-low', 'risk-medium', 'risk-high');
        bar.classList.add(`risk-${risk}`);
        bar.dataset.risk = risk;
      });
    });

    document.querySelectorAll('.pp-chart-bar').forEach(bar => {
      bar.addEventListener('mouseenter', function () {
        this.style.opacity = '0.9';
        this.style.transform = 'scaleY(1.03)';
      });

      bar.addEventListener('mouseleave', function () {
        this.style.opacity = '1';
        this.style.transform = 'scaleY(1)';
      });
    });
  }

  function updateDueDateFooter() {
    const boot = window.patientProfileBootstrap || {};
    const dueDateText = boot.dueDate;
    const footer = document.getElementById('dueDateFoot');
    if (!dueDateText || !footer) return;

    const dueDate = new Date(dueDateText);
    if (Number.isNaN(dueDate.getTime())) return;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    dueDate.setHours(0, 0, 0, 0);

    const days = Math.ceil((dueDate - today) / (1000 * 60 * 60 * 24));
    footer.textContent = days >= 0 ? `${days} days remaining` : 'Due date passed';
  }

  document.addEventListener('DOMContentLoaded', function () {
    setupModals();
    setupAddNoteButtons();
    animateTimeline();
    setupChartBars();
    updateDueDateFooter();

    document.getElementById('btnAddMedicineRow')?.addEventListener('click', addMedicineRow);
    resetMedicineRows();

    document.getElementById('btnSavePrescription')?.addEventListener('click', (e) => {
      e.preventDefault();
      savePrescription();
    });
  });
})();

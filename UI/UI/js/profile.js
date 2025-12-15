// ================================
// Profile Page JavaScript
// ================================

document.addEventListener('DOMContentLoaded', function() {
    
    // Tab Switching
    const tabs = document.querySelectorAll('.profile-tab');
    const tabContents = document.querySelectorAll('.tab-content');

    tabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.dataset.tab;

            // Remove active from all tabs
            tabs.forEach(t => t.classList.remove('active'));
            tabContents.forEach(c => c.classList.remove('active'));

            // Add active to clicked tab
            this.classList.add('active');
            document.getElementById(`${targetTab}-tab`).classList.add('active');
        });
    });

    // Edit Button Functionality
    setupEditButton('editPersonalBtn', 'personalForm', 'personalActions', 'cancelPersonal');
    setupEditButton('editMeasurementsBtn', 'measurementsForm', 'measurementsActions', 'cancelMeasurements');
    setupEditButton('editPregnancyBtn', 'pregnancyForm', 'pregnancyActions', 'cancelPregnancy');
    setupEditButton('editHistoryBtn', 'historyForm', 'historyActions', 'cancelHistory');
    setupEditButton('editBPBtn', 'bpForm', 'bpActions', 'cancelBP');
    setupEditButton('editMedsBtn', 'medsForm', 'medsActions', 'cancelMeds');
    setupEditButton('editLifestyleBtn', 'lifestyleForm', 'lifestyleActions', 'cancelLifestyle');

    function setupEditButton(editBtnId, formId, actionsId, cancelBtnId) {
        const editBtn = document.getElementById(editBtnId);
        const form = document.getElementById(formId);
        const actions = document.getElementById(actionsId);
        const cancelBtn = document.getElementById(cancelBtnId);

        if (!editBtn || !form) return;

        editBtn.addEventListener('click', function() {
            const inputs = form.querySelectorAll('input:not([readonly]), select, textarea');
            const isCurrentlyDisabled = inputs[0]?.disabled;

            if (isCurrentlyDisabled) {
                // Enable editing
                inputs.forEach(input => input.disabled = false);
                if (actions) actions.style.display = 'flex';
                editBtn.innerHTML = '<i class="fas fa-times"></i> Cancel';
                
                // Show medication controls
                document.querySelectorAll('.medication-remove').forEach(btn => btn.style.display = 'inline-flex');
                const addMedBtn = document.getElementById('showAddMedForm');
                if (addMedBtn) addMedBtn.style.display = 'inline-flex';
            } else {
                // Disable editing (cancel)
                cancelEditing(form, actions, editBtn);
            }
        });

        // Cancel button handler
        if (cancelBtn) {
            cancelBtn.addEventListener('click', function() {
                cancelEditing(form, actions, editBtn);
            });
        }

        // Form submit handler
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            saveForm(form, actions, editBtn);
        });
    }

    function cancelEditing(form, actions, editBtn) {
        const inputs = form.querySelectorAll('input:not([readonly]), select, textarea');
        inputs.forEach(input => input.disabled = true);
        if (actions) actions.style.display = 'none';
        if (editBtn) editBtn.innerHTML = '<i class="fas fa-edit"></i> Edit';
        
        // Hide medication controls
        document.querySelectorAll('.medication-remove').forEach(btn => btn.style.display = 'none');
        const addMedBtn = document.getElementById('showAddMedForm');
        if (addMedBtn) addMedBtn.style.display = 'none';
        
        // Hide add medication form
        const addMedForm = document.getElementById('addMedicationForm');
        if (addMedForm) addMedForm.style.display = 'none';
    }

    function saveForm(form, actions, editBtn) {
        const inputs = form.querySelectorAll('input:not([readonly]), select, textarea');
        inputs.forEach(input => input.disabled = true);
        if (actions) actions.style.display = 'none';
        if (editBtn) editBtn.innerHTML = '<i class="fas fa-edit"></i> Edit';
        
        // Hide medication controls
        document.querySelectorAll('.medication-remove').forEach(btn => btn.style.display = 'none');
        const addMedBtn = document.getElementById('showAddMedForm');
        if (addMedBtn) addMedBtn.style.display = 'none';
        
        showNotification('Changes saved successfully!', 'success');
    }

    // Radio button conditional sections
    setupConditionalSection('firstPregnancy', 'no', 'previousPregnancySection');
    setupConditionalSection('bloodPressure', 'yes', 'bpDetailsSection');
    setupConditionalSection('drugs', 'yes', 'drugDetailsSection');
    setupConditionalSection('takingMeds', 'yes', 'medicationListSection');

    function setupConditionalSection(radioName, showValue, sectionId) {
        const radios = document.querySelectorAll(`input[name="${radioName}"]`);
        const section = document.getElementById(sectionId);

        if (!radios.length || !section) return;

        radios.forEach(radio => {
            radio.addEventListener('change', function() {
                if (this.value === showValue) {
                    section.style.display = 'block';
                } else {
                    section.style.display = 'none';
                }
            });
        });
    }

    // Medication Management
    const showAddMedForm = document.getElementById('showAddMedForm');
    const addMedicationForm = document.getElementById('addMedicationForm');
    const cancelAddMed = document.getElementById('cancelAddMed');
    const saveMed = document.getElementById('saveMed');

    if (showAddMedForm && addMedicationForm) {
        showAddMedForm.addEventListener('click', function() {
            addMedicationForm.style.display = 'block';
            this.style.display = 'none';
            addMedicationForm.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    if (cancelAddMed && addMedicationForm && showAddMedForm) {
        cancelAddMed.addEventListener('click', function() {
            addMedicationForm.style.display = 'none';
            showAddMedForm.style.display = 'inline-flex';
            clearMedicationForm();
        });
    }

    if (saveMed) {
        saveMed.addEventListener('click', function() {
            addNewMedication();
        });
    }

    function clearMedicationForm() {
        const fields = ['medName', 'medType', 'activeIngredient', 'medReason', 'medDosage', 'medFrequency'];
        fields.forEach(id => {
            const el = document.getElementById(id);
            if (el) el.value = '';
        });
    }

    function addNewMedication() {
        const medName = document.getElementById('medName')?.value.trim();
        const medType = document.getElementById('medType');
        const activeIngredient = document.getElementById('activeIngredient')?.value.trim();
        const medReason = document.getElementById('medReason')?.value.trim();
        const medDosage = document.getElementById('medDosage')?.value.trim();
        const medFrequency = document.getElementById('medFrequency');

        // Validation
        if (!medName || !medReason || !medDosage) {
            showNotification('Please fill in all required fields (Name, Reason, Dosage).', 'error');
            return;
        }

        const typeName = medType?.options[medType.selectedIndex]?.text || 'Medication';
        const frequencyText = medFrequency?.options[medFrequency.selectedIndex]?.text || 'Once daily';

        // Create new medication card
        const medicationList = document.querySelector('.medication-list');
        if (!medicationList) return;

        const newCard = document.createElement('div');
        newCard.className = 'medication-card';
        newCard.style.animation = 'fadeIn 0.3s ease';
        newCard.innerHTML = `
            <div class="medication-header">
                <div class="medication-icon">
                    <i class="fas fa-capsules"></i>
                </div>
                <div class="medication-title">
                    <h4>${escapeHtml(medName)}</h4>
                    <span class="medication-type">${escapeHtml(typeName)}</span>
                </div>
                <button type="button" class="btn btn-sm btn-outline danger medication-remove">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
            <div class="medication-details">
                <div class="detail-row">
                    <span class="detail-label">Active Ingredient:</span>
                    <span class="detail-value">${escapeHtml(activeIngredient) || 'Not specified'}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Reason:</span>
                    <span class="detail-value">${escapeHtml(medReason)}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Dosage:</span>
                    <span class="detail-value">${escapeHtml(medDosage)} mg per day (${escapeHtml(frequencyText)})</span>
                </div>
            </div>
        `;

        medicationList.appendChild(newCard);

        // Add remove functionality to new card
        newCard.querySelector('.medication-remove').addEventListener('click', function() {
            if (confirm('Are you sure you want to remove this medication?')) {
                newCard.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => newCard.remove(), 300);
            }
        });

        // Clear and hide form
        if (addMedicationForm) addMedicationForm.style.display = 'none';
        if (showAddMedForm) showAddMedForm.style.display = 'inline-flex';
        clearMedicationForm();

        showNotification('Medication added successfully!', 'success');
    }

    // Remove existing medication cards
    document.querySelectorAll('.medication-remove').forEach(btn => {
        btn.addEventListener('click', function() {
            const card = this.closest('.medication-card');
            if (confirm('Are you sure you want to remove this medication?')) {
                card.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => card.remove(), 300);
            }
        });
    });

    // Password visibility toggle
    document.querySelectorAll('.password-toggle').forEach(toggle => {
        toggle.addEventListener('click', function() {
            const input = this.previousElementSibling;
            const icon = this.querySelector('i');

            if (input.type === 'password') {
                input.type = 'text';
                icon.className = 'fas fa-eye-slash';
            } else {
                input.type = 'password';
                icon.className = 'fas fa-eye';
            }
        });
    });

    // Password strength checker
    const newPasswordInput = document.getElementById('newPassword');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    const strengthFill = document.querySelector('.strength-fill');
    const strengthLabel = document.querySelector('.strength-label');

    if (newPasswordInput) {
        newPasswordInput.addEventListener('input', function() {
            const password = this.value;
            const strength = checkPasswordStrength(password);
            updatePasswordUI(strength, password);
        });
    }

    function checkPasswordStrength(password) {
        let score = 0;
        const checks = {
            length: password.length >= 8,
            upper: /[A-Z]/.test(password),
            lower: /[a-z]/.test(password),
            number: /[0-9]/.test(password),
            special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
        };

        Object.values(checks).forEach(passed => {
            if (passed) score++;
        });

        return { score, checks };
    }

    function updatePasswordUI(strength, password) {
        const { score, checks } = strength;

        // Update requirement indicators
        Object.keys(checks).forEach(key => {
            const reqEl = document.getElementById(`req-${key}`);
            if (reqEl) {
                if (checks[key]) {
                    reqEl.classList.add('valid');
                    reqEl.querySelector('i').className = 'fas fa-check-circle';
                } else {
                    reqEl.classList.remove('valid');
                    reqEl.querySelector('i').className = 'fas fa-circle';
                }
            }
        });

        // Update strength bar
        if (strengthFill && strengthLabel) {
            strengthFill.className = 'strength-fill';
            
            if (password.length === 0) {
                strengthFill.style.width = '0%';
                strengthLabel.textContent = 'Password strength';
            } else if (score <= 2) {
                strengthFill.classList.add('weak');
                strengthLabel.textContent = 'Weak password';
            } else if (score === 3) {
                strengthFill.classList.add('fair');
                strengthLabel.textContent = 'Fair password';
            } else if (score === 4) {
                strengthFill.classList.add('good');
                strengthLabel.textContent = 'Good password';
            } else {
                strengthFill.classList.add('strong');
                strengthLabel.textContent = 'Strong password';
            }
        }
    }

    // Password form submission
    const passwordForm = document.getElementById('passwordForm');
    if (passwordForm) {
        passwordForm.addEventListener('submit', function(e) {
            e.preventDefault();

            const currentPassword = document.getElementById('currentPassword')?.value;
            const newPassword = document.getElementById('newPassword')?.value;
            const confirmPassword = document.getElementById('confirmPassword')?.value;

            // Validation
            if (!currentPassword || !newPassword || !confirmPassword) {
                showNotification('Please fill in all password fields.', 'error');
                return;
            }

            if (newPassword !== confirmPassword) {
                showNotification('New passwords do not match.', 'error');
                return;
            }

            const strength = checkPasswordStrength(newPassword);
            if (strength.score < 4) {
                showNotification('Please choose a stronger password.', 'error');
                return;
            }

            // Simulate password change
            showNotification('Password updated successfully!', 'success');
            passwordForm.reset();
            
            // Reset UI
            if (strengthFill) {
                strengthFill.className = 'strength-fill';
                strengthFill.style.width = '0%';
            }
            if (strengthLabel) {
                strengthLabel.textContent = 'Password strength';
            }
            
            document.querySelectorAll('.password-requirements li').forEach(li => {
                li.classList.remove('valid');
                li.querySelector('i').className = 'fas fa-circle';
            });
        });
    }

    // BMI Calculator
    const weightInput = document.getElementById('weight');
    const heightInput = document.getElementById('height');
    const bmiInput = document.getElementById('bmi');
    const bmiStatus = document.querySelector('.status-indicator');

    function calculateBMI() {
        if (!weightInput || !heightInput || !bmiInput) return;

        const weight = parseFloat(weightInput.value);
        const height = parseFloat(heightInput.value) / 100; // Convert cm to m

        if (weight > 0 && height > 0) {
            const bmi = (weight / (height * height)).toFixed(1);
            bmiInput.value = bmi;

            // Update status
            if (bmiStatus) {
                bmiStatus.className = 'status-indicator';
                if (bmi < 18.5) {
                    bmiStatus.textContent = 'Underweight';
                    bmiStatus.classList.add('underweight');
                } else if (bmi < 25) {
                    bmiStatus.textContent = 'Normal';
                    bmiStatus.classList.add('normal');
                } else if (bmi < 30) {
                    bmiStatus.textContent = 'Overweight';
                    bmiStatus.classList.add('overweight');
                } else {
                    bmiStatus.textContent = 'Obese';
                    bmiStatus.classList.add('overweight');
                }
            }
        }
    }

    if (weightInput) weightInput.addEventListener('input', calculateBMI);
    if (heightInput) heightInput.addEventListener('input', calculateBMI);

    // Pregnancy Week Calculator
    const pregnancyDateInput = document.getElementById('pregnancyDate');
    
    function calculateGestationalAge() {
        if (!pregnancyDateInput) return;

        const lmpDate = new Date(pregnancyDateInput.value);
        const today = new Date();
        
        if (isNaN(lmpDate.getTime())) return;

        const diffTime = today - lmpDate;
        const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
        const weeks = Math.floor(diffDays / 7);
        const days = diffDays % 7;

        // Calculate due date (40 weeks from LMP)
        const dueDate = new Date(lmpDate);
        dueDate.setDate(dueDate.getDate() + 280);
        const daysRemaining = Math.ceil((dueDate - today) / (1000 * 60 * 60 * 24));

        // Determine trimester
        let trimester;
        if (weeks < 13) {
            trimester = '1st Trimester';
        } else if (weeks < 27) {
            trimester = '2nd Trimester';
        } else {
            trimester = '3rd Trimester';
        }

        // Update UI elements if they exist
        const gestationalWeeksEl = document.querySelector('#pregnancy-tab .calculated-value');
        const gestationalDaysEl = document.querySelector('#pregnancy-tab .calculated-detail');
        
        if (gestationalWeeksEl) {
            gestationalWeeksEl.textContent = `${weeks} weeks`;
        }
        if (gestationalDaysEl) {
            gestationalDaysEl.textContent = `+ ${days} days`;
        }

        // Update progress bar
        const timelineProgress = document.querySelector('.timeline-progress');
        const timelineMarker = document.querySelector('.timeline-marker');
        const markerLabel = document.querySelector('.marker-label');
        
        if (timelineProgress && weeks <= 40) {
            const progressPercent = Math.min((weeks / 40) * 100, 100);
            timelineProgress.style.width = `${progressPercent}%`;
            
            if (timelineMarker) {
                timelineMarker.style.left = `${progressPercent}%`;
            }
            if (markerLabel) {
                markerLabel.textContent = `Week ${weeks}`;
            }
        }
    }

    if (pregnancyDateInput) {
        pregnancyDateInput.addEventListener('change', calculateGestationalAge);
    }

    // Avatar upload simulation
    const avatarEditBtn = document.querySelector('.avatar-edit-btn');
    if (avatarEditBtn) {
        avatarEditBtn.addEventListener('click', function() {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = 'image/*';
            
            input.addEventListener('change', function() {
                if (this.files && this.files[0]) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        const avatar = document.querySelector('.profile-avatar-large');
                        if (avatar) {
                            avatar.src = e.target.result;
                            showNotification('Profile photo updated!', 'success');
                        }
                    };
                    reader.readAsDataURL(this.files[0]);
                }
            });
            
            input.click();
        });
    }

    // Edit Profile Button (main)
    const editProfileBtn = document.getElementById('editProfileBtn');
    if (editProfileBtn) {
        editProfileBtn.addEventListener('click', function() {
            // Scroll to personal info tab and enable editing
            const personalTab = document.querySelector('[data-tab="personal"]');
            if (personalTab) {
                personalTab.click();
                setTimeout(() => {
                    const editPersonalBtn = document.getElementById('editPersonalBtn');
                    if (editPersonalBtn) {
                        editPersonalBtn.click();
                    }
                }, 100);
            }
        });
    }

    // Lifestyle status update based on selections
    function updateLifestyleStatus(radioName, statusElement) {
        const radios = document.querySelectorAll(`input[name="${radioName}"]`);
        
        radios.forEach(radio => {
            radio.addEventListener('change', function() {
                if (!statusElement) return;
                
                statusElement.className = 'lifestyle-status';
                
                if (this.value === 'no') {
                    statusElement.classList.add('safe');
                    statusElement.innerHTML = '<i class="fas fa-check-circle"></i><span>Safe</span>';
                } else if (this.value === 'quit') {
                    statusElement.classList.add('warning');
                    statusElement.innerHTML = '<i class="fas fa-exclamation-circle"></i><span>Monitor</span>';
                } else {
                    statusElement.classList.add('danger');
                    statusElement.innerHTML = '<i class="fas fa-times-circle"></i><span>Risk</span>';
                }
            });
        });
    }

    // Apply to lifestyle items
    const lifestyleItems = document.querySelectorAll('.lifestyle-item');
    lifestyleItems.forEach(item => {
        const radios = item.querySelectorAll('input[type="radio"]');
        const statusEl = item.querySelector('.lifestyle-status');
        
        if (radios.length && statusEl) {
            const radioName = radios[0].name;
            updateLifestyleStatus(radioName, statusEl);
        }
    });

    // Utility Functions
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showNotification(message, type) {
        // Remove existing notification
        const existing = document.querySelector('.profile-notification');
        if (existing) existing.remove();

        const notification = document.createElement('div');
        notification.className = `profile-notification ${type}`;
        notification.innerHTML = `
            <i class="fas ${type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle'}"></i>
            <span>${message}</span>
            <button class="notification-close"><i class="fas fa-times"></i></button>
        `;

        document.body.appendChild(notification);

        // Close button
        notification.querySelector('.notification-close').addEventListener('click', () => {
            notification.remove();
        });

        // Auto-remove after 4 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.style.animation = 'slideOut 0.3s ease forwards';
                setTimeout(() => notification.remove(), 300);
            }
        }, 4000);
    }

    // Add notification styles
    const notificationStyles = document.createElement('style');
    notificationStyles.textContent = `
        .profile-notification {
            position: fixed;
            top: 24px;
            right: 24px;
            padding: 16px 20px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 500;
            z-index: 9999;
            animation: slideIn 0.3s ease;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            max-width: 400px;
        }
        .profile-notification.success {
            background: #E8F5E9;
            color: #2E7D32;
            border: 1px solid #A5D6A7;
        }
        .profile-notification.error {
            background: #FFEBEE;
            color: #C62828;
            border: 1px solid #EF9A9A;
        }
        .profile-notification span {
            flex: 1;
        }
        .notification-close {
            background: none;
            border: none;
            cursor: pointer;
            color: inherit;
            opacity: 0.7;
            padding: 4px;
        }
        .notification-close:hover {
            opacity: 1;
        }
        @keyframes slideIn {
            from {
                opacity: 0;
                transform: translateX(100px);
            }
            to {
                opacity: 1;
                transform: translateX(0);
            }
        }
        @keyframes slideOut {
            from {
                opacity: 1;
                transform: translateX(0);
            }
            to {
                opacity: 0;
                transform: translateX(100px);
            }
        }
        @keyframes fadeOut {
            from { opacity: 1; }
            to { opacity: 0; }
        }
    `;
    document.head.appendChild(notificationStyles);

    console.log('Profile page initialized successfully! 👤');
});
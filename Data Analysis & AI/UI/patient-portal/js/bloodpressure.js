// ================================
// Blood Pressure Tracker JavaScript
// ================================

document.addEventListener('DOMContentLoaded', function() {
    
    const systolicInput = document.getElementById('systolic');
    const diastolicInput = document.getElementById('diastolic');
    const pulseInput = document.getElementById('pulse');
    const saveBPBtn = document.getElementById('saveBP');
    const bpResult = document.getElementById('bpResult');
    const bpValue = document.getElementById('bpValue');
    const bpStatus = document.getElementById('bpStatus');
    const bpMessage = document.getElementById('bpMessage');

    // Input validation - only allow numbers
    [systolicInput, diastolicInput, pulseInput].forEach(input => {
        if (input) {
            input.addEventListener('input', function() {
                this.value = this.value.replace(/[^0-9]/g, '');
            });
        }
    });

    // Auto-focus next input after entry
    if (systolicInput && diastolicInput) {
        systolicInput.addEventListener('input', function() {
            if (this.value.length >= 3) {
                diastolicInput.focus();
            }
        });

        diastolicInput.addEventListener('input', function() {
            if (this.value.length >= 2 && pulseInput) {
                pulseInput.focus();
            }
        });
    }

    // Save blood pressure reading
    if (saveBPBtn) {
        saveBPBtn.addEventListener('click', function() {
            const systolic = parseInt(systolicInput.value);
            const diastolic = parseInt(diastolicInput.value);
            const pulse = pulseInput ? parseInt(pulseInput.value) : null;

            // Validation
            if (!systolic || !diastolic) {
                showNotification('Please enter both systolic and diastolic values.', 'error');
                return;
            }

            if (systolic < 70 || systolic > 200) {
                showNotification('Systolic value should be between 70-200 mmHg.', 'error');
                return;
            }

            if (diastolic < 40 || diastolic > 130) {
                showNotification('Diastolic value should be between 40-130 mmHg.', 'error');
                return;
            }

            // Analyze blood pressure
            const analysis = analyzeBP(systolic, diastolic);

            // Display result
            bpValue.textContent = `${systolic}/${diastolic}`;
            bpStatus.className = 'bp-status ' + analysis.status;
            bpStatus.innerHTML = `<i class="fas ${analysis.icon}"></i><span>${analysis.label}</span>`;
            bpMessage.textContent = analysis.message;
            bpResult.style.display = 'block';

            // Update the stat card
            updateStatCard(systolic, diastolic, analysis);

            // Add to history (visual only for demo)
            addToHistory(systolic, diastolic, analysis);

            // Show success notification
            showNotification('Blood pressure reading saved successfully!', 'success');

            // Scroll to result
            bpResult.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    function analyzeBP(systolic, diastolic) {
        let status, label, icon, message;

        if (systolic < 90 || diastolic < 60) {
            status = 'low';
            label = 'Low';
            icon = 'fa-arrow-down';
            message = 'Your blood pressure is lower than normal. If you feel dizzy or faint, please consult your doctor.';
        } else if (systolic <= 120 && diastolic <= 80) {
            status = 'normal';
            label = 'Normal';
            icon = 'fa-check-circle';
            message = 'Excellent! Your blood pressure is within the normal range. Keep up the healthy lifestyle!';
        } else if (systolic <= 129 && diastolic < 80) {
            status = 'elevated';
            label = 'Elevated';
            icon = 'fa-exclamation-circle';
            message = 'Your blood pressure is slightly elevated. Consider reducing salt intake and increasing physical activity.';
        } else if (systolic <= 139 || diastolic <= 89) {
            status = 'elevated';
            label = 'High (Stage 1)';
            icon = 'fa-exclamation-triangle';
            message = 'Your blood pressure is high. Please monitor regularly and consult your healthcare provider.';
        } else {
            status = 'high';
            label = 'High (Stage 2)';
            icon = 'fa-exclamation-triangle';
            message = 'Your blood pressure is significantly elevated. Please contact your doctor immediately for guidance.';
        }

        return { status, label, icon, message };
    }

    function updateStatCard(systolic, diastolic, analysis) {
        const bpStatCard = document.querySelector('.stat-card:nth-child(2) .stat-value');
        const bpStatChange = document.querySelector('.stat-card:nth-child(2) .stat-change');
        
        if (bpStatCard) {
            bpStatCard.textContent = `${systolic}/${diastolic}`;
        }
        
        if (bpStatChange) {
            bpStatChange.textContent = analysis.label;
            bpStatChange.className = 'stat-change';
            if (analysis.status === 'normal') {
                bpStatChange.classList.add('positive');
            } else if (analysis.status === 'high') {
                bpStatChange.classList.add('negative');
            }
        }
    }

    function addToHistory(systolic, diastolic, analysis) {
        const historyList = document.querySelector('.bp-history-list');
        if (!historyList) return;

        const now = new Date();
        const day = now.getDate();
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        const month = months[now.getMonth()];
        const time = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        const newItem = document.createElement('div');
        newItem.className = 'bp-history-item';
        newItem.innerHTML = `
            <div class="bp-history-date">
                <span class="day">${day}</span>
                <span class="month">${month}</span>
            </div>
            <div class="bp-history-reading">
                <span class="reading">${systolic}/${diastolic}</span>
                <span class="time">${time}</span>
            </div>
            <div class="bp-history-status ${analysis.status}">
                <i class="fas ${analysis.icon}"></i>
                ${analysis.label}
            </div>
        `;

        // Add animation
        newItem.style.animation = 'fadeIn 0.3s ease';
        
        // Insert at the beginning
        historyList.insertBefore(newItem, historyList.firstChild);

        // Remove last item if more than 5
        const items = historyList.querySelectorAll('.bp-history-item');
        if (items.length > 5) {
            items[items.length - 1].remove();
        }
    }

    function showNotification(message, type) {
        // Remove existing notification
        const existing = document.querySelector('.bp-notification');
        if (existing) {
            existing.remove();
        }

        // Create notification
        const notification = document.createElement('div');
        notification.className = `bp-notification ${type}`;
        notification.innerHTML = `
            <i class="fas ${type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle'}"></i>
            <span>${message}</span>
        `;

        // Add styles
        const styles = `
            .bp-notification {
                position: fixed;
                top: 20px;
                right: 20px;
                padding: 16px 24px;
                border-radius: 12px;
                display: flex;
                align-items: center;
                gap: 12px;
                font-weight: 500;
                z-index: 9999;
                animation: slideIn 0.3s ease;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            }
            .bp-notification.success {
                background: #E8F5E9;
                color: #2E7D32;
                border: 1px solid #A5D6A7;
            }
            .bp-notification.error {
                background: #FFEBEE;
                color: #C62828;
                border: 1px solid #EF9A9A;
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
        `;

        // Add styles if not already added
        if (!document.getElementById('bp-notification-styles')) {
            const styleSheet = document.createElement('style');
            styleSheet.id = 'bp-notification-styles';
            styleSheet.textContent = styles;
            document.head.appendChild(styleSheet);
        }

        document.body.appendChild(notification);

        // Auto-remove after 4 seconds
        setTimeout(() => {
            notification.style.animation = 'slideIn 0.3s ease reverse';
            setTimeout(() => notification.remove(), 300);
        }, 4000);
    }

    // Input highlighting based on value
    [systolicInput, diastolicInput].forEach(input => {
        if (input) {
            input.addEventListener('blur', function() {
                const value = parseInt(this.value);
                const wrapper = this.closest('.bp-input-wrapper');
                
                wrapper.classList.remove('normal', 'elevated', 'high', 'low');
                
                if (!value) return;

                if (this === systolicInput) {
                    if (value < 90) wrapper.classList.add('low');
                    else if (value <= 120) wrapper.classList.add('normal');
                    else if (value <= 139) wrapper.classList.add('elevated');
                    else wrapper.classList.add('high');
                } else {
                    if (value < 60) wrapper.classList.add('low');
                    else if (value <= 80) wrapper.classList.add('normal');
                    else if (value <= 89) wrapper.classList.add('elevated');
                    else wrapper.classList.add('high');
                }
            });
        }
    });
});
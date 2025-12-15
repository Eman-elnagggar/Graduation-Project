// ================================
// Appointments Page JavaScript
// ================================

document.addEventListener('DOMContentLoaded', function() {
    
    let currentStep = 1;
    const totalSteps = 4;
    
    // Step elements
    const step1 = document.getElementById('step1');
    const step2 = document.getElementById('step2');
    const step3 = document.getElementById('step3');
    const step4 = document.getElementById('step4');
    
    // Navigation buttons
    const prevBtn = document.getElementById('prevStep');
    const nextBtn = document.getElementById('nextStep');
    const confirmBtn = document.getElementById('confirmBooking');
    
    // Doctor selection
    const doctorCards = document.querySelectorAll('.doctor-select-card');
    let selectedDoctor = null;
    
    doctorCards.forEach(card => {
        card.addEventListener('click', function() {
            // Remove selection from all cards
            doctorCards.forEach(c => c.classList.remove('selected'));
            // Add selection to clicked card
            this.classList.add('selected');
            selectedDoctor = this.dataset.doctor;
        });
    });

    // Calendar day selection
    const calendarDays = document.querySelectorAll('.calendar-days .day:not(.disabled)');
    let selectedDate = null;
    
    calendarDays.forEach(day => {
        day.addEventListener('click', function() {
            calendarDays.forEach(d => d.classList.remove('selected'));
            this.classList.add('selected');
            selectedDate = this.textContent;
        });
    });

    // Time slot selection
    const timeSlots = document.querySelectorAll('.time-slot:not(.booked)');
    let selectedTime = null;
    
    timeSlots.forEach(slot => {
        slot.addEventListener('click', function() {
            timeSlots.forEach(s => s.classList.remove('selected'));
            this.classList.add('selected');
            selectedTime = this.textContent;
        });
    });

    // Appointment type selection
    const typeOptions = document.querySelectorAll('.type-option');
    let selectedType = 'in-person';
    
    typeOptions.forEach(option => {
        option.addEventListener('click', function() {
            typeOptions.forEach(o => o.classList.remove('selected'));
            this.classList.add('selected');
            selectedType = this.querySelector('span').textContent;
        });
    });

    // Navigation functions
    function showStep(stepNumber) {
        // Hide all steps
        step1.style.display = 'none';
        step2.style.display = 'none';
        step3.style.display = 'none';
        step4.style.display = 'none';
        
        // Show current step
        switch(stepNumber) {
            case 1:
                step1.style.display = 'block';
                break;
            case 2:
                step2.style.display = 'block';
                break;
            case 3:
                step3.style.display = 'block';
                break;
            case 4:
                step4.style.display = 'block';
                break;
        }
        
        // Update navigation buttons
        updateNavigation();
    }

    function updateNavigation() {
        // Previous button
        if (currentStep === 1) {
            prevBtn.style.display = 'none';
        } else {
            prevBtn.style.display = 'inline-flex';
        }
        
        // Next and Confirm buttons
        if (currentStep === totalSteps) {
            nextBtn.style.display = 'none';
            confirmBtn.style.display = 'inline-flex';
        } else {
            nextBtn.style.display = 'inline-flex';
            confirmBtn.style.display = 'none';
        }
    }

    function validateStep(stepNumber) {
        switch(stepNumber) {
            case 1:
                if (!selectedDoctor) {
                    alert('Please select a doctor to continue.');
                    return false;
                }
                return true;
            case 2:
                if (!selectedDate) {
                    alert('Please select a date to continue.');
                    return false;
                }
                return true;
            case 3:
                if (!selectedTime) {
                    alert('Please select a time slot to continue.');
                    return false;
                }
                return true;
            default:
                return true;
        }
    }

    // Next button click
    if (nextBtn) {
        nextBtn.addEventListener('click', function() {
            if (validateStep(currentStep)) {
                currentStep++;
                showStep(currentStep);
                
                // Scroll to top of booking section
                document.querySelector('.book-appointment').scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    }

    // Previous button click
    if (prevBtn) {
        prevBtn.addEventListener('click', function() {
            currentStep--;
            showStep(currentStep);
            
            // Scroll to top of booking section
            document.querySelector('.book-appointment').scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        });
    }

    // Confirm booking button
    if (confirmBtn) {
        confirmBtn.addEventListener('click', function() {
            // Get visit reason
            const visitReason = document.getElementById('visitReason').value;
            
            if (!visitReason) {
                alert('Please select a reason for your visit.');
                return;
            }
            
            // Show success message
            showBookingSuccess();
        });
    }

    function showBookingSuccess() {
        // Create success modal
        const modal = document.createElement('div');
        modal.className = 'booking-success-modal';
        modal.innerHTML = `
            <div class="modal-overlay"></div>
            <div class="modal-content">
                <div class="success-icon">
                    <i class="fas fa-check-circle"></i>
                </div>
                <h2>Booking Confirmed! 🎉</h2>
                <p>Your appointment has been successfully scheduled.</p>
                <div class="booking-details">
                    <div class="detail-row">
                        <i class="fas fa-user-md"></i>
                        <span>Dr. Ahmed Hassan</span>
                    </div>
                    <div class="detail-row">
                        <i class="fas fa-calendar-alt"></i>
                        <span>Monday, March 10, 2025</span>
                    </div>
                    <div class="detail-row">
                        <i class="fas fa-clock"></i>
                        <span>10:30 AM</span>
                    </div>
                    <div class="detail-row">
                        <i class="fas fa-map-marker-alt"></i>
                        <span>City Medical Center, Room 205</span>
                    </div>
                </div>
                <p class="confirmation-note">
                    <i class="fas fa-envelope"></i>
                    A confirmation email has been sent to your email address.
                </p>
                <div class="modal-actions">
                    <button class="btn btn-secondary" onclick="this.closest('.booking-success-modal').remove()">
                        Close
                    </button>
                    <button class="btn btn-primary" onclick="window.location.href='index.html'">
                        <i class="fas fa-home"></i> Go to Dashboard
                    </button>
                </div>
            </div>
        `;
        
        // Add modal styles
        const style = document.createElement('style');
        style.textContent = `
            .booking-success-modal {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                z-index: 9999;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .modal-overlay {
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.5);
            }
            .modal-content {
                position: relative;
                background: white;
                border-radius: 16px;
                padding: 40px;
                max-width: 480px;
                width: 90%;
                text-align: center;
                animation: modalSlideIn 0.3s ease;
            }
            @keyframes modalSlideIn {
                from {
                    opacity: 0;
                    transform: translateY(-20px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }
            .success-icon {
                width: 80px;
                height: 80px;
                background: #E8F5E9;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                margin: 0 auto 20px;
            }
            .success-icon i {
                font-size: 40px;
                color: #4CAF50;
            }
            .modal-content h2 {
                margin-bottom: 10px;
                color: #1E293B;
            }
            .modal-content > p {
                color: #64748B;
                margin-bottom: 24px;
            }
            .booking-details {
                background: #F8FAFC;
                border-radius: 12px;
                padding: 20px;
                margin-bottom: 20px;
            }
            .detail-row {
                display: flex;
                align-items: center;
                gap: 12px;
                padding: 10px 0;
                border-bottom: 1px solid #E2E8F0;
            }
            .detail-row:last-child {
                border-bottom: none;
            }
            .detail-row i {
                width: 20px;
                color: #1BAEBE;
            }
            .confirmation-note {
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 8px;
                font-size: 0.9rem;
                color: #64748B;
                margin-bottom: 24px;
            }
            .modal-actions {
                display: flex;
                gap: 12px;
                justify-content: center;
            }
        `;
        document.head.appendChild(style);
        document.body.appendChild(modal);
    }

    // Change doctor button
    const changeDoctor = document.getElementById('changeDoctor');
    if (changeDoctor) {
        changeDoctor.addEventListener('click', function() {
            currentStep = 1;
            showStep(1);
        });
    }

    // Change date button
    const changeDate = document.getElementById('changeDate');
    if (changeDate) {
        changeDate.addEventListener('click', function() {
            currentStep = 2;
            showStep(2);
        });
    }

    // Calendar navigation
    const prevMonth = document.getElementById('prevMonth');
    const nextMonth = document.getElementById('nextMonth');
    const currentMonthEl = document.getElementById('currentMonth');
    
    let currentMonthIndex = 2; // March (0-indexed)
    let currentYear = 2025;
    const months = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];

    if (prevMonth) {
        prevMonth.addEventListener('click', function() {
            currentMonthIndex--;
            if (currentMonthIndex < 0) {
                currentMonthIndex = 11;
                currentYear--;
            }
            currentMonthEl.textContent = `${months[currentMonthIndex]} ${currentYear}`;
        });
    }

    if (nextMonth) {
        nextMonth.addEventListener('click', function() {
            currentMonthIndex++;
            if (currentMonthIndex > 11) {
                currentMonthIndex = 0;
                currentYear++;
            }
            currentMonthEl.textContent = `${months[currentMonthIndex]} ${currentYear}`;
        });
    }

    // Initialize
    updateNavigation();
});
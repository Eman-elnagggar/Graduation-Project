// ================================
// Appointments Page JavaScript
// ================================

document.addEventListener("DOMContentLoaded", function () {
  // Primary Doctor Management
  let primaryDoctor = localStorage.getItem("primaryDoctor")
    ? JSON.parse(localStorage.getItem("primaryDoctor"))
    : null;
  let visitedDoctors = localStorage.getItem("visitedDoctors")
    ? JSON.parse(localStorage.getItem("visitedDoctors"))
    : [];

  // Initialize primary doctor display
  initializePrimaryDoctor();
  initializeMyDoctors();

  // ================================
  // Toggle Past Appointments
  // ================================
  const showPastBtn = document.getElementById("showPastBtn");
  const togglePastBtn = document.getElementById("togglePastBtn");
  const pastAppointmentsSection = document.getElementById(
    "pastAppointmentsSection"
  );

  if (showPastBtn) {
    showPastBtn.addEventListener("click", function () {
      pastAppointmentsSection.style.display = "block";
      pastAppointmentsSection.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    });
  }

  if (togglePastBtn) {
    togglePastBtn.addEventListener("click", function () {
      pastAppointmentsSection.style.display = "none";
      window.scrollTo({ top: 0, behavior: "smooth" });
    });
  }

  // Note: Booking functionality moved to book-appointment.html
  // The following variables will be undefined on this page, which is fine
  let currentStep = 1;
  const totalSteps = 4;

  // Step elements (may not exist on simplified page)
  const step1 = document.getElementById("step1");
  const step2 = document.getElementById("step2");
  const step3 = document.getElementById("step3");
  const step4 = document.getElementById("step4");

  // Navigation buttons (may not exist on simplified page)
  const prevBtn = document.getElementById("prevStep");
  const nextBtn = document.getElementById("nextStep");
  const confirmBtn = document.getElementById("confirmBooking");

  // Doctor selection (may not exist on simplified page)
  const doctorCards = document.querySelectorAll(".doctor-select-card");
  let selectedDoctor = null;

  // Doctor search and filters (may not exist on simplified page)
  const doctorSearch = document.getElementById("doctorSearch");
  const specialtyFilter = document.getElementById("specialtyFilter");
  const locationFilter = document.getElementById("locationFilter");
  const availabilityFilter = document.getElementById("availabilityFilter");
  const clearFiltersBtn = document.getElementById("clearFilters");

  // Search and filter doctors
  function filterDoctors() {
    if (!doctorSearch) return; // Exit if on simplified appointments page

    const searchTerm = doctorSearch.value.toLowerCase();
    const specialty = specialtyFilter.value;
    const location = locationFilter.value;
    const availability = availabilityFilter.value;

    doctorCards.forEach((card) => {
      const doctorName = card.dataset.name.toLowerCase();
      const doctorSpecialty = card.dataset.specialty;
      const doctorLocation = card.dataset.location;
      const doctorAvailability = card.dataset.availability;

      const matchesSearch =
        doctorName.includes(searchTerm) || searchTerm === "";
      const matchesSpecialty =
        specialty === "" || doctorSpecialty === specialty;
      const matchesLocation = location === "" || doctorLocation === location;
      const matchesAvailability =
        availability === "" || doctorAvailability === availability;

      if (
        matchesSearch &&
        matchesSpecialty &&
        matchesLocation &&
        matchesAvailability
      ) {
        card.style.display = "flex";
      } else {
        card.style.display = "none";
      }
    });
  }

  if (doctorSearch) {
    doctorSearch.addEventListener("input", filterDoctors);
  }

  if (specialtyFilter) {
    specialtyFilter.addEventListener("change", filterDoctors);
  }

  if (locationFilter) {
    locationFilter.addEventListener("change", filterDoctors);
  }

  if (availabilityFilter) {
    availabilityFilter.addEventListener("change", filterDoctors);
  }

  if (clearFiltersBtn) {
    clearFiltersBtn.addEventListener("click", function () {
      doctorSearch.value = "";
      specialtyFilter.value = "";
      locationFilter.value = "";
      availabilityFilter.value = "";
      filterDoctors();
    });
  }

  // Primary Doctor Functions
  function initializePrimaryDoctor() {
    const primaryDoctorInfo = document.getElementById("primaryDoctorInfo");
    const noPrimaryDoctor = document.getElementById("noPrimaryDoctor");

    if (primaryDoctor) {
      // Display primary doctor
      document.getElementById("primaryDoctorImage").src = primaryDoctor.image;
      document.getElementById("primaryDoctorName").textContent =
        primaryDoctor.name;
      document.getElementById(
        "primaryDoctorSpecialty"
      ).innerHTML = `<i class="fas fa-stethoscope"></i> ${primaryDoctor.specialty}`;
      document.getElementById(
        "primaryDoctorLocation"
      ).innerHTML = `<i class="fas fa-map-marker-alt"></i> ${primaryDoctor.location}`;
      document.getElementById("appointmentCount").textContent =
        primaryDoctor.appointmentCount || 0;

      primaryDoctorInfo.style.display = "flex";
      noPrimaryDoctor.style.display = "none";
    } else {
      primaryDoctorInfo.style.display = "none";
      noPrimaryDoctor.style.display = "flex";
    }
  }

  function initializeMyDoctors() {
    const myDoctorsSection = document.getElementById("myDoctorsSection");
    const myDoctorsGrid = document.getElementById("myDoctorsGrid");
    const myDoctorsCount = document.getElementById("myDoctorsCount");

    if (visitedDoctors.length > 0) {
      myDoctorsSection.style.display = "block";
      myDoctorsCount.textContent = `${visitedDoctors.length} doctor${
        visitedDoctors.length > 1 ? "s" : ""
      }`;

      myDoctorsGrid.innerHTML = visitedDoctors
        .map(
          (doctor) => `
                <div class="my-doctor-card" data-doctor-id="${doctor.id}">
                    <div class="doctor-avatar-small">
                        <img src="${doctor.image}" alt="${doctor.name}">
                        <span class="online-status"></span>
                    </div>
                    <div class="doctor-info-small">
                        <h5>${doctor.name}</h5>
                        <p>${doctor.specialty}</p>
                        <span class="visit-count"><i class="fas fa-calendar-check"></i> ${
                          doctor.visitCount || 1
                        } visit${doctor.visitCount > 1 ? "s" : ""}</span>
                    </div>
                    <div class="doctor-quick-actions">
                        <button class="btn btn-sm btn-success chat-doctor-btn" data-doctor-id="${
                          doctor.id
                        }">
                            <i class="fas fa-comment-medical"></i> Chat
                        </button>
                        <button class="btn btn-sm btn-primary book-doctor-btn" data-doctor-id="${
                          doctor.id
                        }">
                            <i class="fas fa-calendar-plus"></i> Book
                        </button>
                        ${
                          primaryDoctor && primaryDoctor.id !== doctor.id
                            ? `
                            <button class="btn btn-sm btn-outline set-primary-btn" data-doctor-id="${doctor.id}">
                                <i class="fas fa-star"></i> Set Primary
                            </button>
                        `
                            : ""
                        }
                    </div>
                </div>
            `
        )
        .join("");

      // Add event listeners for chat and book buttons
      document.querySelectorAll(".chat-doctor-btn").forEach((btn) => {
        btn.addEventListener("click", function () {
          const doctorId = this.dataset.doctorId;
          const doctor = visitedDoctors.find((d) => d.id === doctorId);
          openDoctorChat(doctor);
        });
      });

      document.querySelectorAll(".book-doctor-btn").forEach((btn) => {
        btn.addEventListener("click", function () {
          const doctorId = this.dataset.doctorId;
          const doctor = visitedDoctors.find((d) => d.id === doctorId);
          quickBookDoctor(doctor);
        });
      });

      document.querySelectorAll(".set-primary-btn").forEach((btn) => {
        btn.addEventListener("click", function () {
          const doctorId = this.dataset.doctorId;
          const doctor = visitedDoctors.find((d) => d.id === doctorId);
          setPrimaryDoctor(doctor);
        });
      });
    } else {
      myDoctorsSection.style.display = "none";
    }
  }

  function openDoctorChat(doctor) {
    // Redirect to doctor-chat page with doctor parameter
    window.location.href = `doctor-chat.html?doctor=${
      doctor.id
    }&name=${encodeURIComponent(doctor.name)}`;
  }

  function quickBookDoctor(doctor) {
    // Auto-select doctor and move to step 2
    selectedDoctor = doctor.id;
    doctorCards.forEach((card) => {
      if (card.dataset.doctor === doctor.id) {
        card.classList.add("selected");
      } else {
        card.classList.remove("selected");
      }
    });
    currentStep = 2;
    showStep(currentStep);
    document
      .querySelector(".book-appointment")
      .scrollIntoView({ behavior: "smooth" });
  }

  function setPrimaryDoctor(doctor) {
    primaryDoctor = doctor;
    localStorage.setItem("primaryDoctor", JSON.stringify(primaryDoctor));
    initializePrimaryDoctor();
    initializeMyDoctors();

    // Show success message
    showNotification("Primary doctor updated successfully!", "success");
  }

  function addToVisitedDoctors(doctor) {
    // Check if doctor already exists
    const existingDoctor = visitedDoctors.find((d) => d.id === doctor.id);

    if (existingDoctor) {
      // Increment visit count
      existingDoctor.visitCount = (existingDoctor.visitCount || 1) + 1;
    } else {
      // Add new doctor
      doctor.visitCount = 1;
      visitedDoctors.push(doctor);

      // If this is the first doctor, set as primary
      if (!primaryDoctor) {
        primaryDoctor = doctor;
        localStorage.setItem("primaryDoctor", JSON.stringify(primaryDoctor));
      }
    }

    localStorage.setItem("visitedDoctors", JSON.stringify(visitedDoctors));
    initializeMyDoctors();
    initializePrimaryDoctor();
  }

  // Primary doctor action buttons
  const bookWithPrimary = document.getElementById("bookWithPrimary");
  const chatWithPrimary = document.getElementById("chatWithPrimary");
  const changePrimaryDoctor = document.getElementById("changePrimaryDoctor");
  const selectPrimaryBtn = document.getElementById("selectPrimaryBtn");

  if (bookWithPrimary) {
    bookWithPrimary.addEventListener("click", function () {
      if (primaryDoctor) {
        quickBookDoctor(primaryDoctor);
      }
    });
  }

  if (chatWithPrimary) {
    chatWithPrimary.addEventListener("click", function () {
      if (primaryDoctor) {
        openDoctorChat(primaryDoctor);
      }
    });
  }

  if (changePrimaryDoctor) {
    changePrimaryDoctor.addEventListener("click", function () {
      // Show doctor selection
      document
        .querySelector(".book-appointment")
        .scrollIntoView({ behavior: "smooth" });
    });
  }

  if (selectPrimaryBtn) {
    selectPrimaryBtn.addEventListener("click", function () {
      // Scroll to doctor selection
      document
        .querySelector(".book-appointment")
        .scrollIntoView({ behavior: "smooth" });
    });
  }

  function showNotification(message, type = "info") {
    const notification = document.createElement("div");
    notification.className = `notification-toast ${type}`;
    notification.innerHTML = `
            <i class="fas fa-${
              type === "success" ? "check-circle" : "info-circle"
            }"></i>
            <span>${message}</span>
        `;
    notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === "success" ? "#4CAF50" : "#2196F3"};
            color: white;
            padding: 16px 24px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            display: flex;
            align-items: center;
            gap: 12px;
            animation: slideIn 0.3s ease;
        `;

    document.body.appendChild(notification);

    setTimeout(() => {
      notification.style.animation = "slideOut 0.3s ease";
      setTimeout(() => notification.remove(), 300);
    }, 3000);
  }

  doctorCards.forEach((card) => {
    card.addEventListener("click", function () {
      // Remove selection from all cards
      doctorCards.forEach((c) => c.classList.remove("selected"));
      // Add selection to clicked card
      this.classList.add("selected");
      selectedDoctor = this.dataset.doctor;
    });
  });

  // Calendar day selection
  const calendarDays = document.querySelectorAll(
    ".calendar-days .day:not(.disabled)"
  );
  let selectedDate = null;

  calendarDays.forEach((day) => {
    day.addEventListener("click", function () {
      calendarDays.forEach((d) => d.classList.remove("selected"));
      this.classList.add("selected");
      selectedDate = this.textContent;
    });
  });

  // Time slot selection
  const timeSlots = document.querySelectorAll(".time-slot:not(.booked)");
  let selectedTime = null;

  timeSlots.forEach((slot) => {
    slot.addEventListener("click", function () {
      timeSlots.forEach((s) => s.classList.remove("selected"));
      this.classList.add("selected");
      selectedTime = this.textContent;
    });
  });

  // Appointment type selection
  const typeOptions = document.querySelectorAll(".type-option");
  let selectedType = "in-person";

  typeOptions.forEach((option) => {
    option.addEventListener("click", function () {
      typeOptions.forEach((o) => o.classList.remove("selected"));
      this.classList.add("selected");
      selectedType = this.querySelector("span").textContent;
    });
  });

  // Navigation functions
  function showStep(stepNumber) {
    // Hide all steps
    step1.style.display = "none";
    step2.style.display = "none";
    step3.style.display = "none";
    step4.style.display = "none";

    // Show current step
    switch (stepNumber) {
      case 1:
        step1.style.display = "block";
        break;
      case 2:
        step2.style.display = "block";
        break;
      case 3:
        step3.style.display = "block";
        break;
      case 4:
        step4.style.display = "block";
        break;
    }

    // Update navigation buttons
    updateNavigation();
  }

  function updateNavigation() {
    // Previous button
    if (currentStep === 1) {
      prevBtn.style.display = "none";
    } else {
      prevBtn.style.display = "inline-flex";
    }

    // Next and Confirm buttons
    if (currentStep === totalSteps) {
      nextBtn.style.display = "none";
      confirmBtn.style.display = "inline-flex";
    } else {
      nextBtn.style.display = "inline-flex";
      confirmBtn.style.display = "none";
    }
  }

  function validateStep(stepNumber) {
    switch (stepNumber) {
      case 1:
        if (!selectedDoctor) {
          alert("Please select a doctor to continue.");
          return false;
        }
        return true;
      case 2:
        if (!selectedDate) {
          alert("Please select a date to continue.");
          return false;
        }
        return true;
      case 3:
        if (!selectedTime) {
          alert("Please select a time slot to continue.");
          return false;
        }
        return true;
      default:
        return true;
    }
  }

  // Next button click
  if (nextBtn) {
    nextBtn.addEventListener("click", function () {
      if (validateStep(currentStep)) {
        currentStep++;
        showStep(currentStep);

        // Scroll to top of booking section
        document.querySelector(".book-appointment").scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
      }
    });
  }

  // Previous button click
  if (prevBtn) {
    prevBtn.addEventListener("click", function () {
      currentStep--;
      showStep(currentStep);

      // Scroll to top of booking section
      document.querySelector(".book-appointment").scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    });
  }

  // Confirm booking button
  if (confirmBtn) {
    confirmBtn.addEventListener("click", function () {
      // Get visit reason
      const visitReason = document.getElementById("visitReason").value;

      if (!visitReason) {
        alert("Please select a reason for your visit.");
        return;
      }

      // Add doctor to visited doctors list
      const selectedDoctorCard = document.querySelector(
        ".doctor-select-card.selected"
      );
      if (selectedDoctorCard) {
        const doctorData = {
          id: selectedDoctorCard.dataset.doctor,
          name: selectedDoctorCard.querySelector(".doctor-details h4")
            .textContent,
          specialty: selectedDoctorCard
            .querySelector(".specialty")
            .textContent.replace(/.*?\s/, ""),
          location: selectedDoctorCard
            .querySelector(".location")
            .textContent.replace(/.*?\s/, ""),
          image: selectedDoctorCard.querySelector(".doctor-avatar img").src,
          appointmentCount: 1,
        };

        addToVisitedDoctors(doctorData);
      }

      // Show success message
      showBookingSuccess();
    });
  }

  function showBookingSuccess() {
    // Create success modal
    const modal = document.createElement("div");
    modal.className = "booking-success-modal";
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
    const style = document.createElement("style");
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
  const changeDoctor = document.getElementById("changeDoctor");
  if (changeDoctor) {
    changeDoctor.addEventListener("click", function () {
      currentStep = 1;
      showStep(1);
    });
  }

  // Change date button
  const changeDate = document.getElementById("changeDate");
  if (changeDate) {
    changeDate.addEventListener("click", function () {
      currentStep = 2;
      showStep(2);
    });
  }

  // Calendar navigation
  const prevMonth = document.getElementById("prevMonth");
  const nextMonth = document.getElementById("nextMonth");
  const currentMonthEl = document.getElementById("currentMonth");

  let currentMonthIndex = 2; // March (0-indexed)
  let currentYear = 2025;
  const months = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];

  if (prevMonth) {
    prevMonth.addEventListener("click", function () {
      currentMonthIndex--;
      if (currentMonthIndex < 0) {
        currentMonthIndex = 11;
        currentYear--;
      }
      currentMonthEl.textContent = `${months[currentMonthIndex]} ${currentYear}`;
    });
  }

  if (nextMonth) {
    nextMonth.addEventListener("click", function () {
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

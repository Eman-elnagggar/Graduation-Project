// ================================
// Book Appointment Page JavaScript
// ================================

document.addEventListener("DOMContentLoaded", function () {
  // Primary Doctor Management
  let primaryDoctor = localStorage.getItem("primaryDoctor")
    ? JSON.parse(localStorage.getItem("primaryDoctor"))
    : null;
  let visitedDoctors = localStorage.getItem("visitedDoctors")
    ? JSON.parse(localStorage.getItem("visitedDoctors"))
    : [];

  let currentStep = 1;
  const totalSteps = 4;

  // Step elements
  const step1 = document.getElementById("step1");
  const step2 = document.getElementById("step2");
  const step3 = document.getElementById("step3");
  const step4 = document.getElementById("step4");

  // Navigation buttons
  const prevBtn = document.getElementById("prevStep");
  const nextBtn = document.getElementById("nextStep");
  const confirmBtn = document.getElementById("confirmBooking");

  // Progress indicators
  const progressSteps = document.querySelectorAll(".progress-step");

  // Doctor selection
  const doctorCards = document.querySelectorAll(".doctor-select-card");
  let selectedDoctor = null;

  // Doctor search and filters
  const doctorSearch = document.getElementById("doctorSearch");
  const specialtyFilter = document.getElementById("specialtyFilter");
  const locationFilter = document.getElementById("locationFilter");
  const availabilityFilter = document.getElementById("availabilityFilter");
  const clearFiltersBtn = document.getElementById("clearFilters");

  // Update progress bar
  function updateProgress(step) {
    progressSteps.forEach((progressStep, index) => {
      if (index < step) {
        progressStep.classList.add("completed");
        progressStep.classList.remove("active");
      } else if (index === step - 1) {
        progressStep.classList.add("active");
        progressStep.classList.remove("completed");
      } else {
        progressStep.classList.remove("active", "completed");
      }
    });
  }

  // Search and filter doctors
  function filterDoctors() {
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

  function openDoctorChat(doctor) {
    // Redirect to doctor-chat page with doctor parameter
    window.location.href = `doctor-chat.html?doctor=${
      doctor.id
    }&name=${encodeURIComponent(doctor.name)}`;
  }

  function setPrimaryDoctor(doctor) {
    primaryDoctor = doctor;
    localStorage.setItem("primaryDoctor", JSON.stringify(primaryDoctor));

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

  // Doctor card selection
  doctorCards.forEach((card) => {
    card.addEventListener("click", function () {
      // Remove selection from all cards
      doctorCards.forEach((c) => c.classList.remove("selected"));
      // Add selection to clicked card
      this.classList.add("selected");
      selectedDoctor = this.dataset.doctor;
    });
  });

  // Show/hide steps
  function showStep(step) {
    // Hide all steps
    [step1, step2, step3, step4].forEach((s) => {
      if (s) s.style.display = "none";
    });

    // Show current step
    if (step === 1 && step1) step1.style.display = "block";
    if (step === 2 && step2) step2.style.display = "block";
    if (step === 3 && step3) step3.style.display = "block";
    if (step === 4 && step4) step4.style.display = "block";

    // Update progress
    updateProgress(step);
    updateNavigation();
  }

  function updateNavigation() {
    if (prevBtn) {
      prevBtn.style.display = currentStep === 1 ? "none" : "inline-flex";
    }

    if (nextBtn && confirmBtn) {
      if (currentStep === totalSteps) {
        nextBtn.style.display = "none";
        confirmBtn.style.display = "inline-flex";
      } else {
        nextBtn.style.display = "inline-flex";
        confirmBtn.style.display = "none";
      }
    }
  }

  // Next button
  if (nextBtn) {
    nextBtn.addEventListener("click", function () {
      if (currentStep === 1 && !selectedDoctor) {
        alert("Please select a doctor first");
        return;
      }

      if (currentStep < totalSteps) {
        currentStep++;
        showStep(currentStep);
      }
    });
  }

  // Previous button
  if (prevBtn) {
    prevBtn.addEventListener("click", function () {
      if (currentStep > 1) {
        currentStep--;
        showStep(currentStep);
      }
    });
  }

  // Confirm booking
  if (confirmBtn) {
    confirmBtn.addEventListener("click", function () {
      // Get selected doctor info
      const selectedCard = document.querySelector(
        `.doctor-select-card[data-doctor="${selectedDoctor}"]`
      );
      const doctorInfo = {
        id: selectedDoctor,
        name: selectedCard?.dataset.name || "Doctor",
        specialty: selectedCard?.dataset.specialty || "",
        location: selectedCard?.dataset.location || "",
        image:
          selectedCard?.querySelector("img")?.src ||
          "https://via.placeholder.com/100",
      };

      // Add to visited doctors
      addToVisitedDoctors(doctorInfo);

      showNotification("Appointment booked successfully!", "success");

      // Redirect back to appointments page after 2 seconds
      setTimeout(() => {
        window.location.href = "appointments.html";
      }, 2000);
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

  // Calendar date selection
  const calendarDates = document.querySelectorAll(".calendar-date");
  calendarDates.forEach((date) => {
    if (!date.classList.contains("disabled")) {
      date.addEventListener("click", function () {
        calendarDates.forEach((d) => d.classList.remove("selected"));
        this.classList.add("selected");
      });
    }
  });

  // Time slot selection
  const timeSlots = document.querySelectorAll(".time-slot.available");
  timeSlots.forEach((slot) => {
    slot.addEventListener("click", function () {
      timeSlots.forEach((s) => s.classList.remove("selected"));
      this.classList.add("selected");
    });
  });

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

  // Initialize
  updateNavigation();
  updateProgress(1);
});

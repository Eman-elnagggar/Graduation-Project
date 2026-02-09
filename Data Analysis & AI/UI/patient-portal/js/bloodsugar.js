// ================================
// Blood Sugar Tracker JavaScript
// ================================

document.addEventListener("DOMContentLoaded", function () {
  const bloodSugarInput = document.getElementById("bloodSugar");
  const mealTimeSelect = document.getElementById("mealTime");
  const saveBSBtn = document.getElementById("saveBS");
  const bsResult = document.getElementById("bsResult");
  const bsValue = document.getElementById("bsValue");
  const bsStatus = document.getElementById("bsStatus");
  const bsMessage = document.getElementById("bsMessage");

  // Tab switching functionality
  const tabButtons = document.querySelectorAll(".tab-btn");
  const tabContents = document.querySelectorAll(".tab-content");

  tabButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const targetTab = this.getAttribute("data-tab");

      // Remove active class from all buttons and contents
      tabButtons.forEach((btn) => btn.classList.remove("active"));
      tabContents.forEach((content) => content.classList.remove("active"));

      // Add active class to clicked button and corresponding content
      this.classList.add("active");
      document.getElementById(`${targetTab}-tab`).classList.add("active");
    });
  });

  // Input validation - only allow numbers
  if (bloodSugarInput) {
    bloodSugarInput.addEventListener("input", function () {
      this.value = this.value.replace(/[^0-9]/g, "");
    });
  }

  // Save blood sugar reading
  if (saveBSBtn) {
    saveBSBtn.addEventListener("click", function () {
      const bloodSugar = parseInt(bloodSugarInput.value);
      const mealTime = mealTimeSelect.value;

      // Validation
      if (!bloodSugar) {
        showNotification("Please enter a blood sugar value.", "error");
        return;
      }

      if (bloodSugar < 40 || bloodSugar > 400) {
        showNotification(
          "Blood sugar value should be between 40-400 mg/dL.",
          "error"
        );
        return;
      }

      // Analyze blood sugar
      const analysis = analyzeBloodSugar(bloodSugar, mealTime);

      // Display result
      bsValue.textContent = bloodSugar;
      bsStatus.className = "bs-status " + analysis.status;
      bsStatus.innerHTML = `<i class="fas ${analysis.icon}"></i><span>${analysis.label}</span>`;
      bsMessage.textContent = analysis.message;
      bsResult.style.display = "block";

      // Update the stat card
      updateStatCard(bloodSugar, analysis);

      // Add to history (visual only for demo)
      addToHistory(bloodSugar, mealTime, analysis);

      // Show success notification
      showNotification("Blood sugar reading saved successfully!", "success");

      // Scroll to result
      bsResult.scrollIntoView({ behavior: "smooth", block: "center" });
    });
  }

  function analyzeBloodSugar(value, mealTime) {
    let status, label, icon, message;

    // Determine thresholds based on meal time
    const isFasting =
      mealTime === "fasting" ||
      mealTime === "before-lunch" ||
      mealTime === "before-dinner";

    if (value < 70) {
      status = "low";
      label = "Low";
      icon = "fa-arrow-down";
      message =
        "Your blood sugar is low. Consider eating something with carbohydrates. If you feel symptoms like dizziness or sweating, contact your doctor.";
    } else if (isFasting) {
      // Fasting thresholds
      if (value <= 100) {
        status = "normal";
        label = "Normal";
        icon = "fa-check-circle";
        message =
          "Excellent! Your fasting blood sugar is within the normal range. Keep maintaining your healthy lifestyle!";
      } else if (value <= 125) {
        status = "elevated";
        label = "Elevated";
        icon = "fa-exclamation-circle";
        message =
          "Your fasting blood sugar is slightly elevated. Consider consulting your healthcare provider and monitor your diet.";
      } else {
        status = "high";
        label = "High";
        icon = "fa-exclamation-triangle";
        message =
          "Your fasting blood sugar is high. Please contact your healthcare provider for guidance on managing your blood sugar levels.";
      }
    } else {
      // After meal thresholds
      if (value <= 140) {
        status = "normal";
        label = "Normal";
        icon = "fa-check-circle";
        message =
          "Great! Your post-meal blood sugar is within the normal range. Your body is processing glucose well!";
      } else if (value <= 180) {
        status = "elevated";
        label = "Slightly High";
        icon = "fa-exclamation-circle";
        message =
          "Your post-meal blood sugar is slightly elevated. Try to reduce refined carbohydrates and increase physical activity.";
      } else {
        status = "high";
        label = "High";
        icon = "fa-exclamation-triangle";
        message =
          "Your post-meal blood sugar is high. Please consult your healthcare provider about adjusting your diet and treatment plan.";
      }
    }

    return { status, label, icon, message };
  }

  function updateStatCard(bloodSugar, analysis) {
    const bsStatCard = document.querySelector(
      ".stat-card:nth-child(3) .stat-value"
    );
    const bsStatChange = document.querySelector(
      ".stat-card:nth-child(3) .stat-change"
    );

    if (bsStatCard) {
      bsStatCard.textContent = bloodSugar;
    }

    if (bsStatChange) {
      bsStatChange.textContent = analysis.label;
      bsStatChange.className = "stat-change";
      if (analysis.status === "normal") {
        bsStatChange.classList.add("positive");
      } else if (analysis.status === "high") {
        bsStatChange.classList.add("negative");
      }
    }
  }

  function addToHistory(bloodSugar, mealTime, analysis) {
    const historyList = document.querySelector(".bs-history-list");
    if (!historyList) return;

    const now = new Date();
    const day = now.getDate();
    const months = [
      "Jan",
      "Feb",
      "Mar",
      "Apr",
      "May",
      "Jun",
      "Jul",
      "Aug",
      "Sep",
      "Oct",
      "Nov",
      "Dec",
    ];
    const month = months[now.getMonth()];
    const time = now.toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    });

    // Format meal time for display
    const mealTimeLabels = {
      fasting: "Fasting",
      "after-breakfast": "After Breakfast",
      "before-lunch": "Before Lunch",
      "after-lunch": "After Lunch",
      "before-dinner": "Before Dinner",
      "after-dinner": "After Dinner",
      bedtime: "Bedtime",
    };

    const newItem = document.createElement("div");
    newItem.className = "bs-history-item";
    newItem.innerHTML = `
            <div class="bs-history-date">
                <span class="day">${day}</span>
                <span class="month">${month}</span>
            </div>
            <div class="bs-history-reading">
                <span class="reading">${bloodSugar} mg/dL</span>
                <span class="time">${mealTimeLabels[mealTime]} - ${time}</span>
            </div>
            <div class="bs-history-status ${analysis.status}">
                <i class="fas ${analysis.icon}"></i>
                ${analysis.label}
            </div>
        `;

    // Add animation
    newItem.style.animation = "fadeIn 0.3s ease";

    // Insert at the beginning
    historyList.insertBefore(newItem, historyList.firstChild);

    // Remove last item if more than 5
    const items = historyList.querySelectorAll(".bs-history-item");
    if (items.length > 5) {
      items[items.length - 1].remove();
    }
  }

  function showNotification(message, type) {
    // Remove existing notification
    const existing = document.querySelector(".bs-notification");
    if (existing) {
      existing.remove();
    }

    // Create notification
    const notification = document.createElement("div");
    notification.className = `bs-notification ${type}`;
    notification.innerHTML = `
            <i class="fas ${
              type === "success" ? "fa-check-circle" : "fa-exclamation-circle"
            }"></i>
            <span>${message}</span>
        `;

    document.body.appendChild(notification);

    // Auto-remove after 4 seconds
    setTimeout(() => {
      notification.style.animation = "slideIn 0.3s ease reverse";
      setTimeout(() => notification.remove(), 300);
    }, 4000);
  }

  // Input highlighting based on value
  if (bloodSugarInput) {
    bloodSugarInput.addEventListener("blur", function () {
      const value = parseInt(this.value);
      const wrapper = this.closest(".bs-input-wrapper");

      wrapper.classList.remove("normal", "elevated", "high", "low");

      if (!value) return;

      if (value < 70) {
        wrapper.classList.add("low");
      } else if (value <= 100) {
        wrapper.classList.add("normal");
      } else if (value <= 140) {
        wrapper.classList.add("elevated");
      } else {
        wrapper.classList.add("high");
      }
    });
  }
});

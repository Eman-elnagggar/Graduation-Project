// ================================
// Doctor Chat Page JavaScript
// ================================

document.addEventListener("DOMContentLoaded", function () {
  // Get doctor info from URL parameters
  const urlParams = new URLSearchParams(window.location.search);
  const doctorId = urlParams.get("doctor");
  const doctorName = urlParams.get("name");

  // Get visited doctors from localStorage
  const visitedDoctors = localStorage.getItem("visitedDoctors")
    ? JSON.parse(localStorage.getItem("visitedDoctors"))
    : [];

  // Find the specific doctor
  let currentDoctor = visitedDoctors.find((d) => d.id === doctorId);

  // If doctor not found in visited list, create a basic object
  if (!currentDoctor && doctorName) {
    currentDoctor = {
      id: doctorId,
      name: decodeURIComponent(doctorName),
      specialty: "Gynecologist & Obstetrician",
      location: "Medical Center",
      image:
        "https://img.freepik.com/free-photo/portrait-experienced-professional-therapist-with-stethoscope-looking-camera_1098-19305.jpg",
      experience: "15 years",
      visitCount: 0,
    };
  }

  // Initialize doctor information
  if (currentDoctor) {
    initializeDoctorInfo(currentDoctor);
    loadChatHistory(currentDoctor.id);
  } else {
    // Redirect back if no doctor info
    window.location.href = "appointments.html";
  }

  // Chat functionality
  const chatInput = document.getElementById("chatMessageInput");
  const sendBtn = document.getElementById("sendMessageBtn");
  const chatContainer = document.getElementById("chatMessagesContainer");
  const bookAppointmentBtn = document.getElementById("bookAppointmentBtn");
  const viewProfileBtn = document.getElementById("viewDoctorProfile");

  // Auto-resize textarea
  if (chatInput) {
    chatInput.addEventListener("input", function () {
      this.style.height = "auto";
      this.style.height = Math.min(this.scrollHeight, 120) + "px";
    });

    // Send on Enter (Shift+Enter for new line)
    chatInput.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
      }
    });
  }

  // Send button click
  if (sendBtn) {
    sendBtn.addEventListener("click", sendMessage);
  }

  // Book appointment button
  if (bookAppointmentBtn) {
    bookAppointmentBtn.addEventListener("click", function () {
      window.location.href = "appointments.html";
    });
  }

  // View profile button
  if (viewProfileBtn) {
    viewProfileBtn.addEventListener("click", function () {
      showDoctorProfile(currentDoctor);
    });
  }

  // Attach buttons
  document
    .getElementById("attachFileBtn")
    ?.addEventListener("click", function () {
      // Simulate file attachment
      showNotification("File attachment feature coming soon", "info");
    });

  document
    .getElementById("attachImageBtn")
    ?.addEventListener("click", function () {
      // Simulate image attachment
      showNotification("Image attachment feature coming soon", "info");
    });

  document
    .getElementById("shareTestBtn")
    ?.addEventListener("click", function () {
      // Simulate sharing test results
      showNotification("Test sharing feature coming soon", "info");
    });

  // Functions
  function initializeDoctorInfo(doctor) {
    // Update header
    document.getElementById("doctorChatImage").src =
      doctor.image || "https://via.placeholder.com/100";
    document.getElementById("doctorChatName").textContent = doctor.name;
    document.getElementById("doctorChatSpecialty").textContent =
      doctor.specialty;
    document.getElementById("doctorNameMsg").textContent = doctor.name;
    document.getElementById("doctorAvatarMsg").src =
      doctor.image || "https://via.placeholder.com/40";

    // Update sidebar
    document.getElementById("sidebarSpecialty").textContent = doctor.specialty;
    document.getElementById("sidebarLocation").textContent =
      doctor.location || "Medical Center";
    document.getElementById("sidebarExperience").textContent =
      doctor.experience || "10+ years";
    document.getElementById("sidebarVisits").textContent =
      doctor.visitCount || 0;
  }

  function loadChatHistory(doctorId) {
    // Load chat history from localStorage
    const chatHistory = localStorage.getItem(`chat_${doctorId}`);

    if (chatHistory) {
      const messages = JSON.parse(chatHistory);
      messages.forEach((msg) => {
        addMessageToChat(msg.text, msg.sender, msg.time, false);
      });
    }

    // Scroll to bottom
    scrollToBottom();
  }

  function sendMessage() {
    const message = chatInput.value.trim();

    if (!message) return;

    // Add user message
    const currentTime = getCurrentTime();
    addMessageToChat(message, "patient", currentTime, true);

    // Clear input
    chatInput.value = "";
    chatInput.style.height = "auto";

    // Simulate doctor response after a delay
    setTimeout(() => {
      simulateDoctorResponse(message);
    }, 1500);

    // Save to localStorage
    saveChatMessage(message, "patient", currentTime);
  }

  function addMessageToChat(text, sender, time, animate = true) {
    const messageWrapper = document.createElement("div");
    messageWrapper.className = `message-wrapper ${
      sender === "patient" ? "patient-message" : "doctor-message"
    }`;

    if (animate) {
      messageWrapper.style.opacity = "0";
      messageWrapper.style.transform = "translateY(10px)";
    }

    if (sender === "doctor") {
      messageWrapper.innerHTML = `
                <div class="message-avatar">
                    <img src="${currentDoctor.image}" alt="Doctor">
                </div>
                <div class="message-content">
                    <div class="message-header">
                        <span class="message-sender">${currentDoctor.name}</span>
                        <span class="message-time">${time}</span>
                    </div>
                    <div class="message-bubble doctor">
                        <p>${text}</p>
                    </div>
                </div>
            `;
    } else {
      messageWrapper.innerHTML = `
                <div class="message-content">
                    <div class="message-header">
                        <span class="message-sender">You</span>
                        <span class="message-time">${time}</span>
                    </div>
                    <div class="message-bubble patient">
                        <p>${text}</p>
                    </div>
                </div>
            `;
    }

    chatContainer.appendChild(messageWrapper);

    if (animate) {
      setTimeout(() => {
        messageWrapper.style.transition = "all 0.3s ease";
        messageWrapper.style.opacity = "1";
        messageWrapper.style.transform = "translateY(0)";
      }, 10);
    }

    scrollToBottom();
  }

  function simulateDoctorResponse(patientMessage) {
    const responses = [
      "Thank you for reaching out. Can you provide more details about your symptoms?",
      "I understand your concern. Let me check your medical history.",
      "That's a good question. Based on your pregnancy stage, I recommend...",
      "I see. Have you experienced this before?",
      "Your health is important. Let's schedule a checkup to discuss this further.",
      "I'm here to help. Make sure to monitor your symptoms and keep me updated.",
      "This is common during pregnancy. However, if it persists, please let me know.",
      "Based on what you've shared, I suggest the following steps...",
    ];

    const randomResponse =
      responses[Math.floor(Math.random() * responses.length)];
    const currentTime = getCurrentTime();

    addMessageToChat(randomResponse, "doctor", currentTime, true);
    saveChatMessage(randomResponse, "doctor", currentTime);
  }

  function saveChatMessage(text, sender, time) {
    const chatHistory = localStorage.getItem(`chat_${currentDoctor.id}`);
    let messages = chatHistory ? JSON.parse(chatHistory) : [];

    messages.push({
      text: text,
      sender: sender,
      time: time,
      date: new Date().toISOString(),
    });

    // Keep only last 100 messages
    if (messages.length > 100) {
      messages = messages.slice(-100);
    }

    localStorage.setItem(`chat_${currentDoctor.id}`, JSON.stringify(messages));
  }

  function getCurrentTime() {
    const now = new Date();
    let hours = now.getHours();
    const minutes = now.getMinutes();
    const ampm = hours >= 12 ? "PM" : "AM";

    hours = hours % 12;
    hours = hours ? hours : 12;
    const minutesStr = minutes < 10 ? "0" + minutes : minutes;

    return `${hours}:${minutesStr} ${ampm}`;
  }

  function scrollToBottom() {
    chatContainer.scrollTop = chatContainer.scrollHeight;
  }

  function showDoctorProfile(doctor) {
    // Create modal to show doctor profile
    const modal = document.createElement("div");
    modal.className = "doctor-profile-modal";
    modal.innerHTML = `
            <div class="modal-overlay" onclick="this.parentElement.remove()"></div>
            <div class="modal-content doctor-profile-content">
                <button class="modal-close" onclick="this.closest('.doctor-profile-modal').remove()">
                    <i class="fas fa-times"></i>
                </button>
                <div class="doctor-profile-header">
                    <img src="${doctor.image}" alt="${doctor.name}">
                    <h2>${doctor.name}</h2>
                    <p>${doctor.specialty}</p>
                </div>
                <div class="doctor-profile-details">
                    <div class="profile-detail-item">
                        <i class="fas fa-map-marker-alt"></i>
                        <div>
                            <label>Location</label>
                            <span>${doctor.location || "Medical Center"}</span>
                        </div>
                    </div>
                    <div class="profile-detail-item">
                        <i class="fas fa-briefcase"></i>
                        <div>
                            <label>Experience</label>
                            <span>${doctor.experience || "10+ years"}</span>
                        </div>
                    </div>
                    <div class="profile-detail-item">
                        <i class="fas fa-calendar-check"></i>
                        <div>
                            <label>Total Appointments</label>
                            <span>${doctor.visitCount || 0} visits</span>
                        </div>
                    </div>
                    <div class="profile-detail-item">
                        <i class="fas fa-star"></i>
                        <div>
                            <label>Rating</label>
                            <span>4.8 / 5.0</span>
                        </div>
                    </div>
                </div>
                <div class="profile-actions">
                    <button class="btn btn-primary" onclick="window.location.href='appointments.html'">
                        <i class="fas fa-calendar-plus"></i> Book Appointment
                    </button>
                </div>
            </div>
        `;

    document.body.appendChild(modal);
  }

  function showNotification(message, type = "info") {
    const notification = document.createElement("div");
    notification.className = `notification-toast ${type}`;
    notification.innerHTML = `
            <i class="fas fa-${
              type === "success"
                ? "check-circle"
                : type === "error"
                ? "times-circle"
                : "info-circle"
            }"></i>
            <span>${message}</span>
        `;
    notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${
              type === "success"
                ? "#4CAF50"
                : type === "error"
                ? "#F44336"
                : "#2196F3"
            };
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
});

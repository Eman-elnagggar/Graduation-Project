// ================================
// Product Ingredients Checker JavaScript
// ================================

document.addEventListener("DOMContentLoaded", function () {
  let videoStream = null;
  let capturedImage = null;

  // Tab switching
  const cameraOption = document.getElementById("cameraOption");
  const manualOption = document.getElementById("manualOption");
  const cameraScanner = document.getElementById("cameraScanner");
  const manualEntry = document.getElementById("manualEntry");

  if (cameraOption && manualOption) {
    cameraOption.addEventListener("click", function () {
      cameraOption.classList.add("active");
      manualOption.classList.remove("active");
      cameraScanner.style.display = "block";
      manualEntry.style.display = "none";
      stopCamera(); // Stop camera when switching tabs
    });

    manualOption.addEventListener("click", function () {
      manualOption.classList.add("active");
      cameraOption.classList.remove("active");
      manualEntry.style.display = "block";
      cameraScanner.style.display = "none";
      stopCamera(); // Stop camera when switching tabs
    });
  }

  // Camera controls
  const startCamera = document.getElementById("startCamera");
  const capturePhoto = document.getElementById("capturePhoto");
  const retakePhoto = document.getElementById("retakePhoto");
  const analyzePhoto = document.getElementById("analyzePhoto");
  const videoElement = document.getElementById("videoElement");
  const cameraFrame = document.getElementById("cameraFrame");
  const captureCanvas = document.getElementById("captureCanvas");

  // Start camera
  if (startCamera) {
    startCamera.addEventListener("click", async function () {
      try {
        // Request camera access
        videoStream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: "environment" }, // Use back camera on mobile
        });

        videoElement.srcObject = videoStream;
        videoElement.style.display = "block";
        cameraFrame.style.display = "none";

        // Update button visibility
        startCamera.style.display = "none";
        capturePhoto.style.display = "inline-block";
      } catch (error) {
        console.error("Error accessing camera:", error);
        alert(
          "Could not access camera. Please ensure camera permissions are granted."
        );
      }
    });
  }

  // Capture photo
  if (capturePhoto) {
    capturePhoto.addEventListener("click", function () {
      const canvas = captureCanvas;
      const context = canvas.getContext("2d");

      // Set canvas size to match video
      canvas.width = videoElement.videoWidth;
      canvas.height = videoElement.videoHeight;

      // Draw current video frame to canvas
      context.drawImage(videoElement, 0, 0, canvas.width, canvas.height);

      // Get image data
      capturedImage = canvas.toDataURL("image/jpeg");

      // Stop video stream
      stopCamera();

      // Show captured image
      videoElement.style.display = "none";
      canvas.style.display = "block";

      // Update button visibility
      capturePhoto.style.display = "none";
      retakePhoto.style.display = "inline-block";
      analyzePhoto.style.display = "inline-block";
    });
  }

  // Retake photo
  if (retakePhoto) {
    retakePhoto.addEventListener("click", function () {
      captureCanvas.style.display = "none";
      retakePhoto.style.display = "none";
      analyzePhoto.style.display = "none";
      startCamera.style.display = "inline-block";
      capturedImage = null;
    });
  }

  // Analyze photo
  if (analyzePhoto) {
    analyzePhoto.addEventListener("click", function () {
      if (!capturedImage) {
        alert("Please capture a photo first");
        return;
      }

      // Show analyzing animation
      const cameraPreview = document.querySelector(".camera-preview");
      cameraPreview.innerHTML += `
                <div class="analyzing-overlay" style="position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.7); display: flex; flex-direction: column; align-items: center; justify-content: center; color: white; border-radius: 8px;">
                    <i class="fas fa-spinner fa-spin" style="font-size: 48px; margin-bottom: 16px;"></i>
                    <p style="font-size: 18px;">Analyzing ingredients...</p>
                    <p style="font-size: 14px; opacity: 0.8;">Reading label text</p>
                </div>
            `;

      // Simulate OCR and analysis (in real app, send to backend)
      setTimeout(function () {
        // Remove analyzing overlay
        const overlay = document.querySelector(".analyzing-overlay");
        if (overlay) overlay.remove();

        // Randomly show different results for demo
        const results = ["safe", "danger", "warning"];
        const randomResult =
          results[Math.floor(Math.random() * results.length)];
        showResult(randomResult);

        // Reset camera UI
        captureCanvas.style.display = "none";
        cameraFrame.style.display = "block";
        retakePhoto.style.display = "none";
        analyzePhoto.style.display = "none";
        startCamera.style.display = "inline-block";
      }, 3000);
    });
  }

  // Stop camera function
  function stopCamera() {
    if (videoStream) {
      videoStream.getTracks().forEach((track) => track.stop());
      videoStream = null;
      videoElement.style.display = "none";
    }
  }

  // Manual ingredient check button
  const checkProduct = document.getElementById("checkProduct");
  if (checkProduct) {
    checkProduct.addEventListener("click", function () {
      const ingredientsInput = document
        .getElementById("ingredientsInput")
        .value.trim();
      const productName = document.getElementById("productName").value.trim();

      if (!ingredientsInput) {
        alert("Please enter the product ingredients");
        return;
      }

      // Analyze ingredients (convert to lowercase for checking)
      const ingredients = ingredientsInput.toLowerCase();

      // Check for dangerous ingredients
      if (
        ingredients.includes("retinol") ||
        ingredients.includes("tretinoin") ||
        ingredients.includes("retinyl") ||
        ingredients.includes("isotretinoin") ||
        ingredients.includes("hydroquinone") ||
        ingredients.includes("formaldehyde") ||
        ingredients.includes("toluene")
      ) {
        showResult("danger");
      }
      // Check for caution ingredients
      else if (
        ingredients.includes("caffeine") ||
        ingredients.includes("paraben") ||
        ingredients.includes("salicylic acid") ||
        ingredients.includes("sodium lauryl sulfate") ||
        ingredients.includes("sls") ||
        ingredients.includes("fragrance") ||
        ingredients.includes("parfum")
      ) {
        showResult("warning");
      }
      // Safe ingredients
      else {
        showResult("safe");
      }
    });
  }

  function showResult(type) {
    // Hide placeholder
    document.getElementById("resultsPlaceholder").style.display = "none";

    // Hide all results first
    document.getElementById("safeResult").style.display = "none";
    document.getElementById("dangerResult").style.display = "none";
    document.getElementById("warningResult").style.display = "none";

    // Show appropriate result
    if (type === "safe") {
      document.getElementById("safeResult").style.display = "block";
    } else if (type === "danger") {
      document.getElementById("dangerResult").style.display = "block";
    } else if (type === "warning") {
      document.getElementById("warningResult").style.display = "block";
    }

    // Scroll to results
    document.querySelector(".results-card").scrollIntoView({
      behavior: "smooth",
      block: "start",
    });
  }

  // Clean up camera on page unload
  window.addEventListener("beforeunload", function () {
    stopCamera();
  });
});

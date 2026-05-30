(function () {
  "use strict";

  const boot = window.patientProfileBootstrap || {};

  function getAntiForgeryToken() {
    return (
      document.querySelector('input[name="__RequestVerificationToken"]')
        ?.value || ""
    );
  }

  function showNotification(message, type = "info") {
    const existing = document.querySelector(".pp-notification");
    if (existing) {
      existing.remove();
    }

    const colors = {
      success: "#16a34a",
      error: "#e53e3e",
      warning: "#d97706",
      info: "#2563eb",
    };

    const notification = document.createElement("div");
    notification.className = "pp-notification";
    notification.style.cssText = `position:fixed;top:20px;right:20px;background:#fff;color:#0f1c2e;padding:12px 16px;border-radius:10px;box-shadow:0 8px 22px rgba(11,31,53,.18);border-left:4px solid ${colors[type] || colors.info};z-index:2000;font-size:.86rem;font-weight:600;`;
    notification.textContent = message;
    document.body.appendChild(notification);

    setTimeout(() => notification.remove(), 3000);
  }

  function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;

    if (modalId === "prescriptionModal") {
      preparePrescriptionModal();
    }

    modal.classList.add("active");
    document.body.style.overflow = "hidden";
  }

  function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;

    modal.classList.remove("active");
    if (!document.querySelector(".pp-modal.active")) {
      document.body.style.overflow = "";
    }
  }

  function setupModals() {
    const modalOpeners = {
      btnWritePrescription: "prescriptionModal",
      btnViewAllAlerts: "alertsModal",
      btnViewAllAppointments: "appointmentsModal",
      btnViewAllPrescriptions: "allPrescriptionsModal",
      btnViewFullHistory: "medicalHistoryModal",
      btnViewAllLabs: "labTestsModal",
    };

    Object.entries(modalOpeners).forEach(([id, modalId]) => {
      document
        .getElementById(id)
        ?.addEventListener("click", () => openModal(modalId));
    });

    document.querySelectorAll("[data-open-modal]").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        const modalId = e.currentTarget.getAttribute("data-open-modal");
        if (modalId) {
          document
            .querySelectorAll(".pp-modal.active")
            .forEach((m) => m.classList.remove("active"));
          openModal(modalId);
        }
      });
    });

    document.querySelectorAll(".pp-modal-close,[data-modal]").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        const modalId = e.currentTarget.getAttribute("data-modal");
        if (modalId) {
          closeModal(modalId);
        }
      });
    });

    document.querySelectorAll(".pp-modal-overlay").forEach((overlay) => {
      overlay.addEventListener("click", () => {
        const modal = overlay.closest(".pp-modal");
        if (modal) {
          modal.classList.remove("active");
          if (!document.querySelector(".pp-modal.active")) {
            document.body.style.overflow = "";
          }
        }
      });
    });

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") {
        document
          .querySelectorAll(".pp-modal.active")
          .forEach((m) => m.classList.remove("active"));
        document.body.style.overflow = "";
      }
    });
  }

  function refreshMedicineRowsUI() {
    const rows = Array.from(
      document.querySelectorAll(
        "#prescriptionMedicinesContainer .pp-rx-medicine-item",
      ),
    );
    rows.forEach((row, idx) => {
      const title = row.querySelector(".pp-rx-medicine-title");
      if (title) {
        title.textContent = `Medicine ${idx + 1}`;
      }

      const removeBtn = row.querySelector(".pp-rx-remove-btn");
      if (removeBtn) {
        removeBtn.style.display = rows.length > 1 ? "inline-flex" : "none";
      }
    });
  }

  function bindMedicineRowEvents(row) {
    const removeBtn = row.querySelector(".pp-rx-remove-btn");
    if (!removeBtn) {
      return;
    }

    removeBtn.addEventListener("click", () => {
      row.remove();
      refreshMedicineRowsUI();
    });
  }

  function addMedicineRow() {
    const template = document.getElementById("ppMedicineRowTemplate");
    const container = document.getElementById("prescriptionMedicinesContainer");
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
    const container = document.getElementById("prescriptionMedicinesContainer");
    if (!container) {
      return;
    }

    container.innerHTML = "";
    addMedicineRow();
  }

  function preparePrescriptionModal() {
    const form = document.getElementById("prescriptionForm");
    form?.reset();
    document.getElementById("prescriptionPatientId").value = String(
      boot.patientId || 0,
    );
    document.getElementById("prescriptionPatientName").value =
      boot.patientName || "Patient";
    resetMedicineRows();

    const firstInput = document.querySelector(
      "#prescriptionMedicinesContainer .pp-medicine-name",
    );
    if (firstInput) {
      firstInput.focus();
    }
  }

  async function savePrescription() {
    const patientId = parseInt(
      document.getElementById("prescriptionPatientId")?.value ||
        String(boot.patientId || 0),
      10,
    );
    const notes = document.getElementById("rxNotes")?.value?.trim() || "";

    const medicineRows = Array.from(
      document.querySelectorAll(
        "#prescriptionMedicinesContainer .pp-rx-medicine-item",
      ),
    );
    const items = medicineRows
      .map((row) => {
        const durationRaw =
          row.querySelector(".pp-medicine-duration")?.value?.trim() || "";
        return {
          medicineName:
            row.querySelector(".pp-medicine-name")?.value?.trim() || "",
          dosage: row.querySelector(".pp-medicine-dosage")?.value?.trim() || "",
          frequency:
            row.querySelector(".pp-medicine-frequency")?.value?.trim() || "",
          durationDays: durationRaw
            ? Math.max(0, parseInt(durationRaw, 10) || 0)
            : 0,
          instructions:
            row.querySelector(".pp-medicine-instructions")?.value?.trim() || "",
        };
      })
      .filter((item) => item.medicineName);

    if (!patientId) {
      showNotification("Patient is required.", "warning");
      return;
    }

    if (!items.length) {
      showNotification("Please add at least one medicine name.", "warning");
      return;
    }

    const url = boot.createPrescriptionUrl;
    if (!url || !boot.patientId) {
      showNotification("Prescription endpoint is not available.", "error");
      return;
    }

    const token = getAntiForgeryToken();
    const body = new URLSearchParams();
    body.append("patientId", String(patientId));
    items.forEach((item) => {
      body.append("medicineNames", item.medicineName || "");
      body.append("dosages", item.dosage || "");
      body.append("frequencies", item.frequency || "");
      body.append("durationDays", String(item.durationDays || 0));
      body.append("instructions", item.instructions || "");
    });
    body.append("notes", notes);
    body.append("__RequestVerificationToken", token);

    try {
      const response = await fetch(url, {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
        },
        body: body.toString(),
      });

      const result = await response.json();
      if (!response.ok || !result?.success) {
        showNotification(
          result?.message || "Failed to save prescription.",
          "error",
        );
        return;
      }

      document.getElementById("prescriptionForm")?.reset();
      resetMedicineRows();
      closeModal("prescriptionModal");
      showNotification(
        result.message || "Prescription saved successfully.",
        "success",
      );

      if (result?.prescriptionId && boot?.doctorId) {
        const printUrl = `/Doctor/PrintPrescription/${boot.doctorId}?prescriptionId=${result.prescriptionId}`;
        window.open(printUrl, "_blank");
      }
    } catch {
      showNotification("Failed to save prescription.", "error");
    }
  }

  function toggleAddNoteForm(forceOpen) {
    const form = document.getElementById("addNoteForm");
    if (!form) return;

    const shouldOpen =
      typeof forceOpen === "boolean"
        ? forceOpen
        : form.style.display === "none" || form.style.display === "";

    form.style.display = shouldOpen ? "block" : "none";

    if (shouldOpen) {
      document.getElementById("noteText")?.focus();
      form.scrollIntoView({ behavior: "smooth", block: "center" });
    }
  }

  function setupAddNoteButtons() {
    ["btnAddNote", "btnAddNote2"].forEach((id) => {
      document
        .getElementById(id)
        ?.addEventListener("click", () => toggleAddNoteForm(true));
    });

    document
      .getElementById("cancelNote")
      ?.addEventListener("click", () => toggleAddNoteForm(false));
  }

  function setupBabyGenderEditor() {
    const editBtn = document.getElementById("editBabyGenderBtn");
    const cancelBtn = document.getElementById("cancelBabyGenderEdit");
    const form = document.getElementById("babyGenderForm");
    const value = document.getElementById("babyGenderValue");

    if (!editBtn || !form || !value) {
      return;
    }

    editBtn.addEventListener("click", () => {
      form.style.display = "flex";
      value.style.display = "none";
      editBtn.style.display = "none";
      form.querySelector('select[name="babyGender"]')?.focus();
    });

    cancelBtn?.addEventListener("click", () => {
      form.style.display = "none";
      value.style.display = "";
      editBtn.style.display = "inline-flex";
    });
  }

  function animateTimeline() {
    const progressBar = document.querySelector(".pp-timeline-fill");
    if (!progressBar) return;

    const targetWidth = progressBar.style.width || "0%";
    progressBar.style.width = "0%";
    setTimeout(() => {
      progressBar.style.width = targetWidth;
    }, 80);
  }

  function setupChartBars() {
    const getBloodSugarRisk = (value) => {
      if (value >= 141) return "high";
      if (value >= 96) return "medium";
      return "low";
    };

    const getBloodPressureRisk = (sys, dia) => {
      if (sys >= 140 || dia >= 90) return "high";
      if (sys >= 130 || dia >= 80) return "medium";
      return "low";
    };

    document
      .querySelectorAll(".pp-chart-bars-sugar .pp-chart-bar.sugar")
      .forEach((bar) => {
        const sugar = parseInt(bar.dataset.value || "0", 10) || 0;
        const risk = getBloodSugarRisk(sugar);
        bar.classList.remove("risk-low", "risk-medium", "risk-high");
        bar.classList.add(`risk-${risk}`);
        bar.dataset.risk = risk;
      });

    document
      .querySelectorAll(
        ".pp-chart-bars:not(.pp-chart-bars-sugar) .pp-chart-bar-group",
      )
      .forEach((group) => {
        const sysBar = group.querySelector(".pp-chart-bar.systolic");
        const diaBar = group.querySelector(".pp-chart-bar.diastolic");
        if (!sysBar && !diaBar) return;

        const sys = parseInt(sysBar?.dataset.value || "0", 10) || 0;
        const dia = parseInt(diaBar?.dataset.value || "0", 10) || 0;
        const risk = getBloodPressureRisk(sys, dia);

        [sysBar, diaBar].forEach((bar) => {
          if (!bar) return;
          bar.classList.remove("risk-low", "risk-medium", "risk-high");
          bar.classList.add(`risk-${risk}`);
          bar.dataset.risk = risk;
        });
      });

    document.querySelectorAll(".pp-chart-bar").forEach((bar) => {
      bar.addEventListener("mouseenter", function () {
        this.style.opacity = "0.9";
        this.style.transform = "scaleY(1.03)";
      });

      bar.addEventListener("mouseleave", function () {
        this.style.opacity = "1";
        this.style.transform = "scaleY(1)";
      });
    });
  }

  function updateDueDateFooter() {
    const boot = window.patientProfileBootstrap || {};
    const dueDateText = boot.dueDate;
    const footer = document.getElementById("dueDateFoot");
    if (!dueDateText || !footer) return;

    const dueDate = new Date(dueDateText);
    if (Number.isNaN(dueDate.getTime())) return;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    dueDate.setHours(0, 0, 0, 0);

    const days = Math.ceil((dueDate - today) / (1000 * 60 * 60 * 24));
    footer.textContent =
      days >= 0 ? `${days} days remaining` : "Due date passed";
  }

  const testConfigurations = {
    cbc: {
      name: "CBC (Complete Blood Count)",
      parameters: [
        {
          key: "hb",
          name: "Hemoglobin (HB)",
          unit: "g/dL",
          normalRange: "12-16",
        },
        {
          key: "wbc",
          name: "WBC Count",
          unit: "/uL",
          normalRange: "4000-11000",
        },
        {
          key: "rbcs_count",
          name: "RBC Count",
          unit: "million/uL",
          normalRange: "4.2-5.4",
        },
        { key: "mcv", name: "MCV", unit: "fL", normalRange: "80-100" },
        { key: "mch", name: "MCH", unit: "pg", normalRange: "27-33" },
        { key: "mchc", name: "MCHC", unit: "g/dL", normalRange: "32-36" },
        {
          key: "lymphocytes",
          name: "Lymphocytes",
          unit: "%",
          normalRange: "20-40",
        },
        {
          key: "platelet_count",
          name: "Platelets",
          unit: "/uL",
          normalRange: "150000-400000",
        },
      ],
    },
    urinalysis: {
      name: "Urinalysis",
      parameters: [
        { key: "color", name: "Color", unit: "", normalRange: "Light Yellow" },
        { key: "ph", name: "pH", unit: "", normalRange: "4.5-8.0" },
        {
          key: "specific_gravity",
          name: "Specific Gravity",
          unit: "",
          normalRange: "1.005-1.030",
        },
        { key: "protein", name: "Protein", unit: "", normalRange: "Negative" },
        { key: "glucose", name: "Glucose", unit: "", normalRange: "Negative" },
        { key: "ketones", name: "Ketones", unit: "", normalRange: "Negative" },
        { key: "blood", name: "Blood", unit: "", normalRange: "Negative" },
        { key: "rbcs", name: "RBCs", unit: "/HPF", normalRange: "0-5" },
        {
          key: "leukocytes",
          name: "Leukocytes",
          unit: "/HPF",
          normalRange: "0-5",
        },
        { key: "nitrite", name: "Nitrite", unit: "", normalRange: "Negative" },
      ],
    },
    tsh: {
      name: "TSH (Thyroid)",
      parameters: [
        { key: "tsh", name: "TSH", unit: "mIU/L", normalRange: "0.4-4.0" },
      ],
    },
    ferritin: {
      name: "Ferritin",
      parameters: [
        {
          key: "ferritin_value",
          name: "Ferritin",
          unit: "ng/mL",
          normalRange: "12-150",
        },
      ],
    },
    fbg: {
      name: "Fasting Blood Glucose",
      parameters: [
        { key: "fbg", name: "FBG", unit: "mg/dL", normalRange: "70-100" },
      ],
    },
    hba1c: {
      name: "HbA1c (Sugar Test)",
      parameters: [
        { key: "hba1c", name: "HbA1c", unit: "%", normalRange: "4.0-5.6" },
      ],
    },
    hcv: {
      name: "HCV (Hepatitis C)",
      parameters: [
        { key: "hcv", name: "HCV", unit: "", normalRange: "Non-Reactive" },
      ],
    },
    hbsag: {
      name: "HBsAg (Hepatitis B)",
      parameters: [
        { key: "hbsag", name: "HBsAg", unit: "", normalRange: "Non-Reactive" },
      ],
    },
    bloodgroup: {
      name: "Blood Group",
      parameters: [
        {
          key: "abo_group",
          name: "ABO Group",
          unit: "",
          normalRange: "A/B/AB/O",
        },
        { key: "rh_factor", name: "Rh Factor", unit: "", normalRange: "+/-" },
      ],
    },
  };

  function escHtml(str) {
    return String(str ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function normalizeAssetUrl(path) {
    const raw = String(path ?? "").trim();
    if (!raw) return "";
    if (
      /^https?:\/\//i.test(raw) ||
      raw.startsWith("data:") ||
      raw.startsWith("blob:")
    )
      return raw;
    const normalized = raw.replace(/\\/g, "/");
    return normalized.startsWith("/") ? normalized : "/" + normalized;
  }

  function resolveTestConfig(testName) {
    if (!testName) return null;
    const normalized = testName.toLowerCase();
    return (
      Object.values(testConfigurations).find(
        (cfg) => cfg.name.toLowerCase() === normalized,
      ) ||
      Object.values(testConfigurations).find((cfg) =>
        normalized.includes(cfg.name.toLowerCase()),
      )
    );
  }

  function extractValue(source, key) {
    if (!source) return null;
    const match = Object.keys(source).find(
      (k) => k.toLowerCase() === key.toLowerCase(),
    );
    const value = match ? source[match] : null;
    if (Array.isArray(value)) {
      return value[0] ?? null;
    }
    return value;
  }

  function hasSubmitDiagnosisResults(test) {
    return Object.keys(test || {}).some(
      (key) => !isMetadataKey(key) && Array.isArray(test[key]),
    );
  }

  function isMetadataKey(key) {
    return ["test_name", "confidence"].includes(String(key || "").toLowerCase());
  }

  function getSubmitDiagnosisStatus(value) {
    return Array.isArray(value) ? String(value[0] ?? "") : String(value ?? "");
  }

  function getSubmitDiagnosisDetail(value) {
    if (!Array.isArray(value)) return "";
    if (value.length === 1 && Array.isArray(value[0])) return value[0].join(", ");
    return value.slice(1).flat().filter(Boolean).join(" ");
  }

  function getSubmitDiagnosisClass(status) {
    const s = String(status || "").toLowerCase();
    if (!s || s === "normal") return "normal";
    if (s.includes("low") || s.includes("below")) return "low";
    if (s.includes("moderate") || s.includes("trace")) return "warning";
    return "high";
  }

  function toDisplayLabel(key) {
    return String(key || "").replace(/_/g, " ");
  }

  function getParamStatus(value, normalRange) {
    const s = String(value ?? "")
      .trim()
      .toLowerCase();
    if (!s || s === "negative" || s === "-") return "normal";
    if (s === "positive") return "high";
    const num = parseFloat(s.replace(/,/g, ""));
    const m = normalRange.replace(/,/g, "").match(/([\d.]+)[-]([\d.]+)/);
    if (!isNaN(num) && m) {
      const lo = parseFloat(m[1]);
      const hi = parseFloat(m[2]);
      if (num < lo) return "low";
      if (num > hi) return "high";
      return "normal";
    }
    return "normal";
  }

  function formatReportText(text) {
    if (!text) return "";
    return text
      .split("\n")
      .map((line) => {
        line = line.trim();
        if (!line) return "";
        if (line.startsWith("*")) {
          return (
            '<div class="pp-report-bullet"><i class="fas fa-angle-right"></i>' +
            escHtml(line.replace(/^\*\s*/, "")) +
            "</div>"
          );
        }
        if (line.endsWith(":") && line.length < 120)
          return "<h6>" + escHtml(line) + "</h6>";
        return "<p>" + escHtml(line) + "</p>";
      })
      .join("");
  }

  function mapAnalysisResponse(data) {
    const tests = data.tests || [];
    let hasHigh = false;
    let hasLow = false;

    tests.forEach((test) => {
      if (hasSubmitDiagnosisResults(test)) {
        Object.keys(test).forEach((key) => {
          if (isMetadataKey(key)) return;
          const cls = getSubmitDiagnosisClass(getSubmitDiagnosisStatus(test[key]));
          if (cls === "high") hasHigh = true;
          if (cls === "warning") hasLow = true;
          if (cls === "low") hasLow = true;
        });
      } else {
        const cfg = resolveTestConfig(test.test_name);
        if (!cfg) return;
        cfg.parameters.forEach((p) => {
          const s = getParamStatus(extractValue(test, p.key), p.normalRange);
          if (s === "high") hasHigh = true;
          if (s === "low") hasLow = true;
        });
      }
    });

    const riskRaw = data.riskPrediction;
    let riskObj = {};
    if (riskRaw && typeof riskRaw === "object" && !Array.isArray(riskRaw)) {
      riskObj = riskRaw;
    } else if (Array.isArray(riskRaw)) {
      for (let i = riskRaw.length - 1; i >= 0; i--) {
        if (
          riskRaw[i] &&
          typeof riskRaw[i] === "object" &&
          Object.keys(riskRaw[i]).length > 0
        ) {
          riskObj = riskRaw[i];
          break;
        }
      }
    }

    const riskText = String(riskObj.risk_level || "").toLowerCase();
    if (riskText.includes("high")) hasHigh = true;
    if (riskText.includes("moderate") || riskText.includes("medium")) hasLow = true;

    const verdict = hasHigh ? "danger" : hasLow ? "warning" : "safe";
    const overall = hasHigh
      ? "Abnormal Values Detected"
      : hasLow
        ? "Some Values Below Normal"
        : "All Values Normal";

    return {
      verdict,
      overall,
      personalInfo: data.personalInfo || null,
      confidence: riskObj.confidence ?? "-",
      diabetesStatus: riskObj.diabetes_status || null,
      recommendations: data.alerts || [],
      tests,
      riskLevel: riskObj.risk_level || null,
      report: data.report || null,
    };
  }

  function setupLabReports() {
    const labReportModal = document.getElementById("labReportModal");
    const labReportLoading = document.getElementById("labReportLoading");
    const labReportContent = document.getElementById("labReportContent");
    const labReportBody = document.getElementById("labReportBody");
    const labReportError = document.getElementById("labReportError");
    const labReportErrorMsg = document.getElementById("labReportErrorMsg");
    const labReportTitle = document.getElementById("labReportTitle");
    const labReportSubtitle = document.getElementById("labReportSubtitle");
    const labReportBadge = document.getElementById("labReportBadge");

    const labImagesModal = document.getElementById("labImagesModal");
    const labImagesGrid = document.getElementById("labImagesGrid");
    const labImagesTitle = document.getElementById("labImagesTitle");
    const labImagesSubtitle = document.getElementById("labImagesSubtitle");

    if (
      !labReportModal ||
      !labReportLoading ||
      !labReportContent ||
      !labReportBody ||
      !labReportError
    ) {
      return;
    }

    function showReportLoading() {
      labReportLoading.style.display = "flex";
      labReportContent.style.display = "none";
      labReportError.style.display = "none";
    }

    function showReportError(msg) {
      labReportLoading.style.display = "none";
      labReportContent.style.display = "none";
      labReportError.style.display = "flex";
      if (labReportErrorMsg)
        labReportErrorMsg.textContent = msg || "Could not load the report.";
    }

    function showReportContent() {
      labReportLoading.style.display = "none";
      labReportContent.style.display = "block";
      labReportError.style.display = "none";
    }

    function renderReport(analysis) {
      const iconMap = {
        safe: "fa-check-circle",
        warning: "fa-exclamation-circle",
        danger: "fa-exclamation-triangle",
      };
      const verdict = analysis.verdict || "safe";
      let html =
        '<div class="pp-report-summary ' +
        verdict +
        '">' +
        '<div class="pp-report-verdict"><i class="fas ' +
        iconMap[verdict] +
        '"></i>' +
        "<span>" +
        escHtml(analysis.overall || "Report") +
        "</span></div>" +
        '<div class="pp-report-meta-row">' +
        (analysis.riskLevel
          ? "<span>Risk: " + escHtml(analysis.riskLevel) + "</span>"
          : "") +
        (analysis.diabetesStatus
          ? "<span>Diabetes: " + escHtml(analysis.diabetesStatus) + "</span>"
          : "") +
        (analysis.confidence && analysis.confidence !== "-"
          ? "<span>Confidence: " + escHtml(analysis.confidence) + "%</span>"
          : "") +
        "</div></div>";

      if (analysis.personalInfo) {
        const pi = analysis.personalInfo;
        const piFields = [
          { label: "Name", value: pi.name, suffix: "" },
          { label: "Age", value: pi.age, suffix: " yrs" },
          { label: "Trimester", value: pi.trimester, suffix: "" },
          { label: "Week", value: pi.week, suffix: "" },
          { label: "Baby Gender", value: pi.baby_gender, suffix: "" },
          { label: "Height", value: pi.height, suffix: " cm" },
          { label: "Weight", value: pi.weight, suffix: " kg" },
          { label: "RBS Avg", value: pi.rbs_avg, suffix: " mg/dL" },
          {
            label: "BP Avg",
            value:
              pi.avg_systolic && pi.avg_diastolic
                ? pi.avg_systolic + "/" + pi.avg_diastolic
                : null,
            suffix: " mmHg",
          },
          { label: "Risk", value: pi.risk_state, suffix: "" },
        ];
        let cards = "";
        piFields.forEach((f) => {
          if (f.value == null || f.value === "") return;
          cards +=
            '<div class="pp-report-card"><div class="pp-report-card-label">' +
            escHtml(f.label) +
            '</div><div class="pp-report-card-value">' +
            escHtml(String(f.value)) +
            (f.suffix || "") +
            "</div></div>";
        });
        if (cards) {
          html +=
            '<div class="pp-report-section"><h4>Patient Information</h4>' +
            '<div class="pp-report-grid">' +
            cards +
            "</div></div>";
        }
      }

      if (analysis.tests && analysis.tests.length) {
        analysis.tests.forEach((test) => {
          const cfg = resolveTestConfig(test.test_name);
          if (!cfg && !hasSubmitDiagnosisResults(test)) return;
          const confVal = parseFloat(test.confidence || "");
          let metrics = "";
          if (hasSubmitDiagnosisResults(test)) {
            metrics = Object.keys(test)
              .filter((key) => !isMetadataKey(key))
              .map((key) => {
                const status = getSubmitDiagnosisStatus(test[key]);
                const detail = getSubmitDiagnosisDetail(test[key]);
                const cls = getSubmitDiagnosisClass(status);
                return (
                  '<div class="pp-report-metric ' +
                  cls +
                  '">' +
                  '<div class="pp-report-metric-label">' +
                  escHtml(toDisplayLabel(key)) +
                  "</div>" +
                  '<div class="pp-report-metric-value">' +
                  escHtml(status || "Normal") +
                  "</div>" +
                  (detail
                    ? '<div class="pp-report-metric-detail">' +
                      escHtml(detail) +
                      "</div>"
                    : "") +
                  "</div>"
                );
              })
              .join("");
          } else {
            cfg.parameters.forEach((p) => {
              const val = extractValue(test, p.key) ?? "-";
              const status = getParamStatus(val, p.normalRange);
              metrics +=
                '<div class="pp-report-metric ' +
                status +
                '">' +
                '<div class="pp-report-metric-label">' +
                escHtml(p.name) +
                "</div>" +
                '<div class="pp-report-metric-value">' +
                escHtml(String(val)) +
                (p.unit ? " <span>" + escHtml(p.unit) + "</span>" : "") +
                "</div>" +
                '<div class="pp-report-metric-range">Normal: ' +
                escHtml(p.normalRange) +
                (p.unit ? " " + escHtml(p.unit) : "") +
                "</div></div>";
            });
          }

          html +=
            '<div class="pp-report-section"><div class="pp-report-test">' +
            '<div class="pp-report-test-title">' +
            escHtml(test.test_name || cfg?.name || "Lab Test") +
            (!isNaN(confVal)
              ? '<span class="pp-report-pill">' +
                Math.round(confVal * 100) +
                "% confidence</span>"
              : "") +
            '</div><div class="pp-report-metrics">' +
            metrics +
            "</div></div></div>";
        });
      }

      if (analysis.report) {
        html +=
          '<div class="pp-report-section"><h4>AI Medical Report</h4>' +
          '<div class="pp-report-text">' +
          formatReportText(analysis.report) +
          "</div></div>";
      }

      if (analysis.recommendations && analysis.recommendations.length) {
        html +=
          '<div class="pp-report-section"><h4>Clinical Recommendations</h4>' +
          '<ul class="pp-report-list">' +
          analysis.recommendations
            .map(
              (r) =>
                '<li><i class="fas fa-check-circle"></i> ' +
                escHtml(r) +
                "</li>",
            )
            .join("") +
          "</ul></div>";
      }

      labReportBody.innerHTML =
        html ||
        '<p class="pp-report-empty">No detailed data available for this report.</p>';
      showReportContent();
    }

    async function loadReport(
      labTestId,
      testName,
      testDate,
      resultText,
      resultClass,
    ) {
      const reportUrl = boot.labReportUrl;
      if (!reportUrl) {
        showNotification("Lab report endpoint is not available.", "error");
        return;
      }

      if (labReportTitle) labReportTitle.textContent = testName || "Lab Report";
      if (labReportSubtitle) labReportSubtitle.textContent = testDate || "";
      if (labReportBadge && resultText && resultText !== "Pending") {
        labReportBadge.className =
          "pp-report-badge " + (resultClass || "pending");
        labReportBadge.textContent = resultText;
        labReportBadge.style.display = "inline-flex";
      } else if (labReportBadge) {
        labReportBadge.style.display = "none";
      }

      showReportLoading();
      openModal("labReportModal");

      try {
        const resp = await fetch(
          `${reportUrl}?labTestId=${encodeURIComponent(labTestId)}`,
        );
        if (!resp.ok) {
          showReportError("Report not found or not ready.");
          return;
        }
        const data = await resp.json();
        if (data.status !== "Completed") {
          showReportError(
            "This report is still being processed (" + data.status + ").",
          );
          return;
        }
        const analysis = mapAnalysisResponse(data);
        renderReport(analysis);
      } catch (err) {
        showReportError(err?.message || "Failed to load report.");
      }
    }

    document.addEventListener("click", (e) => {
      const reportBtn = e.target.closest(".pp-lab-view-report");
      if (reportBtn) {
        const id = reportBtn.dataset.testId;
        if (id) {
          loadReport(
            parseInt(id, 10),
            reportBtn.dataset.testName,
            reportBtn.dataset.testDate,
            reportBtn.dataset.result,
            reportBtn.dataset.resultClass,
          );
        }
      }

      const imagesBtn = e.target.closest(".pp-lab-view-images");
      if (imagesBtn && labImagesModal && labImagesGrid) {
        try {
          const images = JSON.parse(imagesBtn.dataset.images || "[]");
          const title = imagesBtn.dataset.testName || "Lab Images";
          const date = imagesBtn.dataset.testDate || "";
          if (labImagesTitle) labImagesTitle.textContent = title;
          if (labImagesSubtitle) labImagesSubtitle.textContent = date;

          if (!images.length) {
            labImagesGrid.innerHTML =
              '<div class="pp-report-empty">No images available.</div>';
          } else {
            labImagesGrid.innerHTML = images
              .map((img, idx) => {
                const safeUrl = normalizeAssetUrl(img.path);
                const label = escHtml(img.name || "Image " + (idx + 1));
                const ext = (safeUrl || "").split(".").pop().toLowerCase();
                if (ext === "pdf") {
                  return (
                    '<a class="pp-image-card" href="' +
                    escHtml(safeUrl) +
                    '" target="_blank" rel="noopener noreferrer">' +
                    '<div class="pp-image-pdf"><i class="fas fa-file-pdf"></i></div>' +
                    '<span class="pp-image-label">' +
                    label +
                    "</span></a>"
                  );
                }
                return (
                  '<a class="pp-image-card" href="' +
                  escHtml(safeUrl) +
                  '" target="_blank" rel="noopener noreferrer">' +
                  '<img src="' +
                  escHtml(safeUrl) +
                  '" alt="' +
                  label +
                  '" loading="lazy" referrerpolicy="no-referrer">' +
                  '<span class="pp-image-label">' +
                  label +
                  "</span></a>"
                );
              })
              .join("");
          }

          openModal("labImagesModal");
        } catch (err) {
          showNotification("Could not load images.", "error");
        }
      }
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    setupModals();
    setupAddNoteButtons();
    setupBabyGenderEditor();
    animateTimeline();
    setupChartBars();
    updateDueDateFooter();
    setupLabReports();

    document
      .getElementById("btnAddMedicineRow")
      ?.addEventListener("click", addMedicineRow);
    resetMedicineRows();

    document
      .getElementById("btnSavePrescription")
      ?.addEventListener("click", (e) => {
        e.preventDefault();
        savePrescription();
      });
  });
})();

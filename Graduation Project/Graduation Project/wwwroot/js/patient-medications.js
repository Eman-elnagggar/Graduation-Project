document.addEventListener("DOMContentLoaded", () => {
    const notify = (message, type = "info") => {
        if (typeof window.showNotification === "function") {
            window.showNotification(message, type);
            return;
        }
        alert(message);
    };

    const form = document.getElementById("selfMedicationForm");
    if (form) {
        form.addEventListener("submit", async (event) => {
            event.preventDefault();

            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value;
            const patientId = document.getElementById("patientId")?.value;
            const payload = {
                patientId,
                name: document.getElementById("medName")?.value || "",
                dosage: document.getElementById("medDosage")?.value || "",
                frequency: document.getElementById("medFrequency")?.value || "",
                instructions: document.getElementById("medInstructions")?.value || "",
                startDate: document.getElementById("medStart")?.value || "",
                durationDays: document.getElementById("medDuration")?.value || ""
            };

            const response = await fetch("/PatientMedication/AddMedication", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify(payload)
            });

            const data = await response.json();
            if (data?.success) {
                window.location.href = `/PatientMedication/Index/${patientId}`;
            } else {
                notify(data?.message || "Unable to save medication.", "error");
            }
        });
    }

    const saveGlobalButton = document.getElementById("saveGlobalLeadTime");
    if (saveGlobalButton) {
        saveGlobalButton.addEventListener("click", async () => {
            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value;
            const patientId = document.getElementById("patientId")?.value;
            const leadTime = document.getElementById("globalLeadTime")?.value || "0";

            const response = await fetch("/PatientMedication/SaveGlobalLeadTime", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({ patientId, leadTimeMinutes: leadTime })
            });

            const data = await response.json();
            if (!data?.success) {
                alert(data?.message || "Unable to save lead time.");
            }
        });
    }

    document.querySelectorAll(".pm-lead-save").forEach((button) => {
        button.addEventListener("click", async () => {
            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value;
            const patientId = document.getElementById("patientId")?.value;
            const medicationId = button.getAttribute("data-medication-id");
            const input = document.querySelector(`.pm-lead-input[data-medication-id='${medicationId}']`);
            const leadTime = input?.value || "";

            const response = await fetch("/PatientMedication/SaveMedicationLeadTime", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({ patientId, medicationId, leadTimeMinutes: leadTime === "" ? null : leadTime })
            });

            const data = await response.json();
            if (!data?.success) {
                alert(data?.message || "Unable to save medication lead time.");
            }
        });
    });

    document.querySelectorAll("[data-patient-medications] .pm-delete-med").forEach((button) => {
        button.addEventListener("click", async () => {
            const patientId = document.getElementById("patientId")?.value;
            const card = button.closest("[data-medication-id]");
            const medicationId = card?.getAttribute("data-medication-id");
            if (!patientId || !medicationId) return;

            if (!confirm("Remove this medication from your tracker? Reminders for it will stop.")) return;

            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value;
            const response = await fetch("/PatientMedication/DeleteMedication", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({ patientId: Number(patientId), medicationId: Number(medicationId) })
            });

            const data = await response.json();
            if (data?.success) {
                window.location.reload();
            } else {
                notify(data?.message || "Unable to remove medication.", "error");
            }
        });
    });

    document.querySelectorAll("[data-patient-medications] .pm-card-actions button, [data-patient-medications] .med-card-actions button").forEach((button) => {
        button.addEventListener("click", async () => {
            const card = button.closest(".med-card");
            const targetCard = card || button.closest(".pm-card");
            if (!targetCard) return;

            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value;
            const patientId = document.querySelector("meta[name='patient-id']")?.content || document.getElementById("patientId")?.value;
            const medicationId = targetCard.getAttribute("data-medication-id");
            const scheduledAt = targetCard.getAttribute("data-scheduled-at");
            const status = button.getAttribute("data-action");

            const response = await fetch("/PatientMedication/LogDose", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({ patientId, medicationId, scheduledAt, status })
            });

            const data = await response.json();
            if (data?.success) {
                const statusEl = targetCard.querySelector(".med-status");
                if (statusEl) statusEl.textContent = status;
                notify(`Marked as ${status}.`, "success");
            } else {
                notify(data?.message || "Unable to update medication log.", "error");
            }
        });
    });
});

document.addEventListener("DOMContentLoaded", () => {

    /* ── Helpers ─────────────────────────────────────────── */
    const notify = (message, type = "info") => {
        if (typeof window.showNotification === "function") {
            window.showNotification(message, type);
        } else {
            alert(message);
        }
    };

    const token     = () => document.querySelector("input[name='__RequestVerificationToken']")?.value;
    const patientId = () => document.getElementById("patientId")?.value;

    const postJson = async (url, body) => {
        const res = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token()
            },
            body: JSON.stringify(body)
        });
        return res.json();
    };

    /* ── Wizard (Add.cshtml & Edit.cshtml) ───────────────── */
    const wizardSteps = document.getElementById("wizardSteps");
    if (wizardSteps) {
        let currentStep = 1;
        const totalSteps = wizardSteps.querySelectorAll(".pm-wstep").length;

        const showStep = (n) => {
            // Hide all panels
            document.querySelectorAll(".pm-wiz-panel").forEach(p => p.classList.remove("active"));
            // Show target
            const panel = document.getElementById(`step${n}`);
            if (panel) panel.classList.add("active");

            // Update step indicators
            wizardSteps.querySelectorAll(".pm-wstep").forEach(s => {
                const sNum = parseInt(s.dataset.step);
                s.classList.remove("active", "done");
                if (sNum === n)   s.classList.add("active");
                if (sNum < n)     s.classList.add("done");
            });

            // Update connector lines
            wizardSteps.querySelectorAll(".pm-wconn").forEach((c, i) => {
                c.classList.toggle("done", i + 1 < n);
            });

            currentStep = n;

            // Populate review card when landing on step 5
            if (n === 5) populateReview();

            // Recalculate duration display if landing on step 3
            if (n === 3) updateCalc();
        };

        // Next / Back buttons
        document.querySelectorAll(".pm-wbtn.next").forEach(btn => {
            btn.addEventListener("click", () => {
                const target = parseInt(btn.dataset.next);
                if (currentStep === 1) {
                    const name = document.getElementById("medName")?.value.trim();
                    if (!name) { notify("Please enter the medication name.", "error"); return; }
                }
                showStep(target);
                window.scrollTo({ top: 0, behavior: "smooth" });
            });
        });

        document.querySelectorAll(".pm-wbtn.back").forEach(btn => {
            btn.addEventListener("click", () => {
                showStep(parseInt(btn.dataset.back));
                window.scrollTo({ top: 0, behavior: "smooth" });
            });
        });

        /* ── Frequency chips ──────────────────────────────── */
        document.querySelectorAll("#freqChips .pm-fchip").forEach(chip => {
            chip.addEventListener("click", () => {
                const freq = chip.dataset.freq;
                if (freq === "custom") {
                    document.getElementById("medFrequency").focus();
                    return;
                }
                // Toggle active
                document.querySelectorAll("#freqChips .pm-fchip").forEach(c => c.classList.remove("active"));
                chip.classList.add("active");
                const input = document.getElementById("medFrequency");
                if (input) input.value = freq;
            });
        });

        /* ── Example instruction chips ────────────────────── */
        document.querySelectorAll("#exampleChips .pm-echip").forEach(chip => {
            chip.addEventListener("click", () => {
                const ta = document.getElementById("medInstructions");
                if (!ta) return;
                const ex = chip.dataset.ex;
                ta.value = ta.value ? ta.value + ". " + ex : ex;
                ta.focus();
            });
        });

        /* ── Duration calculation ────────────────────────── */
        const updateCalc = () => {
            const startInput    = document.getElementById("medStart");
            const durationInput = document.getElementById("medDuration");
            const calcRow       = document.getElementById("calcRow");
            const calcEndDate   = document.getElementById("calcEndDate");
            const calcDaysLeft  = document.getElementById("calcDaysLeft");

            if (!startInput || !durationInput || !calcRow) return;

            const startVal    = startInput.value;
            const durationVal = parseInt(durationInput.value);

            if (startVal && !isNaN(durationVal) && durationVal > 0) {
                const start  = new Date(startVal);
                const end    = new Date(start);
                end.setDate(end.getDate() + durationVal);

                const now      = new Date();
                const daysLeft = Math.ceil((end - now) / (1000 * 60 * 60 * 24));

                calcEndDate.textContent  = end.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
                calcDaysLeft.textContent = daysLeft > 0 ? `${daysLeft} days` : "Ended";
                calcRow.style.display   = "flex";
            } else {
                calcRow.style.display = "none";
            }
        };

        ["medStart", "medDuration"].forEach(id => {
            document.getElementById(id)?.addEventListener("input", updateCalc);
        });
        updateCalc();

        /* ── Populate review card ─────────────────────────── */
        const populateReview = () => {
            const get = id => document.getElementById(id)?.value?.trim() || "—";

            const name     = get("medName");
            const type     = document.getElementById("medType")?.value || "—";
            const dosage   = get("medDosage");
            const pills    = get("medPillsPerDose");
            const freq     = get("medFrequency");
            const start    = get("medStart");
            const duration = get("medDuration");
            const total    = get("medTotalPills");
            const instr    = get("medInstructions");

            const set = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val || "—";
            };

            set("rv-name", name);
            set("rv-type", type !== "—" ? type : "—");
            set("rv-dosage", dosage);
            set("rv-pills", pills !== "—" ? pills + " pill(s)" : "—");
            set("rv-freq", freq);
            set("rv-start", start);
            set("rv-duration", duration !== "—" ? duration + " days" : "Ongoing");
            set("rv-total-pills", total !== "—" ? total + " pills" : "—");
            set("rv-instructions", instr);
        };
    }

    /* ── Add Medication Form (step5 submit) ──────────────── */
    document.getElementById("submitAddMed")?.addEventListener("click", async () => {
        const pid = patientId();
        const btn = document.getElementById("submitAddMed");
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving…';

        try {
            const data = await postJson("/PatientMedication/AddMedication", {
                patientId:    pid,
                name:         document.getElementById("medName")?.value || "",
                dosage:       document.getElementById("medDosage")?.value || "",
                frequency:    document.getElementById("medFrequency")?.value || "",
                instructions: document.getElementById("medInstructions")?.value || "",
                startDate:    document.getElementById("medStart")?.value || "",
                durationDays: document.getElementById("medDuration")?.value || "",
                totalPills:   document.getElementById("medTotalPills")?.value || null,
                pillsPerDose: document.getElementById("medPillsPerDose")?.value || null
            });
            if (data?.success) {
                notify("Medication saved!", "success");
                window.location.href = `/PatientMedication/Index/${pid}`;
            } else {
                notify(data?.message || "Unable to save medication.", "error");
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-save"></i> Save Medication';
            }
        } catch {
            notify("Network error. Please try again.", "error");
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-save"></i> Save Medication';
        }
    });

    /* ── Edit Medication Form (step5 submit) ─────────────── */
    document.getElementById("submitEditMed")?.addEventListener("click", async () => {
        const pid = patientId();
        const mid = document.getElementById("medicationId")?.value;
        const btn = document.getElementById("submitEditMed");
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving…';

        try {
            const data = await postJson("/PatientMedication/UpdateMedication", {
                patientId:    pid,
                medicationId: mid,
                name:         document.getElementById("medName")?.value || "",
                dosage:       document.getElementById("medDosage")?.value || "",
                frequency:    document.getElementById("medFrequency")?.value || "",
                instructions: document.getElementById("medInstructions")?.value || "",
                startDate:    document.getElementById("medStart")?.value || "",
                durationDays: document.getElementById("medDuration")?.value || "",
                totalPills:   document.getElementById("medTotalPills")?.value || null,
                pillsPerDose: document.getElementById("medPillsPerDose")?.value || null
            });
            if (data?.success) {
                notify("Medication updated!", "success");
                window.location.href = `/PatientMedication/Index/${pid}`;
            } else {
                notify(data?.message || "Unable to update medication.", "error");
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-save"></i> Save Changes';
            }
        } catch {
            notify("Network error. Please try again.", "error");
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-save"></i> Save Changes';
        }
    });

    /* ── Global lead time reminder chips + save ──────────── */
    document.querySelectorAll(".pm-rc[data-val]").forEach(chip => {
        chip.addEventListener("click", () => {
            document.querySelectorAll(".pm-rc[data-val]").forEach(c => c.classList.remove("active"));
            chip.classList.add("active");
            const input = document.getElementById("globalLeadTime");
            if (input) input.value = chip.dataset.val;
        });
    });

    document.getElementById("saveGlobalLeadTime")?.addEventListener("click", async () => {
        const leadTime = document.getElementById("globalLeadTime")?.value || "0";
        const data = await postJson("/PatientMedication/SaveGlobalLeadTime", {
            patientId: patientId(),
            leadTimeMinutes: leadTime
        });
        if (data?.success) {
            notify("Reminder lead time saved.", "success");
        } else {
            notify(data?.message || "Unable to save lead time.", "error");
        }
    });

    /* ── Per-medication lead time save ───────────────────── */
    document.querySelectorAll(".pm-lead-save").forEach(button => {
        button.addEventListener("click", async () => {
            const medicationId = button.getAttribute("data-medication-id");
            const input = document.querySelector(`.pm-lead-input[data-medication-id='${medicationId}']`);
            const leadTime = input?.value || "";
            const data = await postJson("/PatientMedication/SaveMedicationLeadTime", {
                patientId: patientId(),
                medicationId,
                leadTimeMinutes: leadTime === "" ? null : leadTime
            });
            notify(data?.success ? "Reminder saved." : (data?.message || "Unable to save."), data?.success ? "success" : "error");
        });
    });

    /* ── Delete medication ───────────────────────────────── */
    document.querySelectorAll("[data-patient-medications] .pm-delete-med").forEach(button => {
        button.addEventListener("click", async () => {
            const card = button.closest("[data-medication-id]");
            const medicationId = card?.getAttribute("data-medication-id");
            if (!patientId() || !medicationId) return;
            if (!confirm("Remove this medication from your tracker? Reminders will stop.")) return;

            const data = await postJson("/PatientMedication/DeleteMedication", {
                patientId: Number(patientId()),
                medicationId: Number(medicationId)
            });
            if (data?.success) {
                window.location.reload();
            } else {
                notify(data?.message || "Unable to remove medication.", "error");
            }
        });
    });

    /* ── Expandable medication cards (Index) ─────────────── */
    document.querySelectorAll(".pm-med-card-v2 .pm-med-hdr").forEach(hdr => {
        hdr.addEventListener("click", () => {
            const card = hdr.closest(".pm-med-card-v2");
            card.classList.toggle("open");
        });
    });

    /* ── Tab switching inside expanded medication cards ──── */
    document.querySelectorAll(".pm-med-tab").forEach(tab => {
        tab.addEventListener("click", (e) => {
            e.stopPropagation();
            const card    = tab.closest(".pm-med-card-v2");
            const tabName = tab.dataset.tab;

            card.querySelectorAll(".pm-med-tab").forEach(t => t.classList.remove("active"));
            card.querySelectorAll(".pm-med-tab-panel").forEach(p => p.classList.remove("active"));

            tab.classList.add("active");
            card.querySelector(`[data-panel="${tabName}"]`)?.classList.add("active");
        });
    });

    /* ── Dose logging — Daily page buttons ───────────────── */
    document.querySelectorAll("[data-patient-medications] .pm-dose-btn-v2").forEach(button => {
        button.addEventListener("click", async () => {
            const card = button.closest("[data-medication-id]");
            if (!card) return;

            const medicationId = card.getAttribute("data-medication-id");
            const scheduledAt  = card.getAttribute("data-scheduled-at");
            const status       = button.getAttribute("data-action");
            const notesInput   = card.querySelector(".pm-notes-input");
            const notes        = notesInput?.value?.trim() || null;

            const data = await postJson("/PatientMedication/LogDose", {
                patientId: patientId(),
                medicationId,
                scheduledAt,
                status,
                notes
            });

            if (data?.success) {
                // Update status pill
                const statusEl = card.querySelector(".med-status");
                if (statusEl) {
                    const icons = { Taken: "fa-check-circle", Skipped: "fa-forward", Missed: "fa-times-circle" };
                    const cls   = { Taken: "s-taken", Skipped: "s-skipped", Missed: "s-missed" };
                    statusEl.className = `pm-dose-status-pill ${cls[status] || ""} med-status`;
                    statusEl.innerHTML = `<i class="fas ${icons[status] || "fa-clock"}"></i> ${status}`;
                }

                // Apply card color class
                card.classList.remove("dc-taken", "dc-missed", "dc-skipped");
                if (status === "Taken")   card.classList.add("dc-taken");
                if (status === "Missed")  card.classList.add("dc-missed");
                if (status === "Skipped") card.classList.add("dc-skipped");

                // Animate
                card.classList.add("dose-done");
                setTimeout(() => card.classList.remove("dose-done"), 500);

                notify(`Marked as ${status}.`, "success");
            } else {
                notify(data?.message || "Unable to update medication log.", "error");
            }
        });
    });

    /* ── Dose logging — Index page timeline buttons ──────── */
    document.querySelectorAll("[data-patient-medications] .pm-tl-btn").forEach(button => {
        button.addEventListener("click", async () => {
            const card = button.closest("[data-medication-id]");
            if (!card) return;

            const medicationId = card.getAttribute("data-medication-id");
            const scheduledAt  = card.getAttribute("data-scheduled-at");
            const status       = button.getAttribute("data-action");

            const data = await postJson("/PatientMedication/LogDose", {
                patientId: patientId(),
                medicationId,
                scheduledAt,
                status,
                notes: null
            });

            if (data?.success) {
                // Update badge
                const badge = card.querySelector(".pm-tl-badge");
                if (badge) {
                    const cls   = { Taken: "b-taken", Skipped: "b-skipped" };
                    badge.className = `pm-tl-badge ${cls[status] || "b-scheduled"}`;
                    badge.textContent = status;
                }

                // Update card color
                card.classList.remove("tl-taken", "tl-missed", "tl-skipped");
                if (status === "Taken")   card.classList.add("tl-taken");
                if (status === "Skipped") card.classList.add("tl-skipped");

                // Hide action buttons after logging
                const actions = card.querySelector(".pm-tl-actions");
                if (actions) {
                    actions.style.opacity = "0.4";
                    actions.style.pointerEvents = "none";
                }

                notify(`Marked as ${status}.`, "success");
            } else {
                notify(data?.message || "Unable to update medication log.", "error");
            }
        });
    });

});

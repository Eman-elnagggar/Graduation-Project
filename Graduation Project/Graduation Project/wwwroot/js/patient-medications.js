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

    // Always resolves to a { success, message } shape — a non-JSON body or an error
    // status never throws, so callers can show a real message instead of "network error".
    const postJson = async (url, body) => {
        let res;
        try {
            res = await fetch(url, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token()
                },
                body: JSON.stringify(body)
            });
        } catch {
            return { success: false, message: "Could not reach the server. Check your connection and try again." };
        }

        let data = null;
        try {
            const text = await res.text();
            if (text) data = JSON.parse(text);
        } catch {
            /* non-JSON response — fall through to the status-based message */
        }

        if (!res.ok) {
            return {
                success: false,
                message: data?.message || `The server rejected the request (${res.status}). Please try again.`
            };
        }
        return data ?? { success: false, message: "The server returned an empty response." };
    };

    // Blank optional numbers must go to the server as null, never as "" — an empty
    // string cannot be deserialized into int? and fails the whole request body.
    const numberOrNull = (id) => {
        const raw = document.getElementById(id)?.value?.trim();
        if (!raw) return null;
        const n = Number(raw);
        return Number.isFinite(n) ? n : null;
    };

    const textOrEmpty = (id) => document.getElementById(id)?.value?.trim() || "";

    /* ── Shared dose status rendering (Daily + Index) ────── */
    const setText = (id, val) => {
        const el = document.getElementById(id);
        if (el) el.textContent = val;
    };

    // Daily page card (.pm-dose-card-v2 with a .med-status pill)
    const applyDailyStatus = (card, status) => {
        const statusEl = card.querySelector(".med-status");
        if (statusEl) {
            const icons = { Taken: "fa-check-circle", Skipped: "fa-forward", Missed: "fa-times-circle", Scheduled: "fa-clock" };
            const cls   = { Taken: "s-taken", Skipped: "s-skipped", Missed: "s-missed", Scheduled: "s-scheduled" };
            statusEl.className = `pm-dose-status-pill ${cls[status] || "s-scheduled"} med-status`;
            statusEl.innerHTML = `<i class="fas ${icons[status] || "fa-clock"}"></i> ${status}`;
        }
        card.classList.remove("dc-taken", "dc-missed", "dc-skipped");
        if (status === "Taken")   card.classList.add("dc-taken");
        if (status === "Missed")  card.classList.add("dc-missed");
        if (status === "Skipped") card.classList.add("dc-skipped");
    };

    // Index page timeline card (.meds-dose with a .pm-tl-badge)
    const applyIndexStatus = (card, status) => {
        const badge = card.querySelector(".pm-tl-badge");
        if (badge) {
            const cls = { Taken: "b-taken", Skipped: "b-skipped", Missed: "b-missed", Scheduled: "b-scheduled" };
            badge.className = `pm-tl-badge ${cls[status] || "b-scheduled"}`;
            badge.textContent = status === "Scheduled" ? "Pending" : status;
        }
        card.classList.remove("meds-dose--taken", "meds-dose--missed", "meds-dose--skipped");
        if (status === "Taken")   card.classList.add("meds-dose--taken");
        if (status === "Missed")  card.classList.add("meds-dose--missed");
        if (status === "Skipped") card.classList.add("meds-dose--skipped");
        if (status !== "Scheduled") {
            const actions = card.querySelector(".pm-tl-actions");
            if (actions) {
                actions.style.opacity = "0.4";
                actions.style.pointerEvents = "none";
            }
        }
    };

    // Recompute the Daily page's summary counters + progress bar from the cards.
    const recomputeDailyStats = () => {
        if (!document.querySelector("[data-meds-daily]")) return;
        const cards   = Array.from(document.querySelectorAll(".pm-dose-card-v2"));
        const total   = cards.length;
        const taken   = cards.filter(c => c.classList.contains("dc-taken")).length;
        const missed  = cards.filter(c => c.classList.contains("dc-missed")).length;
        const skipped = cards.filter(c => c.classList.contains("dc-skipped")).length;
        const pending = Math.max(0, total - taken - missed - skipped);
        const pct     = total > 0 ? Math.round(taken / total * 100) : 0;
        setText("pmStatScheduled", total);
        setText("pmStatTaken", taken);
        setText("pmStatPending", pending);
        setText("pmStatMissed", missed);
        setText("pmProgressTaken", taken);
        const fill = document.getElementById("pmProgressFill");
        if (fill) fill.style.width = pct + "%";
    };

    // Apply an incoming status change to whichever cards match this dose.
    const syncDoseCards = (medicationId, scheduledAt, status) => {
        const mid = String(medicationId);
        const at  = new Date(scheduledAt).getTime();
        document.querySelectorAll(".pm-dose-card-v2[data-medication-id]").forEach(card => {
            if (card.getAttribute("data-medication-id") !== mid) return;
            if (new Date(card.getAttribute("data-scheduled-at")).getTime() === at) {
                applyDailyStatus(card, status);
            }
        });
        document.querySelectorAll(".meds-dose[data-medication-id]").forEach(card => {
            if (card.getAttribute("data-medication-id") !== mid) return;
            if (new Date(card.getAttribute("data-scheduled-at")).getTime() === at) {
                applyIndexStatus(card, status);
            }
        });
        recomputeDailyStats();
    };

    /* ── Real-time sync via SignalR ──────────────────────── */
    const medsRoot = document.querySelector("[data-patient-medications]");
    if (medsRoot && patientId()) {
        const SIGNALR_CDNS = [
            "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js",
            "https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js",
            "https://unpkg.com/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js"
        ];
        const loadScript = (src) => new Promise(resolve => {
            const s = document.createElement("script");
            s.src = src;
            s.async = true;
            s.onload = () => resolve(true);
            s.onerror = () => resolve(false);
            document.head.appendChild(s);
        });
        (async () => {
            if (!window.signalR) {
                for (const url of SIGNALR_CDNS) {
                    await loadScript(url);
                    if (window.signalR) break;
                }
            }
            if (!window.signalR) return; // graceful: clicks still update locally

            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/medicationHub")
                .withAutomaticReconnect()
                .build();

            connection.on("DoseUpdated", (payload) => {
                if (!payload) return;
                syncDoseCards(payload.medicationId, payload.scheduledAt, payload.status);
            });

            try {
                await connection.start();
            } catch {
                /* ignore — local updates keep working */
            }
        })();
    }

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

        /* ── Field-level validation ───────────────────────── */
        const setFieldError = (id, message) => {
            const box   = document.querySelector(`.pm-ferror[data-error-for="${id}"]`);
            const input = document.getElementById(id);
            if (box) {
                box.textContent = message || "";
                box.classList.toggle("show", !!message);
            }
            input?.classList.toggle("has-error", !!message);
        };

        const clearErrors = () =>
            document.querySelectorAll(".pm-ferror").forEach(b => {
                b.textContent = "";
                b.classList.remove("show");
                document.getElementById(b.dataset.errorFor)?.classList.remove("has-error");
            });

        // A positive whole number, or blank. Blank is always allowed — these fields
        // are optional and must not block the save.
        const validateOptionalCount = (id, label) => {
            const raw = document.getElementById(id)?.value?.trim();
            if (!raw) { setFieldError(id, ""); return true; }

            const n = Number(raw);
            if (!Number.isInteger(n) || n < 1) {
                setFieldError(id, `${label} must be a whole number of 1 or more, or left blank.`);
                return false;
            }
            setFieldError(id, "");
            return true;
        };

        const validateStep = (step) => {
            if (step === 1) {
                const name = textOrEmpty("medName");
                if (!name) {
                    setFieldError("medName", "Please enter the medication name.");
                    document.getElementById("medName")?.focus();
                    return false;
                }
                setFieldError("medName", "");
                return true;
            }

            if (step === 2) {
                const okPills = validateOptionalCount("medPillsPerDose", "Pills per dose");
                const freq = readFrequency();
                if (freq.timesPerDay > 0 && freq.times.some(t => !t)) {
                    setFieldError("medFrequency", "Please fill in every dose time.");
                    return false;
                }
                setFieldError("medFrequency", "");
                return okPills;
            }

            if (step === 3) {
                const okDuration = validateOptionalCount("medDuration", "Duration");
                const okTotal    = validateOptionalCount("medTotalPills", "Total pills");
                return okDuration && okTotal;
            }

            return true;
        };

        // Next / Back buttons
        document.querySelectorAll(".pm-wbtn.next").forEach(btn => {
            btn.addEventListener("click", () => {
                if (!validateStep(currentStep)) return;
                showStep(parseInt(btn.dataset.next));
                window.scrollTo({ top: 0, behavior: "smooth" });
            });
        });

        document.querySelectorAll(".pm-wbtn.back").forEach(btn => {
            btn.addEventListener("click", () => {
                showStep(parseInt(btn.dataset.back));
                window.scrollTo({ top: 0, behavior: "smooth" });
            });
        });

        /* ── Frequency ────────────────────────────────────────
           The <option>s carry the schedule from the server-side catalogue
           (MedicationFrequencies), so the two never drift apart. Picking one
           renders an editable time input per dose. */
        const freqSelect   = document.querySelector("[data-frequency-select]");
        const freqCustom   = document.getElementById("freqCustom");
        const freqTimes    = document.getElementById("freqTimes");
        const freqSummary  = document.getElementById("freqSummary");
        const freqPerDay   = document.getElementById("freqTimesPerDay");
        const freqInterval = document.getElementById("freqIntervalDays");

        const selectedOption = () => freqSelect?.selectedOptions?.[0] ?? null;
        const isCustom = () => selectedOption()?.value === "custom";

        const clampInt = (value, min, max, fallback) => {
            const n = parseInt(value, 10);
            if (!Number.isFinite(n)) return fallback;
            return Math.min(Math.max(n, min), max);
        };

        // Reads the chosen frequency plus whatever times are currently in the inputs.
        const readFrequency = () => {
            const opt = selectedOption();
            if (!opt) return { code: "once-daily", label: "Once daily", timesPerDay: 1, intervalDays: 1, times: ["09:00"] };

            const custom      = opt.value === "custom";
            const timesPerDay = custom
                ? clampInt(freqPerDay?.value, 1, 8, 1)
                : parseInt(opt.dataset.timesPerDay, 10) || 0;
            const intervalDays = custom
                ? clampInt(freqInterval?.value, 1, 90, 1)
                : parseInt(opt.dataset.intervalDays, 10) || 1;

            const times = Array.from(freqTimes?.querySelectorAll(".pm-time-input") || [])
                .map(i => i.value)
                .slice(0, timesPerDay);

            return { code: opt.value, label: opt.dataset.label || opt.textContent.trim(), timesPerDay, intervalDays, times };
        };

        // Spreads N doses across a waking day — mirrors MedicationFrequencies.SpreadEvenly.
        const spreadEvenly = (count) => {
            if (count <= 1) return ["09:00"];
            const first = 8, last = 22;
            const step  = (last - first) / (count - 1);
            return Array.from({ length: count }, (_, i) =>
                `${String(Math.round(first + step * i) % 24).padStart(2, "0")}:00`);
        };

        const describe = (freq, times) => {
            if (freq.timesPerDay === 0)
                return "No doses are scheduled — log a dose whenever you take it.";

            const doses  = `${freq.timesPerDay} dose${freq.timesPerDay === 1 ? "" : "s"}`;
            const repeat = freq.intervalDays === 1
                ? "every day"
                : freq.intervalDays === 7
                    ? "once a week"
                    : `every ${freq.intervalDays} days`;
            const at = times.filter(Boolean).length ? ` at ${times.filter(Boolean).join(", ")}` : "";
            return `${doses} ${repeat}${at}.`;
        };

        // Re-renders the time inputs for the current selection, keeping any times the
        // user already typed (and, on the Edit page, the medication's saved times).
        const renderFrequency = ({ preserve = true } = {}) => {
            if (!freqSelect || !freqTimes) return;

            const opt    = selectedOption();
            const custom = isCustom();
            if (freqCustom) freqCustom.hidden = !custom;

            const freq = readFrequency();

            let previous = preserve
                ? Array.from(freqTimes.querySelectorAll(".pm-time-input")).map(i => i.value).filter(Boolean)
                : [];

            // First render on the Edit page: seed from the medication's saved schedule.
            const initial = freqTimes.dataset.initialTimes;
            if (previous.length === 0 && initial) {
                previous = initial.split(",").map(t => t.trim()).filter(Boolean);
                freqTimes.dataset.initialTimes = "";
            }

            let defaults = custom
                ? spreadEvenly(freq.timesPerDay)
                : (opt?.dataset.times || "").split(",").map(t => t.trim()).filter(Boolean);

            if (defaults.length !== freq.timesPerDay)
                defaults = spreadEvenly(freq.timesPerDay);

            // Keep the user's own times only while the dose count is unchanged;
            // switching to a different frequency should adopt that frequency's times.
            const useDefaults = previous.length !== freq.timesPerDay;
            const times = Array.from({ length: freq.timesPerDay }, (_, i) =>
                (useDefaults ? defaults[i] : previous[i]) || defaults[i] || "09:00");

            freqTimes.innerHTML = times.length === 0
                ? ""
                : `<div class="pm-time-list-lbl">Dose times</div>` +
                  times.map((t, i) => `
                    <label class="pm-time-slot">
                        <span>Dose ${i + 1}</span>
                        <input type="time" class="pm-finput pm-time-input" value="${t}" required />
                    </label>`).join("");

            if (freqSummary) freqSummary.textContent = describe(freq, times);
            setFieldError("medFrequency", "");
        };

        freqSelect?.addEventListener("change", () => renderFrequency({ preserve: false }));
        freqPerDay?.addEventListener("input", () => renderFrequency({ preserve: true }));
        freqInterval?.addEventListener("input", () => renderFrequency({ preserve: true }));
        freqTimes?.addEventListener("change", () => {
            const freq = readFrequency();
            const times = Array.from(freqTimes.querySelectorAll(".pm-time-input")).map(i => i.value);
            if (freqSummary) freqSummary.textContent = describe(freq, times);
        });
        renderFrequency({ preserve: true });

        ["medPillsPerDose", "medDuration", "medTotalPills"].forEach(id => {
            document.getElementById(id)?.addEventListener("input", () => setFieldError(id, ""));
        });
        document.getElementById("medName")?.addEventListener("input", () => setFieldError("medName", ""));

        // Everything the Add/Edit endpoints need, with blanks sent as null.
        window.pmCollectMedicationPayload = () => {
            const freq = readFrequency();
            return {
                patientId:      Number(patientId()),
                name:           textOrEmpty("medName"),
                dosage:         textOrEmpty("medDosage"),
                form:           textOrEmpty("medType") || null,
                frequencyCode:  freq.code,
                frequencyLabel: freq.label,
                timesPerDay:    freq.timesPerDay,
                intervalDays:   freq.intervalDays,
                times:          freq.times.filter(Boolean),
                instructions:   textOrEmpty("medInstructions"),
                startDate:      textOrEmpty("medStart") || null,
                durationDays:   numberOrNull("medDuration"),
                totalPills:     numberOrNull("medTotalPills"),
                pillsPerDose:   numberOrNull("medPillsPerDose")
            };
        };

        // Run every step's rules before saving, and jump back to the first bad one.
        window.pmValidateAllSteps = () => {
            clearErrors();
            for (const step of [1, 2, 3]) {
                if (!validateStep(step)) {
                    showStep(step);
                    window.scrollTo({ top: 0, behavior: "smooth" });
                    return false;
                }
            }
            return true;
        };

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

            const freq     = readFrequency();
            const name     = get("medName");
            const type     = document.getElementById("medType")?.value || "—";
            const dosage   = get("medDosage");
            const pills    = get("medPillsPerDose");
            const start    = get("medStart");
            const duration = get("medDuration");
            const total    = get("medTotalPills");
            const instr    = get("medInstructions");

            const set = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val || "—";
            };

            const repeat = freq.intervalDays === 1 ? "" : ` · every ${freq.intervalDays} days`;

            set("rv-name", name);
            set("rv-type", type || "—");
            set("rv-dosage", dosage);
            set("rv-pills", pills !== "—" ? pills + " pill(s)" : "—");
            set("rv-freq", freq.label + repeat);
            set("rv-times", freq.times.filter(Boolean).join(", ") || "No scheduled doses");
            set("rv-start", start);
            set("rv-duration", duration !== "—" ? duration + " days" : "Ongoing");
            set("rv-total-pills", total !== "—" ? total + " pills" : "—");
            set("rv-instructions", instr);
        };
    }

    /* ── Add / Edit submit (step 5) ──────────────────────── */
    const wireMedicationSubmit = (buttonId, url, { savingLabel, idleLabel, successMessage, extra = () => ({}) }) => {
        const btn = document.getElementById(buttonId);
        if (!btn) return;

        btn.addEventListener("click", async () => {
            if (typeof window.pmValidateAllSteps === "function" && !window.pmValidateAllSteps()) {
                notify("Please fix the highlighted fields.", "error");
                return;
            }

            const pid = patientId();
            btn.disabled = true;
            btn.innerHTML = `<i class="fas fa-spinner fa-spin"></i> ${savingLabel}`;

            const payload = { ...window.pmCollectMedicationPayload(), ...extra() };
            const data = await postJson(url, payload);

            if (data?.success) {
                notify(successMessage, "success");
                window.location.href = `/PatientMedication/Index/${pid}`;
                return;
            }

            notify(data?.message || "Unable to save medication.", "error");
            btn.disabled = false;
            btn.innerHTML = idleLabel;
        });
    };

    wireMedicationSubmit("submitAddMed", "/PatientMedication/AddMedication", {
        savingLabel: "Saving…",
        idleLabel: '<i class="fas fa-save"></i> Save Medication',
        successMessage: "Medication saved!"
    });

    wireMedicationSubmit("submitEditMed", "/PatientMedication/UpdateMedication", {
        savingLabel: "Saving…",
        idleLabel: '<i class="fas fa-save"></i> Save Changes',
        successMessage: "Medication updated!",
        extra: () => ({ medicationId: Number(document.getElementById("medicationId")?.value) })
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
        const leadTime = numberOrNull("globalLeadTime");
        if (leadTime === null || leadTime < 0) {
            notify("Enter a reminder lead time in minutes (0 or more).", "error");
            return;
        }
        const data = await postJson("/PatientMedication/SaveGlobalLeadTime", {
            patientId: Number(patientId()),
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
            const raw = input?.value?.trim() || "";
            const leadTime = raw === "" ? null : Number(raw);

            if (leadTime !== null && (!Number.isFinite(leadTime) || leadTime < 0)) {
                notify("Enter a lead time in minutes (0 or more), or leave it blank.", "error");
                return;
            }

            const data = await postJson("/PatientMedication/SaveMedicationLeadTime", {
                patientId: Number(patientId()),
                medicationId: Number(medicationId),
                leadTimeMinutes: leadTime
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
                patientId: Number(patientId()),
                medicationId: Number(medicationId),
                scheduledAt,
                status,
                notes
            });

            if (data?.success) {
                applyDailyStatus(card, status);
                recomputeDailyStats();

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
                patientId: Number(patientId()),
                medicationId: Number(medicationId),
                scheduledAt,
                status,
                notes: null
            });

            if (data?.success) {
                applyIndexStatus(card, status);
                notify(`Marked as ${status}.`, "success");
            } else {
                notify(data?.message || "Unable to update medication log.", "error");
            }
        });
    });

});

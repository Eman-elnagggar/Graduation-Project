/* ════════════════════════════════════════════════════════════════
   NABD · Lab Test Analysis — guided AI health journey
   Workflow engine: upload → OCR review → AI analysis report.
   Configuration (URLs, approved doctors) is injected via
   window.NABD_TESTS by the view.
════════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    const TOKEN = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const CONFIG = window.NABD_TESTS || {};
    const APPROVED_DOCTORS = CONFIG.approvedDoctors || [];
    const UPLOAD_REPORT_URL = CONFIG.uploadReportUrl || '';
    const SEND_REPORT_URL   = CONFIG.sendReportUrl || '';
    // Tracks which DOM elements to read when generating a PDF (null = main report view)
    let _reportSourceOpts = null;
    let nextClientId = 1;

    /* ══════════════════════════════════════════════════════
       TEST CONFIGURATIONS
    ══════════════════════════════════════════════════════ */
    const testConfigurations = {
        cbc: {
            name: 'CBC (Complete Blood Count)',
            parameters: [
                { key: 'hb',              name: 'Hemoglobin (HB)', unit: 'g/dL',         normalRange: '12–16'           },
                { key: 'wbc',             name: 'WBC Count',        unit: '/µL',           normalRange: '4000–11000'      },
                { key: 'rbcs_count',      name: 'RBC Count',        unit: 'million/µL',    normalRange: '4.2–5.4'         },
                { key: 'mcv',             name: 'MCV',              unit: 'fL',            normalRange: '80–100'          },
                { key: 'mch',             name: 'MCH',              unit: 'pg',            normalRange: '27–33'           },
                { key: 'rdw',             name: 'RDW',              unit: '%',             normalRange: '11.5–15'         },
                { key: 'mchc',            name: 'MCHC',             unit: 'g/dL',          normalRange: '32–36'           },
                { key: 'lymphocytes',     name: 'Lymphocytes',      unit: '%',             normalRange: '20–40'           },
                { key: 'platelet_count',  name: 'Platelets',        unit: '/µL',           normalRange: '150000–400000'   }
            ]
        },
        urinalysis: {
            name: 'Urinalysis',
            parameters: [
                { key: 'color',            name: 'Color',            unit: '',        normalRange: 'Light Yellow'  },
                { key: 'ph',               name: 'pH',               unit: '',        normalRange: '4.5–8.0'       },
                { key: 'specific_gravity', name: 'Specific Gravity', unit: '',        normalRange: '1.005–1.030'   },
                { key: 'protein',          name: 'Protein',          unit: '',        normalRange: 'Negative'      },
                { key: 'glucose',          name: 'Glucose',          unit: '',        normalRange: 'Negative'      },
                { key: 'ketones',          name: 'Ketones',          unit: '',        normalRange: 'Negative'      },
                { key: 'blood',            name: 'Blood',            unit: '',        normalRange: 'Negative'      },
                { key: 'rbcs',             name: 'RBCs',             unit: '/HPF',    normalRange: '0–5'           },
                { key: 'leukocytes',       name: 'Leukocytes',       unit: '/HPF',    normalRange: '0–5'           },
                { key: 'nitrite',          name: 'Nitrite',          unit: '',        normalRange: 'Negative'      }
            ]
        },
        tsh: {
            name: 'TSH (Thyroid)',
            parameters: [
                { key: 'tsh', name: 'TSH', unit: 'mIU/L', normalRange: '0.4–4.0' }
            ]
        },
        ferritin: {
            name: 'Ferritin',
            parameters: [
                { key: 'ferritin_value', name: 'Ferritin', unit: 'ng/mL', normalRange: '12–150' }
            ]
        },
        fbg: {
            name: 'Fasting Blood Glucose',
            parameters: [
                { key: 'fbg', name: 'FBG', unit: 'mg/dL', normalRange: '70–100' }
            ]
        },
        hba1c: {
            name: 'HbA1c (Sugar Test)',
            parameters: [
                { key: 'hba1c', name: 'HbA1c', unit: '%', normalRange: '4.0–5.6' }
            ]
        },
        hcv: {
            name: 'HCV (Hepatitis C)',
            parameters: [
                { key: 'hcv', name: 'HCV', unit: '', normalRange: 'Non-Reactive' }
            ]
        },
        hbsag: {
            name: 'HBsAg (Hepatitis B)',
            parameters: [
                { key: 'hbsag', name: 'HBsAg', unit: '', normalRange: 'Non-Reactive' }
            ]
        },
        bloodgroup: {
            name: 'Blood Group',
            parameters: [
                { key: 'abo_group', name: 'ABO Group', unit: '', normalRange: 'A/B/AB/O' },
                { key: 'rh_factor', name: 'Rh Factor', unit: '', normalRange: '+/–'       }
            ]
        }
    };

    async function apiExtractValues(testType, file) {
        const formData = new FormData();
        formData.append('testType', testType);
        formData.append('image', file);

        const response = await fetchWithTimeout('/api/analysis/ocr-only', {
            method: 'POST',
            body: formData
        }, 45000);

        if (!response.ok) {
            const error = await safeReadError(response);
            const status = response.status;
            if (status === 502 || status === 503 || status === 504) {
                throw new Error('The OCR service is temporarily unavailable. Please try again in a moment.');
            }
            throw new Error(error || 'Failed to extract values from the uploaded image. Make sure it is a clear, readable lab test document.');
        }

        const data = await response.json();

        return {
            id: 'temp-' + (nextClientId++),
            tempImagePath: data.tempImagePath,
            testType,
            testTypeName: data.testName || testConfigurations[testType].name,
            fileName: file.name,
            confidence: data.confidence ?? '—',
            extractedValues: data.extractedValues || {}
        };
    }

    async function apiAnalyzeTests(tests) {
        const patientId = parseInt(document.body.dataset.patientId || '0');
        if (!patientId) throw new Error('Patient profile not found. Please refresh and try again.');

        const payload = {
            patientId,
            tests: tests.map(t => ({
                tempImagePath: t.tempImagePath || null,
                testType: t.testType,
                testName: t.testTypeName,
                confirmedValues: t.extractedValues || {}
            }))
        };

        const response = await fetchWithTimeout('/api/analysis/batch-submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        }, 60000);

        if (!response.ok) {
            const error = await safeReadError(response);
            const status = response.status;
            if (status === 504) throw new Error('The submission timed out. The server may be busy — please try again.');
            if (status === 502 || status === 503) throw new Error('The analysis service is temporarily unavailable. Please try again in a moment.');
            throw new Error(error || 'Failed to submit tests for analysis.');
        }

        const data = await response.json();
        const labTestId = data.labTestId ?? data.LabTestId;
        if (!labTestId) throw new Error('Server did not return a test ID after submission.');

        // Persist so user can close and resume later
        try {
            localStorage.setItem('nabd_pending_analysis', JSON.stringify({
                labTestId,
                reportId: data.reportId ?? data.ReportId,
                submittedAt: Date.now()
            }));
        } catch { /* storage unavailable */ }

        return waitForAnalysisResult(labTestId);
    }

    async function waitForAnalysisResult(labTestId) {
        // The submit service (HF space) can intermittently fail and is retried server-side for
        // up to ~90s, so poll long enough to outlast those retries. Returns early as soon as the
        // status becomes Completed or Failed, so a fast analysis is unaffected.
        const maxAttempts = 75;
        for (let attempt = 0; attempt < maxAttempts; attempt++) {
            const response = await fetchWithTimeout(`/api/analysis/${labTestId}`, {}, 15000);
            if (!response.ok) {
                const error = await safeReadError(response);
                throw new Error(error || 'Failed to fetch analysis status.');
            }
            const data = await response.json();
            if (data.status === 'Completed') {
                return mapAnalysisResponse(data);
            }
            if (data.status === 'Failed') {
                throw new Error(data.error || data.message || 'Analysis failed. Please go back, check your values, and try again.');
            }
            await delay(2000);
        }
        throw new Error('Analysis is taking too long. Please try again later.');
    }

    async function fetchWithTimeout(url, options, timeoutMs) {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), timeoutMs);
        try {
            return await fetch(url, { ...options, signal: controller.signal });
        } finally {
            clearTimeout(timeout);
        }
    }

    async function safeReadError(response) {
        try {
            const data = await response.json();
            return data.error || data.message || null;
        } catch {
            return null;
        }
    }

    function mapAnalysisResponse(data) {
        const tests = data.tests || [];
        let hasHigh = false;
        let hasLow = false;
        tests.forEach(test => {
            if (hasSubmitDiagnosisResults(test)) {
                Object.keys(test).forEach(key => {
                    if (isMetadataKey(key)) return;
                    if (key.toLowerCase() === 'other_diagnoses') return; // handled separately
                    const cls = getSubmitDiagnosisClass(getSubmitDiagnosisStatus(test[key]));
                    if (cls === 'high') hasHigh = true;
                    if (cls === 'warning') hasLow = true;
                });
            }
        });

        // Extract risk from result_3 (can be object or array)
        const riskRaw = data.riskPrediction;
        let riskObj = {};
        if (riskRaw && typeof riskRaw === 'object' && !Array.isArray(riskRaw)) {
            riskObj = riskRaw;
        } else if (Array.isArray(riskRaw)) {
            for (let i = riskRaw.length - 1; i >= 0; i--) {
                if (riskRaw[i] && typeof riskRaw[i] === 'object' && Object.keys(riskRaw[i]).length > 0) { riskObj = riskRaw[i]; break; }
            }
        }

        const riskText = String(riskObj.risk_level || '').toLowerCase();
        if (riskText.includes('high')) hasHigh = true;
        if (riskText.includes('moderate') || riskText.includes('medium')) hasLow = true;

        const verdict = hasHigh ? 'danger' : hasLow ? 'warning' : 'safe';
        const overall = hasHigh ? 'Abnormal Values Detected'
            : hasLow ? 'Some Values Below Normal'
                : 'All Values Normal';

        return {
            verdict,
            overall,
            personalInfo: data.personalInfo || null,
            confidence: riskObj.confidence ?? '—',
            diabetesStatus: riskObj.diabetes_status || null,
            recommendations: data.alerts || [],
            tests,
            riskLevel: riskObj.risk_level || null,
            report: data.report || null
        };
    }

    function resolveTestConfig(testName) {
        if (!testName) return null;
        const normalized = testName.toLowerCase();
        return Object.values(testConfigurations).find(cfg => cfg.name.toLowerCase() === normalized)
            || Object.values(testConfigurations).find(cfg => normalized.includes(cfg.name.toLowerCase()));
    }

    /* ══════════════════════════════════════════════════════
       STATE
    ══════════════════════════════════════════════════════ */
    let queuedFiles  = [];   // { file, testType }
    let uploadedTests = [];  // extracted + { id, approved, reportId }

    /* ══════════════════════════════════════════════════════
       DOM REFS
    ══════════════════════════════════════════════════════ */
    const uploadZone           = document.getElementById('uploadZone');
    const fileInput            = document.getElementById('fileInput');
    const testTypeSelect       = document.getElementById('testType');
    const sendToAIBtn          = document.getElementById('sendToAIBtn');
    const addMoreBtn           = document.getElementById('addMoreTests');
    const fileCountBadge       = document.getElementById('fileCountBadge');
    const queuedFilesList      = document.getElementById('queuedFilesList');
    const queuedFilesInner     = document.getElementById('queuedFilesInner');

    const uploadedTestsSection = document.getElementById('uploadedTestsSection');
    const uploadedTestsList    = document.getElementById('uploadedTestsList');
    const extractionProgress   = document.getElementById('extractionProgress');
    const extractionBarFill    = document.getElementById('extractionBarFill');
    const extractionStatus     = document.getElementById('extractionStatus');
    const submitRow            = document.getElementById('submitRow');
    const submitAllBtn         = document.getElementById('submitAllTests');
    const approvalCounter      = document.getElementById('approvalCounter');
    const clearAllBtn          = document.getElementById('clearAllTests');

    const comprehensiveResults = document.getElementById('comprehensiveResults');
    const reportLoading        = document.getElementById('reportLoading');
    const reportReady          = document.getElementById('reportReady');
    const loadingStepsEl       = document.getElementById('loadingSteps');
    const reportBarFill        = document.getElementById('reportBarFill');
    const overallConfidence    = document.getElementById('overallConfidence');
    const reportVerdictRow     = document.getElementById('reportVerdictRow');
    const reportContent        = document.getElementById('comprehensiveReportContent');
    const uploadMoreBtn        = document.getElementById('uploadMoreBtn');

    /* ══════════════════════════════════════════════════════
       PROGRESS STEPPER
    ══════════════════════════════════════════════════════ */
    function setStep(n) {
        [1, 2, 3].forEach(i => {
            const el = document.getElementById('step' + i);
            if (!el) return;
            el.classList.remove('active', 'completed');
            if (i < n)  el.classList.add('completed');
            if (i === n) el.classList.add('active');
            // Show checkmark vs number
            const num   = el.querySelector('.tu-step-num');
            const check = el.querySelector('.tu-step-check');
            if (num)   num.style.display   = i < n ? 'none' : '';
            if (check) check.style.display = i < n ? 'inline' : 'none';
        });
        const c1 = document.getElementById('conn1');
        const c2 = document.getElementById('conn2');
        if (c1) c1.classList.toggle('active', n >= 2);
        if (c2) c2.classList.toggle('active', n >= 3);
    }

    /* ══════════════════════════════════════════════════════
       TEST TYPE VALIDATION
    ══════════════════════════════════════════════════════ */
    function showTestTypeError() {
        testTypeSelect.classList.add('lsel--error');
        const fg = testTypeSelect.closest('.lfg');
        document.getElementById('testTypeHint').style.display = 'none';
        document.getElementById('testTypeError').style.display = 'block';
        if (fg) {
            fg.classList.remove('lfg--shake');
            void fg.offsetWidth; // reflow to restart animation
            fg.classList.add('lfg--shake');
        }
        testTypeSelect.focus();
    }

    function clearTestTypeError() {
        testTypeSelect.classList.remove('lsel--error');
        document.getElementById('testTypeHint').style.display = 'block';
        document.getElementById('testTypeError').style.display = 'none';
    }

    testTypeSelect.addEventListener('change', () => {
        if (testTypeSelect.value) clearTestTypeError();
    });

    /* ══════════════════════════════════════════════════════
       UPLOAD ZONE
    ══════════════════════════════════════════════════════ */
    uploadZone.addEventListener('click', () => {
        if (!testTypeSelect.value) { showTestTypeError(); return; }
        fileInput.click();
    });
    uploadZone.addEventListener('dragover',  e => { e.preventDefault(); uploadZone.classList.add('drag-over'); });
    uploadZone.addEventListener('dragleave', () => uploadZone.classList.remove('drag-over'));
    uploadZone.addEventListener('drop', e => {
        e.preventDefault(); uploadZone.classList.remove('drag-over');
        if (e.dataTransfer.files.length) enqueueFiles(e.dataTransfer.files);
    });
    fileInput.addEventListener('change', () => {
        if (fileInput.files.length) enqueueFiles(fileInput.files);
        fileInput.value = '';
    });
    addMoreBtn.addEventListener('click', () => {
        if (!testTypeSelect.value) { showTestTypeError(); return; }
        fileInput.click();
    });

    function enqueueFiles(files) {
        const type = testTypeSelect.value;
        if (!type) { showTestTypeError(); return; }
        Array.from(files).forEach(f => {
            if (f.size > 10 * 1024 * 1024) { notify('"' + f.name + '" exceeds 10 MB — maximum file size is 10 MB.', 'error'); return; }
            queuedFiles.push({ file: f, testType: type });
        });
        renderQueue();
    }

    function renderQueue() {
        if (!queuedFiles.length) {
            queuedFilesList.style.display = 'none';
            sendToAIBtn.style.display     = 'none';
            return;
        }
        queuedFilesList.style.display = 'block';
        sendToAIBtn.style.display     = 'inline-flex';
        fileCountBadge.textContent    = queuedFiles.length;

        queuedFilesInner.innerHTML = '';
        queuedFiles.forEach((item, idx) => {
            const div = document.createElement('div');
            div.className = 'tu-queue-item';
            div.innerHTML =
                '<span class="tu-queue-item-name"><i class="fas fa-file-image"></i>'
                + '<span>' + escHtml(item.file.name) + '</span>'
                + '<span style="font-size:.7rem;color:var(--tu-muted);margin-left:.2rem">' + Math.round(item.file.size / 1024) + ' KB</span>'
                + '</span>'
                + '<span class="tu-queue-type">' + escHtml(testConfigurations[item.testType].name) + '</span>'
                + '<button class="tu-queue-remove" type="button" data-idx="' + idx + '" title="Remove"><i class="fas fa-times"></i></button>';

            div.querySelector('.tu-queue-remove').addEventListener('click', function () {
                queuedFiles.splice(parseInt(this.dataset.idx), 1);
                renderQueue();
            });
            queuedFilesInner.appendChild(div);
        });
    }

    /* ══════════════════════════════════════════════════════
       STEP 1 → 2  :  SEND TO AI (OCR EXTRACTION)
    ══════════════════════════════════════════════════════ */
    sendToAIBtn.addEventListener('click', async () => {
        if (!queuedFiles.length) return;
        if (!document.body.dataset.patientId || document.body.dataset.patientId === '0') {
            notify('Patient profile not found. Please refresh and try again.', 'error');
            return;
        }
        const batch = [...queuedFiles];
        queuedFiles = [];
        renderQueue();

        // Transition: hide step 1 panel, show step 2
        const sUp = document.getElementById('section-upload');
        if (sUp) sUp.style.display = 'none';
        uploadedTestsSection.style.display = 'block';
        window.scrollTo({ top: 0, behavior: 'smooth' });
        setStep(2);
        extractionProgress.style.display = 'block';
        submitRow.style.display = 'none';

        sendToAIBtn.disabled = true;
        sendToAIBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Extracting…';

        let extractionErrors = 0;
        for (let i = 0; i < batch.length; i++) {
            const pct = Math.round((i / batch.length) * 90);
            extractionBarFill.style.width = pct + '%';
            extractionStatus.textContent  = 'Reading "' + batch[i].file.name + '" (' + (i + 1) + ' of ' + batch.length + ')…';

            try {
                const result = await apiExtractValues(batch[i].testType, batch[i].file);
                result.approved = false;
                uploadedTests.push(result);
                renderTestCard(result);
            } catch (err) {
                extractionErrors++;
                renderErrorCard(batch[i].file.name, batch[i].testType, err?.message || 'Failed to extract values from this image.');
            }
        }

        extractionBarFill.style.width = '100%';
        if (extractionErrors > 0 && uploadedTests.length === 0) {
            extractionStatus.textContent = 'All ' + batch.length + ' image(s) failed. Please check the files and try again.';
        } else if (extractionErrors > 0) {
            extractionStatus.textContent = (batch.length - extractionErrors) + ' of ' + batch.length + ' image(s) processed. ' + extractionErrors + ' failed — see errors below.';
        } else {
            extractionStatus.textContent = 'Extraction complete — review the values below and approve each test.';
        }
        await delay(700);
        extractionProgress.style.display = 'none';
        if (uploadedTests.length > 0) submitRow.style.display = 'flex';
        updateApprovalCounter();

        sendToAIBtn.disabled = false;
        sendToAIBtn.innerHTML = '<i class="fas fa-wand-magic-sparkles"></i> Analyze <span class="lbadge" id="fileCountBadge">0</span>';
    });

    /* ══════════════════════════════════════════════════════
       RENDER REVIEW CARD
    ══════════════════════════════════════════════════════ */
    function renderErrorCard(fileName, testType, errorMessage) {
        const card = document.createElement('div');
        card.className = 'tu-test-card tu-test-card--error';
        const typeName = testConfigurations[testType]?.name || testType;
        card.innerHTML =
            '<div class="tu-card-hd">'
            + '<div class="tu-card-title"><i class="fas fa-exclamation-circle" style="color:#e53935;margin-right:.35rem"></i>'
            + escHtml(typeName) + ' — Extraction Failed</div>'
            + '<span class="tu-badge-status high"><i class="fas fa-times"></i> Failed</span>'
            + '</div>'
            + '<div class="tu-error-body">'
            + '<p class="tu-error-msg"><i class="fas fa-info-circle"></i> ' + escHtml(errorMessage) + '</p>'
            + '<p class="tu-error-file">File: ' + escHtml(fileName) + '</p>'
            + '<p style="margin-top:.4rem;font-size:.78rem;color:var(--tu-muted)">Please upload a clearer, well-lit image of the lab report and try again.</p>'
            + '</div>';
        uploadedTestsList.appendChild(card);
    }

    function renderTestCard(testData) {
        document.getElementById('test-' + testData.id)?.remove();
        const cfg  = testConfigurations[testData.testType];
        if (!cfg) {
            notify('Unknown test type "' + (testData.testType || '') + '". Please select the correct test type before uploading.', 'error');
            return;
        }
        const card = document.createElement('div');
        card.className = 'tu-test-card' + (testData.approved ? ' approved' : '');
        card.id = 'test-' + testData.id;

        let rows = '';
        cfg.parameters.forEach(param => {
            const val = extractValue(testData.extractedValues, param.key) ?? '';
            rows += '<tr>'
                + '<td style="font-weight:600">' + escHtml(param.name) + '</td>'
                + '<td><input class="tu-value-input" type="text" data-param="' + escHtml(param.key) + '"'
                + ' value="' + escHtml(String(val)) + '"'
                + (testData.approved ? ' disabled' : '') + '>'
                + '<span class="tu-value-unit">' + escHtml(param.unit) + '</span></td>'
                + '</tr>';
        });

        const approvedBadge = testData.approved
            ? '<span class="tu-badge-status success"><i class="fas fa-check-circle"></i> Approved</span>'
            : '<span class="tu-badge-status pending"><i class="fas fa-clock"></i> Pending Review</span>';

        const footerBtns = testData.approved
            ? '<button class="tu-btn tu-btn-sm" data-action="unapprove" type="button"><i class="fas fa-undo"></i> Edit Again</button>'
            : '<button class="tu-btn tu-btn-sm" data-action="edit" type="button"><i class="fas fa-edit"></i> Edit Values</button>'
              + '<button class="tu-btn tu-btn-sm approve" data-action="approve" type="button"><i class="fas fa-check"></i> Approve</button>';

        const confRaw = parseFloat(testData.confidence);
        // API returns confidence as 0–1 decimal; normalize to 0–100 for threshold comparison
        const confPct = !isNaN(confRaw) ? (confRaw <= 1 ? confRaw * 100 : confRaw) : null;
        const confWarn = (confPct !== null && confPct < 70)
            ? '<div class="lconf-warn"><i class="fas fa-exclamation-triangle"></i> Low AI confidence (' + confPct.toFixed(1) + '%) — please verify all values carefully before approving.</div>'
            : '';

        card.innerHTML =
            '<div class="tu-test-card-header">'
            + '<div><h4><i class="fas fa-file-medical"></i> ' + escHtml(testData.testTypeName) + '</h4>'
            + '<div class="tu-test-meta">'
            + '<span><i class="fas fa-image"></i> ' + escHtml(testData.fileName) + '</span>'
            + '<span class="tu-confidence-pill"><i class="fas fa-brain"></i> AI Confidence: ' + testData.confidence + '%</span>'
            + '</div></div>'
            + '<div class="tu-test-actions">' + approvedBadge
            + '<button class="tu-btn tu-btn-sm" data-action="remove" title="Remove" type="button" style="color:#c62828;border-color:#ef9a9a"><i class="fas fa-trash"></i></button>'
            + '</div></div>'
            + confWarn
            + '<div class="tu-values-table-wrap"><table class="tu-values-table">'
            + '<thead><tr><th>Parameter</th><th>Value</th></tr></thead>'
            + '<tbody>' + rows + '</tbody></table></div>'
            + '<div class="tu-test-card-footer">' + footerBtns + '</div>';

        uploadedTestsList.appendChild(card);
    }

    function findUploadedTest(testId) {
        const id = String(testId);
        return uploadedTests.find(t => String(t.id) === id) || null;
    }

    function handleCardAction(action, testId) {
        const test = findUploadedTest(testId);
        if (!test) {
            notify('Could not find this test. Please refresh the page and try again.', 'error');
            return;
        }

        const cardRoot = document.getElementById('test-' + test.id);
        const cardId = cardRoot ? ('test-' + test.id) : null;

        if (action === 'edit') {
            if (cardId) {
                document.querySelectorAll('#' + cardId + ' .tu-value-input').forEach(i => i.disabled = false);
            }
            notify('Values unlocked — edit them, then click Approve.', 'info');
            return;
        }
        if (action === 'approve') {
            if (cardId) {
                document.querySelectorAll('#' + cardId + ' .tu-value-input').forEach(input => {
                    test.extractedValues[input.dataset.param] = input.value;
                });
            }
            test.approved = true;
            renderTestCard(test);
            updateApprovalCounter();
            notify(test.testTypeName + ' approved. Approve all tests, then submit for analysis.', 'success');
            return;
        }
        if (action === 'unapprove') {
            test.approved = false;
            renderTestCard(test);
            updateApprovalCounter();
            return;
        }
        if (action === 'remove') {
            if (!confirm('Remove this test?')) return;
            uploadedTests = uploadedTests.filter(t => String(t.id) !== String(testId));
            document.getElementById('test-' + test.id)?.remove();
            updateApprovalCounter();
            if (!uploadedTests.length) {
                submitRow.style.display            = 'none';
                uploadedTestsSection.style.display = 'none';
            }
        }
    }

    if (uploadedTestsList) {
        uploadedTestsList.addEventListener('click', e => {
            const btn = e.target.closest('[data-action]');
            if (!btn || !uploadedTestsList.contains(btn)) return;
            const card = btn.closest('.tu-test-card');
            if (!card || !card.id.startsWith('test-')) return;
            const testId = card.id.slice('test-'.length);
            handleCardAction(btn.dataset.action, testId);
        });
    }

    function updateApprovalCounter() {
        const total    = uploadedTests.length;
        const approved = uploadedTests.filter(t => t.approved).length;
        if (approvalCounter) approvalCounter.textContent = approved + ' / ' + total + ' approved';
        const allReady = total > 0 && approved === total;
        submitAllBtn.disabled  = !allReady;
        submitAllBtn.innerHTML = allReady
            ? '<i class="fas fa-wand-magic-sparkles"></i> Generate AI Analysis'
            : '<i class="fas fa-lock"></i> Approve all tests to continue';
    }

    /* ══════════════════════════════════════════════════════
       CLEAR ALL
    ══════════════════════════════════════════════════════ */
    clearAllBtn.addEventListener('click', () => {
        if (!confirm('Remove all uploaded tests?')) return;
        uploadedTests = [];
        uploadedTestsList.innerHTML = '';
        submitRow.style.display             = 'none';
        uploadedTestsSection.style.display  = 'none';
        comprehensiveResults.style.display  = 'none';
        const sUp2 = document.getElementById('section-upload');
        if (sUp2) sUp2.style.display = 'block';
        setStep(1);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    /* ══════════════════════════════════════════════════════
       STEP 2 → 3  :  SUBMIT FOR ANALYSIS
    ══════════════════════════════════════════════════════ */
    function showAnalysisError(message) {
        reportLoading.style.display = 'none';
        reportReady.style.display = 'none';
        overallConfidence.style.display = 'none';
        const errorSection = document.getElementById('reportError');
        if (errorSection) {
            document.getElementById('reportErrorMsg').textContent = message;
            errorSection.style.display = 'block';
        } else {
            notify(message, 'error');
        }
        submitAllBtn.disabled = false;
        submitAllBtn.innerHTML = '<i class="fas fa-wand-magic-sparkles"></i> Generate AI Analysis';
    }

    document.getElementById('reportErrorBackBtn')?.addEventListener('click', () => {
        const errorSection = document.getElementById('reportError');
        if (errorSection) errorSection.style.display = 'none';
        comprehensiveResults.style.display = 'none';
        uploadedTestsSection.style.display = 'block';
        setStep(2);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    document.getElementById('reportErrorRetryBtn')?.addEventListener('click', () => {
        const errorSection = document.getElementById('reportError');
        if (errorSection) errorSection.style.display = 'none';
        submitAllBtn.click();
    });

    submitAllBtn.addEventListener('click', async () => {
        if (!uploadedTests.every(t => t.approved)) return;

        // Transition: hide step 2 panel, show step 3
        uploadedTestsSection.style.display = 'none';
        comprehensiveResults.style.display = 'block';
        reportLoading.style.display        = 'block';
        reportReady.style.display          = 'none';
        const errorSection = document.getElementById('reportError');
        if (errorSection) errorSection.style.display = 'none';
        overallConfidence.style.display    = 'none';
        window.scrollTo({ top: 0, behavior: 'smooth' });
        setStep(3);
        submitAllBtn.disabled  = true;
        submitAllBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Submitting…';

        if (loadingStepsEl) loadingStepsEl.innerHTML = '';
        if (reportBarFill) reportBarFill.style.width = '0%';

        let analysis;
        try {
            analysis = await apiAnalyzeTests(uploadedTests);
        } catch (err) {
            try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
            showAnalysisError(err?.message || 'Failed to analyze tests. Please try again.');
            return;
        }

        try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
        generateComprehensiveReport(analysis);
        overallConfidence.innerHTML = '<i class="fas fa-brain"></i> Confidence: ' + analysis.confidence + '%';
        refreshPreviousTests();

        document.getElementById('reportSubtitle').textContent =
            uploadedTests.length + ' test(s) · ' + new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' });
    });

    /* ══════════════════════════════════════════════════════
       GENERATE REPORT HTML
    ══════════════════════════════════════════════════════ */
    function generateComprehensiveReport(analysis) {
        const iconMap = { safe: 'fa-check-circle', warning: 'fa-exclamation-circle', danger: 'fa-exclamation-triangle' };

        reportVerdictRow.innerHTML =
            '<span class="tu-verdict-badge ' + analysis.verdict + '">'
            + '<i class="fas ' + iconMap[analysis.verdict] + '"></i> ' + escHtml(analysis.overall)
            + '</span>'
            + '<p class="tu-verdict-summary">' + uploadedTests.length + ' test(s) analyzed &mdash; ' + escHtml(analysis.riskLevel || 'see detailed breakdown below') + '</p>';

        let html = '';

        // ── Personal Information (result_1) ──
        if (analysis.personalInfo) {
            const pi = analysis.personalInfo;
            const piFields = [
                { label: 'Name', value: pi.name, icon: 'fa-user' },
                { label: 'Age', value: pi.age, icon: 'fa-birthday-cake', suffix: ' years' },
                { label: 'Trimester', value: pi.trimester, icon: 'fa-baby' },
                { label: 'Week', value: pi.week, icon: 'fa-calendar-week' },
                { label: 'Baby Gender', value: pi.baby_gender, icon: 'fa-venus-mars' },
                { label: 'Height', value: pi.height, icon: 'fa-ruler-vertical', suffix: ' cm' },
                { label: 'Weight', value: pi.weight, icon: 'fa-weight', suffix: ' kg' },
                { label: 'RBS Avg', value: pi.rbs_avg, icon: 'fa-tint', suffix: ' mg/dL' },
                { label: 'BP', value: (pi.avg_systolic && pi.avg_diastolic) ? pi.avg_systolic + '/' + pi.avg_diastolic : null, icon: 'fa-heartbeat', suffix: ' mmHg' },
                { label: 'Parity', value: pi.parity, icon: 'fa-child' },
                { label: 'DG State', value: pi.dg_state, icon: 'fa-notes-medical' },
                { label: 'Risk State', value: pi.risk_state, icon: 'fa-shield-alt' }
            ];
            let piChips = '';
            piFields.forEach(f => {
                if (f.value == null || f.value === '') return;
                piChips += '<div class="tu-pi-chip">'
                    + '<span class="tu-pi-chip-label">' + escHtml(f.label) + '</span>'
                    + '<span class="tu-pi-chip-value">' + escHtml(String(f.value)) + (f.suffix || '') + '</span>'
                    + '</div>';
            });
            html += '<div class="tu-report-section"><h5 class="tu-section-h"><i class="fas fa-id-card"></i> Patient Information</h5>'
                + '<div class="tu-pi-strip">' + piChips + '</div></div>';
        }

        // ── Analysis Summary ──
        html += '<div class="tu-report-summary">'
            + '<h4><i class="fas fa-chart-line"></i> Analysis Summary</h4>'
            + '<p>Based on <strong>' + (analysis.tests?.length || uploadedTests.length) + '</strong> approved test(s). Review each section below.</p></div>';

        // ── Test Results (result_2) ──
        const reportTests = analysis.tests && analysis.tests.length
            ? analysis.tests.map(t => ({ testTypeName: t.test_name, extractedValues: t }))
            : uploadedTests;
        reportTests.forEach(test => {
            const cfg = testConfigurations[test.testType] || resolveTestConfig(test.testTypeName);
            if (!cfg && !hasSubmitDiagnosisResults(test.extractedValues)) return;
            const confVal = test.extractedValues?.confidence;
            let cards = '';
            if (hasSubmitDiagnosisResults(test.extractedValues)) {
                cards = renderSubmitDiagnosisCards(test.extractedValues);
            } else {
            cfg.parameters.forEach(p => {
                const val = extractValue(test.extractedValues, p.key) ?? '—';
                cards += '<div class="tu-insight-card normal">'
                    + '<div class="tu-insight-label">' + escHtml(p.name) + '</div>'
                    + '<div class="tu-insight-value"><span class="tu-insight-dot"></span>' + escHtml(String(val))
                    + (p.unit ? '<span class="tu-insight-unit">' + escHtml(p.unit) + '</span>' : '') + '</div></div>';
            });
            }
            html += '<div class="tu-report-section"><h5 class="tu-section-h"><i class="fas fa-vial"></i> ' + escHtml(test.testTypeName || cfg?.name || 'Lab Test')
                + (confVal ? '<span class="tu-confidence-pill" style="margin-left:.75rem"><i class="fas fa-brain"></i> ' + (parseFloat(confVal) * 100).toFixed(0) + '% confidence</span>' : '')
                + '</h5><div class="tu-insight-grid">' + cards + '</div></div>';
        });

        // ── AI Medical Report (result_4) ──
        if (analysis.report) {
            html += '<div class="tu-report-section tu-ai-report-section"><h5 class="tu-section-h"><i class="fas fa-file-medical-alt"></i> AI Medical Report</h5>'
                + '<div class="tu-ai-block"><div class="tu-ai-block-body">' + formatReportText(analysis.report) + '</div></div></div>';
        }

        reportContent.innerHTML = html;
        reportLoading.style.display = 'none';
        reportReady.style.display = 'block';
        overallConfidence.style.display = 'inline-flex';
    }

    function formatReportText(text) {
        if (!text) return '';
        return text.split('\n').map(line => {
            line = line.trim();
            if (!line) return '';
            if (line.startsWith('*')) return '<li class="tu-report-bullet"><i class="fas fa-angle-right"></i>' + escHtml(line.replace(/^\*\s*/, '')) + '</li>';
            if (line.endsWith(':') && line.length < 120) return '<h6 class="tu-report-heading">' + escHtml(line) + '</h6>';
            return '<p class="tu-report-para">' + escHtml(line) + '</p>';
        }).join('');
    }

    /* ══════════════════════════════════════════════════════
       UPLOAD MORE (reset to step 1)
    ══════════════════════════════════════════════════════ */

    /* Shared PDF generation — returns a Blob.
       opts: { contentElId, verdictElId, title } — defaults to main report view */
    async function generateReportBlob(opts) {
        const contentElId = opts?.contentElId || 'comprehensiveReportContent';
        const verdictElId = opts?.verdictElId || 'reportVerdictRow';
        const title       = opts?.title       || 'Comprehensive AI Analysis Report';

        const reportEl  = document.getElementById(contentElId);
        const verdictEl = verdictElId ? document.getElementById(verdictElId) : null;
        if (!reportEl) throw new Error('Report not found.');

        const wrapper = document.createElement('div');
        wrapper.style.cssText = 'background:#fff;padding:24px 28px;font-family:sans-serif;width:820px;position:absolute;left:-9999px;top:0';

        const header = document.createElement('div');
        header.style.cssText = 'border-bottom:2px solid #e8bcd4;padding-bottom:12px;margin-bottom:18px';
        header.innerHTML = '<h2 style="color:#c2185b;margin:0;font-size:18px">' + escHtml(title) + '</h2>'
            + '<p style="color:#888;margin:4px 0 0;font-size:12px">Generated: ' + new Date().toLocaleDateString() + '</p>';
        wrapper.appendChild(header);

        if (verdictEl && verdictEl.innerHTML.trim())
            wrapper.appendChild(verdictEl.cloneNode(true));
        wrapper.appendChild(reportEl.cloneNode(true));
        document.body.appendChild(wrapper);

        const canvas = await html2canvas(wrapper, { scale: 1.5, useCORS: true, logging: false });
        document.body.removeChild(wrapper);

        const { jsPDF } = window.jspdf;
        const pdf   = new jsPDF({ orientation: 'p', unit: 'mm', format: 'a4' });
        const pageW = pdf.internal.pageSize.getWidth();
        const pageH = pdf.internal.pageSize.getHeight();
        const imgW  = pageW;
        const imgH  = canvas.height * imgW / canvas.width;
        const imgData = canvas.toDataURL('image/jpeg', 0.92);

        let y = 0, remaining = imgH;
        pdf.addImage(imgData, 'JPEG', 0, y, imgW, imgH);
        remaining -= pageH;
        while (remaining > 0) {
            y -= pageH;
            pdf.addPage();
            pdf.addImage(imgData, 'JPEG', 0, y, imgW, imgH);
            remaining -= pageH;
        }
        return pdf.output('blob');
    }

    /* Download report as PDF */
    document.getElementById('downloadReportBtn')?.addEventListener('click', async () => {
        const btn = document.getElementById('downloadReportBtn');
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Generating PDF…';
        try {
            const blob = await generateReportBlob();
            const url  = URL.createObjectURL(blob);
            const a    = document.createElement('a');
            a.href = url;
            a.download = 'Lab-Analysis-Report.pdf';
            a.click();
            URL.revokeObjectURL(url);
        } catch (err) {
            console.error(err);
            notify('Could not generate PDF. Please try again.', 'error');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-download"></i> Download Report';
        }
    });

    /* Share with Doctor — generate PDF and send via chat */
    function openShareModal() {
        const select = document.getElementById('shareReportDoctorSelect');
        if (select) {
            select.innerHTML = APPROVED_DOCTORS.map(d =>
                '<option value="' + escAttr(d.userId) + '">' + escHtml(d.name) + '</option>'
            ).join('');
        }
        document.getElementById('shareReportModal').style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }

    function closeShareModal() {
        document.getElementById('shareReportModal').style.display = 'none';
        document.body.style.overflow = '';
    }

    document.getElementById('shareReportModalClose')?.addEventListener('click', closeShareModal);
    document.getElementById('shareReportCancelBtn')?.addEventListener('click', closeShareModal);
    document.getElementById('shareReportModal')?.addEventListener('click', function (e) {
        if (e.target === this) closeShareModal();
    });

    document.getElementById('shareWithDoctorBtn')?.addEventListener('click', () => {
        _reportSourceOpts = null; // use main report view
        if (!APPROVED_DOCTORS.length) {
            notify('No approved doctor linked to your account. Please connect with a doctor first.', 'error');
            return;
        }
        if (APPROVED_DOCTORS.length === 1) {
            sendReportToDoctor(APPROVED_DOCTORS[0]);
            return;
        }
        openShareModal();
    });

    document.getElementById('shareReportConfirmBtn')?.addEventListener('click', () => {
        const select = document.getElementById('shareReportDoctorSelect');
        const uid    = select?.value;
        const doctor = APPROVED_DOCTORS.find(d => d.userId === uid);
        if (!doctor) return;
        closeShareModal();
        sendReportToDoctor(doctor);
    });

    async function sendReportToDoctor(doctor) {
        const shareBtn = document.getElementById('shareWithDoctorBtn');
        shareBtn.disabled = true;
        shareBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending…';
        const sourceOpts = _reportSourceOpts;
        try {
            const blob     = await generateReportBlob(sourceOpts);
            const fileName = 'Lab-Analysis-Report.pdf';

            const fd = new FormData();
            fd.append('file', new File([blob], fileName, { type: 'application/pdf' }));
            fd.append('__RequestVerificationToken', TOKEN);
            const uploadResp = await fetch(UPLOAD_REPORT_URL, { method: 'POST', body: fd, credentials: 'same-origin' });
            if (!uploadResp.ok) throw new Error('Upload failed.');
            const { url } = await uploadResp.json();

            const sendResp = await fetch(SEND_REPORT_URL, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': TOKEN },
                body: JSON.stringify({ doctorUserId: doctor.userId, attachmentUrl: url, fileName, caption: 'Lab Analysis Report' })
            });
            if (!sendResp.ok) throw new Error('Send failed.');
            notify('Report sent to ' + escHtml(doctor.name) + ' successfully!', 'success');
        } catch (err) {
            console.error(err);
            notify('Could not send report. Please try again.', 'error');
        } finally {
            shareBtn.disabled = false;
            shareBtn.innerHTML = '<i class="fas fa-share-alt"></i> Share with Doctor';
        }
    }

    function escAttr(s) {
        return String(s ?? '').replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/'/g, '&#39;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    uploadMoreBtn.addEventListener('click', () => {
        uploadedTests = [];
        uploadedTestsList.innerHTML = '';
        uploadedTestsSection.style.display = 'none';
        comprehensiveResults.style.display = 'none';
        submitRow.style.display            = 'none';
        const sUp3 = document.getElementById('section-upload');
        if (sUp3) sUp3.style.display = 'block';
        setStep(1);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    /* ══════════════════════════════════════════════════════
       PENDING ANALYSIS BANNER
    ══════════════════════════════════════════════════════ */
    document.getElementById('dismissPendingBanner')?.addEventListener('click', () => {
        document.getElementById('pendingAnalysisBanner').style.display = 'none';
        try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
    });

    async function resumePendingAnalysis(pending) {
        document.getElementById('pendingAnalysisBanner').style.display = 'none';
        const sUp = document.getElementById('section-upload');
        if (sUp) sUp.style.display = 'none';
        uploadedTestsSection.style.display = 'none';
        comprehensiveResults.style.display = 'block';
        reportLoading.style.display = 'block';
        reportReady.style.display = 'none';
        const errSec = document.getElementById('reportError');
        if (errSec) errSec.style.display = 'none';
        overallConfidence.style.display = 'none';
        setStep(3);
        window.scrollTo({ top: 0, behavior: 'smooth' });

        try {
            const response = await fetchWithTimeout('/api/analysis/' + pending.labTestId, {}, 15000);
            if (!response.ok) throw new Error('Could not check analysis status.');
            const data = await response.json();

            if (data.status === 'Completed') {
                const analysis = mapAnalysisResponse(data);
                try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
                generateComprehensiveReport(analysis);
                overallConfidence.innerHTML = '<i class="fas fa-brain"></i> Confidence: ' + analysis.confidence + '%';
                refreshPreviousTests();
                document.getElementById('reportSubtitle').textContent =
                    'Resumed from previous session · ' + new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' });
            } else if (data.status === 'Failed') {
                try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
                const errMsg = data.error || data.message || 'Analysis failed. Please upload your tests again.';
                showAnalysisError(errMsg);
            } else {
                // Still processing — continue polling
                try {
                    const analysis = await waitForAnalysisResult(pending.labTestId);
                    try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
                    generateComprehensiveReport(analysis);
                    overallConfidence.innerHTML = '<i class="fas fa-brain"></i> Confidence: ' + analysis.confidence + '%';
                    refreshPreviousTests();
                    document.getElementById('reportSubtitle').textContent =
                        'Resumed from previous session · ' + new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' });
                } catch (pollErr) {
                    try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
                    showAnalysisError(pollErr?.message || 'Analysis timed out. Please upload your tests again.');
                }
            }
        } catch (err) {
            try { localStorage.removeItem('nabd_pending_analysis'); } catch {}
            showAnalysisError(err?.message || 'Could not resume analysis. Please upload your tests again.');
        }
    }

    // Check for pending analysis on page load
    (function checkPendingAnalysis() {
        try {
            const raw = localStorage.getItem('nabd_pending_analysis');
            if (!raw) return;
            const pending = JSON.parse(raw);
            if (!pending?.labTestId) { localStorage.removeItem('nabd_pending_analysis'); return; }
            const ageHours = (Date.now() - (pending.submittedAt || 0)) / 3600000;
            if (ageHours > 24) { localStorage.removeItem('nabd_pending_analysis'); return; }
            const banner = document.getElementById('pendingAnalysisBanner');
            const msg = document.getElementById('pendingAnalysisBannerMsg');
            if (banner) {
                if (msg) {
                    const mins = Math.round((Date.now() - (pending.submittedAt || 0)) / 60000);
                    const timeAgo = mins < 2 ? 'just now' : mins < 60 ? mins + ' minutes ago' : Math.floor(mins / 60) + ' hour(s) ago';
                    msg.textContent = 'Analysis submitted ' + timeAgo + '. Resuming automatically…';
                }
                banner.style.display = 'block';
                setTimeout(() => resumePendingAnalysis(pending), 1500);
            }
        } catch { /* ignore storage errors */ }
    })();

    /* ══════════════════════════════════════════════════════
       REFRESH PREVIOUS TESTS TABLE
    ══════════════════════════════════════════════════════ */
    function getStatusClass(s) {
        const l = (s || '').toLowerCase();
        if (l === 'completed' || l === 'reviewed') return 'reviewed';
        if (l === 'failed') return 'failed';
        return 'pending';
    }

    function getResultLabel(r) {
        const l = (r || '').toLowerCase();
        if (l === 'all values normal')        return { text: 'Normal',   cls: 'normal'  };
        if (l === 'normal')                   return { text: 'Normal',   cls: 'normal'  };
        if (l === 'abnormal values detected') return { text: 'Abnormal', cls: 'high'    };
        if (l === 'abnormal')                 return { text: 'Abnormal', cls: 'high'    };
        if (l === 'some values below normal') return { text: 'Attention',cls: 'warning' };
        if (l === 'requires attention')       return { text: 'Attention',cls: 'warning' };
        if (l === 'attention')                return { text: 'Attention',cls: 'warning' };
        if (!r) return { text: '—', cls: 'pending' };
        return { text: r, cls: 'pending' };
    }

    async function refreshPreviousTests() {
        const patientId = document.body.dataset.patientId || '0';
        if (patientId === '0') return;
        try {
            const resp = await fetch('/api/analysis/patient/' + patientId + '/tests');
            if (!resp.ok) return;
            const sessions = await resp.json();
            const tbody = document.getElementById('previousTestsBody');
            if (!tbody) return;

            if (!sessions.length) {
                tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:2rem;color:var(--tu-muted);">'
                    + '<i class="fas fa-flask" style="font-size:1.6rem;display:block;margin-bottom:.5rem;opacity:.4;"></i>'
                    + 'No previous tests found.</td></tr>';
                return;
            }

            tbody.innerHTML = sessions.map(t => {
                const names     = Array.isArray(t.testNames) && t.testNames.length ? t.testNames : [t.displayName || 'Unknown'];
                const badgesHtml = '<div class="tu-test-names-cell">'
                    + names.map(n => '<span class="tu-test-badge-js">' + escHtml(n) + '</span>').join('')
                    + '</div>';

                const stCls = getStatusClass(t.status);
                const stTxt = (t.status === 'Completed') ? 'Reviewed' : (t.status || 'Pending');
                const res   = getResultLabel(t.overallStatus);

                const images = Array.isArray(t.images) ? t.images : [];
                  const imagesBtn = images.length
                      ? '<button class="tu-btn tu-btn-sm tu-btn-view-images"'
                        + ' data-images="' + escHtml(JSON.stringify(images)) + '"'
                        + ' data-test-name="' + escHtml(t.displayName || names.join(' · ')) + '"'
                        + ' data-test-date="' + escHtml(t.uploadDate) + '"'
                        + ' type="button"><i class="fas fa-images"></i> Images'
                        + (images.length > 1 ? ' (' + images.length + ')' : '')
                        + '</button>'
                      : '';
                  const reportBtn = t.hasReport && t.labTestId
                      ? '<button class="tu-btn tu-btn-sm tu-btn-view-report"'
                        + ' data-test-id="' + t.labTestId + '"'
                        + ' data-test-name="' + escHtml(t.displayName || names.join(' · ')) + '"'
                        + ' data-test-date="' + escHtml(t.uploadDate) + '"'
                        + ' data-result="' + escHtml(res.text) + '"'
                        + ' data-result-class="' + res.cls + '"'
                        + ' type="button"><i class="fas fa-eye"></i> Report</button>'
                      : '<span class="tu-pending-label"><i class="fas fa-clock"></i> Pending</span>';

                  return '<tr>'
                      + '<td><span class="tu-date"><i class="fas fa-calendar-alt"></i> ' + escHtml(t.uploadDate) + '</span></td>'
                      + '<td>' + badgesHtml + '</td>'
                      + '<td><span class="tu-badge-status ' + stCls + '">' + escHtml(stTxt) + '</span></td>'
                      + '<td><span class="tu-badge-status ' + res.cls + '">' + escHtml(res.text) + '</span></td>'
                      + '<td style="text-align:center"><div class="tu-action-cell">' + imagesBtn + reportBtn + '</div></td>'
                      + '</tr>';
            }).join('');
        } catch (e) {
            // silently ignore refresh errors
        }
    }

    /* ══════════════════════════════════════════════════════
       HELPERS
    ══════════════════════════════════════════════════════ */
    function renderSubmitDiagnosisCards(test) {
        // Collect any other_diagnoses entries first for a special section at the bottom
        const diagnosisEntries = [];
        const fieldKeys = Object.keys(test || {}).filter(key => {
            if (isMetadataKey(key)) return false;
            if (key.toLowerCase() === 'other_diagnoses') {
                // Collect diagnoses to render separately
                const val = test[key];
                if (Array.isArray(val)) val.forEach(d => { if (d) diagnosisEntries.push(String(d)); });
                else if (val) diagnosisEntries.push(String(val));
                return false;
            }
            return true;
        });

        let cards = fieldKeys.map(key => {
            const value = test[key];
            const status = getSubmitDiagnosisStatus(value);
            const detail = getSubmitDiagnosisDetail(value);
            const cls = getSubmitDiagnosisClass(status);
            // Extract normal range and measured value from the API detail string
            const normalRange = extractNormalRangeFromDetail(detail);
            const measuredVal = extractMeasuredValueFromDetail(detail);
            return '<div class="tu-insight-card ' + cls + '">'
                + '<div class="tu-insight-label">' + escHtml(toDisplayLabel(key)) + '</div>'
                + '<div class="tu-insight-value"><span class="tu-insight-dot"></span>' + escHtml(status || 'Normal')
                + (measuredVal ? '<span class="tu-insight-unit" style="margin-left:.3rem;opacity:.75">' + escHtml(measuredVal) + '</span>' : '')
                + '</div>'
                + (normalRange ? '<div class="tu-insight-range">Normal: ' + escHtml(normalRange) + '</div>' : (detail ? '<div class="tu-insight-detail">' + escHtml(detail) + '</div>' : ''))
                + '</div>';
        }).join('');

        // Render other_diagnoses as a highlighted banner
        if (diagnosisEntries.length) {
            cards += '<div class="tu-insight-card high" style="grid-column:1/-1;flex-direction:row;align-items:flex-start;gap:.7rem;">'
                + '<div style="flex-shrink:0;margin-top:.1rem"><i class="fas fa-exclamation-triangle" style="color:#c62828"></i></div>'
                + '<div><div class="tu-insight-label">Additional Diagnoses</div>'
                + diagnosisEntries.map(d => '<div class="tu-insight-value" style="margin-top:.25rem"><span class="tu-insight-dot"></span>' + escHtml(d) + '</div>').join('')
                + '</div></div>';
        }

        return cards;
    }

    function hasSubmitDiagnosisResults(test) {
        return Object.keys(test || {}).some(key => !isMetadataKey(key) && Array.isArray(test[key]));
    }

    function getSubmitDiagnosisStatus(value) {
        return Array.isArray(value) ? String(value[0] ?? '') : String(value ?? '');
    }

    function getSubmitDiagnosisDetail(value) {
        if (!Array.isArray(value)) return '';
        if (value.length === 1 && Array.isArray(value[0])) return value[0].join(', ');
        return value.slice(1).flat().filter(Boolean).join(' ');
    }

    /**
     * Extracts the normal range from an API detail string.
     * e.g. "The value 12.5 is within the normal range 12-15 g/dl" → "12-15 g/dl"
     * e.g. "The value 5 not in the normal range 4.6 – 8.0" → "4.6 – 8.0"
     */
    function extractNormalRangeFromDetail(detail) {
        if (!detail) return '';
        const m = detail.match(/normal\s+range\s+([^\n]+?)(?:\s*$)/i);
        return m ? m[1].trim() : '';
    }

    /**
     * Extracts the measured value from an API detail string.
     * e.g. "The value 12.5 is within..." → "12.5"
     * e.g. "The value amber yellow within..." → "amber yellow"
     */
    function extractMeasuredValueFromDetail(detail) {
        if (!detail) return '';
        const m = detail.match(/^The value\s+(.+?)\s+(?:is\s+)?(?:within|not in)/i);
        return m ? m[1].trim() : '';
    }

    function getSubmitDiagnosisClass(status) {
        const s = String(status || '').toLowerCase();
        if (!s || s === 'normal') return 'normal';
        // Warnings / mild abnormalities
        if (s.includes('trace') || s.includes('elevated') || s.includes('mild') ||
            s.includes('moderate') || s.includes('borderline') || s.includes('low specific')) return 'warning';
        // Low / below
        if (s.includes('low') || s.includes('below') || s.includes('deficiency') ||
            s.includes('anemia') || s.includes('acidic') || s.includes('alkaline')) return 'warning';
        // Severe / high / positive / infection
        if (s.includes('high') || s.includes('positive') || s.includes('detected') ||
            s.includes('infection') || s.includes('hematuria') || s.includes('thrombocytopenia') ||
            s.includes('concentrated') || s.includes('leukocyt') || s.includes('bacteriuria') ||
            s.includes('abnormal')) return 'high';
        return 'high'; // Default unknown abnormal to high
    }

    function isMetadataKey(key) {
        return ['test_name', 'confidence'].includes(String(key || '').toLowerCase());
    }

    function toDisplayLabel(key) {
        return String(key || '')
            .replace(/_/g, ' ')
            .replace(/\b\w/g, c => c.toUpperCase());
    }

    function extractValue(source, key) {
        if (!source) return null;
        const match = Object.keys(source).find(k => k.toLowerCase() === key.toLowerCase());
        const value = match ? source[match] : null;
        if (Array.isArray(value)) {
            return value[0] ?? null;
        }
        return value;
    }

    function delay(ms) { return new Promise(r => setTimeout(r, ms)); }

    function notify(msg, type) {
        if (typeof window.showNotification === 'function') {
            window.showNotification(msg, type);
            return;
        }
        if (typeof window.showToast === 'function') {
            window.showToast(msg, type || 'info');
            return;
        }
        if (type === 'error') {
            alert(msg);
            return;
        }
        console.log('[notify]', type || 'info', msg);
    }

    function escHtml(str) {
        return String(str ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function normalizeAssetUrl(path) {
        const raw = String(path ?? '').trim();
        if (!raw) return '';
        if (/^https?:\/\//i.test(raw) || raw.startsWith('data:') || raw.startsWith('blob:')) return raw;
        const normalized = raw.replace(/\\/g, '/');
        return normalized.startsWith('/') ? normalized : ('/' + normalized);
    }

    /* ══════════════════════════════════════════════════════
       REPORT MODAL LOGIC
    ══════════════════════════════════════════════════════ */
    const tuModal          = document.getElementById('tuReportModal');
    const tuModalClose     = document.getElementById('tuModalClose');
    const tuModalCloseFooter = document.getElementById('tuModalCloseFooter');
    const tuModalTitle     = document.getElementById('tuModalTitle');
    const tuModalSubtitle  = document.getElementById('tuModalSubtitle');
    const tuModalResultBadge = document.getElementById('tuModalResultBadge');
    const tuModalLoading   = document.getElementById('tuModalLoading');
    const tuModalContent   = document.getElementById('tuModalContent');
    const tuModalError     = document.getElementById('tuModalError');
    const tuModalErrorMsg  = document.getElementById('tuModalErrorMsg');
    const tuModalVerdictRow = document.getElementById('tuModalVerdictRow');
    const tuModalReportContent = document.getElementById('tuModalReportContent');

    function openModal() {
        tuModal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        requestAnimationFrame(() => tuModal.querySelector('.tu-modal').focus?.());
    }

    function closeModal() {
        tuModal.style.display = 'none';
        document.body.style.overflow = '';
    }

    const tuModalDownloadBtn = document.getElementById('tuModalDownloadBtn');
    const tuModalShareBtn    = document.getElementById('tuModalShareBtn');

    function showModalLoading() {
        tuModalLoading.style.display  = 'flex';
        tuModalContent.style.display  = 'none';
        tuModalError.style.display    = 'none';
        if (tuModalDownloadBtn) tuModalDownloadBtn.style.display = 'none';
        if (tuModalShareBtn)    tuModalShareBtn.style.display    = 'none';
    }

    function showModalContent() {
        tuModalLoading.style.display  = 'none';
        tuModalContent.style.display  = 'block';
        tuModalError.style.display    = 'none';
        if (tuModalDownloadBtn) tuModalDownloadBtn.style.display = 'inline-flex';
        if (tuModalShareBtn)    tuModalShareBtn.style.display    = 'inline-flex';
    }

    function showModalError(msg) {
        tuModalLoading.style.display  = 'none';
        tuModalContent.style.display  = 'none';
        tuModalError.style.display    = 'block';
        tuModalErrorMsg.textContent   = msg || 'Could not load the report.';
        if (tuModalDownloadBtn) tuModalDownloadBtn.style.display = 'none';
        if (tuModalShareBtn)    tuModalShareBtn.style.display    = 'none';
    }

    function renderReportInModal(analysis) {
        const iconMap = { safe: 'fa-check-circle', warning: 'fa-exclamation-circle', danger: 'fa-exclamation-triangle' };

        tuModalVerdictRow.innerHTML =
            '<span class="tu-verdict-badge ' + analysis.verdict + '">'
            + '<i class="fas ' + iconMap[analysis.verdict] + '"></i> ' + escHtml(analysis.overall) + '</span>'
            + '<p class="tu-verdict-summary" style="margin:.5rem 0 0">'
            + escHtml(analysis.riskLevel || 'See detailed breakdown below') + '</p>';

        let html = '';

        // Personal Info
        if (analysis.personalInfo) {
            const pi = analysis.personalInfo;
            const piFields = [
                { label: 'Name',     value: pi.name,    icon: 'fa-user' },
                { label: 'Age',      value: pi.age,     icon: 'fa-birthday-cake', suffix: ' yrs' },
                { label: 'Trimester',value: pi.trimester,icon:'fa-baby' },
                { label: 'Week',     value: pi.week,    icon: 'fa-calendar-week' },
                { label: 'Gender',   value: pi.baby_gender, icon:'fa-venus-mars' },
                { label: 'Height',   value: pi.height,  icon: 'fa-ruler-vertical', suffix: ' cm' },
                { label: 'Weight',   value: pi.weight,  icon: 'fa-weight', suffix: ' kg' },
                { label: 'RBS Avg',  value: pi.rbs_avg, icon: 'fa-tint', suffix: ' mg/dL' },
                { label: 'BP',       value: (pi.avg_systolic && pi.avg_diastolic) ? pi.avg_systolic + '/' + pi.avg_diastolic : null, icon: 'fa-heartbeat', suffix: ' mmHg' },
                { label: 'Risk',     value: pi.risk_state, icon: 'fa-shield-alt' }
            ];
            let piChips2 = '';
            piFields.forEach(f => {
                if (f.value == null || f.value === '') return;
                piChips2 += '<div class="tu-pi-chip">'
                    + '<span class="tu-pi-chip-label">' + escHtml(f.label) + '</span>'
                    + '<span class="tu-pi-chip-value">' + escHtml(String(f.value)) + (f.suffix || '') + '</span>'
                    + '</div>';
            });
            if (piChips2) html += '<div class="tu-report-section"><h5 class="tu-section-h"><i class="fas fa-id-card"></i> Patient Information</h5>'
                + '<div class="tu-pi-strip">' + piChips2 + '</div></div>';
        }

        // Test Results
        if (analysis.tests && analysis.tests.length) {
            analysis.tests.forEach(test => {
                const cfg = resolveTestConfig(test.test_name);
                if (!cfg && !hasSubmitDiagnosisResults(test)) return;
                const confVal = test.confidence;
                let cards = '';
                if (hasSubmitDiagnosisResults(test)) {
                    cards = renderSubmitDiagnosisCards(test);
                } else {
                cfg.parameters.forEach(p => {
                    const val = extractValue(test, p.key) ?? '—';
                    cards += '<div class="tu-insight-card normal">'
                        + '<div class="tu-insight-label">' + escHtml(p.name) + '</div>'
                        + '<div class="tu-insight-value"><span class="tu-insight-dot"></span>' + escHtml(String(val))
                        + (p.unit ? '<span class="tu-insight-unit">' + escHtml(p.unit) + '</span>' : '') + '</div></div>';
                });
                }
                html += '<div class="tu-report-section"><h5 class="tu-section-h"><i class="fas fa-vial"></i> ' + escHtml(test.test_name || cfg?.name || 'Lab Test')
                    + (confVal ? '<span class="tu-confidence-pill" style="margin-left:.75rem"><i class="fas fa-brain"></i> '
                        + (parseFloat(confVal) * 100).toFixed(0) + '% confidence</span>' : '')
                    + '</h5><div class="tu-insight-grid">' + cards + '</div></div>';
            });
        }

        // AI Medical Report
        if (analysis.report) {
            html += '<div class="tu-report-section tu-ai-report-section"><h5 class="tu-section-h"><i class="fas fa-file-medical-alt"></i> AI Medical Report</h5>'
                + '<div class="tu-ai-block"><div class="tu-ai-block-body">' + formatReportText(analysis.report) + '</div></div></div>';
        }

        tuModalReportContent.innerHTML = html || '<p style="padding:1.5rem;color:var(--tu-muted)">No detailed data available for this report.</p>';
        showModalContent();
    }

    async function loadAndShowReport(labTestId, testName, uploadDate, resultText, resultClass) {
        tuModalTitle.textContent    = testName || 'Test Report';
        tuModalSubtitle.textContent = uploadDate || '';
        if (resultText && resultText !== '—') {
            tuModalResultBadge.className = 'tu-badge-status ' + (resultClass || 'pending');
            tuModalResultBadge.textContent = resultText;
            tuModalResultBadge.style.display = 'inline-flex';
        } else {
            tuModalResultBadge.style.display = 'none';
        }
        showModalLoading();
        openModal();

        try {
            const resp = await fetchWithTimeout('/api/analysis/' + labTestId, {}, 15000);
            if (!resp.ok) { showModalError('Report not found or not yet ready.'); return; }
            const data = await resp.json();
            if (data.status !== 'Completed') {
                showModalError('This report is still being processed (' + data.status + '). Please try again shortly.');
                return;
            }
            const analysis = mapAnalysisResponse(data);
            renderReportInModal(analysis);
        } catch (err) {
            showModalError(err?.message || 'Failed to load report.');
        }
    }

    // ── Event Delegation: View Report buttons ──
    document.addEventListener('click', e => {
        const btn = e.target.closest('.tu-btn-view-report');
        if (btn) {
            const id          = btn.dataset.testId;
            const testName    = btn.dataset.testName;
            const testDate    = btn.dataset.testDate;
            const resultText  = btn.dataset.result;
            const resultClass = btn.dataset.resultClass;
            if (id) loadAndShowReport(parseInt(id), testName, testDate, resultText, resultClass);
        }
    });

    // ── Close modal handlers ──
    tuModalClose.addEventListener('click', closeModal);
    tuModalCloseFooter.addEventListener('click', closeModal);
    tuModal.addEventListener('click', e => { if (e.target === tuModal) closeModal(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && tuModal.style.display !== 'none') closeModal(); });

    // ── Modal: Download PDF ──
    tuModalDownloadBtn?.addEventListener('click', async () => {
        const opts = {
            contentElId: 'tuModalReportContent',
            verdictElId: 'tuModalVerdictRow',
            title: tuModalTitle.textContent || 'Lab Analysis Report'
        };
        tuModalDownloadBtn.disabled = true;
        tuModalDownloadBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Generating…';
        try {
            const blob = await generateReportBlob(opts);
            const url  = URL.createObjectURL(blob);
            const a    = document.createElement('a');
            a.href = url;
            a.download = 'Lab-Analysis-Report.pdf';
            a.click();
            URL.revokeObjectURL(url);
        } catch (err) {
            console.error(err);
            notify('Could not generate PDF. Please try again.', 'error');
        } finally {
            tuModalDownloadBtn.disabled = false;
            tuModalDownloadBtn.innerHTML = '<i class="fas fa-download"></i> Download PDF';
        }
    });

    // ── Modal: Share with Doctor ──
    tuModalShareBtn?.addEventListener('click', () => {
        _reportSourceOpts = {
            contentElId: 'tuModalReportContent',
            verdictElId: 'tuModalVerdictRow',
            title: tuModalTitle.textContent || 'Lab Analysis Report'
        };
        if (!APPROVED_DOCTORS.length) {
            notify('No approved doctor linked to your account. Please connect with a doctor first.', 'error');
            return;
        }
        if (APPROVED_DOCTORS.length === 1) {
            sendReportToDoctor(APPROVED_DOCTORS[0]);
            return;
        }
        openShareModal();
    });

      /* ══════════════════════════════════════════════════════
         IMAGE GALLERY MODAL
      ══════════════════════════════════════════════════════ */
      const tuGallery         = document.getElementById('tuImageGallery');
      const tuGalleryClose    = document.getElementById('tuGalleryClose');
      const tuGalleryTitle    = document.getElementById('tuGalleryTitle');
      const tuGallerySubtitle = document.getElementById('tuGallerySubtitle');
      const tuGalleryGrid     = document.getElementById('tuGalleryGrid');

      function openGallery(images, testName, date) {
          tuGalleryTitle.textContent    = testName || 'Test Images';
          tuGallerySubtitle.textContent = date || '';
          document.querySelectorAll('.tu-gallery-hint-extra').forEach(el => el.remove());

          if (!images || !images.length) {
              tuGalleryGrid.innerHTML = '<p class="tu-gallery-hint">No images stored for this session.</p>';
          } else {
              tuGalleryGrid.innerHTML = images.map((img, i) => {
                  const safeUrl = normalizeAssetUrl(img.path);
                  const ext = (safeUrl || '').split('.').pop().toLowerCase();
                  const label = escHtml(img.testName || img.name || ('Image ' + (i + 1)));
                  if (ext === 'pdf') {
                      return '<div class="tu-gallery-item tu-gallery-pdf">'
                          + '<a href="' + escHtml(safeUrl) + '" target="_blank" rel="noopener noreferrer">'
                          + '<i class="fas fa-file-pdf"></i>'
                          + '<span class="tu-gallery-item-name">' + label + '</span>'
                          + '</a></div>';
                  }
                  return '<div class="tu-gallery-item">'
                      + '<a href="' + escHtml(safeUrl) + '" target="_blank" rel="noopener noreferrer">'
                      + '<img src="' + escHtml(safeUrl) + '" alt="' + label + '" loading="lazy" referrerpolicy="no-referrer">'
                      + '<span class="tu-gallery-item-name">' + label + '</span>'
                      + '</a></div>';
              }).join('');
              if (images.length > 1) {
                  tuGalleryGrid.insertAdjacentHTML('afterend', '<p class="tu-gallery-hint tu-gallery-hint-extra"><i class="fas fa-external-link-alt"></i> Click any image to open full size</p>');
              }
          }
          tuGallery.style.display    = 'flex';
          document.body.style.overflow = 'hidden';
      }

      function closeGallery() {
          tuGallery.style.display    = 'none';
          document.body.style.overflow = '';
      }

      if (tuGalleryClose) tuGalleryClose.addEventListener('click', closeGallery);
      if (tuGallery)      tuGallery.addEventListener('click', e => { if (e.target === tuGallery) closeGallery(); });

      document.addEventListener('keydown', e => {
          if (e.key === 'Escape' && tuGallery?.style.display === 'flex') closeGallery();
      });

      /* View Images handler (delegated; works across table variants) */
      document.addEventListener('click', e => {
          const btn = e.target.closest('.tu-btn-view-images');
          if (!btn) return;
          try {
              const images   = JSON.parse(btn.dataset.images || '[]');
              const testName = btn.dataset.testName || 'Test Images';
              const date     = btn.dataset.testDate || '';
              openGallery(images, testName, date);
          } catch (_) {
              notify('Could not load images.', 'error');
          }
      });

    /* ══════════════════════════════════════════════════════
       VIEW ALL REPORTS MODAL
    ══════════════════════════════════════════════════════ */
    const allReportsModal = document.getElementById('allReportsModal');
    if (allReportsModal) {
        const openAllBtn  = document.getElementById('viewAllReportsBtn');
        const closeEls    = allReportsModal.querySelectorAll('[data-close-all-reports]');

        function openAllReports() {
            allReportsModal.style.display = 'flex';
            document.body.style.overflow = 'hidden';
        }
        function closeAllReports() {
            allReportsModal.style.display = 'none';
            document.body.style.overflow = '';
        }
        if (openAllBtn) openAllBtn.addEventListener('click', openAllReports);
        closeEls.forEach(el => el.addEventListener('click', closeAllReports));
        allReportsModal.addEventListener('click', e => { if (e.target === allReportsModal) closeAllReports(); });
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape' && allReportsModal.style.display === 'flex') closeAllReports();
        });
    }
}());

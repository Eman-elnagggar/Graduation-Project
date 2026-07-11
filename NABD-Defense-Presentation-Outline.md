# NABD — Graduation Project Defense Presentation (Outline)

> Expanded from the 10 landing topics into a full defense deck.
> Each slide = **Title** + on-slide content + 🎤 **Talk track** (what you say).
> Language: English slides. Suggested total: ~40 slides / ~18–22 min.

---

## SECTION 0 — Opening

### Slide 1 — Title
- **NABD (نبض)** — An AI-Powered Pregnancy Health Management Platform
- Graduation Project — [Department / University / Year]
- Team names + Supervisor name
- 🎤 *"Good morning. We're presenting NABD — Arabic for 'pulse' — an AI-powered platform that monitors the health of a mother and her baby together. I'm [name], and over the next 20 minutes we'll walk you through the problem, our solution, the architecture, and a live demo."*

### Slide 2 — Agenda
- Problem & Motivation
- Related Work & Gap Analysis
- Proposed Solution & Objectives
- System Architecture & Technology
- AI Capabilities
- Implementation & Demo
- Results, Future Work & Conclusion
- 🎤 *"Here's the roadmap of the talk. We'll start with the clinical problem, show how existing apps fall short, then go deep into how we built NABD and the AI behind it, finish with a demo and our future plans."*

---

## SECTION 1 — Introduction (from "Two heartbeats")

### Slide 3 — The Vision
- "Two heartbeats. One intelligent guardian."
- A pregnancy is **two patients** monitored as one
- Calm, continuous, AI-powered care — not periodic check-ups
- 🎤 *"Our core idea is simple: during pregnancy there are two heartbeats to protect, the mother's and the baby's. Traditional care looks at them only during scheduled visits. NABD watches continuously and flags what matters early."*

### Slide 4 — NABD in One Sentence
- An ASP.NET Core platform that unifies **monitoring, communication, and early risk detection** for mothers and their care teams
- Turns scattered scans, lab results, and symptoms into clear, actionable insight — 24/7
- 🎤 *"In one sentence: NABD takes everything that's normally scattered across clinics, labs, and notebooks, and turns it into one continuous, intelligent stream of insight available around the clock."*

### Slide 5 — At a Glance
- 1 unified platform · 5 user roles · 5+ AI capabilities
- Real-time chat · OCR lab analysis · ultrasound AI · risk prediction · product safety
- 🎤 *"At a glance, this is one platform serving five roles, with five distinct AI capabilities working underneath. We'll unpack each of these."*
- *(Note: landing page says "4 roles" — your codebase actually has 5: Patient, Doctor, Assistant, Admin, Lab. Use 5 in the defense.)*

---

## SECTION 2 — The Challenge (from "Real Problems Pregnant Women Face")

### Slide 6 — Problem Statement
- Traditional maternal care is **reactive and fragmented**
- Critical complications are often detected **too late**
- 🎤 *"The problem we set out to solve: maternal care today is reactive and fragmented. Between visits, no one is watching the data, and by the time a complication shows up in a routine check-up, it can already be serious."*

### Slide 7 — Gaps in Current Care (1/2)
- **Late Risk Detection** — complications found too late; no real-time data
- **Missed Gestational Diabetes** — goes unnoticed without continuous analysis
- **Poor Doctor–Patient Communication** — delays in critical moments
- 🎤 *"We identified six concrete gaps. First three: risks caught too late, gestational diabetes slipping through without monitoring, and patients unable to reach their doctor quickly when it matters most."*

### Slide 8 — Gaps in Current Care (2/2)
- **Fragmented Medical Records** — history scattered across systems
- **Unsafe Product Usage** — harmful ingredients used unknowingly
- **Lack of Continuous Monitoring** — only periodic check-ups; silent risks between visits
- 🎤 *"The other three: medical records scattered across different clinics, mothers unknowingly using products with unsafe ingredients, and the absence of any monitoring between appointments. NABD is designed to close every one of these."*

### Slide 9 — Why It Matters (Motivation)
- Gestational diabetes & preeclampsia are leading, **largely preventable** complications when caught early
- Early intervention depends on **continuous data**, which the current system lacks
- 🎤 *"This matters because conditions like preeclampsia and gestational diabetes are highly manageable when caught early — the whole battle is detection time. That's exactly what continuous monitoring gives us."*
- *(Tip: add one cited statistic from WHO / Egyptian MoH here to strengthen the motivation.)*

---

## SECTION 3 — Related Work (from "How NABD Compares")

### Slide 10 — Existing Solutions
- **Vezeeta** — booking only
- **Flo** — period/pregnancy tracking
- **Yuka** — product ingredient scanning
- **BabyCenter** — content + community
- 🎤 *"We studied the leading apps mothers already use. Each does one thing well: Vezeeta books appointments, Flo tracks pregnancy, Yuka scans products, BabyCenter offers content and community. But none of them connect."*

### Slide 11 — Feature Comparison
- Comparison matrix: Appointment Booking, Pregnancy Tracking, AI Risk Prediction, OCR Report Analysis, Product Analysis, Real-Time Alerts, Doctor–Patient Communication, GDM Prediction, Maternal Risk Detection, Community
- ✓ full · ~ partial · — none — **NABD is the only column that's full across the board**
- 🎤 *"Here's the side-by-side. The existing apps light up one or two cells each. NABD is the only solution that covers the whole row — and crucially, the AI prediction features simply don't exist in any competitor."*

### Slide 12 — The Gap We Fill
- No single product combines **clinical workflow + AI risk detection + communication**
- NABD's contribution: an **integrated, AI-driven, multi-role** maternal platform
- 🎤 *"So the gap is clear: there is no integrated, AI-driven platform that connects the patient, the doctor, and the clinic around continuous risk detection. That integration is our contribution."*

---

## SECTION 4 — Project Overview (from "What is NABD?")

### Slide 13 — What is NABD?
- AI-powered pregnancy health management platform
- Built on **ASP.NET Core 8 MVC**
- Unifies monitoring, communication, and early risk detection
- 🎤 *"NABD is a web platform built on ASP.NET Core 8. It brings every stakeholder in the pregnancy journey into one system and links them through shared records and AI insight."*

### Slide 14 — Objectives & Scope
- Enable **continuous, proactive** monitoring instead of periodic visits
- Provide **early, explainable risk detection** (GDM, preeclampsia, lab anomalies)
- Unify records and **real-time doctor–patient communication**
- Scope: web platform (mobile + wearables are future work)
- 🎤 *"Our objectives were three: make monitoring continuous, make risk detection early and explainable, and unify communication and records. We deliberately scoped this release to the web; mobile and wearables are on the roadmap."*

### Slide 15 — One Platform, Five Roles
- **Patient** — tracks pregnancy, uploads tests, chats with doctor & AI
- **Doctor** — reviews AI reports, manages patients, prescribes
- **Assistant** — coordinates appointments, availability, clinic schedules
- **Lab** — uploads and manages lab results
- **Admin** — oversees clinics, users, and platform operation
- 🎤 *"The platform serves five roles. Each gets a tailored interface and permissions — enforced by role-based authorization, which I'll show in the architecture. The Patient and Doctor roles are where most of the AI value lands."*

---

## SECTION 5 — What Makes NABD Different (from "The NABD Solution")

### Slide 16 — Reactive → Proactive
- Traditional care **waits** for the next appointment
- NABD **continuously monitors** and alerts patient + doctor before risks escalate
- 🎤 *"The single biggest shift NABD makes is from reactive to proactive. Instead of waiting for the next visit, the system is always analyzing and will reach out the moment something looks off."*

### Slide 17 — Key Differentiators (1/2)
- **All-in-One Platform** — tools, records, communication unified
- **AI-Powered Monitoring** — models trained on pregnancy data, 24/7
- **Early Risk Detection** — GDM, preeclampsia flagged weeks earlier
- 🎤 *"Three things set us apart technically: everything lives in one place, the monitoring is AI-driven and continuous, and the risk detection is genuinely early — giving doctors time to act."*

### Slide 18 — Key Differentiators (2/2)
- **Continuous Follow-Up** — proactive alerts between visits
- **Smart Healthcare Experience** — accessible, understandable medical data
- **Doctor–Patient Unity** — shared records + AI-assisted reporting
- 🎤 *"And three more on the experience side: continuous follow-up, an interface designed so non-technical mothers can understand their own medical data, and a shared workspace that keeps doctor and patient perfectly aligned."*

---

## SECTION 6 — System Architecture & Technology (ADDED — core defense content)

### Slide 19 — Technology Stack
- **Backend:** ASP.NET Core 8 MVC · C#
- **Data:** SQL Server · Entity Framework Core (code-first, 45+ migrations)
- **Auth:** ASP.NET Core Identity (cookie sessions)
- **Real-time:** SignalR
- **AI services:** Hugging Face–hosted models (REST)
- **Frontend:** Bootstrap 5 · jQuery
- 🎤 *"On the technical side, the backend is ASP.NET Core 8 with C#. Data is SQL Server through Entity Framework Core using a code-first approach — we have over 45 migrations tracking the schema's evolution. Authentication is ASP.NET Identity, real-time chat is SignalR, and the AI runs as separate model services we call over REST."*

### Slide 20 — System Architecture (Layered)
- `Controllers → Services → Repositories → AppDbContext (EF Core)`
- Interfaces layer for **dependency injection** (testable, swappable)
- ViewModels separate domain entities from the views
- 🎤 *"Architecturally we used a clean layered design: controllers handle requests, services hold business logic, repositories abstract data access, all wired through interfaces and dependency injection. This separation means each layer can change or be tested independently — for example, swapping the data source without touching controllers."*
- *(Draw the 4-box vertical diagram here; mention Repository + Service + MVC pattern by name.)*

### Slide 21 — Database Design
- Core entities: Patient, Doctor, Assistant, Clinic, PregnancyRecord, Appointment, Medication(+Schedule/Log), LabTest, TestReport, UltrasoundImage, Alert, Prescription(+Items), Community Post/Comment
- `ClinicDoctor` — many-to-many junction (clinics ↔ doctors)
- Relationships configured with **EF Fluent API**
- 🎤 *"The data model centers on the pregnancy record, which links to appointments, medications, lab tests, ultrasound images, alerts, and prescriptions. Doctors and clinics are a many-to-many relationship through a junction table. All relationships are configured explicitly with EF's Fluent API."*
- *(Show a simplified ERD — don't put all tables; highlight PregnancyRecord at the center.)*

### Slide 22 — Security & Authorization
- ASP.NET Identity with **5 roles**, `[Authorize(Roles=...)]` per controller
- Cookie sessions (sliding, 30-day), password complexity rules
- **Encrypted** doctor–patient messages (`ChatMessageCrypto`) at rest
- File uploads scoped per entity type and user ID
- 🎤 *"Security was a first-class concern given the medical data. Every action is gated by role-based authorization. Chat messages are encrypted before they're stored and decrypted only on retrieval, so even the database doesn't hold plaintext conversations."*

### Slide 23 — Real-Time Messaging (SignalR)
- `ChatHub` powers live doctor–patient chat
- Messages encrypted via `ChatMessageCrypto` before storage, decrypted on retrieval
- Multimedia support (images, reports)
- 🎤 *"The chat is built on SignalR, so messages are pushed instantly with no refresh. Each message passes through our encryption layer on the way in and out, keeping conversations private end-to-end at rest."*

### Slide 24 — Background Services
- `MedicationReminderHostedService` — polls and fires medication reminders
- `AnalysisBackgroundJob` — processes lab-test OCR asynchronously
- Email via Gmail SMTP for notifications
- 🎤 *"Two background workers run continuously: one fires medication reminders on schedule, and another processes lab-test OCR in the background so the user isn't blocked while a report is analyzed. Notifications go out over email as well."*

---

## SECTION 7 — For Patients (from "Your Complete Pregnancy Companion")

### Slide 25 — Patient Experience Overview
- Six pillars: Doctor Chat · Medication Reminders · Medical History · Smart Alerts · Appointment Booking · Community
- 🎤 *"From the mother's side, NABD is a complete companion built around six pillars. Let me highlight the ones that show the most engineering and AI depth."*

### Slide 26 — Communication, Reminders & History
- **Real-Time Doctor Chat** — instant, encrypted, multimedia
- **Smart Medication Reminders** — trimester-adapted schedules, adherence logs
- **Complete Medical History** — full timeline, ultrasound & lab archive
- 🎤 *"She can message her OB/GYN instantly with encrypted chat, get personalized medication reminders that adapt to her trimester with adherence tracking, and see her entire pregnancy as one searchable timeline — every test, report, and note in one place."*

### Slide 27 — Alerts, Booking & Community
- **Smart Alerts** — AI-triggered risk alerts, appointment/medication reminders
- **Easy Appointment Booking** — browse clinics, doctor availability, one-tap booking
- **Mothers Community** — moderated peer support, posts & comments
- 🎤 *"She also receives AI-triggered alerts for abnormal results, books appointments without a single phone call, and has a moderated community of other mothers for emotional support and shared experience."*

---

## SECTION 8 — For Doctors & Clinics (from "Empowering Doctors")

### Slide 28 — The Doctor Dashboard
- **Schedule & Appointments** — smart scheduling, conflict-free booking
- **Patient Profiles & Tracking** — full health timeline + pregnancy status
- **AI Report Review** — review, annotate, approve AI lab & ultrasound reports
- **Digital Prescriptions** — create, print, share in seconds
- 🎤 *"On the clinical side, the doctor gets one organized dashboard: today's appointments and patient list, a full timeline per patient, and — importantly — a review queue where the AI's lab and ultrasound findings are presented for the doctor to verify and approve. The AI assists; the doctor decides. Prescriptions are generated digitally."*
- *(Emphasize "human-in-the-loop" — examiners love this for medical AI.)*

---

## SECTION 9 — AI Capabilities (from "Powered by Intelligent AI") — DEEP DIVE

### Slide 29 — AI Capabilities Overview
- Five AI capabilities: Chatbot · Lab Test Analysis (OCR) · Ultrasound Analysis (CV) · Product Safety · Predictive Risk
- Each runs as an independent service called over REST (separation of concerns)
- 🎤 *"This is the heart of the project. We have five AI capabilities, each implemented as its own service so they can scale and be updated independently. Let me take them one by one."*

### Slide 30 — Conversational AI Chatbot
- Pregnancy-aware assistant, 24/7, plain language
- Returns a **risk level** + an **actionable recommendation**, not just an answer
- Integrated via `ChatbotService` (Hugging Face)
- 🎤 *"The chatbot is pregnancy-aware: ask it about symptoms, medications, or nutrition and it doesn't just answer — it assesses a risk level and gives a concrete next step. For example, if a mother reports a headache with swelling at 31 weeks, it flags a moderate risk and tells her to check her blood pressure and contact her doctor."*

### Slide 31 — AI Medical Test Analysis (OCR)
- Upload blood/urine/lab reports → **OCR** extracts every value
- Compared against **pregnancy-specific reference ranges**, anomalies flagged
- Implemented via `AnalysisService` + `AnalysisBackgroundJob`
- 🎤 *"For lab reports, the mother just uploads a photo. Our OCR pipeline reads each value, compares it against pregnancy-specific reference ranges — which differ from normal adult ranges — and flags anomalies instantly. The heavy processing runs as a background job so the UI stays responsive."*

### Slide 32 — AI Ultrasound Analysis (Computer Vision)
- Computer vision on ultrasound images
- Assesses fetal development, detects abnormalities, tracks growth
- Implemented via `UltrasoundAIService`
- 🎤 *"For ultrasounds, a computer-vision model analyzes the image to assess fetal development and detect abnormalities, then produces a growth-tracking report. As with the lab analysis, the output is a draft the doctor reviews and approves."*

### Slide 33 — Product Safety Checker
- Scan a product's ingredient list
- Cross-references substances against pregnancy safety databases
- Flags harmful ingredients (e.g., "Retinol — avoid in pregnancy")
- 🎤 *"The product safety checker closes one of the six gaps directly: a mother scans any product's ingredients and we cross-reference them against pregnancy safety data, warning her about anything unsafe — like retinol in a face cream."*

### Slide 34 — Predictive Risk Detection
- ML models flag early signs of **gestational diabetes & preeclampsia**
- Driven by trends across labs, vitals, and history — not single readings
- Feeds the **Smart Alerts** system
- 🎤 *"And the predictive layer ties it together: rather than reacting to a single bad reading, the models look at trends over time to surface early signs of gestational diabetes or preeclampsia, then push an alert to both mother and doctor — weeks before a routine visit would have caught it."*
- *(Be ready for "what data/model did you train on, and what accuracy?" — have your dataset, features, metric, and validation method ready. Don't overstate the 99.2% figure unless you can defend it.)*

---

## SECTION 10 — Implementation & Demo

### Slide 35 — Live Demo
- Demo flow: Patient signup → upload lab report → AI analysis → alert → doctor reviews → chat → prescription
- 🎤 *"Now let's see it live. I'll log in as a patient, upload a lab report, watch the AI analyze it and raise an alert, then switch to the doctor view to review the AI report and respond over chat."*
- *(Have screenshots as a fallback in case the live demo fails. Pre-seed the demo account: default seeded password is `Nabd@123`.)*

---

## SECTION 11 — Future Work (from "The Future of NABD")

### Slide 36 — Roadmap
- **Wearables & IoT** — stream glucose/BP/vitals in real time
- **Multilingual Voice AI** — hands-free, spoken guidance
- **Full Arabic (RTL) Support** — localized for the region
- **Location-Based Recommendations** — nearest clinics/labs/pharmacies
- **EHR Integration** — standards-based (FHIR/HL7) hospital integration
- 🎤 *"NABD is built to grow. Next is connecting wearables for true real-time vitals, a multilingual voice assistant, full Arabic right-to-left support, location-based care suggestions, and standards-based EHR integration so hospitals can plug in directly."*

---

## SECTION 12 — Team (from "Built by Dedicated Innovators")

### Slide 37 — The Team
- Karim Helal — Full Stack · Kareem Shakl — Full Stack · Haneen Elagamy — Full Stack
- Eman Elnagar · Menna Taha · Mariam Elnemrawy — AI Development
- Youssef Khedr — Data Analyst
- 🎤 *"NABD was built by seven of us: three on full-stack, three on AI, and one on data analysis — supervised by [name]. Each AI capability you saw was owned end-to-end by a member of the AI team."*

---

## SECTION 13 — Closing

### Slide 38 — Conclusion
- NABD delivers **integrated, proactive, AI-driven** maternal care
- Closes all six gaps in traditional pregnancy monitoring
- Working platform: 5 roles · 5 AI capabilities · real-time + secure
- 🎤 *"To conclude: NABD is a working, integrated platform that shifts maternal care from reactive to proactive. It closes every gap we identified, backed by a clean architecture and five real AI capabilities. Thank you."*

### Slide 39 — Thank You / Q&A
- "Two heartbeats. One intelligent guardian."
- Contact / repo / demo link
- 🎤 *"We're happy to take your questions."*

---

## Appendix — Anticipated Examiner Questions (prep, not slides)
- **AI accuracy:** dataset source, size, features, metric, train/test split, and how 99.2% was measured.
- **Why ASP.NET Core MVC** over alternatives? (team expertise, integrated Identity, EF Core, SignalR.)
- **Data privacy/compliance:** encryption at rest, role-based access, where uploads are stored.
- **Scalability:** stateless controllers, background jobs, external AI services decoupled over REST.
- **Human-in-the-loop:** AI outputs are doctor-reviewed before acting — clarify liability boundary.
- **Why Hugging Face hosting** and what happens if a model service is down (graceful degradation?).
- **What's truly "real-time"** today vs. roadmap (current alerts are data-driven; wearables are future).

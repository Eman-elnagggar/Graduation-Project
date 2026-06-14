# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**NABD (نبض)** — A pregnancy health management platform built with ASP.NET Core 8 MVC targeting maternal and fetal health monitoring. The platform serves five roles: Patient, Doctor, Assistant, Admin, and Lab.

## Build & Run Commands

```bash
# Build the solution
dotnet build

# Run with HTTPS (default)
dotnet run --project "Graduation Project" --launch-profile https
# App available at https://localhost:7263

# Run with HTTP
dotnet run --project "Graduation Project" --launch-profile http
# App available at http://localhost:5209

# Apply EF Core migrations manually (normally auto-applied on startup)
dotnet ef database update --project "Graduation Project"

# Add a new migration
dotnet ef migrations add <MigrationName> --project "Graduation Project"
```

There is no test project. The database is seeded automatically on first run via `DataSeeder.SeedAsync()`. Default credentials for all seeded users are `Nabd@123`.

## Architecture

The project uses a **Repository + Service + MVC** layered architecture:

```
Controllers  →  Services  →  Repositories  →  AppDbContext (EF Core)
                    ↕
              Interfaces/   (abstractions for DI)
```

- **Controllers/** — MVC controllers with role-based `[Authorize]` attributes
- **Services/** — Business logic; injected into controllers
- **Interfaces/** — Abstractions for all services and repositories used with DI
- **Repository/** — Generic EF Core repository implementations
- **Models/** — EF Core entity classes; `ApplicationUser` extends `IdentityUser`
- **ViewModels/** — DTOs used to pass data between controllers and views
- **Hubs/** — SignalR `ChatHub` for real-time encrypted doctor-patient messaging
- **Data/** — `AppDbContext` extending `IdentityDbContext<ApplicationUser>`
- **Migrations/** — 45+ code-first EF migrations (Feb–May 2026)

## Identity & Authorization

Five roles registered at startup: `Admin`, `Doctor`, `Patient`, `Assistant`, `Lab`.

Use `[Authorize(Roles = "Patient")]` / `[Authorize(Roles = "Doctor")]` etc. on controllers/actions. Unauthenticated users are redirected to `/Account/Login`; unauthorized to `/Account/AccessDenied`.

Cookie sessions expire after 30 days (sliding). Password rules: min 6 chars, digit + upper + lowercase required, no special chars required.

## Database

- SQL Server (local) via connection string `Server=.;Database=GraduationProject3;Integrated Security=True;TrustServerCertificate=True;`
- Migrations run automatically at startup — no manual `dotnet ef database update` needed in development
- Core entities: `Patient`, `Doctor`, `Assistant`, `Clinic`, `PregnancyRecord`, `Appointment`, `Medication`, `MedicationSchedule`, `MedicationLog`, `LabTest`, `TestReport`, `UltrasoundImage`, `Alert`, `Prescription`, `PrescriptionItem`, `CommunityPost`, `CommunityComment`
- `ClinicDoctor` is a many-to-many junction between `Clinic` and `Doctor`

## External API Integrations

All external HTTP calls go through dedicated services injected via DI:

| Service | Endpoint |
|---|---|
| `ChatbotService` | Hugging Face pregnancy chatbot |
| `UltrasoundAIService` | Fetal abnormality detection model |
| `AnalysisService` | Lab test OCR extraction |

API base URLs are hardcoded in the service classes (Hugging Face spaces). Email uses Gmail SMTP configured in `appsettings.json` under `EmailSettings`.

## Background Services

- `MedicationReminderHostedService` — `IHostedService` that polls and fires medication reminders
- `AnalysisBackgroundJob` — Processes lab test OCR results asynchronously

## Real-time Messaging

`ChatHub` (SignalR) handles doctor-patient chat. Messages are encrypted/decrypted via `ChatMessageCrypto` before storage and after retrieval. The JS client lives in `wwwroot/js/patient-messages.js` and `wwwroot/js/doctor-messages.js`.

## Frontend

- Bootstrap 5 (bundled in `wwwroot/lib/bootstrap/`)
- jQuery + jQuery Validation (bundled in `wwwroot/lib/`)
- SignalR JS client
- Role-scoped CSS files: `patient-style.css`, `doctor-style.css`, `admin-style.css`, etc. in `wwwroot/css/`
- Views are organized by role under `Views/Patient/`, `Views/Doctor/`, `Views/Admin/`, `Views/Assistant/`

## Key Conventions

- ViewModels live in `ViewModels/` and are named `<Feature>ViewModel` or `<Action>ViewModel`
- Services are registered as scoped in `Program.cs`; hosted services as singletons
- EF relationships use explicit foreign key properties and navigation properties with fluent API config in `AppDbContext`
- File uploads (ultrasound images, lab test images) are stored under `wwwroot/uploads/` in subfolders by entity type and user ID

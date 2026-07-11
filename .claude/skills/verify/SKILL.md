---
name: verify
description: Build, run, and drive the NABD ASP.NET Core MVC app to verify a change end-to-end (login, hit role-scoped pages, POST forms with antiforgery tokens).
---

# Verifying NABD changes

The app is server-rendered ASP.NET Core 8 MVC. The surface is HTTP: you log in,
GET the page, and POST the form. Driving it with `curl` + a cookie jar exercises
the exact same path a browser does (including antiforgery), so it counts as real
end-to-end verification.

## Build & run

The running app **locks `Graduation Project.exe`** — always stop it before rebuilding,
or the build fails with `MSB3027 ... file is locked`.

```bash
# stop any running instance first (PowerShell)
Get-Process -Name "Graduation Project" -ErrorAction SilentlyContinue | Stop-Process -Force

dotnet build                                                    # from repo root
dotnet run --project "Graduation Project" --launch-profile http --no-build
# -> http://localhost:5209   (https profile: 7263)
```

Wait for `Application started` in the log before driving it. Startup auto-applies
migrations and runs `DataSeeder`, so it takes ~20s.

**Razor views are NOT runtime-compiled.** A `.cshtml` edit needs a full
stop → `dotnet build` → restart. If a view change seems to have no effect, this is why.

## Database — read this before writing any SQL

The connection string in `appsettings.json` is the only source of truth:

```
Server=.;Database=GraduationProjectFinal;...
```

**`CLAUDE.md` says `GraduationProject3`, which is stale.** A leftover
`GraduationProject3` database still exists on the local server with the *same seed
data and the same IDs*, so queries against it look completely plausible while having
nothing to do with the running app. Verify the DB name before trusting any `sqlcmd`
result:

```bash
sqlcmd -S . -d GraduationProjectFinal -E -W -Q "SELECT ..."
```

Seeded logins are `<name>@nabd.com` / `Nabd@123` (e.g. `admin@nabd.com`).

## Driving it over HTTP

Every POST needs the `__RequestVerificationToken` hidden field from a page you
GET first, plus the antiforgery cookie. Use one cookie jar for both.

```bash
# 1. log in
curl -s -c cj.txt http://localhost:5209/Account/Login -o lg.html
T=$(grep -oE 'name="__RequestVerificationToken"[^>]*value="[^"]*"' lg.html \
    | head -1 | sed -E 's/.*value="([^"]*)".*/\1/')
curl -s -b cj.txt -c cj.txt -X POST http://localhost:5209/Account/Login \
  --data-urlencode "Email=admin@nabd.com" \
  --data-urlencode "Password=Nabd@123" \
  --data-urlencode "__RequestVerificationToken=$T" -w "%{http_code}\n"   # 302 = success

# 2. GET a page, then POST a form using a token scraped from that page
curl -s -b cj.txt -c cj.txt http://localhost:5209/Admin/ClinicDetail/1 -o cur.html
T=$(grep -oE 'name="__RequestVerificationToken"[^>]*value="[^"]*"' cur.html \
    | head -1 | sed -E 's/.*value="([^"]*)".*/\1/')
curl -s -b cj.txt -c cj.txt -X POST http://localhost:5209/Admin/AddDoctorToClinic \
  --data-urlencode "clinicId=1" --data-urlencode "doctorId=3" \
  --data-urlencode "__RequestVerificationToken=$T" -w "%{http_code}\n"   # 302 = handled
```

Gotchas:

- Write scratch files to the **current directory, not `/tmp`** — under Git Bash on
  Windows `/tmp` paths silently don't round-trip, and an empty token yields a
  confusing `400`.
- Don't follow the redirect on the POST. Controllers report results via
  `TempData["AdminSuccess"] / ["AdminError"]`, which the *next* GET renders as
  `<div class="admin-toast success|error">`. Re-GET the page and scrape that toast —
  it is the assertion.
- Strip `<style>`/`<script>` before turning HTML into text, or page CSS drowns the content.

## Restore data you mutate

The seeded DB is shared dev state. If a flow mutates it (assigning doctors, moving
assistants, changing dates), record the rows first and put them back with `sqlcmd`
when done. `Data/DataSeeder.cs` holds the original values.

## Known data quirk

`Doctor.VerificationStatus` holds **both** `"Verified"` (written by `DataSeeder`) and
`"Approved"` (written by the admin verification screen). They mean the same thing.
Any new code that gates on approval must accept both, or it will silently exclude the
seeded doctors.

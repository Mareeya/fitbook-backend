# FitBook — Implementation Plan

Working plan for the next phases of the backend. Companion to [fitbook-roadmap.html](fitbook-roadmap.html),
which holds the reasoning; this file holds the tasks.

- **Snapshot taken:** 2026-09-20 (commit `37caf9a`)
- **Status:** Phases 2–5 written, plus the Phase 0 small wins — see *Session log*
- **Convention:** `[ ]` not started · `[~]` in progress · `[x]` done

### Decisions made 2026-09-20

| # | Question | Decision |
|---|---|---|
| 1 | Lookups or enums for attendance status / booking source | **Enums**, `HasConversion<byte>()` |
| 2 | UTC or local in `Sessions.StartAt` | **UTC**, converted at the edges |
| 3 | Does Staff manage the timetable | **Yes** — `Admin,Staff` on management endpoints |
| 4 | Late-cancel window | **None for now** — `LateCancel` defined but unused |
| 5 | Multi-location | **No** — single location, keep it simple |

### Session log

**2026-09-20 — Phases 2–5, CRUD pass.** Sessions, Bookings, Attendance and Dashboard written:
4 controllers, 4 services, 8 model files, domain + `AppDbContext` changes. Deferred by design:
the no-show sweep, the utilization heatmap, at-risk list, caching, and tests.

**2026-09-20 — small wins.** Signing key moved out of source control, Swagger bearer auth,
`GET /api/auth/me`, and the no-show sweep.

> **Not yet verified.** There is no .NET SDK on this machine, so none of this has been compiled and
> **the EF migration has not been generated**. Run `dotnet ef migrations add AttendanceAndCounters`
> then `dotnet ef database update` before the new endpoints will work — see Phase 1.

---

## Where the code is today

**Done**

- Users / Lookups / Trainers / Classes — schema, services, controllers
- Auth: login + register, password hashing, JWT issuing and validation (`37caf9a`)
- Global exception handling via `AppException` + `ProblemDetails`
- Class list carries `SessionsThisMonth` and `AverageFillRate`

**Written 2026-09-20, not yet compiled or migrated**

- Sessions, Bookings, Attendance, Dashboard — services + controllers
- Attendance + counters on the model; `UserRole.Staff`

**Not started**

- Phase 0 auth hardening (the signing key is still committed)
- Soft delete, the no-show sweep, tests

**Known weak points in existing code**

- `ClassService.ClassQuery()` eager-loads `Sessions → Bookings → Status` for *every* class on an
  unpaged `GET /api/classes`, then computes fill rate in memory. Works now; degrades badly with real
  data. Phase 5 replaces it with a projection over the Phase 1 counters.
- `DeleteAsync` returns the magic string `"NotFound"` in both `ClassService` and `TrainerService`.
- `CreatedAt` / `UpdatedAt` are assigned by hand in every service method.

---

## Phase 0 — Auth hardening

The JWT work landed and the shape is right: correct middleware order, all four controllers marked,
`AllowAnonymous` scoped to auth only, register hard-codes `UserRole.Member` so nobody can
self-promote, role claim emitted as the enum name so `[Authorize(Roles = "Admin")]` will work
unchanged once roles are applied. These are the gaps left.

**Depends on:** nothing · **Size:** small · **Do this before Phase 1**

### 0.1 Get the signing key out of source control — *do this first*

`appsettings.json` contains `"Key": "FitBook-Evaluation-Jwt-Signing-Key-32b!"`, committed. Anyone who
can read the repo can mint a token with any `sub` and any role and call every endpoint as an admin.
The `[Authorize]` attributes added in `37caf9a` do not hold against a forged token.

The project already has the right machinery — `UserSecretsId` is set in `FitBook-App.csproj`, and
`appsettings.Local.json` is gitignored. The key just went into the wrong file.

- [x] Removed the `Jwt:Key` value from `appsettings.json`; `Issuer`, `Audience`, `ExpireMinutes` stay
- [x] `appsettings.Local.json` created with a fresh 64-character random key (gitignored — verified
      with `git check-ignore`). `dotnet user-secrets set "Jwt:Key" "<key>"` works the same way
- [x] `Jwt:Key` placeholder added to `appsettings.Local.example.json`
- [x] New key generated — **the committed one is burned**; it stays in git history forever, so it must
      never be used anywhere real
- [x] Fails fast at startup with a message naming both fixes, and the validated value is what's handed
      to `TokenValidationParameters` (the `jwt["Key"]!` null-forgiving read is gone)
- [ ] Note in the README that a fresh clone needs `appsettings.Local.json` before the app will start

### 0.2 Roles, not just authentication

`[Authorize]` with no role means *any logged-in user*. A member who registers through the public
endpoint can currently `POST /api/classes` and `DELETE /api/trainers/{id}`. Anonymous access is
closed; privilege escalation is not.

- [ ] `ClassesController` — `POST`, `PUT`, `DELETE` → Admin (add Staff in Phase 1 if staff manage the timetable)
- [ ] `TrainersController` — `POST`, `PUT`, `DELETE` → Admin
- [ ] `GET` endpoints stay open to any authenticated user
- [ ] `LookupsController` — fine as is

### 0.3 Default-deny

Five new controllers arrive in Phases 2–5. Any one of them added without `[Authorize]` ships open.

- [ ] Add a fallback policy so unmarked endpoints require authentication:
      `options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()`
- [ ] Confirm `[AllowAnonymous]` on `AuthController` still overrides it (it does — verify anyway)

### 0.4 Consistent error shape

`OnChallenge` and `OnForbidden` write plain text, so 401 and 403 return `text/plain` while every
other error returns `ProblemDetails` JSON. Clients need two parsers.

- [ ] Write `ProblemDetails` from both handlers, matching `GlobalExceptionHandler`

### 0.5 Make Swagger usable

Swagger UI is enabled in development but has no way to send a bearer token, so no protected endpoint
can be tried from it — which is now every endpoint.

- [x] Bearer security definition + requirement added to `AddSwaggerGen`. Log in via
      `/api/auth/login`, copy the token, click **Authorize** — Swagger adds the `Bearer ` prefix
      itself, so paste the raw token

### 0.6 Small additions

- [x] `GET /api/auth/me` — returns id, name, email, role, role name, and `TrainerId` when the user
      is a trainer, so a client can restore a session without a second lookup
- [x] `Helpers/ClaimsPrincipalExtensions.cs` — `GetUserId()`, `IsInRole(UserRole)`, `CanManage()`.
      Every booking and attendance path reads the caller from the token, never from the body

**Also changed:** `AuthController` carried a **class-level `[AllowAnonymous]`**, which wins over any
method-level `[Authorize]` — so `/me` would have been anonymous. `[AllowAnonymous]` now sits on
`login` and `register` individually and the controller defaults to `[Authorize]`.

### Done when

Signing key lives only in user-secrets · a member token gets 403 on class/trainer writes ·
Swagger can authenticate · 401/403 return JSON

---

## Phase 1 — Schema

Three migrations, no API changes. Everything downstream is written against this shape, so it lands
in one go before any new controller work.

**Depends on:** Phase 0 · **Size:** medium

### 1.1 Decide first (blocks the migration)

- [x] **Lookups or enums?** → **Enums.** `Domain/Enums/AttendanceStatus.cs` and `BookingSource.cs`,
      mapped with `HasConversion<byte>()` in `AppDbContext`. `Lookups` keeps class categories and the
      session/booking lifecycle statuses.
- [x] **UTC or local?** → **UTC.** `SessionService.ToUtc` normalises every inbound `DateTime`
      (unspecified kind is assumed UTC); responses stamp `DateTimeKind.Utc` on the way out.

### 1.2 M1 — attendance on bookings

- [x] `Bookings.AttendanceStatus` — enum column (was going to be `AttendanceStatusId`; enums won)
- [x] `Bookings.Source` — enum column
- [x] `Bookings.CheckedInAt` (null), `MarkedByUserId` (FK → Users, null), `CancelledAt` (null)
- [x] Second `Users → Bookings` relationship configured with `.WithMany()` and no inverse navigation
- [x] Defaults set at the database level (`Booked` / `Online`), so existing rows backfill on migrate
- [ ] **Generate and apply the migration** — `dotnet ef migrations add AttendanceAndCounters`

### 1.3 M2 — counters and indexes

- [x] `Sessions.SeatsTaken`, `Sessions.CheckedInCount` — `int not null default 0`
- [x] `IX_Sessions_StartAt`
- [x] `IX_Sessions_ClassId_StartAt`
- [x] `IX_Bookings_SessionId_AttendanceStatus`
- [x] `IX_Bookings_UserId_CreatedAt`
- [ ] A reconciliation query that recomputes both counters from `Bookings` — you will need it the
      first time a bug leaves a counter wrong

### 1.4 M3 — Staff role, soft delete, auth hooks

- [x] `UserRole.Staff = 4` — appended, existing values untouched. `Helpers/RoleNames.cs` holds the
      role strings so `[Authorize(Roles = ...)]` can't drift from the enum
- [ ] `IsActive` + `DeletedAt` on Users, Trainers, Classes (Sessions and Bookings stay hard rows —
      they are the history the dashboard reads) — *not done in the CRUD pass*
- [ ] EF global query filters (`HasQueryFilter`) so reads exclude soft-deleted rows automatically
- [ ] **Make `Classes.Name` and `Users.Email` filtered unique indexes** (`WHERE IsActive = 1`) or a
      soft-deleted row squats that name forever
- [ ] `Users.SecurityStamp` (guid, null) — the revocation hook for when refresh tokens arrive
- [ ] `Users.LastLoginAt` (null) — set it in `AuthService.LoginAsync`
- [ ] Add `Staff` to the seeder

### Done when

Migrations apply cleanly on an existing database · soft-deleted rows disappear from reads without
call-site changes · a name freed by a soft delete can be reused

---

## Phase 2 — Sessions API

**Depends on:** Phase 1 · **Size:** medium

- [x] `GET /api/sessions?from=&to=&classId=&trainerId=` — date range required (400 without it)
- [x] `GET /api/sessions/{id}` — carries `SeatsTaken`, `SeatsLeft`, `CheckedInCount`
- [x] `POST /api/sessions` — Admin/Staff; capacity falls back to `Class.InitCapacity`
- [x] `POST /api/sessions/generate` — repeats every N days to an end date; skips occurrences that
      already exist, so re-running it is safe
- [x] `PUT /api/sessions/{id}` — 409 if capacity would drop below `SeatsTaken`
- [x] `POST /api/sessions/{id}/cancel` — status change, bookings preserved
- [x] `GET /api/sessions/{id}/roster` — Admin/Staff, or the session's own trainer
- [x] Paged envelope `{ items, page, pageSize, total }` — `Models/PagedResponse.cs`

**Files:** `Services/SessionService.cs`, `Controllers/SessionsController.cs`, `Models/Session*.cs`

---

## Phase 3 — Bookings API

**Depends on:** Phase 2 · **Size:** medium — *the concurrency bit is the real work*

- [x] `POST /api/bookings` — body carries `sessionId` only; member id read from the token
- [x] `DELETE /api/bookings/{id}` — owner or Admin/Staff; sets `CancelledAt`, returns the seat
- [x] `GET /api/bookings/me?from=&to=`
- [x] `POST /api/bookings/for-member` — Admin/Staff; writes `Source = StaffBooked`
- [x] Rejects a started session, a cancelled session, and a duplicate booking
- [x] Returns **409** when full
- [x] `GET /api/bookings/{id}` — owner or Admin/Staff (added; `CreatedAtAction` needs it)
- — Late-cancel classification skipped: no window for now (decision 4), `LateCancel` stays unused

### The seat claim

```sql
UPDATE Sessions SET SeatsTaken = SeatsTaken + 1
WHERE Id = @id AND SeatsTaken < Capacity
```

Zero rows affected → full → 409. Inside the same transaction as the booking insert. A read-then-write
here lets two members take the last seat, and it is the bug most likely to reach production.

- [x] Implement claim + insert in one transaction — `ExecuteUpdateAsync` carrying the
      `SeatsTaken < Capacity` predicate; 0 rows affected means full, so it rolls back and returns 409
- [x] Mirror it on cancel, guarded by `SeatsTaken > 0` so a double cancel cannot go negative
- [ ] **Write the concurrent last-seat test** — two parallel bookings against a session with one seat;
      exactly one succeeds. Still the first test this project should have

---

## Phase 4 — Attendance API

**Depends on:** Phase 3 · **Size:** small–medium

- [x] `POST /api/sessions/{id}/attendance` — bulk `{bookingId, status}` list, returns the updated
      roster; rejects duplicates, foreign bookings and cancelled bookings
- [x] `POST /api/sessions/{id}/walk-in` — books with `Source = WalkIn`, then marks Attended
- [x] `PATCH /api/bookings/{id}/attendance` — single correction; records `MarkedByUserId`
- [x] Trainers may mark their own sessions only — checked from the token via
      `ISessionService.IsTrainerForSessionAsync`
- [x] `CheckedInCount` kept in step, including when a mark is reversed

### The no-show sweep — not optional

- [x] `Services/NoShowSweepService.cs` — a `BackgroundService` on a `PeriodicTimer` that flips stale
      `Booked` rows to `NoShow`, leaving `MarkedByUserId` null. That null is how you tell an automatic
      no-show from one a person recorded
- [x] Grace and interval configurable — `Configuration/AttendanceOptions.cs`, bound to the
      `Attendance` section (`NoShowGraceMinutes` 120, `SweepIntervalMinutes` 15)
- [x] One set-based `ExecuteUpdateAsync`, no entities loaded; skips cancelled bookings and cancelled
      sessions; sweeps once at startup so a restart catches anything missed while the app was down;
      a failed pass is logged and retried on the next tick rather than killing the loop

> **`NoShowGraceMinutes` is standing in for a duration the schema doesn't have.** Sessions have a
> `StartAt` but no length, so "the session has ended" is currently "120 minutes after it started".
> When sessions gain a duration this should become `StartAt + duration + a short grace`. Worth
> knowing if class lengths vary a lot — a 45-minute class is marked 75 minutes later than it needs.

Without this, "no-show rate" measures which sessions nobody remembered to close out, and every
attendance-derived number on the dashboard is wrong.

---

## Phase 5 — Dashboard API

**Depends on:** Phase 4 · **Size:** medium

- [x] `GET /api/dashboard/summary?from=&to=` — one payload: attendance rate, no-show rate,
      utilization, active members, plus the raw counts behind them. Defaults to the last 7 days
- [x] `GET /api/dashboard/today` — sessions with fill, check-in count, `IsUnderFilled` / `IsFull`
- [ ] `GET /api/dashboard/utilization?weeks=8` — day × hour heatmap; cache 5 minutes
- [ ] `GET /api/dashboard/at-risk` — named members (4+ attendances last month, 0 in 14 days); returns
      a call list, not a count
- [ ] `GET /api/dashboard/trainers` — **Admin only**; useful and quietly political
- [ ] Every figure ships with its comparison period — a bare number gives the reader nothing to judge

### Also in this phase

- [ ] Rewrite `ClassService.ToResponse` fill-rate over the `SeatsTaken` counter instead of loading
      `Sessions → Bookings → Status` for every class
- [ ] Add paging to `GET /api/classes`

Both still outstanding — the CRUD pass left `ClassService` untouched.

---

## Phase 6 — Seed data

**Depends on:** Phase 5 · **Size:** small — *disproportionate payoff*

- [ ] ~8 weeks of backdated sessions, bookings and attendance, behind the existing `Seed:Enabled` flag
- [ ] Plausible shape: evenings fuller than mid-mornings, ~15% no-shows, one clearly failing class
- [ ] A Staff user alongside the existing Admin and trainers

An empty dashboard cannot be evaluated or demoed. This is what makes Phase 5 assessable.

---

## Backlog

Nothing here blocks the phases above.

| Item | Roadmap | Note |
|---|---|---|
| Waitlist | M4 | Highest-value feature after attendance; reuses the Phase 3 seat claim |
| Memberships & credits | M5 | `CreditsLeft` has the same concurrency shape as `SeatsTaken` |
| Locations + templates | M6 | Add `LocationId` early if a second branch is plausible — expensive to retrofit |
| Refresh tokens | — | `RefreshTokens` table; plan together with password reset, same table |
| Notifications | — | Reminders measurably cut no-shows; pays back the attendance work |
| Timestamps in `SaveChanges` | — | Removes a whole category of forgetting |
| Drop the `"NotFound"` string | — | `AppException` already exists; use it |
| Check constraints | — | `Capacity > 0`, `SeatsTaken <= Capacity` |
| CI on PRs | — | Build + tests, once tests exist |

---

## Open questions

All five original questions were answered on 2026-09-20 — see the decisions table at the top.

Raised by the CRUD pass:

1. **Should cancelling a session release its bookings?** `POST /sessions/{id}/cancel` currently leaves
   them Confirmed, so members keep a booking against a session that will not run. Auto-cancelling is
   probably right, but it needs a notification story to be worth much.
2. **Should `GET /api/sessions` hide cancelled sessions by default?** It returns them with their
   status today — right for staff, probably wrong for members.
3. **How far ahead may `POST /sessions/generate` run?** No upper bound on the date range today, so one
   call can create a lot of rows.

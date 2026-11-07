# School Management System — Project Brief

## Purpose of this project (read first)

This is a **portfolio project** for Yael Turk (GitHub: `yael-72`), a junior full-stack developer
who is actively job hunting. The single most important goal: **the repository must impress
technical recruiters and hiring engineers** who open it on GitHub.

That means:
- Code quality, structure and naming matter as much as features working.
- The README, commit history, tests and live demo are part of the deliverable.
- Prefer a smaller set of features done *properly* over many half-finished ones.
- **Yael must be able to explain every line in a job interview.** Use industry-standard,
  well-known patterns. Do not over-engineer or add clever abstractions she can't defend.
  When you make an architectural decision, explain *why* briefly, in plain terms.

## What the app is

An admin system for a school. The principal (Admin) and teachers log in to manage classes,
students, attendance, exams and grades, and see an analytics dashboard summarizing the school.

The UI is based on the **Horizon UI** free admin template Figma design (by Simmmple):
left sidebar + top navbar + card-based content area. Yael has the Figma file.

## Tech stack

| Layer     | Technology |
|-----------|------------|
| Backend   | ASP.NET Core Web API (latest LTS .NET), C# |
| ORM       | Entity Framework Core, **code-first with migrations** |
| Database  | SQL Server |
| Auth      | JWT bearer tokens, password hashing (never store plain passwords) |
| Frontend  | Angular (latest), standalone components, signals, reactive forms |
| Charts    | ng-apexcharts |
| Styling   | SCSS with CSS variables (design tokens) — supports light/dark mode |
| Tests     | xUnit (+ FluentAssertions) for backend; Angular unit tests for key services |
| DevOps    | Docker Compose (API + SQL Server), GitHub Actions CI (build + test) |

## Repository structure

```
/
├── CLAUDE.md
├── README.md                     # with screenshots, features, how to run, tech decisions
├── docker-compose.yml
├── .github/workflows/ci.yml
├── database/
│   └── school_db.sql             # original schema design (reference only; EF migrations are the source of truth)
├── backend/
│   ├── SchoolManagement.sln
│   ├── src/
│   │   ├── SchoolManagement.Api/            # controllers, middleware, Program.cs, auth config
│   │   ├── SchoolManagement.Application/    # services, DTOs, validators, interfaces
│   │   ├── SchoolManagement.Domain/         # entities, enums, business rules
│   │   └── SchoolManagement.Infrastructure/ # DbContext, EF configurations, migrations, seed data
│   └── tests/
│       ├── SchoolManagement.UnitTests/
│       └── SchoolManagement.IntegrationTests/
└── frontend/
    └── src/app/
        ├── core/        # auth service, JWT interceptor, error interceptor, guards, models
        ├── layout/      # sidebar, navbar, main layout shell
        ├── shared/      # reusable UI: card, stat-card, data-table, status-badge, avatar, chart wrappers
        └── features/
            ├── auth/        # sign in
            ├── dashboard/
            ├── classes/
            ├── students/
            ├── attendance/
            ├── assessments/ # exams & grades
            └── profile/
```

Keep the layering simple: Controllers → Application services → DbContext.
No generic repository on top of EF Core unless clearly justified.

## Database

Tables (see `database/school_db.sql` for full column definitions and constraints):

- **Roles** (`Id`, `Role`) — seeded with `Admin`, `Teacher`
- **Users** — `RoleId` FK → Roles (one role per user)
- **Classes** — `Name`, `GradeLevel`, `SchoolYear`, `HomeroomTeacherId` FK → Users
- **Students** — belongs to one class; soft delete via `IsActive`
- **Subjects**
- **TeacherAssignments** — which teacher teaches which subject in which class.
  **This drives teacher permissions**, not the role. An Admin can also have assignments.
- **Assessments** — exam / assignment / quiz for a class + subject, with `MaxScore`
- **Scores** — unique per (AssessmentId, StudentId)
- **Attendance** — unique per (StudentId, Date); status Present / Absent / Late / Excused
- **Events** — calendar (class-specific or school-wide when `ClassId` is null)
- **TodoTasks** — personal to-do list per user (dashboard widget)
- **UploadedFiles** — user file uploads (profile page)
- **NotificationSettings** — per-user toggles

Recreate this schema with EF Core entities + Fluent API configurations + migrations.

### Seed data
Provide realistic seed data (development only) so the dashboard looks alive on first run:
~1 admin, ~6 teachers, ~8 classes (grades 7–12), ~200 students with Hebrew names,
several subjects, exams across the last few months with scores, and attendance history.
Include a documented demo login in the README.

## Business rules (enforce on the server, not only in the UI)

- Admin: full access to everything.
- Teacher: sees and edits **only** classes/subjects she is assigned to via TeacherAssignments
  (and her homeroom class). Return 403 otherwise.
- A score cannot be negative or exceed the assessment's `MaxScore`.
- One score per student per assessment; one attendance record per student per day (upsert).
- Attendance cannot be recorded for a future date.
- Inactive students do not appear in lists, stats or attendance by default.
- Deleting a class that still has active students is rejected with a clear error.

## API endpoints

All under `/api`, JSON, RESTful, ProblemDetails for errors.
List endpoints support `page`, `pageSize`, `search`, `sortBy`, `sortDir` and return
`{ items, totalCount, page, pageSize }`.

**Auth**
- `POST /auth/login` → JWT + user info
- `GET  /auth/me`

**Users & roles** (Admin)
- `GET/POST /users`, `GET/PUT/DELETE /users/{id}`, `PATCH /users/{id}/status`
- `GET /roles`
- `GET/PUT /users/me`, `GET/PUT /users/me/notification-settings` (any logged-in user)

**Classes**
- `GET /classes` (teacher → only her classes), `GET /classes/{id}`
- `POST/PUT/DELETE /classes/{id}` (Admin)
- `GET /classes/{id}/students`

**Students**
- `GET /students?classId=`, `GET /students/{id}`, `POST`, `PUT`, `DELETE` (soft)
- `GET /students/{id}/summary` → average, attendance %, recent scores

**Subjects** — CRUD (Admin)
**Teacher assignments** — `GET/POST/DELETE /teacher-assignments` (Admin)

**Assessments & scores**
- `GET /assessments?classId=&subjectId=&from=&to=`, `GET /assessments/{id}`, `POST`, `PUT`, `DELETE`
- `GET /assessments/{id}/scores`
- `PUT /assessments/{id}/scores` → bulk upsert of all scores for the class

**Attendance**
- `GET /attendance?classId=&date=`
- `PUT /attendance` → bulk upsert for a class on a date
- `GET /attendance/report?classId=&from=&to=`

**Dashboard** (scoped by role)
- `GET /dashboard/stats` → students, classes, today's attendance %, average score,
  exams this week, students with frequent absences
- `GET /dashboard/grades-trend` (line chart), `GET /dashboard/attendance-weekly` (bar chart),
  `GET /dashboard/grade-distribution` (pie), `GET /dashboard/top-students`,
  `GET /dashboard/recent-activity`

**Events** — `GET /events?from=&to=`, `POST`, `PUT`, `DELETE`
**Tasks** — `GET/POST /tasks`, `PUT/DELETE /tasks/{id}`, `PATCH /tasks/{id}/toggle`
**Files** — `POST /files` (multipart, size + type limits), `GET /files`,
`GET /files/{id}/download`, `DELETE /files/{id}`

## Screens — mapping from the Horizon UI design

| Horizon UI screen | This app |
|---|---|
| Sign In | Login (email + password, show/hide password, "keep me logged in") |
| Main Dashboard | KPI stat cards, grades trend line chart, weekly attendance bar chart, grade distribution pie, to-do list, mini calendar, top students |
| NFT Marketplace | **Classes** — card grid (class name, homeroom teacher, student count, average), tabs filtering by grade level; side panel: top students + recent activity |
| Data Tables | **Students** table (search/sort/paginate), **Attendance** table with checkboxes per student, **Grades** table with color-coded scores, **Assessments** table |
| Profile | User profile, uploaded files, storage usage, notification settings toggles |

Design notes:
- Extract design tokens (colors, radius, shadows, font) from the Figma into CSS variables.
  Horizon defaults to approximately: primary `#4318FF`, text `#2B3674`, secondary text `#A3AED0`,
  background `#F4F7FE`, card radius 20px, font DM Sans. Verify against Figma.
- **Avatars: use initials-based avatars (colored circle with initials) — no photos of people.**
- Support **Hebrew RTL** layout and **dark mode** toggle.
- Fully responsive (sidebar collapses on mobile).
- Loading skeletons, empty states, and friendly error messages on every data view.

## Quality bar — things recruiters look for

Backend:
- DTOs for all input/output — never expose EF entities from controllers.
- Input validation (FluentValidation or DataAnnotations) with clear messages.
- Global exception-handling middleware → RFC 7807 ProblemDetails.
- Async all the way; `AsNoTracking` for reads; projection with `Select` for list endpoints.
- Configuration via `appsettings` + user-secrets / environment variables — **no secrets in the repo**.
- Swagger/OpenAPI with JWT "Authorize" button.
- Structured logging (built-in ILogger or Serilog).
- Unit tests for business rules (permissions, score limits, attendance rules);
  integration tests for key endpoints (WebApplicationFactory).

Frontend:
- Strong typing (interfaces for all API models), no `any`.
- HTTP interceptor adds the JWT; error interceptor handles 401 → redirect to login.
- Route guards by role.
- Reusable shared components instead of copy-pasted markup.
- Reactive forms with validation messages.
- Lazy-loaded feature routes.

Repo:
- Clean, small commits with clear messages (e.g. `feat: add attendance bulk upsert endpoint`).
- README: short pitch, screenshots/GIF, feature list, tech stack, architecture diagram,
  how to run locally (Docker one-liner), demo credentials, and "technical decisions" section.
- CI workflow runs build + tests on every push.
- Code, comments, commit messages and README in **English**; UI text in Hebrew.

## Build order (milestones)

1. **Foundation** — solution + projects, EF entities & migrations, seed data, Angular app with
   layout shell (sidebar/navbar), design tokens, routing.
2. **Auth** — login endpoint, JWT, password hashing, Angular login page, interceptor, guards.
3. **Students & Classes** — CRUD endpoints with pagination/search, Classes card grid, Students table.
4. **Attendance** — bulk attendance screen + endpoints + rules.
5. **Assessments & Grades** — exams, bulk score entry, color-coded grades table.
6. **Dashboard** — stats + chart endpoints, charts, to-do list, calendar.
7. **Profile** — profile edit, file upload, notification settings.
8. **Polish** — dark mode, RTL review, responsiveness, empty/loading states, tests, Docker,
   CI, README with screenshots, deploy a live demo.

Each milestone should end in a working, committed state.

## How to work with Yael

- She is a junior developer: explain non-obvious decisions briefly and clearly.
- Work milestone by milestone; don't jump ahead or generate the whole app at once.
- Run `dotnet build`, `dotnet test` and `ng build` before saying something is done.
- Ask before major decisions not covered here (new libraries, structural changes).
- Commit after each meaningful step with a clear message.
## Project status (keep this section up to date at the end of every milestone)

Work happens on branch `claude/gifted-keller-p6dfi2`. Start each new session by reading this section.

### Done
- **Milestone 1 – Foundation**: .NET 10 solution (Api / Application / Domain / Infrastructure + 2 test
  projects), EF Core entities aligned with `database/school_db.sql`, `InitialCreate` migration,
  development seed data, Angular 22 shell (sidebar, navbar, design tokens, RTL, dark mode, lazy routes).
- **Milestone 2 – Auth**: `POST /api/auth/login`, `GET /api/auth/me`, JWT bearer, PBKDF2 hashing,
  global exception handler → ProblemDetails, authenticated-by-default fallback policy, Swagger
  Authorize button. Angular: AuthService (signals), auth + 401 interceptors, `authGuard`,
  `guestGuard`, `roleGuard(...)`, sign-in page from Figma, user menu with logout.

### Next
- **Milestone 3 – Students & Classes** (see "Build order" above).

### Decisions already made (do not re-ask)
- **Design source**: Horizon UI Figma file `YH394Y7LXSOwGdQuhrKoLU` (Figma connector). Match it
  exactly (measurements, colors, typography) except: RTL mirror, Hebrew font fallback Heebo,
  initials avatars, and template-only items removed (Google sign-in, "Upgrade to PRO", marketing links).
  Light pages are on canvas `0:1`, dark pages on `1:18051`. Key frames: Main Dashboard `201:1804`,
  NFT Marketplace `101:10012`, Tables `101:9569`, Profile `101:9076`, Sign In `101:9004`.
  figma.com downloads are blocked by the network: export images with `get_screenshot`.
- **Demo data**: male names only (students, teachers, admin). Demo login: `admin@school.demo` /
  `teacher@school.demo`, password `Demo123!`. Dev database name: `SchoolManagementDev`.
- **Naming**: entity `SchoolClass` → table `Classes`; `SchoolEvent` → `Events`; `Score.Value` → column `Score`.
- **Layering**: services in Application use `IApplicationDbContext`; Infrastructure implements
  `IPasswordHasher`, `ITokenService`; Api implements `ICurrentUser`. Services throw
  `NotFoundException` / `ForbiddenException` / `BusinessRuleException` / `AuthenticationFailedException`;
  `GlobalExceptionHandler` maps them to 404 / 403 / 400 / 401.
- **Validation**: DataAnnotations on request DTOs (no FluentValidation). API messages in English,
  the Angular app translates them to Hebrew.
- **Testing**: xUnit + FluentAssertions **7.2** (v8 needs a paid license). EF InMemory for unit tests
  and for `ApiFactory` integration tests (Testcontainers was not chosen yet – ask first).
  Angular uses Vitest (`ng test`).
- **Frontend conventions**: Angular 22 file naming (`login-page.ts`, class `LoginPage`), signals,
  standalone components, logical CSS properties only (RTL), every color from `styles/_tokens.scss`,
  shared form classes in `styles/_forms.scss`. `/api` is proxied to `http://localhost:5097`
  (`proxy.conf.json`). JWT stored in localStorage ("keep me logged in") or sessionStorage.
- **Security**: no secrets in the repo; in Development a random JWT key is generated when
  `Jwt:SigningKey` is missing.

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

Latest work is on branch `claude/vigilant-fermat-rh4wkw` (milestones 5–7). Earlier branches: `claude/serene-feynman-cx8m35`
(milestone 4), `claude/ecstatic-knuth-hgil1o` (3), `claude/gifted-keller-p6dfi2` (1–2).
Start each new session by reading this section.

### Done
- **Milestone 1 – Foundation**: .NET 10 solution (Api / Application / Domain / Infrastructure + 2 test
  projects), EF Core entities aligned with `database/school_db.sql`, `InitialCreate` migration,
  development seed data, Angular 22 shell (sidebar, navbar, design tokens, RTL, dark mode, lazy routes).
- **Milestone 2 – Auth**: `POST /api/auth/login`, `GET /api/auth/me`, JWT bearer, PBKDF2 hashing,
  global exception handler → ProblemDetails, authenticated-by-default fallback policy, Swagger
  Authorize button. Angular: AuthService (signals), auth + 401 interceptors, `authGuard`,
  `guestGuard`, `roleGuard(...)`, sign-in page from Figma, user menu with logout.
- **Milestone 3 – Students & Classes**: `GET/POST/PUT/DELETE /api/classes`, `GET /api/classes/{id}/students`,
  `GET/POST/PUT/DELETE /api/students` (soft delete), `GET /api/students/{id}/summary`, read-only
  `GET /api/users` (admin, feeds the homeroom teacher picker). Shared `ListQuery` / `PagedResult<T>` /
  `ToPagedResultAsync`. Angular: classes card grid (grade tabs, search, admin dialogs), students table
  (debounced search, sortable headers, paging, `?classId=` filter), student details page (`/students/:id`).
  Shared: `Paginator`, `Dialog` (native `<dialog>`), `Skeleton`, `ScoreBadge`, `toErrorMessage()`.

- **Milestone 4 – Attendance**: `GET /api/attendance?classId=&date=` (register: every active student +
  that day's status or null), `PUT /api/attendance` (bulk upsert for one class and day; rejects future
  dates, duplicate students, and students who are not active in the class), `GET /api/attendance/report`
  (per-student totals + class rate, range ≤ 366 days). `AttendanceRate.Calculate` is the single formula.
  Enums are serialized as names (`JsonStringEnumConverter`). Angular: attendance page with a class picker
  and two views – daily register (status radio pills, note, "mark the rest present", one save, stacked
  rows on phones) and period report. Shared `dates.ts` (`toIsoDate` uses local date parts) and
  `route-params.ts` (`toPositiveId`).

- **Milestone 5 – Assessments & Grades**: `GET/POST/PUT/DELETE /api/assessments` (filters classId, subjectId,
  type, from, to; default order newest first; each row has `canEdit`), `GET /api/assessments/{id}/scores`
  (grade sheet: every active student + average/highest/lowest), `PUT /api/assessments/{id}/scores` (bulk
  upsert), `GET /api/subjects` (any user). Rules: 0 ≤ score ≤ MaxScore, at most 2 decimals, no scores before
  the assessment date, only active students of the class, MaxScore cannot drop below an existing score,
  a graded assessment cannot move to another class, delete removes its scores. Angular: `/assessments`
  table (search, class/subject filters, type tabs, sorting, paging, create/edit/delete dialogs) and
  `/assessments/:id` (details, stat tiles, grade sheet with live color-coded percentages, read-only mode,
  stacked rows on phones). Shared `_tabs.scss` (`.pill-tabs`), `AssessmentTypeChip`.

- **Milestone 6 – Dashboard**: `GET /api/dashboard/stats | grades-trend?months= | attendance-weekly |
  grade-distribution | top-students?count= | recent-activity?count=` (all scoped by role, computed in SQL),
  `GET/POST /api/tasks`, `PUT/DELETE /api/tasks/{id}`, `PATCH /api/tasks/{id}/toggle`,
  `GET /api/events?from=&to=` (overlapping events, ≤ 366 days), `POST/PUT/DELETE /api/events`.
  Domain `SchoolCalendar` (Sunday–Thursday school days, week starts Sunday). Angular: dashboard page laid
  out like the Figma Main Dashboard (6 `StatCard`s, grades-trend area chart, stacked weekly-attendance bars,
  top students, frequent absences, grade-distribution pie, recent activity, to-do list, mini calendar with an
  add-event dialog). Charts use ng-apexcharts (lazy-loaded with the dashboard) and `injectChartColors()`
  (reads the CSS tokens, recalculated on theme change). Classes page got its side panel (top students +
  recent activity), closing the milestone-3 open item.

- **Milestone 7 – Profile**: `GET/PUT /api/users/me` (name + phone only; email and role are admin-managed),
  `GET/PUT /api/users/me/notification-settings` (defaults when no row yet), `GET /api/files` (files +
  used/quota bytes), `POST /api/files` (multipart field `file`), `GET /api/files/{id}/download`,
  `DELETE /api/files/{id}`. `IFileStorage` (Application) ← `LocalFileStorage` (Infrastructure, folder from
  `FileStorage:RootPath`, default `App_Data/uploads`, git-ignored). Angular: profile page laid out like the
  Figma Profile frame (gradient banner + initials avatar + 3 counts, storage bar, drag-and-drop upload with
  progress, files list with authenticated blob download, personal details tiles + edit form, notification
  toggles saved instantly). Shared `Toggle` (controlled switch) and `formatBytes()`.

### Next
- **Milestone 8 – Polish** (see "Build order" above): Docker, CI, README with screenshots, live demo,
  RTL/responsive review, and the remaining admin screens (users, subjects, teacher assignments) if time allows.

### Open items
- Classes and Students screens were built from the design tokens without opening their Figma frames
  (`101:10012`, `101:9569`): the Figma MCP Starter-plan call limit was reached. Compare and fine-tune.

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
- **Permissions**: `IClassAccessService` is the single place for the teacher rule (TeacherAssignments ∪
  homeroom class). Every service touching class data uses it: filter lists with `AccessibleClassIds()`
  (null = admin), and call `EnsureCanAccessClassAsync` for a single item (404 is checked before 403).
  Class and student create/update/delete are Admin-only (`[Authorize(Roles = Admin)]`); teachers read only.
- **List endpoints**: query class inherits `ListQuery` (page, pageSize ≤ 100, search, sortBy, sortDir);
  unknown `sortBy` falls back to the default order; always end with a unique key (Id) for stable pages.
  DTOs that a query sorts on must be init-property records built with an object initializer – EF Core
  cannot translate ORDER BY on a constructor-built DTO. EF InMemory does NOT catch this; check new
  queries with `ToQueryString()` against the SQL Server provider.
- **Attendance**: teachers record attendance for every class they can access (homeroom or subject
  teacher). The register sends only rows that have a status; unmarked students stay unrecorded.
  "Today" on the server is the UTC date (same as the birth-date rule), so between midnight and ~02:00
  Israel time the server may still reject "today" as a future date – acceptable for now.
- **Grading permission** (`IClassAccessService.EnsureCanGradeAsync` / `GetGradableSubjectsAsync`):
  admin always; a teacher only for a (class, subject) pair in TeacherAssignments. A homeroom teacher
  *reads* all assessments of her class but edits only her own subjects. Reading needs class access only.
  Assessments have no Admin-only endpoints – the rule is data-based, so it lives in the service.
- **Scores**: a row with score null and no comment clears the student's score (row deleted); a null
  score with a comment is kept ("missed the exam"). The grade sheet sends only the changed rows.
- **Dashboard**: "frequent absences" = ≥ 4 absences in the last 30 days (top 5). Grade bands
  (90/75/55) live in `GradeBands` (API) and `scoreLevel()` (Angular) – keep them equal. Averages, top
  students and the distribution are all-time (not per school year). Recent activity merges "assessment
  created" (with the creator) and "scores entered" (latest Score.UpdatedAt; scores do not store who).
  Charts keep a left-to-right time axis inside `dir="ltr"` also in the RTL layout.
- **Tasks & events**: tasks are personal; another user's task is 404 (not 403). Tasks and events lists are
  plain arrays (not paged). School-wide events (ClassId null) are admin-only; a teacher adds events to
  her accessible classes and edits/deletes only the events she created.
- **Files**: private per user (another user's file → 404). Limits in `FileRules`: 5 MB per file, 50 MB per
  user, documents and images only (allowlist of extensions); the Angular `FILE_RULES` mirror them for instant
  feedback. Stored under a server-generated name (`yyyy/MM/{guid}.ext`), path checked to stay inside the root,
  always downloaded as an attachment with `X-Content-Type-Options: nosniff`. Over-size requests: the multipart
  limit returns 400 (validation), Kestrel's request limit may return 413 (`BadHttpRequestException` is mapped).
- **Profile**: after saving, `AuthService.updateCurrentUser()` updates the stored session so the navbar shows
  the new name. Changing the password is not implemented yet.
- **Averages** are percentages: `Score.Value * 100 / Assessment.MaxScore`, ungraded (null) scores and
  inactive students excluded, rounded to 1 decimal in C#. Attendance rate = (Present + Late) / recorded days.
- **Validation**: DataAnnotations on request DTOs (no FluentValidation). API messages in English,
  the Angular app translates them to Hebrew.
- **Testing**: xUnit + FluentAssertions **7.2** (v8 needs a paid license). EF InMemory for unit tests
  and for `ApiFactory` integration tests (Testcontainers was not chosen yet – ask first).
  Angular uses Vitest (`ng test`).
- **Frontend conventions**: Angular 22 file naming (`login-page.ts`, class `LoginPage`), signals,
  standalone components, logical CSS properties only (RTL), every color from `styles/_tokens.scss`,
  shared form classes in `styles/_forms.scss`. `/api` is proxied to `http://localhost:5097`
  (`proxy.conf.json`). JWT stored in localStorage ("keep me logged in") or sessionStorage.
  Data loading: feature services return Observables; pages use `rxResource({ params, stream })` driven by
  signals, with search debounced via `toSignal(toObservable(...).pipe(debounceTime(300)))`. Global styles:
  `_buttons.scss` (`.button`, `.icon-button`), `_tables.scss` (`.data-table`, `.sort-button`, `.list-toolbar`).
  Grade bands (ScoreBadge): ≥90 excellent, ≥75 good, ≥55 fair, else low.
- **Cloud sessions**: the .NET SDK is not preinstalled and dot.net is blocked; install it with
  `apt-get install -y dotnet-sdk-10.0`. There is no SQL Server, so check UI changes with Playwright and
  mocked `/api` responses (the backend is covered by the integration tests).
- **Security**: no secrets in the repo; in Development a random JWT key is generated when
  `Jwt:SigningKey` is missing.

# Backend Pendencies — Admin Panel: Courses (screen skipped)

No spec was written for this screen. Decision, 2026-09-04: evaluated
building the mockup's admin panel (artboard `1k`, "Painel admin —
cursos e áreas", "Cursos" section) and chose to skip it for now rather
than ship it with a foot-gun at its center. Revisit once the two
blocking gaps below are closed on the CourseCore side.

**Update, 2026-09-07: all three backend gaps below are closed.** The
admin screen itself remains unbuilt — no spec was ever written for it,
and writing one is frontend work out of scope for this backend repo —
but neither of the two reasons it was originally skipped applies
anymore.

## 1. No endpoint lists all courses (published + draft) — CLOSED (was BLOCKING)

- **Mockup expects**: an admin course table showing every course
  regardless of status, including drafts ("Rascunho" — the "C.D.F 2026"
  row in the mockup).
- **Backend today**: `GET /api/courses/available` is the only course
  listing endpoint that exists, and it's the *student-facing* catalog
  endpoint — `ListAvailableCoursesUseCase` →
  `CourseAccessService.ListCatalogAsync` →
  `ICourseRepository.ListPublishedAsync()`, published courses only.
  `ICourseRepository.ListAsync()` exists at the repository layer (would
  return everything, drafts included) but no UseCase or Controller
  route anywhere calls it — the same "capability exists internally,
  never wired to a consumer" pattern already documented for
  `IVideoRepository.FindByLessonIdAsync` in
  `Docs/backend-pendencies/catalog/lesson-player.md`.
- **What's needed**: a new admin-only UseCase + Controller route (e.g.
  `GET /api/courses`, behind the existing `ManageCourses` policy) that
  calls the repository's already-existing `ListAsync()`.
- **Why this blocked building the screen instead of just being another
  missing field**: without it, a course created as a draft and not
  published in the same admin session becomes permanently unreachable
  through this screen — no way to rediscover its id later, since it
  never appears in the only list endpoint available. That's a real
  workflow break, not a cosmetic gap, so this plan chose not to ship it
  rather than document it as an accepted quirk.
- **Severity**: Blocking.
- **Resolved, 2026-09-07**: `GET /api/courses` (new `ListAllCoursesUseCase`,
  calling the already-existing `ICourseRepository.ListAsync()`),
  `ManageCourses` policy — exactly what "What's needed" already
  specified. Returns every course regardless of `Published` status.

## 2. No audit-log read endpoint — CLOSED (was BLOCKING)

- **Mockup expects**: "Últimas ações auditadas" panel listing recent
  admin actions with relative timestamps.
- **Backend today**: the `AuditLogs` module fully exists and genuinely
  records actions — `IAuditLogService.RecordAsync` is called by real use
  cases (e.g. `PublishCourseUseCase` records
  `AuditLogActionNames.CoursePublished`). The mockup's own example
  strings ("CoursePublished · Curso de Batismo",
  "UserAreaAccessGranted · ana.souza@email.com → Liderança",
  "VideoCreated · Aula 03 — Módulo 01") match the real
  `AuditLogActionNames` constants and their `RecordAsync` call sites
  exactly — this panel was clearly designed against real backend
  action names, not invented. But
  `Modules/AuditLogs/Presentation/Controllers/` is empty (only a
  `.gitkeep`) — no Controller, no read route anywhere over the
  already-populated `audit_logs` table.
- **What's needed**: a `GET /api/audit-logs` (or similar, admin-only)
  endpoint.
- **Severity**: Blocking for this specific panel — the rest of the
  screen doesn't depend on it, but there's no partial version of an
  audit trail worth shipping.
- **Resolved, 2026-09-07**: `GET /api/audit-logs?page=&pageSize=`,
  paginated (mirrors `Modules/Users/`'s existing `PagedResult`/
  `PagedResponse` pattern exactly), newest-first, each entry exposing
  `Action, EntityName, EntityId, UserId, Metadata (parsed from the
  already-stored MetadataJson), CreatedAt`. Gated by a **new**
  `AuthPolicyNames.ReadAudit` policy wired to `AuthPermissionNames.ReadAudit` —
  that permission constant already existed in the codebase (even
  already seeded as a real `Permission` row in the integration test
  factory) but was never attached to a policy or a controller before
  this; this closes that gap too.

## 3. No unpublish endpoint — CLOSED

- Not shown directly in the mockup, but a real course-management screen
  would need it once courses can be listed at all.
- **Backend today**: `Course.Unpublish()` exists as a domain method but
  no UseCase or Controller ever calls it — the same "exists, not wired"
  pattern as pendency 1.
- **Severity**: Feature gap.
- **Resolved, 2026-09-07**: `POST /api/courses/{courseId:guid}/unpublish`
  (new `UnpublishCourseUseCase`, structural mirror of the existing
  `PublishCourseUseCase`), same `ManageCourses` policy, records a new
  `AuditLogActionNames.CourseUnpublished`.

## What's already real (for when this screen gets picked back up)

- `POST /api/courses` (create), `PUT /api/courses/{id}` (update),
  `POST /api/courses/{id}/publish` (publish), `POST /api/courses/{id}/unpublish`
  (unpublish), `GET /api/courses` (admin list, all statuses) — all real,
  `ManageCourses` policy.
- `GET /api/audit-logs` (paginated, newest-first) — `ReadAudit` policy.
- Areas: full CRUD via `AreaManagementController` (`POST`/`PUT`/`GET
  /api/areas`), `ManageAreas` policy.
- Access grants: `POST /api/access/user-area`,
  `POST /api/access/role-area` (`ManageUserAreaAccess`/
  `ManageRoleAreaAccess` policies) — real backing for the audit
  example's "UserAreaAccessGranted" action.
- Users: full CRUD via `UsersController` (`ManageUsers` policy) — not
  drawn in this mockup artboard, but real if a "Usuários" screen gets
  specced later.
- Real price amount now exists (`Course.PriceAmount`, see
  `Docs/backend-pendencies/catalog/course-catalog.md` pendency 3) — the
  mockup's "Cobrança" column ("R$ 149", "R$ 89", "R$ 199") has real data
  behind it now, no longer a standing gap.

Every backend gap that blocked this screen is closed. Building the
admin "Cursos" screen itself is now unblocked whenever it gets a spec —
that's frontend work, not tracked further here.

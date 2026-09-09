# Backend Pendencies — Admin Panel: Lesson editor (artboard `1p`)

Most of `1p`'s backing API was already resolved while speccing
[`course-modules-lessons.md`](course-modules-lessons.md) (pendencies 1-3:
lesson CRUD, per-lesson video GET/PUT/DELETE, and the YouTube-link video
hosting decision). This file covers the gaps left once `1p` itself
(`Docs/specs/admin/lesson-editor.md`) got specced in detail.

## 1. No endpoint to move a lesson to a different module — CLOSED

- **Mockup expects**: `1p`'s "Módulo" field is drawn as a dropdown
  (`▾` affordance), implying a lesson can be reassigned to another module
  from its own edit screen.
- **Backend today**: `LessonsController`'s update route is
  `PUT /api/courses/{courseId}/modules/{moduleId}/lessons/{lessonId}` —
  `moduleId` is part of the route itself, not a body field, and
  `UpdateLessonRequest` (`Title`, `Description`, `FreePreview`, `Published`)
  has no module reference at all. There is no "move lesson" operation
  anywhere in `Modules/Courses`.
- **What's needed**: either a dedicated
  `PUT .../lessons/{lessonId}/move` (target module id, likely appended to
  the target module's `DisplayOrder` sequence the same way create does) or
  adding a settable `ModuleId` to the update path — the latter would need
  care around the unique `(ModuleId, DisplayOrder)` index the same way
  `course-modules-lessons.md` pendency 1's reorder logic already handles.
- **Workaround shipped**: `lesson-editor.md` renders "Módulo" read-only,
  no dropdown.
- **Severity**: Feature gap.
- **Resolved (2026-09-09)**: took the dedicated-endpoint option —
  `PUT /api/courses/{courseId}/modules/{moduleId}/lessons/{lessonId}/move`
  (new `MoveLessonUseCase`), body `{ TargetModuleId }`, same `ManageCourses`
  policy as the rest of `LessonsController`. The moved lesson is appended
  to the end of the target module's order (`ListByModuleIdAsync` → max + 1,
  same "no manual ordering on creation" convention `CreateLessonUseCase`
  already uses) — no unique-index rewrite needed since it's always an
  append, never an insert. Guards: target module must belong to the
  *same course* as the lesson's current module (400 otherwise — the
  mockup's dropdown only ever lists modules within one course, and a
  cross-course move isn't safe given course-scoped
  video/progress/certificate assumptions elsewhere); target module must
  not already be at `CourseValidationLimits.MaxLessonsPerModule` (409
  otherwise, same guard `CreateLessonUseCase` applies); moving a lesson to
  its own current module is a no-op (200, no audit log). Records a new
  `AuditLogActionNames.LessonMoved` audit entry with `fromModuleId`/
  `toModuleId`/`displayName`.

## 2. No endpoint to set a lesson's display order directly — CLOSED (won't implement)

- **Mockup expects**: `1p`'s "Ordem no módulo" field is drawn as a plain
  value (just "2"), which reads as editable alongside the rest of the form.
- **Backend today**: the only way to change a lesson's `DisplayOrder` is
  `PUT .../lessons/reorder` with the *entire* module's lesson-id list in
  the desired order (`ReorderLessonsRequest`) — there's no "set this one
  lesson's order to N" call.
- **What's needed**: either a dedicated set-order endpoint, or accept that
  reordering only ever happens list-at-a-time (arguably the safer design,
  given the unique-index rewrite dance `course-modules-lessons.md`
  pendency 1 already describes for the list-based reorder).
- **Workaround shipped**: `lesson-editor.md` renders "Ordem no módulo"
  read-only (the lesson's current 1-based position); reordering stays on
  `course-modules.md`'s up/down buttons.
- **Severity**: Cosmetic — the mockup's field reads as informational once
  you can't submit a bare number without also specifying the rest of the
  module's order.
- **Decision (2026-09-09)**: won't implement a dedicated set-order
  endpoint — list-based reorder (`PUT .../lessons/reorder`, already real)
  stays the only mechanism, per this pendency's own analysis ("arguably
  the safer design," avoiding the unique `(ModuleId, DisplayOrder)`
  index-rewrite dance for a rarely-needed single-item operation).
  "Ordem no módulo" keeps rendering read-only.

## 3. Registered (YouTube-hosted) videos never leave "Processing" on their own — CLOSED

- **Mockup expects**: `1p`'s video panel shows "processado" as soon as a
  video is attached — no separate "waiting for processing" step is drawn.
- **Backend today**: `Video.Create` always starts a new video in
  `VideoStatus.Processing` (`Modules/Media/Domain/Entities/Video.cs`), and
  `ReplaceLessonVideoUseCase` calls `existingVideo.MarkAsProcessing()` again
  on every update. Nothing transitions a video to `Ready` except
  `POST /api/videos/{id}/ready` (`MarkVideoReadyUseCase`) — a route that
  exists to let some external transcoding pipeline report completion, which
  doesn't apply to YouTube-hosted videos at all.
- **What's needed**: nothing backend-side — this is a real gap only in the
  sense that admin-registered YouTube videos have no transcoding step to
  wait for, so the existing "mark ready" call needs to be triggered by
  *something*.
- **Workaround shipped**: `lesson-editor.md` has the frontend call
  `POST /api/videos/{videoId}/ready` immediately after every successful
  `PUT /api/videos/lessons/{lessonId}`, using the id from that response —
  a client-side orchestration choice, not a new backend capability.
  Revisit if a real transcoding-backed provider (`S3`/`Mux`/etc.) is ever
  wired up, since that path *should* wait for an external "ready" signal
  instead of firing it eagerly.
- **Severity**: Cosmetic, given the workaround — would become a real gap
  the moment a non-instant provider is added.
- **Resolved (2026-09-09)**: `CreateVideoUseCase` and
  `ReplaceLessonVideoUseCase` (both branches: new video and update-in-place)
  now call `video.MarkAsReady()` automatically right after
  `Video.Create`/before persisting, whenever
  `StorageProvider == VideoStorageProvider.YouTube` and
  `DurationSeconds > 0` — the admin already supplies the duration when
  registering a YouTube link, so there's nothing left to wait for. No
  domain change (`MarkAsReady()` already existed as a plain, idempotent
  status flip). The frontend's existing `POST /ready` follow-up call
  becomes redundant but harmless for YouTube going forward (removing it
  is frontend work, not tracked further here); it remains the only path
  to `Ready` for every other provider, unchanged.

## What's already real

Everything else `1p` needs was already closed while speccing
`course-modules-lessons.md`:

- `PUT /api/courses/{courseId}/modules/{moduleId}/lessons/{lessonId}` —
  title/description/freePreview/published (pendency 1 there).
- `GET/PUT/DELETE /api/videos/lessons/{lessonId}` — view, register/replace,
  remove a lesson's video (pendency 2 there).
- `YouTube` as a `VideoStorageProvider`, with `StorageKey` = the YouTube
  video id (pendency 3 there, decision).
- `DELETE .../lessons/{lessonId}` already returns `409` when the lesson has
  recorded student progress (pendency 1 there) — reused as-is for this
  screen's "Excluir aula".

# Backend Pendencies — Admin Panel: Modules & lessons management

No spec was written for these screens yet — same status as
[`courses-panel.md`](courses-panel.md). Covers mockup artboards `1o`
("Curso → Módulos e aulas") and `1p` ("Aula — criar/editar"), added under
the new "Painel admin — CRUDs de entidades" mockup group.

## 1. No endpoint to add, edit, reorder, or remove modules/lessons on an existing course — CLOSED (was BLOCKING)

- **Mockup expects**: `1o` shows "Novo módulo", "Editar módulo", and
  "+ Nova aula neste módulo" actions, plus drag handles ("⋮⋮") implying
  reordering; `1p` is a full edit form for a single lesson (title, module
  assignment, description/transcript, video, free-preview toggle, order,
  delete).
- **Backend today**: modules and lessons can only be supplied as a nested
  `Modules[].Lessons[]` collection inside `CreateCourseRequest`
  (`Modules/Courses/Presentation/Requests/CreateCourseRequest.cs` →
  `CreateCourseModuleRequest` → `CreateLessonRequest`) at course-creation
  time. `UpdateCourseRequest`
  (`Modules/Courses/Presentation/Requests/UpdateCourseRequest.cs`) carries
  no module/lesson data, and there is no `ModulesController` or
  `LessonsController` anywhere in the codebase — `CoursesController` is the
  only controller in the `Courses` module. Once a course exists, its
  content structure is effectively frozen through the API: no add module,
  no add lesson, no edit lesson title/description/order, no reorder, no
  remove.
- **What's needed**: a real content-management surface — at minimum
  `POST/PUT/DELETE` routes for modules and lessons scoped to a course
  (e.g. `POST /api/courses/{id}/modules`,
  `PUT /api/courses/{id}/modules/{moduleId}/lessons/{lessonId}`, etc.),
  behind `ManageCourses`.
- **Why this is blocking rather than a missing field**: this isn't a
  cosmetic gap on an otherwise-working screen — the entire premise of both
  `1o` and `1p` (editing content after the fact) has no backing API. A
  course's content becomes fixed the moment it's created.
- **Severity**: Blocking.
- **Resolved (2026-09-07)**: added `CourseModulesController`
  (`api/courses/{courseId}/modules`: `POST`, `PUT {moduleId}`,
  `DELETE {moduleId}`, `PUT reorder`) and `LessonsController`
  (`api/courses/{courseId}/modules/{moduleId}/lessons`: `POST`,
  `PUT {lessonId}`, `DELETE {lessonId}`, `PUT reorder`), both behind
  `ManageCourses`. New modules/lessons are appended with an auto-computed
  next `DisplayOrder` (no manual ordering input at creation, avoiding the
  unique `(CourseId, DisplayOrder)` / `(ModuleId, DisplayOrder)` DB index
  collisions); explicit reordering goes through a dedicated two-phase
  (negative-then-final) `DisplayOrder` rewrite so the unique indexes are
  never violated mid-transaction. Removing a module requires it have no
  lessons first, and removing a lesson requires it have no recorded
  student progress (both return 409 Conflict otherwise) — a lesson's
  video, if any, is removed automatically as part of lesson removal. This
  mirrors the same conservative, no-cascade philosophy chosen for course
  deletion in `course-crud.md` pendency 5.

## 2. No per-lesson video replace/detach — CLOSED

- **Mockup expects** (`1p`): a "Vídeo da aula" panel showing "Vídeo
  enviado · 18min · processado", implying a video can be viewed, replaced,
  or removed from an existing lesson.
- **Backend today**: `VideosController`
  (`Modules/Media/Presentation/Controllers/VideosController.cs`) only
  supports `POST /api/videos` (create, tied to a `LessonId`) and
  `POST /api/videos/{id}/ready` (mark processed) — no update, no delete,
  no "replace this lesson's video" operation, and no `GET` to look up the
  video currently attached to a given lesson (the reverse lookup,
  `IVideoRepository.FindByLessonIdAsync`, exists at the repository layer
  but was already flagged in
  [`Docs/backend-pendencies/catalog/lesson-player.md`](../catalog/lesson-player.md)
  as never wired to a route).
- **What's needed**: a way to fetch a lesson's current video and to
  replace/remove it.
- **Severity**: Feature gap (compounds with pendency 1 — moot until lessons
  themselves can be edited at all).
- **Resolved (2026-09-07)**: added `GET/PUT/DELETE /api/videos/lessons/{lessonId}`
  behind `ManageVideos` — `GET` wires the already-existing
  `IVideoRepository.FindByLessonIdAsync` to a real read route (closing the
  same gap flagged in `catalog/lesson-player.md`); `PUT` is an upsert
  (updates the existing video in place if one exists, else creates one);
  `DELETE` removes it.

## 3. No file-upload endpoint for video bytes — CLOSED (via decision)

- **Mockup expects**: a drop zone implying the admin can hand the platform
  a video file directly.
- **Backend today**: `CreateVideoRequest`
  (`Modules/Media/Presentation/Requests/CreateVideoRequest.cs`) takes
  `StorageProvider`, `StorageKey`, `PlaybackUrl`, `ThumbnailUrl`,
  `DurationSeconds`, `SizeBytes` — it registers metadata for a video
  *already placed* in external storage. There is no endpoint in
  CourseCore that accepts raw file bytes or issues a pre-signed upload URL
  for the admin's browser to upload to directly.
  `MarkVideoReadyUseCase` (`POST /api/videos/{id}/ready`) then flips a
  processed flag, implying an external transcoding step the admin UI would
  also need to trigger or wait on.
  This is the same standing gap noted for course cover images in
  [`course-crud.md`](course-crud.md) pendency 4.
- **What's needed**: either a direct-upload endpoint or a documented
  pattern for issuing a pre-signed URL to some storage provider, plus
  whatever triggers transcoding before `.../ready` is called.
- **Severity**: Feature gap — this is a real product decision (which
  storage/transcoding provider), not a small addition.
- **Decision (2026-09-07)**: no bucket/upload endpoint for now — video
  hosting is "via link," using private/unlisted YouTube videos as the
  storage provider. Added `YouTube` to `VideoStorageProvider`
  (`Modules/Media/Domain/Enums/VideoStorageProvider.cs`); an admin
  registers a lesson's video the same way as any other provider (`PUT
  /api/videos/lessons/{lessonId}` with `storageProvider: "YouTube"` and
  `storageKey` set to the YouTube video id) — no raw bytes ever pass
  through CourseCore. `VideoStorageService.GeneratePlaybackUrlAsync`
  returns a `youtube-nocookie.com/embed/{videoId}` URL for that provider
  instead of the generic signed backend-proxy URL used by the other
  providers, but the existing entitlement check in
  `RequestVideoPlaybackUseCase` (has access, or lesson is a free preview)
  still gates whether that URL is ever returned at all — so "private"
  YouTube videos stay access-controlled the same way any other lesson
  video is.

## What's already real

- Lesson-level `FreePreview` (`Modules/Courses/Presentation/Requests/CreateLessonRequest.cs`)
  matches the mockup's "Aula gratuita (freePreview)" toggle exactly — the
  field name and semantics line up, it's just only settable at course
  creation (see pendency 1).
- `POST /api/videos` + `POST /api/videos/{id}/ready`, `ManageVideos`
  policy — real, once a lesson to attach to already exists.

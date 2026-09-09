# Painel admin — vídeos (`1zc`) — backend pendencies

No spec exists for this screen yet, but as of 2026-09-09 all backend gaps
that blocked it are closed (see per-pendency resolutions below) — building
the admin "Vídeos" screen itself is now unblocked whenever it gets a spec,
same as `courses-panel.md`. This mirrors that file's own history: it too
was "skipped outright" until its blocking backend gaps closed.

## 1. No "list all videos" endpoint — Blocking — CLOSED

- **Mockup expects**: a single table listing every video in the system
  (62 rows in the mockup), independent of which lesson each belongs to.
- **Backend has today**: `Modules/Media/Presentation/Controllers/VideosController.cs`
  exposes exactly six routes, all either lesson-scoped or id-scoped —
  `POST /api/videos`, `POST /api/videos/{id}/ready`,
  `GET /api/videos/{videoId}/playback`, `GET /api/videos/lessons/{lessonId}`,
  `PUT /api/videos/lessons/{lessonId}`, `DELETE /api/videos/lessons/{lessonId}`.
  `IVideoRepository` (`Modules/Media/Domain/Repositories/IVideoRepository.cs`)
  only exposes `FindByIdAsync`, `FindByLessonIdAsync`,
  `ListDurationSecondsByLessonIdsAsync`, `ListByLessonIdsAsync` — every
  query is keyed by a lesson id (or id list); there's no unkeyed "get
  all" query to back a collection endpoint.
- **What closing the gap would need**: a new `GET /api/videos`
  (paginated, `ManageVideos`-gated) endpoint and a matching repository
  query with no lesson-id filter.
- **Workaround shipped**: none. An N+1 client-side reconstruction
  (walk `GET /api/courses` → modules → lessons → `GET /api/videos/lessons/{lessonId}`
  per lesson) is technically possible but was rejected: one HTTP request
  per lesson system-wide just to paint a table is the kind of thing this
  codebase avoids elsewhere (see the users-list pendency for a similar
  per-row-request tradeoff that was accepted only because the row count
  is small; here it's system-wide, not one admin screen's own rows).
- **Severity**: Blocking.
- **Resolved (2026-09-09)**: `GET /api/videos` (new `ListVideosUseCase`),
  paginated (`page`/`pageSize`, same `PaginationLimits`/`PagedResult`/
  `PagedResponse` template `AuditLogsController` already uses), gated by
  `ManageVideos`. New `IVideoRepository.ListPagedAsync` on
  `EfVideoRepository` has no lesson filter at all — every video,
  `OrderByDescending(CreatedAt).ThenByDescending(Id)`, identical shape to
  `EfAuditLogRepository.ListPagedAsync`. No N+1, no client-side
  reconstruction needed.

## 2. No YouTube-ID field on `Video` — Feature gap — CLOSED

- **Mockup expects**: a `Video` identified by a YouTube video id
  (`dQw4w9WgXcQ`), with a derived `youtube.com/watch?v=...` URL shown
  underneath.
- **Backend has today**: `Modules/Media/Domain/Entities/Video.cs` has no
  YouTube-specific field. Storage is generic: `StorageProvider` (enum:
  `Local`, `S3`, `AzureBlob`, `CloudflareR2`, `YouTube`, `Vimeo`, `Mux`)
  plus an opaque `StorageKey` string. A YouTube video's id would live in
  `StorageKey` when `StorageProvider == YouTube`, but there's no
  dedicated, typed field the frontend can rely on being a YouTube id
  specifically (the admin lesson-editor's existing "register video" form
  already works this way — a plain `StorageKey` string treated as a
  YouTube id by convention, not by schema).
- **Severity**: Feature gap (workable via convention, not a hard block on
  its own — only listed because it compounds with pendency 1).
- **Resolved (2026-09-09), derived field, no schema change**: `VideoOutput`/
  `VideoResponse` (and therefore every video response — create, replace,
  mark-ready, get-lesson-video, and the new list) gained `YouTubeVideoId`
  and `YouTubeUrl`, computed once in `VideoOutput.FromVideo`: when
  `StorageProvider == YouTube`, `YouTubeVideoId = StorageKey` and
  `YouTubeUrl = "https://www.youtube.com/watch?v={StorageKey}"`; both
  `null` otherwise. `StorageKey` remains the single source of truth — no
  duplicated column, no migration.

## 3. No "unlinked" video state — Feature gap — CLOSED (won't implement)

- **Mockup expects**: a "Sem vínculo" (unlinked) row — a video that
  exists but isn't attached to any lesson.
- **Backend has today**: `Video`'s constructor validates
  `lessonId != Guid.Empty` (`Video.cs`, `ValidateId`) — a `Video` cannot
  be created without a lesson. `CreateVideoUseCase` additionally enforces
  one video per lesson (409 if the lesson already has one). An unlinked
  video is not a representable state in the domain model today.
- **Severity**: Feature gap (this is a domain invariant, not a missing
  read — closing it would mean relaxing a constraint the backend
  currently enforces on purpose).
- **Decision (2026-09-09)**: won't implement. `Video.LessonId` stays a
  required, unique FK (`VideoConfiguration.cs`:
  `HasIndex(x => x.LessonId).IsUnique()`, `DeleteBehavior.Restrict`) —
  relaxing it would ripple through playback, progress, and certificate
  flows that all assume a video belongs to exactly one lesson, for a
  screen-only affordance. Every video the new `GET /api/videos` returns
  is linked to exactly one lesson; the mockup's "Sem vínculo" row has no
  backing data and won't be shown.

## 4. No "Ativo / Não listado" visibility status — Feature gap — CLOSED

- **Mockup expects**: a per-video visibility status distinct from
  processing state.
- **Backend has today**: `Modules/Media/Domain/Enums/VideoStatus.cs` has
  exactly three values — `Processing`, `Ready`, `Failed` — a processing
  pipeline state, not a visibility/publish concept. There's no
  `Unlisted`/"não listado" member and no separate visibility flag
  anywhere on `Video`.
- **Severity**: Feature gap.
- **Resolved (2026-09-09)**: new `VideoVisibility` enum (`Active`,
  `Unlisted`), additive `Video.Visibility` field (defaults `Active`,
  migration `AddVideoVisibility` — one column, `NOT NULL DEFAULT
  'Active'`, backfills every existing row with no behavior change).
  Mirrors the `Publish`/`Unpublish` pattern already used for courses/
  testimonials/modules — a moderation-style toggle, not part of the
  create/replace payload: new `POST /api/videos/{videoId}/activate` and
  `POST /api/videos/{videoId}/unlist` (`ManageVideos`), backed by
  `Video.MarkAsActive()`/`MarkAsUnlisted()` and new `ActivateVideoUseCase`/
  `UnlistVideoUseCase`, each recording a `VideoActivated`/`VideoUnlisted`
  audit entry.

## Verdict

All four gaps closed 2026-09-09. `GET /api/videos` (pendency 1) was the
actual blocker; pendencies 2 and 4 shipped as real (derived-field and
additive-schema, respectively) closures; pendency 3 was closed via an
explicit won't-implement decision rather than relaxing a domain
invariant that several other flows (playback, progress, certificates)
depend on. Building the admin "Vídeos" screen itself is now unblocked
whenever it gets a spec — that's frontend work, not tracked further
here.

# Backend Pendencies — Lesson Player Page

Spec: [`Docs/specs/catalog/lesson-player.md`](../../specs/catalog/lesson-player.md)

## 1. No route resolves a lesson's video ID — CLOSED (was BLOCKING)

- **Mockup expects**: a working video player, since that's the entire
  reason this screen exists.
- **Backend today**: `GET /api/videos/{videoId}/playback` is real and works
  (`VideoPlaybackOutput` with a signed `PlaybackUrl`, `DurationSeconds`,
  `ExpiresAt`) — but it needs a `videoId` the caller already has, and
  nothing gives the frontend one:
  - `LessonResponse` (nested under `GET /api/courses/{id}`) has no video
    reference at all — `Id`, `ModuleId`, `Title`, `Description`,
    `DisplayOrder`, `FreePreview`, `Published`. No `videoId`.
  - There is no Lessons controller, and no route on any of this backend's
    7 controllers (`Areas`, `AreaManagement`, `Auth`, `Courses`, `Videos`,
    `Progress`, `Users` — all checked) resolves "video for lesson X." The
    only place that lookup happens
    (`IVideoRepository.FindByLessonIdAsync`) is internal to
    `RegisterLessonProgressUseCase`, never returned to a caller.
  - The `Videos` controller's own routes are create (admin), mark-ready
    (admin), and playback-by-id — no list, no by-lesson query.
- **What's needed**: a new endpoint (e.g. `GET
  /api/courses/{courseId}/lessons/{lessonId}/video` or a `VideoId` field
  added directly to `LessonResponse`) that resolves a lesson to its
  attached video, so the frontend can then call the existing playback
  endpoint.
- **Workaround shipped**: the video area renders as an honest, inert
  placeholder — no play button wired to nothing. Per your explicit call on
  this gap (2026-09-04), the page was built around what's real instead of
  faking playback.
- **Severity**: **Blocking** — not "a field is missing," the screen's core
  feature has no viable data path at all until this endpoint exists.
- **Resolved, 2026-09-04**: `LessonResponse` (nested under
  `GET /api/courses/{id}`) now carries `VideoId` and `DurationSeconds`
  (both nullable — a lesson with no attached video reports `null`, not a
  misleading `0:00`), computed server-side via a new bulk
  `IVideoRepository.ListByLessonIdsAsync` lookup in `GetCourseDetailsUseCase`
  (one query for the whole course, no per-lesson N+1). The frontend now has
  exactly the handoff this pendency asked for: read `videoId` off the
  lesson, then call the already-working `GET /api/videos/{videoId}/playback`.
  No new endpoint needed — the field-on-`LessonResponse` option from "What's
  needed" below was the one implemented.

## 2. No per-lesson duration reachable — CLOSED

- **Mockup expects**: "14:20" next to each lesson in the sidebar.
- **Backend today**: only on `VideoResponse.DurationSeconds` — same
  unreachable-without-a-videoId problem as pendency 1. Would be solved for
  free once pendency 1 is fixed (assuming the new endpoint/field also
  returns duration).
- **Severity**: Feature gap, downstream of pendency 1.
- **Resolved, 2026-09-04**: solved by the same change as pendency 1 —
  `LessonResponse.DurationSeconds` is populated in the same bulk lookup.

## 3. No attached-materials concept

- **Mockup expects**: "Apostila — Módulo 01" and similar PDF attachments
  per module/lesson, with file sizes.
- **Backend today**: no attachment/material concept anywhere in the
  domain.
- **Severity**: Feature gap.

## 4. No notes or Q&A concept

- **Mockup expects**: "Anotações" / "Perguntas" tabs alongside the video.
- **Backend today**: nothing anywhere in the domain.
- **Severity**: Feature gap.

## 5. Captions, playback speed, scrubber — unblocked, frontend work only

- **Mockup expects**: CC toggle, speed control, a real scrubber over the
  video timeline.
- **Backend today**: irrelevant while there's no real video to control —
  chrome for a player that doesn't play here.
- **Severity**: was "Blocking, same root cause as pendency 1." Now that
  pendency 1 is resolved (2026-09-04) and a real signed playback URL is
  reachable, speed control and a scrubber are pure frontend `<video>`-element
  features against that URL — no backend gap remains for them. Captions
  specifically would need a subtitle-track concept added to `Video` if real
  ones are wanted (not requested here, not implemented) — otherwise this
  item is frontend scope, not a backend pendency anymore.

## What already works and needed no workaround

For context when scoping pendency 1 — these are real, working endpoints
this screen already uses successfully:

- `GET /api/progress/courses/{courseId}` — course-level percent + per-lesson
  `Completed`/`WatchedSeconds`/`LastWatchedAt`, with a graceful empty state
  when nothing's been watched yet.
- `POST /api/progress/lessons` (`{ lessonId, watchedSeconds }`) — the server
  decides completion server-side at `watchedSeconds >= videoDurationSeconds
  × 90%` (`ProgressOptions.LessonCompletionThresholdPercent`, defaults to
  90 — matches the mockup's own footnote). **Note**: for any lesson with no
  attached video (true of everything seeded so far), this always returns
  `Completed: false` regardless of `watchedSeconds` sent
  (`RegisterLessonProgressUseCase`: `if (video is null ...) return false`)
  — expected behavior given pendency 1, not a bug in this endpoint. The
  request also accepts a `markAsCompleted` boolean
  (`RegisterLessonProgressRequest.MarkAsCompleted`) that is bound all the
  way through to `RegisterLessonProgressInput` but never actually read in
  `RegisterLessonProgressUseCase.ExecuteAsync` — a third dead field in the
  same category as `FreePreview`. The frontend deliberately never sends it.

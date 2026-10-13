# Lesson watch tracking and resume

## Why

The course bar only moved when a whole lesson crossed 90%, YouTube lessons had no progress at all, dragging the player to the end counted as "watched", and the player never resumed where the student stopped.

## Behavior

### Backend (`POST /api/progress/lessons`)

- New optional `positionSeconds` in the request: where the player is now. Stored as `LastPositionSeconds` (clamped to the video duration, never negative → `400`) and returned as `lastPositionSeconds` on the lesson progress (also inside `GET /api/progress/courses/{id}`).
- `watchedSeconds` keeps its server rules: monotonic (never decreases), clamped to the video duration, and the lesson completes at `Progress:LessonCompletionThresholdPercent` (90) of the duration. Omitting `positionSeconds` keeps the stored position.
- Migration `AddLessonProgressLastPosition` (int, default 0).

### Frontend

- For videos played by the native `<video>`, `watchedSeconds` now means **time really played**: only small forward steps of `currentTime` count, so seeking forward/backwards adds nothing (`watch-tracker.ts`). The count starts from the saved value, reports every 5 s and immediately on pause, end, tab hidden and leaving the lesson.
- The video opens at the saved position (not within the last 5 s).
- The "% concluído" label and bar in the lesson header, and a small bar per lesson in the sidebar, include partial progress. A lesson counts as full at the 90% point, so the display agrees with when the backend completes it. 100% only appears when every lesson is completed.
- The manual "Marcar aula como assistida" button now shows only for iframe (YouTube) videos, which expose no playback events. Lessons without a video show no button (the backend can not complete them).

## Known limits

- The time-watched count is produced by the browser; the backend trusts it (as before) and only bounds it by the video duration. Real verification would need server-side playback heartbeats.
- YouTube lessons are still completed by the manual button, based on time spent on the page. Tracking them automatically needs the YouTube IFrame Player API (not done).

## Acceptance

- [x] Watching a native video moves the lesson and course bars live; finishing it completes the lesson and updates the course percent.
- [x] Seeking to the end does not add watched time (verified in the browser: position saved, 0 s watched, lesson not completed).
- [x] `positionSeconds` is persisted, clamped, validated and returned.
- [ ] Resume position verified in the browser (the 5 s test clip is shorter than the resume margin; covered by logic only).

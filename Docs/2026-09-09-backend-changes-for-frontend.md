# Backend changes ready for frontend — 2026-09-09

Everything below shipped in this backend session, closing every pendency
in `Docs/backend-pendencies/` and every gap found in `Docs/audits/`. Use
this doc alongside those two folders when picking the next frontend work:
`backend-pendencies/` and `audits/` describe the *screens/mockups* and
what was missing; this doc lists what the *API* actually looks like now,
so you don't have to re-derive it from the pendency prose.

No breaking changes — every item below is either a new endpoint or a new
optional/additive field on an existing response. Existing fields/routes
are unchanged unless noted.

## Quick index

| Area | What's new |
|---|---|
| Landing | Featured/highlighted course cards now carry real pricing/duration/area data |
| Profile | Self-service profile update, password change, phone/avatar fields |
| Admin — audit log | Every entry now carries a human-readable `displayName` |
| Admin — lessons | Move a lesson to another module |
| Admin — users | Role picker (`GET /api/roles`), area grants folded into the user list |
| Admin — videos | Full video list, visibility toggle, YouTube id/URL fields |
| Testimonials | Self-service submission endpoint for students |

---

## 1. Landing — featured/highlighted course cards

`GET /api/courses/public-summary` — **no route change**, `FeaturedCourses`
and `HighlightedCourse` entries gained fields:

```jsonc
{
  "id": "...", "title": "...", "slug": "...", "description": "...", "thumbnailUrl": "...",
  "pricingModel": "Free" | "Paid" | "EnrollmentControlled",
  "priceAmount": 149.90,           // decimal? — null for Free/EnrollmentControlled
  "moduleCount": 6,
  "lessonCount": 24,
  "durationSeconds": 25200,
  "areaName": "Discipulado"        // string? — null if the course has no active area
}
```

The "Comece por aqui" cards and the "Formação em destaque" panel can now
be fully data-driven instead of hardcoded editorial copy.

## 2. Profile — self-service update, password change, phone/avatar

**`PUT /api/auth/me`** (any authenticated user) — updates the caller's own
profile.

Request:
```jsonc
{ "name": "New Name", "phone": "+55 11 99999-0000", "avatarUrl": "https://..." }
```
`phone`/`avatarUrl` are optional (send `null` or omit to clear). **No
`email` field** — self-service email change is intentionally unsupported
(admin-only via `PUT /api/users/{id}`). Response: `CurrentUserResponse`
(same shape as `GET /api/auth/me`, now including `phone`/`avatarUrl`).
Only actually-changed fields are audit-logged; unlike admin edits, this
does **not** force other sessions to log out.

**`POST /api/auth/change-password`** (any authenticated user) — rate
limited (5/min per IP, same as `/reset-password`).

Request: `{ "currentPassword": "...", "newPassword": "..." }`.
Response: `204 No Content`. On success, **all other sessions are logged
out** (refresh tokens revoked) — same behavior as the forgot-password
flow. `401` if `currentPassword` is wrong, `400` if `newPassword` fails
the password policy.

**`GET /api/auth/me`** and **`GET /api/users/{userId}`** — both gained
`phone` (`string?`) and `avatarUrl` (`string?`) fields (`avatarUrl` is a
plain URL string — there is no upload endpoint, same as course covers).

## 3. Admin — audit log entries now carry a readable name

`GET /api/audit-logs` — **no route/shape change**, but the `metadata`
object on every entry now includes a `displayName` key for most action
types:

```jsonc
{ "action": "CoursePublished", "metadata": { "displayName": "Curso de Batismo", ... } }
{ "action": "UserAreaAccessGranted", "metadata": { "displayName": "ana.souza@email.com → Liderança", ... } }
```

- Single-entity actions (course/module/lesson/video/testimonial/area/user
  created/updated/published/etc.) → that entity's own title/name.
- Relationship actions (grant/revoke/approve/reject linking a user or
  role to an area/course) → `"{email or role} → {target}"`.
- **Not covered** (still needs the old `{EntityName} #{shortId}`
  fallback): the 9 Auth-module action types (login/logout/refresh/
  confirm-email/password flows — entity is the acting user) and the
  secondary `UserTokenVersionIncremented`/`UserSessionsRevoked` entries
  that always accompany a primary event.
- New action names to recognize: `LessonMoved`, `VideoActivated`,
  `VideoUnlisted`, `TestimonialSubmitted`, `UserProfileUpdated`,
  `PasswordChanged`.

## 4. Admin — move a lesson between modules

**`PUT /api/courses/{courseId}/modules/{moduleId}/lessons/{lessonId}/move`**
(`ManageCourses`) — `moduleId` in the route is the lesson's *current*
module.

Request: `{ "targetModuleId": "..." }`. Response: `LessonResponse`
(`moduleId` now reflects the target, `displayOrder` is appended to the
end of the target module — not manually positionable).

- `400` if the target module belongs to a different course (cross-course
  move isn't supported).
- `404` if the lesson or target module doesn't exist.
- `409` if the target module is already at the max-lessons-per-module cap.
- Moving to the lesson's own current module is a no-op, `200` with
  unchanged data.

Direct "set this lesson's display order" stays unsupported by decision —
keep using `PUT .../lessons/reorder` (whole-module list reorder, already
existed).

## 5. Admin — users list/detail: roles and area grants

**`GET /api/roles`** (new, `ManageUsers`) — populates a role picker.

```jsonc
[{ "id": "...", "name": "Admin" }, { "id": "...", "name": "Editor" }]
```
Active roles only, ordered by name. Use the returned `id` with the
already-existing `POST /api/users/{userId}/roles/{roleId}` /
`DELETE /api/users/{userId}/roles/{roleId}`.

**`GET /api/users`** and **`GET /api/users/{userId}`** — both gained
`areaNames` (`string[]`), same shape/pattern as the existing `roleNames`.
No more per-row `GET /api/access/user-area/{userId}` calls needed just to
paint the "Áreas liberadas" column — it's now batched server-side and
scales past the current small page sizes.

## 6. Admin — videos panel (this screen was entirely skipped before)

**`GET /api/videos`** (new, `ManageVideos`, paginated) — the previously-
missing collection endpoint; not lesson-scoped.

Query params: `page` (default 1), `pageSize` (default 50, max 100).
Response: `PagedResponse<VideoResponse>`:
```jsonc
{ "items": [ /* VideoResponse[] */ ], "page": 1, "pageSize": 50, "totalItems": 62, "totalPages": 2 }
```

**Every** video response (`create`, `replace`, `mark-ready`,
`get-lesson-video`, and this new list) now also includes:
```jsonc
{
  "visibility": "Active" | "Unlisted",
  "youTubeVideoId": "dQw4w9WgXcQ",     // string? — null unless storageProvider == "YouTube"
  "youTubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ"  // string?, same condition
}
```

**`POST /api/videos/{videoId}/activate`** / **`POST /api/videos/{videoId}/unlist`**
(new, `ManageVideos`) — toggle `visibility`, same shape as course
publish/unpublish. Response: `VideoResponse`.

Also, registering or replacing a **YouTube**-provider video with a
known duration now comes back `"status": "Ready"` immediately (no need
to also call `POST /api/videos/{id}/ready` for YouTube specifically —
that endpoint still matters for every other provider).

**Not supported, by decision**: an "unlinked" video (no lesson) — every
video the API returns is always attached to exactly one lesson.

## 7. Testimonials — student self-service submission

**`POST /api/testimonials/mine`** (new, any authenticated user, **not**
`ManageCourses`) — the "Enviar depoimento" form's backing endpoint.

Request: `{ "quote": "...", "courseId": "..." }` — `courseId` optional.
**No `authorName`/`avatarUrl` fields** — those are filled in server-side
from the caller's own profile (`name`/`avatarUrl`), so nobody can submit
a testimonial impersonating someone else. Response: `TestimonialResponse`
(`201 Created`), always `"published": false` (pending admin moderation,
same as admin-created ones). `404` if `courseId` doesn't exist.

`TestimonialResponse` also gained `submittedByUserId` (`string?` GUID) on
every testimonial (admin list included) — `null` for testimonials the
admin typed in directly via `POST /api/testimonials`.

The existing admin CRUD (`POST/PUT /api/testimonials`,
`.../publish`, `.../unpublish`, `GET /api/testimonials`) is unchanged and
still `ManageCourses`-gated — build the admin moderation view against
that, filtering/badging by `submittedByUserId != null` if you want to
flag self-submitted entries.

---

## Nothing left blocked

Every pendency in `Docs/backend-pendencies/` and every gap in
`Docs/audits/` is closed as of this session, except two things that are
explicit **won't-implement** decisions (not bugs, don't re-file):

- Checkout (Pix/cartão) — no payment endpoint exists at all, deliberate
  backend non-goal.
- "Unlinked video" (a video with no lesson) — `Video.LessonId` stays a
  required, unique FK; relaxing it would ripple through playback,
  progress, and certificate flows.

Also two config-only items waiting on credentials, not code:
- Turnstile public site key + secret key (Resend's key is already
  configured locally, `register.md` pendency #1 is otherwise closed).

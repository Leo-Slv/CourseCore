# Backend endpoints not yet used by the frontend

Snapshot date: 2026-09-09. Cross-references the CourseCore backend's actual
controllers (`c:\Users\leonardo.silva\source\repos\CourseCore`) against every
`apiFetch` call in this repo. Companion to
[`Docs/backend-pendencies/`](../backend-pendencies/) (which tracks the
opposite direction — mockup requirements the backend can't satisfy). This
doc tracks backend capabilities that exist today but have no frontend screen
consuming them yet, so they surface as "what could we build next" candidates
instead of getting lost in controller source.

A companion pass over the mockup file — which desktop screens have no
implemented route yet — is tracked separately; see
`Docs/audits/unimplemented-mockup-screens.md` once that lands.

## (A) Full backend endpoint inventory — 66 actions across 13 controllers

**Auth** — `api/auth` (`AuthController.cs`)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/login | AllowAnonymous | Login; returns `AuthResponse`, sets refresh-token cookie |
| POST | /api/auth/register | AllowAnonymous | Register new user, returns `AuthResponse` (201) |
| POST | /api/auth/confirm-email | authenticated | Confirms email via token for the current user |
| POST | /api/auth/resend-confirmation | authenticated | Resends confirmation email |
| POST | /api/auth/forgot-password | AllowAnonymous | Requests a password reset email |
| POST | /api/auth/reset-password | AllowAnonymous | Confirms a password reset with token |
| GET | /api/auth/me | authenticated | Returns `CurrentUserResponse` for the caller |
| POST | /api/auth/refresh-token | AllowAnonymous | Exchanges refresh token (cookie/body) for new access token |
| POST | /api/auth/logout | AllowAnonymous | Revokes refresh token, clears cookie |

**Users** — `api/users` (`UsersController.cs`, `[Authorize(Policy=ManageUsers)]` at controller level)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/users | ManageUsers | Create user |
| PUT | /api/users/{userId} | ManageUsers | Update user |
| GET | /api/users | ManageUsers | List users (paged/filtered) |
| GET | /api/users/{userId} | ManageUsers | Get user by id |
| POST | /api/users/{userId}/roles/{roleId} | ManageUsers | Assign role to user |
| DELETE | /api/users/{userId}/roles/{roleId} | ManageUsers | Remove role from user |

**Area management** — `api/areas` (`AreaManagementController.cs`, `[Authorize(Policy=ManageAreas)]`)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/areas | ManageAreas | Create area |
| PUT | /api/areas/{areaId} | ManageAreas | Update area |
| GET | /api/areas/{areaId} | ManageAreas | Get area by id |
| GET | /api/areas | ManageAreas | List areas |

**Access (grants/checks)** — `api/access` (`AreasController.cs`, `[Authorize]` + per-action policies)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/access/user-area | ManageUserAreaAccess | Grant a user access to an area |
| POST | /api/access/role-area | ManageRoleAreaAccess | Grant a role access to an area |
| GET | /api/access/user-area/{userId} | ManageUserAreaAccess | List a user's area-access grants |
| DELETE | /api/access/user-area/{userId}/{areaId} | ManageUserAreaAccess | Revoke a user's area access |
| POST | /api/access/course/check | CheckOwnCourseAccess | **[Obsolete]** check own course access (superseded below) |
| GET | /api/access/courses/{courseId} | CheckOwnCourseAccess | Check current user's access to a course |
| GET | /api/access/users/{userId}/courses/{courseId} | CheckUserCourseAccess | Check a specific user's access to a course |

**Access requests** — `api/access/requests` (`AccessRequestsController.cs`, `[Authorize]`)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/access/requests | authenticated | Self-service: request access to a course |
| GET | /api/access/requests/mine | authenticated | List my own access requests |
| GET | /api/access/requests | ManageUserAreaAccess | List all access requests (admin queue) |
| POST | /api/access/requests/{id}/approve | ManageUserAreaAccess | Approve a request |
| POST | /api/access/requests/{id}/reject | ManageUserAreaAccess | Reject a request |
| POST | /api/access/requests/grant | ManageUserAreaAccess | Directly grant course access (no request flow) |
| GET | /api/access/requests/users/{userId}/granted | ManageUserAreaAccess | List granted course access for a user |

**Courses** — `api/courses` (`CoursesController.cs`, `[Authorize]` + mixed policies/anon)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/courses | ManageCourses | Create course |
| PUT | /api/courses/{courseId} | ManageCourses | Update course |
| POST | /api/courses/{courseId}/publish | ManageCourses | Publish course |
| POST | /api/courses/{courseId}/unpublish | ManageCourses | Unpublish course |
| GET | /api/courses | ManageCourses | List all courses (admin) |
| GET | /api/courses/{courseId} | authenticated | Course details (access-aware) |
| GET | /api/courses/available | authenticated | List courses available to caller (catalog) |
| GET | /api/courses/public-summary | AllowAnonymous | Public catalog summary (landing page) |

**Course modules** — `api/courses/{courseId}/modules` (`CourseModulesController.cs`, `[Authorize(ManageCourses)]`)

| Verb | Route | Description |
|---|---|---|
| GET | .../modules | List modules |
| POST | .../modules | Create module |
| PUT | .../modules/{moduleId} | Update module |
| DELETE | .../modules/{moduleId} | Remove module |
| PUT | .../modules/reorder | Reorder modules |

**Lessons** — `api/courses/{courseId}/modules/{moduleId}/lessons` (`LessonsController.cs`, `[Authorize(ManageCourses)]`)

| Verb | Route | Description |
|---|---|---|
| POST | .../lessons | Create lesson |
| PUT | .../lessons/{lessonId} | Update lesson |
| DELETE | .../lessons/{lessonId} | Remove lesson |
| PUT | .../lessons/reorder | Reorder lessons |

**Progress** — `api/progress` (`ProgressController.cs`, `[Authorize]`)

| Verb | Route | Description |
|---|---|---|
| POST | /api/progress/lessons | Register lesson-watch progress |
| GET | /api/progress/courses/{courseId} | Get course progress for caller |

**Certificates** — `api/certificates` (`CertificatesController.cs`, `[Authorize]`)

| Verb | Route | Description |
|---|---|---|
| GET | /api/certificates/mine | List caller's certificates |

**Videos** — `api/videos` (`VideosController.cs`, `[Authorize]` + `ManageVideos` on admin actions)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/videos | ManageVideos | Create video record |
| POST | /api/videos/{id}/ready | ManageVideos | Mark video processing as ready |
| GET | /api/videos/{videoId}/playback | authenticated | Request playback (signed URL) |
| GET | /api/videos/lessons/{lessonId} | ManageVideos | Get a lesson's video |
| PUT | /api/videos/lessons/{lessonId} | ManageVideos | Replace a lesson's video |
| DELETE | /api/videos/lessons/{lessonId} | ManageVideos | Remove a lesson's video |

**Audit logs** — `api/audit-logs` (`AuditLogsController.cs`, `[Authorize(Policy=ReadAudit)]`)

| Verb | Route | Description |
|---|---|---|
| GET | /api/audit-logs | Paged audit log list |

**Testimonials** — `api/testimonials` (`TestimonialsController.cs`, `[Authorize(ManageCourses)]` except public)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | /api/testimonials | ManageCourses | Create testimonial |
| PUT | /api/testimonials/{testimonialId} | ManageCourses | Update testimonial |
| POST | /api/testimonials/{testimonialId}/publish | ManageCourses | Publish testimonial |
| POST | /api/testimonials/{testimonialId}/unpublish | ManageCourses | Unpublish testimonial |
| GET | /api/testimonials | ManageCourses | List all testimonials (admin) |
| GET | /api/testimonials/public | AllowAnonymous | List published testimonials (public) |

## (B) Endpoints already consumed by the frontend (45)

- **Auth**: `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/logout`, `GET /api/auth/me`, `POST /api/auth/confirm-email`, `POST /api/auth/resend-confirmation`
- **Landing**: `GET /api/testimonials/public`, `GET /api/courses/public-summary`
- **Catalog**: `GET /api/courses/available`, `GET /api/courses/{courseId}`, `GET /api/videos/{videoId}/playback`, `POST /api/progress/lessons`, `GET /api/progress/courses/{courseId}`
- **Admin/Areas**: `POST /api/areas`, `PUT /api/areas/{areaId}`, `GET /api/areas/{areaId}`, `GET /api/areas`
- **Admin/Users**: `POST /api/users`, `PUT /api/users/{userId}`, `GET /api/users/{userId}`, `GET /api/users?...`
- **Admin/Access**: `GET /api/access/requests/users/{userId}/granted`, `POST /api/access/requests/grant`, `GET /api/access/user-area/{userId}`, `POST /api/access/user-area`, `DELETE /api/access/user-area/{userId}/{areaId}`
- **Admin/Courses**: `GET /api/courses`, `POST /api/courses`, `PUT /api/courses/{courseId}`, `POST /api/courses/{courseId}/publish`, `POST /api/courses/{courseId}/unpublish`
- **Admin/Modules**: `GET .../modules`, `POST .../modules`, `PUT .../modules/{moduleId}`, `DELETE .../modules/{moduleId}`, `PUT .../modules/reorder`
- **Admin/Lessons**: `POST .../lessons`, `PUT .../lessons/{lessonId}`, `DELETE .../lessons/{lessonId}`, `PUT .../lessons/reorder`
- **Admin/Videos**: `GET /api/videos/lessons/{lessonId}`, `PUT /api/videos/lessons/{lessonId}`, `DELETE /api/videos/lessons/{lessonId}`, `POST /api/videos/{videoId}/ready`
- **Admin/Audit**: `GET /api/audit-logs`

## (C) Backend endpoints never called by the frontend (21)

**Auth**

- `POST /api/auth/forgot-password` (`ForgotPasswordAsync`) — the `/forgot-password` route constant exists (`src/lib/routes/app-routes.ts`) and is linked from `password-field.tsx`, but no page/API wrapper calls this endpoint yet. Needed for a "forgot password" screen.
- `POST /api/auth/reset-password` (`ResetPasswordAsync`) — companion "set new password" screen (reached via emailed link/token) is unbuilt.
- `POST /api/auth/refresh-token` (`RefreshAsync`) — no silent session-renewal flow client-side; access tokens presumably just expire and force re-login today.

**Users**

- `POST /api/users/{userId}/roles/{roleId}` (`AssignRoleAsync`) — admin "assign role to user" UI not built.
- `DELETE /api/users/{userId}/roles/{roleId}` (`RemoveRoleAsync`) — companion "remove role" action.

**Access**

- `POST /api/access/role-area` (`GrantRoleAreaAccessAsync`) — bulk "grant this role access to this area" UI (vs. current per-user grant) not built.
- `GET /api/access/courses/{courseId}` (`CheckOwnCourseAccessAsync`) — could gate a course-player route/paywall check before rendering, or drive a "request access" prompt for a locked course.
- `GET /api/access/users/{userId}/courses/{courseId}` (`CheckUserCourseAccessAsync`) — admin-side "does user X have access to course Y" lookup.
- `POST /api/access/course/check` — obsolete, superseded by the GET above; expected to stay unused.

**Access requests**

- `POST /api/access/requests` (`RequestCourseAccessUseCase`) — self-service "request access" button for a locked course in the catalog.
- `GET /api/access/requests/mine` (`ListMyAccessRequestsUseCase`) — student-facing "my access requests" status screen.
- `GET /api/access/requests` (`ListAccessRequestsUseCase`) — admin "pending access requests" inbox/queue.
- `POST /api/access/requests/{id}/approve` — approve action in that admin queue.
- `POST /api/access/requests/{id}/reject` — reject action in that admin queue.

(`POST /api/access/requests/grant` and `GET /api/access/requests/users/{userId}/granted` are already used by the frontend's direct-grant admin flow — only the request/approve/reject lifecycle is missing.)

**Certificates**

- `GET /api/certificates/mine` (`ListMyCertificatesUseCase`) — student "my certificates" screen; entirely unbuilt on the frontend.

**Videos**

- `POST /api/videos` (`CreateVideoUseCase`) — standalone "create a video record" step; the frontend only calls `PUT /api/videos/lessons/{lessonId}` (replace-on-lesson), so either the backend expects create-then-attach and the frontend currently skips straight to replace, or this action is reserved for a future upload pipeline.

**Testimonials** (admin-side CRUD is entirely absent — only the public list is consumed)

- `GET /api/testimonials` (`ListTestimonialsUseCase`) — admin testimonials management list (incl. unpublished).
- `POST /api/testimonials` (`CreateTestimonialUseCase`) — admin "add testimonial" form (a testimonial mockup, `1zb`, already exists per commit `a9c2721`).
- `PUT /api/testimonials/{testimonialId}` — edit testimonial.
- `POST /api/testimonials/{testimonialId}/publish` — publish toggle.
- `POST /api/testimonials/{testimonialId}/unpublish` — unpublish toggle.

## Addendum (2026-09-09)

While closing `unimplemented-mockup-screens.md`'s `1zb` row, found that
none of the five testimonials endpoints above actually let a *student*
submit their own testimonial — `POST /api/testimonials` requires
`ManageCourses` (admin-only). Added a new endpoint not covered by this
snapshot's inventory: `POST /api/testimonials/mine` (any authenticated
user, `AuthorName`/`AvatarUrl` derived server-side from the caller's own
profile — not free text). This is additive, not a correction to the
66-action inventory above, which stays accurate for what it audited at
the time.

## Takeaway

The biggest unbuilt clusters, by endpoint volume: the full access-request
self-service/approval lifecycle (5 endpoints), testimonials admin CRUD (5
endpoints), and auth password-reset + token-refresh (3 endpoints). These
look like the natural next features to spec, in roughly that priority order
(access requests and testimonials both have real screens already in the
mockup file backing them; token refresh doesn't need a screen at all — it's
a background session-renewal concern for `apiFetch`/`use-require-auth.ts`).

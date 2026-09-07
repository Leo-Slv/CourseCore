# Backend Pendencies — Admin Panel: Course create/edit

No spec was written for this screen yet — same status as
[`courses-panel.md`](courses-panel.md). Covers mockup artboard `1n`
("Curso — criar/editar"), added under the new "Painel admin — CRUDs de
entidades" mockup group. This screen sits on top of the same
`GET /api/courses` + `POST /api/courses` + `PUT /api/courses/{id}` surface
already documented as real in `courses-panel.md`'s "What's already real"
section (which, as of this backend snapshot, now also includes
`ListAllCoursesUseCase` behind `GET /api/courses` — the endpoint that
pendency 1 in that file said was missing; worth revisiting that file's
skip decision separately).

## 1. No "por inscrição" (enrollment-controlled) pricing model — CLOSED

- **Mockup expects**: a "Modelo de cobrança" control with three options —
  "Gratuito para a área", "Pago" (with a price field), and "Por inscrição
  (turma controlada)" — the third implying a cohort with controlled
  enrollment, distinct from a plain paid course.
- **Backend today**: `CoursePricingModel`
  (`Modules/Courses/Domain/Enums/CoursePricingModel.cs`) only has two
  values: `Free` and `Paid`. There is no third state, and no cohort/"turma"
  concept anywhere in the courses domain (no enrollment window, capacity,
  or class-grouping entity). Separately, `AccessRequestsController`
  (`Modules/Access/Presentation/Controllers/AccessRequestsController.cs`)
  does implement a request → approve/reject workflow
  (`POST /api/access/requests`, `GET /api/access/requests/mine`,
  `GET /api/access/requests`, `.../approve`, `.../reject`) that is
  conceptually close to "por inscrição", but it isn't wired to
  `CoursePricingModel` at all — it's a general access-request flow, not a
  third pricing option a course can be set to.
- **What's needed**: either add a third `CoursePricingModel` value backed
  by the existing access-request approval flow, or treat "por inscrição"
  as out of scope until that mapping is designed.
- **Workaround shipped**: none yet — screen not implemented.
- **Severity**: Feature gap.
- **Resolved (2026-09-07)**: added `EnrollmentControlled` as a third
  `CoursePricingModel` value (fits the existing `varchar(20)` column exactly
  — no migration needed). No new access-gating code was required:
  `ApproveAccessRequestUseCase` already grants real `UserAreaAccess` for
  every active area linked to a course on approval, so the existing
  request → approve/reject flow already works generically for any pricing
  model, `EnrollmentControlled` included. A course/turma concept beyond
  that (enrollment windows, capacity, presencial cadence) remains out of
  scope, same as the landing page's "featured formation" pendency.

## 2. No modules/lessons management after course creation — CLOSED

- **Mockup expects**: a "Conteúdo" panel on this screen linking to
  "Gerenciar módulos →" (artboard `1o`), implying modules and lessons can
  be added, edited, reordered, and removed on a course that already
  exists.
- **Backend today**: `CreateCourseRequest`
  (`Modules/Courses/Presentation/Requests/CreateCourseRequest.cs`) accepts
  a nested `Modules` collection — but only at creation time.
  `UpdateCourseRequest`
  (`Modules/Courses/Presentation/Requests/UpdateCourseRequest.cs`) has no
  `Modules` field at all (`Title`, `Slug`, `Description`, `ThumbnailUrl`,
  `DisplayOrder`, `PricingModel`, `PriceAmount`, `AreaIds` only), and there
  is no separate modules or lessons controller anywhere in
  `Modules/Courses/Presentation/Controllers/` (only `CoursesController`
  exists). Once a course is created, its module/lesson structure is frozen
  through the API — full detail in
  [`course-modules-lessons.md`](course-modules-lessons.md).
- **What's needed**: see `course-modules-lessons.md`; this card's "Gerenciar
  módulos" link has nothing to route to functionally.
- **Severity**: Blocking for that part of the screen — same root cause as
  the linked file.
- **Resolved (2026-09-07)**: see `course-modules-lessons.md` pendency 1 —
  `CourseModulesController`/`LessonsController` now provide add, edit,
  reorder, and remove for modules and lessons on an existing course. The
  "Gerenciar módulos →" link now has a real API to route to.

## 3. No certificate opt-in per course — CLOSED

- **Mockup expects**: an "Emitir certificado" toggle on the course edit
  screen, implying certificate issuance is a per-course setting an admin
  turns on or off.
- **Backend today**: the `Certificates` module is real
  (`Modules/Certificates/`, `Certificate.Issue(userId, courseId)`), but
  issuance is unconditional: `RegisterLessonProgressUseCase`
  (`Modules/Progress/Application/UseCases/RegisterLessonProgressUseCase.cs`,
  line ~140) calls `Certificate.Issue` automatically whenever a user
  reaches 100% completion on *any* course. `Course`
  (`Modules/Courses/Domain/Entities/Course.cs`) has no
  `HasCertificate`/`IssuesCertificate` flag or equivalent — there is no way
  to turn certificate issuance off for a specific course.
- **What's needed**: a per-course flag gating the `Certificate.Issue` call
  in `RegisterLessonProgressUseCase`, plus exposing it through
  create/update requests and course output DTOs.
- **Workaround shipped**: none yet — screen not implemented. Shipping the
  toggle as-is would be misleading (it would look like it controls
  something it doesn't).
- **Severity**: Feature gap.
- **Resolved (2026-09-07)**: added `Course.IssuesCertificate` (defaults
  `true` for existing behavior), exposed on create/update requests and the
  course output DTO, and `RegisterLessonProgressUseCase`'s automatic
  `Certificate.Issue` call is now gated on `course.IssuesCertificate`.

## 4. No cover-image upload

- **Mockup expects**: a "Capa do curso" drop zone ("Arraste uma imagem
  16:9") implying the admin can upload an image file directly from this
  screen.
- **Backend today**: `Course.ThumbnailUrl` is a plain `string?` set via
  `CreateCourseRequest`/`UpdateCourseRequest` — the API expects an already-
  hosted URL, not a file. There is no image/file upload endpoint anywhere
  in CourseCore (the only upload-adjacent flow is `VideosController`'s
  `POST /api/videos`, and even that only registers metadata for a video
  already placed in external storage — see pendency 3 in
  `course-modules-lessons.md`).
- **What's needed**: an image upload endpoint (or a documented external
  hosting step) if the drag-and-drop affordance is meant to work as drawn.
- **Workaround shipped**: none yet — screen not implemented; would ship as
  a plain URL field instead of a drop zone if built today.
- **Severity**: Cosmetic — a URL input covers the same end result, just
  with a worse admin workflow.
- **Decision (2026-09-07)**: out of scope for now — ship the "Capa do
  curso" field as a plain URL input instead of a drag-and-drop upload. No
  code change; this is a deliberate scope choice, not a gap to revisit
  unless the product direction changes.

## 5. No delete-course endpoint

- **Mockup expects**: an "Excluir curso" action on this screen.
- **Backend today**: no `DELETE` route exists anywhere in CourseCore (see
  the equivalent note in `areas-crud.md` pendency 1). `Unpublish()` is
  wired (`POST /api/courses/{id}/unpublish`), so hiding a course from
  students is possible; permanent removal is not.
- **What's needed**: either rely on unpublish as the shipped behavior for
  "Excluir curso", or add a real `DELETE /api/courses/{id}` endpoint.
- **Severity**: Feature gap.
- **Decision (2026-09-07)**: `Unpublish` (`POST /api/courses/{id}/unpublish`,
  already wired) is the shipped behavior behind "Excluir curso" — it hides
  the course from students without destroying progress/certificate/access
  history tied to it via FK. No real `DELETE` endpoint added, deliberately
  — same conservative, no-cascade choice applied to modules/lessons below.

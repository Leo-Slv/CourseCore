# Backend Pendencies — Course Detail Page

Spec: [`Docs/specs/catalog/course-detail.md`](../../specs/catalog/course-detail.md)

## 1. All-or-nothing course access — no partial data for locked courses — CLOSED

- **Mockup expects**: a "browse before you buy" layout — 2 of 8 modules
  shown unlocked as a preview, "Assistir aula grátis" on individual
  lessons, even for a course the account doesn't fully own.
- **Backend today**: `GetCourseDetailsUseCase` throws a **403 for the
  entire request** if `CourseAccessService.CanUserAccessCourseAsync`
  returns false. Access itself is all-or-nothing per course (free, or an
  area grant covering the whole course) — there is no partial state to
  return even if the endpoint wanted to.
- **What's needed**: either a lighter "preview" response
  (`GetCourseDetailsUseCase` returning module/lesson titles without full
  content for locked courses), or a real free-preview access path through
  `RequestVideoPlaybackUseCase` for lessons flagged as such.
- **Workaround shipped**: locked courses fall back to catalog-list data
  only (title, description, area, pricingModel) with no module breakdown —
  see pendency 2 below for why "just show the free ones" isn't possible
  either.
- **Severity**: Feature gap — this is the single biggest reason the page
  looks structurally different from the mockup, not just missing a field.
- **Resolved, 2026-09-04**: `GetCourseDetailsUseCase` no longer 403s the
  whole request for a locked-but-otherwise-valid paid course (user in good
  standing, course published) — it now returns the full module/lesson
  structure with a new `CourseDetailsResponse.HasAccess: bool` flag, exactly
  the "preview" option this pendency's own "What's needed" listed. The other
  two denial reasons (bad account, unpublished course) are unchanged hard
  403/404s — there's genuinely nothing to preview there. Locked, non-preview
  lessons report `VideoId: null` (see pendency 2 — that's the enforcement
  mechanism). Implemented as an allow-list against
  `CourseAccessService`'s existing denial reasons (now named constants,
  `CourseAccessDenialReasons`), so any future denial reason defaults to hard
  403 rather than silently starting to leak preview data.

## 2. `LessonResponse.FreePreview` exists but is completely unenforced — CLOSED

- **Mockup expects**: lessons marked free-preview are actually watchable
  without full course access.
- **Backend today**: the field is real on `LessonResponse` and is written
  by admin tooling (course creation already accepts `freePreview` per
  lesson), but nothing reads it on the access-check side —
  `GetCourseDetailsUseCase` 403s before any lesson is reachable at all
  without full access, and `RequestVideoPlaybackUseCase` checks the same
  all-or-nothing `CanUserAccessCourseAsync`, never `FreePreview`. Same
  category `User.EmailVerifiedAt` was in before the confirm-email feature
  gave it a consumer.
- **What's needed**: `RequestVideoPlaybackUseCase` (and
  `GetCourseDetailsUseCase`) would need a branch: allow access when
  `FreePreview == true`, independent of `CanUserAccessCourseAsync`.
- **Workaround shipped**: no UI built around `FreePreview` at all — a
  "grátis" badge on a lesson that 403s the moment its details or playback
  is requested would be a worse bug than not showing the badge.
- **Severity**: Feature gap — a CourseCore change, explicitly out of scope
  for this frontend to work around; noted so it isn't mistaken for an
  oversight later.
- **Resolved, 2026-09-04**: both branches this pendency asked for now exist.
  `RequestVideoPlaybackUseCase` allows playback when `lesson.FreePreview ==
  true`, independent of `CanUserAccessCourseAsync` — the real enforcement
  point, since a `VideoId` reaching the client some other way still can't be
  played without either full access or `FreePreview`. `GetCourseDetailsUseCase`
  (pendency 1) is the second layer: a free-preview lesson's `VideoId` is
  populated even on a locked course; every other lesson's is `null`.

## 3. No duration reachable in bulk — CLOSED

- **Mockup expects**: "12h de vídeo" at the course level.
- **Backend today**: duration only exists per-video
  (`VideoPlaybackOutput.DurationSeconds`), reachable one playback request
  at a time — not a course-level or bulk figure.
- **What's needed**: either a summed duration field on
  `CourseDetailsResponse`, or per-lesson duration exposed without needing a
  full playback request per video.
- **Severity**: Feature gap.
- **Resolved, 2026-09-04**: `LessonResponse.DurationSeconds` (added for
  `catalog/lesson-player.md` pendency 1) covers the "per-lesson duration
  exposed" option this pendency's own "What's needed" already accepted —
  "12h de vídeo" at course level is a client-side sum over the lessons
  already returned by `GET /api/courses/{id}`, no further backend field
  needed.

## 4. No certificate concept

- Same gap as [catalog/course-catalog.md](course-catalog.md) pendency 5 —
  "Sim" / certificado in the mockup has nothing behind it anywhere in the
  backend.
- **Severity**: Feature gap.

## 5. No price amount or installments — PARTIALLY CLOSED

- **Mockup expects**: "R$ 149", "ou 3× de R$ 49,67".
- **Backend today**: same as the catalog page — `PricingModel` is
  `Free`/`Paid` only, no amount, no installment concept.
- **Severity**: Feature gap.
- **Resolved, 2026-09-04**: the amount half of this gap is closed — see
  `Docs/backend-pendencies/catalog/course-catalog.md` pendency #3, same
  `Course.PriceAmount` field, surfaced on `CourseDetailsResponse` too.
  **Installments remain unresolved** — no installment concept was added (a
  single total amount only); that's a real gap still, not just a display
  choice, since "3× de R$ 49,67" implies a payment/billing concept this
  backend still has zero scope for.

## 6. No purchase/checkout endpoint

- **Mockup expects**: "Inscrever-se agora" starting a real checkout.
- **Backend today**: no purchase/checkout endpoint — explicit non-goal in
  the backend's own spec.
- **Workaround shipped**: button renders (so the page doesn't look
  visually broken vs. the mockup) but is inert — starts no flow.
- **Severity**: Feature gap — likely the largest, most deliberate scope
  boundary in the whole backend (payments were never in scope there).
- **Update, 2026-09-04**: evaluated building the mockup's own checkout
  screen (artboard `1i`, "Checkout — Pix / cartão") as its own page and
  confirmed the same finding applies to all of it, not just this button:
  `Docs/specs/catalog/self-registration-and-free-courses.md` §10 ("Fora
  de escopo") states outright that payment/checkout/subscription/billing
  is out of scope, that the catalog's lock is "só visual/informativo,
  sem nenhum caminho de 'clicar para comprar'", and that `PricingModel`
  records only `Free`/`Paid`, never an amount or currency. Free courses
  never reach this path at all (access is automatic once confirmed);
  Paid courses have zero self-service grant path — only an admin can
  grant `UserAreaAccess`/`RoleAreaAccess` manually. **Decision: skip
  building `1i` as a screen for now** rather than ship a fully inert
  shell with fabricated prices/discounts/a fake Pix QR code — no spec or
  implementation plan written. Revisit only once a real payments module
  gets its own spec on the CourseCore side first, same condition already
  attached to the `/auth/me` and forgot-password gaps.

## 7. No course "kind" field

- **Mockup expects**: "· Formação" next to the area name.
- **Backend today**: no such field on `CourseDetailsResponse` or the
  catalog item.
- **Severity**: Cosmetic.

## 8. No module-level progress — CLOSED

- **Mockup expects**: a progress bar per module card.
- **Backend today**: same per-course-only progress endpoint gap as the
  catalog page (`GET /api/progress/courses/{courseId}`) — nothing
  module-level.
- **Severity**: Feature gap.
- **Resolved, 2026-09-04**: `GET /api/progress/courses/{courseId}` now
  returns a `Modules` array (`ModuleId`, `LessonCount`,
  `CompletedLessonCount`, `ProgressPercent`) alongside the existing
  course-level percent and flat lesson list. Computed live at read time in
  `GetCourseProgressUseCase` from data it already loads (`Course.Modules`
  plus every lesson-progress row for the course) — no new query, no new
  persistence, no migration. This deliberately diverges from how
  course-level percent works (a denormalized column recalculated on every
  `POST /api/progress/lessons`): the write path (`RegisterLessonProgressUseCase`)
  is untouched, so per-module numbers are always freshly computed rather
  than relying on a second cached value staying in sync with the first.

# Backend Pendencies — My Courses Page

Spec: [`Docs/specs/catalog/my-courses.md`](../../specs/catalog/my-courses.md)

This page is assembled entirely from three endpoints already used by
earlier screens (`GET /api/courses/available?hasAccess=true`,
`GET /api/courses/{id}`, `GET /api/progress/courses/{id}`) — no new
endpoint gap of its own. The gaps below are the same standing ones
those earlier screens already hit, reconfirmed here because this page's
mockup leans on them harder (a dashboard card shows more per-course
detail than a catalog card does).

## 1. No duration anywhere — course-level, remaining-time, or per-lesson

- **Mockup expects**: "24 aulas · 7h" on a completed course's cover,
  "restam 8min" on the hero card's current-lesson line, "12min
  restantes" / "21min restantes" on in-progress cards.
- **Backend today**: same root gap as
  `Docs/backend-pendencies/catalog/lesson-player.md` pendency 1 — no
  route resolves a lesson's video, so no duration is reachable in bulk,
  remaining or total, at any level.
- **Workaround shipped**: lesson **count** ships (real, summed from
  `GET /api/courses/{id}`); every duration figure is dropped entirely,
  not estimated.
- **Severity**: Feature gap, same root cause as lesson-player pendency 1
  — closing it there closes it here too.

## 2. No certificate concept

- **Mockup expects**: "Certificado emitido" on a 100%-complete course's
  card.
- **Backend today**: same gap already documented in
  `Docs/backend-pendencies/catalog/course-catalog.md` pendency 5 — zero
  certificate-related code anywhere in the backend.
- **Workaround shipped**: completed cards show "100% concluído · rever"
  only, no certificate badge.
- **Severity**: Feature gap.

## 3. No per-card action backing the "⋮" kebab

- **Mockup shows**: a kebab icon on every course card, same as the
  catalog's own cards.
- **Backend today**: nothing to back any per-card action (unenroll,
  hide, report, etc.) — not a documented gap elsewhere because no prior
  screen's spec proposed one either.
- **Workaround shipped**: renders, inert — same treatment
  `course-card.tsx` already gives it on `/catalog`.
- **Severity**: Cosmetic.

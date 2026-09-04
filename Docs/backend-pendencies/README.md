# Backend Pendencies

Every screen spec under `Docs/specs/` is checked against what the CourseCore
backend (`c:\Users\leonardo.silva\source\repos\CourseCore`) actually exposes.
When a mockup asks for something the backend doesn't support today, it's
recorded here — one file per screen, mirroring `Docs/specs/<domain>/<feature>.md`
— instead of only living inside prose in the spec itself, so the full list can
be handed to backend work without re-reading every spec.

Each pendency entry states what the mockup expects, what the backend
actually has today (with the source evidence), what closing the gap would
require, the workaround shipped instead, and a rough severity:

- **Blocking** — the screen's core feature has no viable data path at all.
- **Feature gap** — a real capability is simply missing; the workaround is a
  stub or reduced scope.
- **Cosmetic** — decorative/informational only; the screen works without it.
- **Config** — not a backend code gap, just missing credentials/secrets.

## Index

| Screen | File | Worst severity |
|---|---|---|
| Landing (`/`) | [landing/landing-page.md](landing/landing-page.md) | Cosmetic |
| Register (`/register`) | [auth/register.md](auth/register.md) | Config |
| Login (`/login`) | [auth/login.md](auth/login.md) | Feature gap |
| Confirm email (`/confirm-email`) | [auth/confirm-email.md](auth/confirm-email.md) | Feature gap |
| Catalog (`/catalog`) | [catalog/course-catalog.md](catalog/course-catalog.md) | Feature gap |
| Course detail (`/courses/[slug]`) | [catalog/course-detail.md](catalog/course-detail.md) | Feature gap |
| Lesson player (`/courses/[slug]/lessons/[lessonId]`) | [catalog/lesson-player.md](catalog/lesson-player.md) | **Blocking** |
| My courses (`/my-courses`) | [catalog/my-courses.md](catalog/my-courses.md) | Feature gap |

## Skipped screens

Two mockup screens were evaluated and deliberately not built — the
backend gap was severe enough that shipping them would have meant
either fabricating data across the board or a workflow that silently
loses admin work. No spec exists for either; each is documented as a
pendency only, to revisit once its blocking gap closes on the
CourseCore side.

| Screen | File | Why skipped |
|---|---|---|
| Checkout — Pix/cartão (`1i`) | [catalog/course-detail.md](catalog/course-detail.md) (pendency 6 update) | No payment/checkout endpoint at all — explicit backend non-goal, not just a missing field. |
| Painel admin — Cursos (`1k`) | [admin/courses-panel.md](admin/courses-panel.md) | No endpoint lists draft courses (a draft would become unreachable after leaving the screen) and no audit-log read endpoint. |

New screens get their own file here as part of the standard spec workflow
(see `CLAUDE.md`, "Implementation Workflow") — add a row above when one is
created.

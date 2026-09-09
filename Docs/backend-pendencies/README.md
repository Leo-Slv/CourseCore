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
| Landing (`/`) | [landing/landing-page.md](landing/landing-page.md) | Cosmetic (pendency 4) |
| Register (`/register`) | [auth/register.md](auth/register.md) | Config |
| Login (`/login`) | [auth/login.md](auth/login.md) | Feature gap |
| Confirm email (`/confirm-email`) | [auth/confirm-email.md](auth/confirm-email.md) | Feature gap |
| Catalog (`/catalog`) | [catalog/course-catalog.md](catalog/course-catalog.md) | Feature gap |
| Course detail (`/courses/[slug]`) | [catalog/course-detail.md](catalog/course-detail.md) | Feature gap (pendencies 5-7 remain: installments, checkout, course "kind") |
| Lesson player (`/courses/[slug]/lessons/[lessonId]`) | [catalog/lesson-player.md](catalog/lesson-player.md) | **Blocking** |
| My courses (`/my-courses`) | [catalog/my-courses.md](catalog/my-courses.md) | Feature gap |
| Profile (`/profile`) | [auth/profile.md](auth/profile.md) | Feature gap |

## Skipped screens

Mockup screens evaluated and not (yet) built because the backend gap was
severe enough that shipping them would have meant either fabricating data
across the board or a workflow that silently loses admin work. No spec
exists for any of these; each is documented as a pendency only, to
revisit once its blocking gap(s) close on the CourseCore side.

| Screen | File | Why skipped |
|---|---|---|
| Checkout — Pix/cartão (`1i`) | [catalog/course-detail.md](catalog/course-detail.md) (pendency 6 update) | No payment/checkout endpoint at all — explicit backend non-goal, not just a missing field. |
| Painel admin — Cursos (`1k`) | [admin/courses-panel.md](admin/courses-panel.md) | **Built** — see `Docs/specs/admin/courses-panel.md`. |
| Áreas — lista / criar-editar (`1l`, `1m`) | [admin/areas-crud.md](admin/areas-crud.md) | **Built** — see `Docs/specs/admin/areas-list.md` and `Docs/specs/admin/area-form.md`. |
| Curso — criar/editar (`1n`) | [admin/course-crud.md](admin/course-crud.md) | **Built** — see `Docs/specs/admin/course-form.md`. Cover-image upload and course delete stay deliberately out of scope (plain URL field, `Unpublish` respectively). |
| Curso → Módulos e aulas (`1o`) | [admin/course-modules-lessons.md](admin/course-modules-lessons.md) | **Built** — see `Docs/specs/admin/course-modules.md`. |
| Aula — criar/editar (`1p`) | [admin/lesson-editor.md](admin/lesson-editor.md) | **Built** — see `Docs/specs/admin/lesson-editor.md`. Module reassignment now has a real endpoint (`PUT .../lessons/{lessonId}/move`, same-course only); direct order editing stays a won't-implement decision (list-based reorder only); video registration ships as a YouTube-link form, not an upload. |
| Usuários — lista (`1q`) | [admin/users-panel.md](admin/users-panel.md) | **Built** — see `Docs/specs/admin/users-list.md`. Role read, area-grant read (now batched into `GET /api/users` via `AreaNames`, pendency 8 closed), aggregate counts, and search are all wired. |
| Usuário — editar acesso (`1r`) | [admin/users-panel.md](admin/users-panel.md) | **Built** — see `Docs/specs/admin/user-access-edit.md`. Area grant/revoke, account status, paid-course grants, and role assignment (pendency 9 closed via `GET /api/roles`) are all wired. |
| Painel admin — Vídeos (`1zc`) | [admin/videos-panel.md](admin/videos-panel.md) | **Backend unblocked (2026-09-09)** — see `admin/videos-panel.md`. `GET /api/videos` (paginated, not lesson-scoped) now exists; YouTube id/URL are derived response fields; visibility (`Active`/`Unlisted`) is a real additive field with `Publish`/`Unpublish`-style toggle endpoints. "Unlinked video" stays a won't-implement decision (`LessonId` remains a required, unique FK). Building the screen itself is still frontend work — no spec exists yet. |

New screens get their own file here as part of the standard spec workflow
(see `CLAUDE.md`, "Implementation Workflow") — add a row above when one is
created.

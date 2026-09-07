# Backend Pendencies — Landing Page

Spec: [`Docs/specs/landing/landing-page.md`](../../specs/landing/landing-page.md)

## 1. No public/anonymous catalog or stats endpoint — CLOSED

- **Mockup expects**: a hero stat row ("6 áreas de ensino", "18 cursos
  publicados", "11 anos de igreja"), three featured course cards
  (Fundamentos da Fé, Curso de Batismo, Escola de Líderes), and (added in the
  latest mockup revision) a full "Áreas de ensino" grid listing all 6 areas
  with a per-area course count (Discipulado · 6 cursos, Liderança · 4 cursos,
  Família · 3 cursos, Ministérios · 2 cursos, Teologia · 2 cursos,
  Conferências · 1 curso) — all reflecting real data, visible to an anonymous
  visitor before they create an account.
- **Backend today**: `GET /api/courses/available` requires a Bearer token;
  `GET /api/areas` (`AreasController`) requires the `ManageAreas`-family
  policies (`[Authorize]` at the controller level, stricter policies per
  action) — there is no anonymous catalog, areas, or stats endpoint anywhere
  in CourseCore. Additionally, `AreaSummaryOutput`
  (`Modules/Courses/Application/DTOs/AreaSummaryOutput.cs`) — the summary DTO
  areas are projected to — only carries `Id`, `Name`, `Slug`, and
  `DisplayOrder`; it has no course-count field, so even an authenticated
  caller can't get the per-area counts the areas grid needs without an
  additional aggregation.
- **What's needed**: a public (no-auth) endpoint that returns area/course
  counts (including a per-area course count), and a small set of published,
  featured courses — safe to expose to a visitor with no account.
- **Workaround shipped**: hero stats, the three featured cards, and the
  areas grid (name + course count per area) are static editorial content
  hardcoded in the frontend (decided 2026-09-03, extended for the areas grid
  2026-09-07). They will silently drift from the real catalog over time.
- **Severity**: Cosmetic — the page renders and functions fully without this;
  the cost is stale numbers/picks, not a broken feature.
- **Resolved (2026-09-07)**: added `GET /api/courses/public-summary`
  (`[AllowAnonymous]`) returning `ActiveAreaCount`, `PublishedCourseCount`,
  up to 3 `FeaturedCourses` (published courses, safe public subset only —
  no `HasAccess`/pricing/area/certificate fields), and a full `Areas` list
  (active areas only, each with `Id`/`Name`/`Slug`/`PublishedCourseCount`)
  — covers both the hero stats and the "Áreas de ensino" grid with real
  per-area counts. `GetPublicCatalogSummaryUseCase` composes existing
  `ICourseRepository.ListPublishedAsync()` and `IAreaRepository.ListAsync()`
  — no new repository methods or schema changes needed. Covered by unit
  tests (`GetPublicCatalogSummaryUseCaseTests`) and integration tests
  (`CoursesIntegrationTests`, anonymous access + field-exposure checks).

## 2. No "featured formation" concept in the backend — CLOSED (minimum scope)

- **Mockup expects** (new section in the latest mockup revision): a
  "Formação em destaque" panel highlighting one specific offering ("Escola
  de Líderes 2026" — 8 módulos, mentoria em grupo, certificado, turma
  limitada com encontros presenciais mensais) with its own image, copy, and
  CTAs ("Inscrever-se" / "Saiba mais →"), distinct from the plain
  "Comece por aqui" course cards.
- **Backend today**: there is no "featured"/"highlighted" flag or field
  anywhere in the courses domain (no `IsFeatured`, `Featured`, or
  `Highlight` member on any course entity/DTO). Nothing in CourseCore
  distinguishes one course/offering as promotable above the rest, and there
  is no concept of a "turma" (cohort/class) with its own enrollment window,
  presencial meeting cadence, or capacity limit — the mockup's copy implies
  scheduling/capacity data CourseCore doesn't model at all.
- **What's needed**: at minimum, a way to mark a course as featured (for the
  panel's course reference) and, if the "turma" framing is meant literally,
  a cohort/enrollment-window concept — currently absent from the domain
  model entirely.
- **Workaround shipped**: none yet — this section is not implemented; it
  would ship as static editorial content (same pattern as pendency 1) since
  no backend data exists to back it.
- **Severity**: Feature gap — the section can't be wired to real data at
  all, not even partially; shipping it means 100% hardcoded copy with no
  path to a real course reference until a featured flag exists.
- **Resolved (2026-09-07), minimum scope**: added `Course.IsFeatured`
  (bool, defaults `false`), exposed through create/update requests and
  `CourseOutput`/`CourseResponse`. `GET /api/courses/public-summary` now
  returns `HighlightedCourse` (the first published course with
  `IsFeatured = true`, same public-safe shape as `FeaturedCourses`, `null`
  if none is marked). The "turma"/cohort concept (enrollment window,
  presencial cadence, capacity) was deliberately **not** built — same
  decision already made for `course-crud.md`'s "por inscrição" pricing
  model; the panel's "Inscrever-se" CTA and cohort-specific copy still
  need to ship as static text until/unless that concept is designed.

## 3. No testimonials/reviews entity or endpoint — CLOSED (minimum scope)

- **Mockup expects** (new section in the latest mockup revision): an "O que
  os alunos dizem" section with three student testimonials, each a quote
  plus author name and avatar (Marina Souza, Carlos Andrade, Renata Lima),
  attributed to specific courses (Curso de Batismo, Escola de Líderes,
  and an unattributed general one).
- **Backend today**: no testimonial, review, or feedback entity exists
  anywhere in CourseCore (no `Testimonial`, `Review`, or `Depoimento` type
  in the domain, application, or persistence layers). There is nothing to
  moderate, store, or fetch this content from.
- **What's needed**: a new domain concept end-to-end — an entity to store a
  testimonial (author, quote, optional course reference, optional avatar),
  an admin-side way to create/moderate them, and a public endpoint to read a
  featured subset. This is a full new feature, not a gap in an existing one.
- **Workaround shipped**: none yet — this section is not implemented; it
  would ship as static editorial content (same pattern as pendency 1) since
  no backend data exists to back it, and the three named individuals in the
  mockup are placeholder copy, not real submissions.
- **Severity**: Feature gap — same as pendency 2, there is no partial data
  path here; it's all hardcoded copy or the section doesn't ship.
- **Resolved (2026-09-07), minimum scope**: added a new `Testimonials`
  module (mirrors the `Certificates` module precedent from earlier this
  session — a standalone bounded concept, not shoehorned into `Courses`).
  `Testimonial` (`AuthorName`, `Quote`, optional `AvatarUrl`, optional
  `CourseId` FK to `courses`, `Published`) defaults unpublished on
  creation (moderation gate). Admin surface — all behind the existing
  `ManageCourses` policy (reused rather than wiring a new policy for one
  module, same call already made for role assignment in
  `users-panel.md`): `POST /api/testimonials`, `PUT /api/testimonials/{id}`,
  `POST /api/testimonials/{id}/publish`, `POST /api/testimonials/{id}/unpublish`,
  `GET /api/testimonials` (all, any status). Public surface:
  `GET /api/testimonials/public` (`[AllowAnonymous]`, published only,
  capped at 3, newest first) — matches the mockup's three-testimonial
  section exactly. No delete endpoint (same conservative, no-cascade
  choice already applied to courses/areas/modules this session) — a bad
  testimonial gets unpublished, not removed.

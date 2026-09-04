# Backend Pendencies — Landing Page

Spec: [`Docs/specs/landing/landing-page.md`](../../specs/landing/landing-page.md)

## 1. No public/anonymous catalog or stats endpoint

- **Mockup expects**: a hero stat row ("6 áreas de ensino", "18 cursos
  publicados", "11 anos de igreja") and three featured course cards
  (Fundamentos da Fé, Curso de Batismo, Escola de Líderes), all reflecting
  real data, visible to an anonymous visitor before they create an account.
- **Backend today**: `GET /api/courses/available` requires a Bearer token;
  `GET /api/areas` requires the `ManageAreas` policy. There is no anonymous
  catalog or stats endpoint anywhere in CourseCore.
- **What's needed**: a public (no-auth) endpoint that returns area/course
  counts and a small set of published, featured courses — safe to expose to
  a visitor with no account.
- **Workaround shipped**: hero stats and the three featured cards are static
  editorial content hardcoded in the frontend (decided 2026-09-03). They will
  silently drift from the real catalog over time.
- **Severity**: Cosmetic — the page renders and functions fully without this;
  the cost is stale numbers/picks, not a broken feature.

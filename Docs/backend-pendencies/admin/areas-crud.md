# Backend Pendencies — Admin Panel: Areas CRUD

No spec was written for these screens yet — same status as
[`courses-panel.md`](courses-panel.md). Covers mockup artboards `1l`
("Áreas — lista") and `1m`("Área — criar/editar"), added under the new
"Painel admin — CRUDs de entidades" mockup group.

## 1. No delete-area endpoint — CLOSED (via decision)

- **Mockup expects**: an "Excluir área" action on the area edit screen
  (`1m`).
- **Backend today**: `AreaManagementController`
  (`Modules/Access/Presentation/Controllers/AreaManagementController.cs`)
  only exposes `POST /api/areas` (create), `PUT /api/areas/{id}` (update),
  `GET /api/areas/{id}`, and `GET /api/areas` (list) — no `DELETE`. In fact,
  there is no `[HttpDelete]` route anywhere in the entire CourseCore
  codebase; deletion as an operation doesn't exist yet for any entity.
  `Area.Deactivate()` exists as a domain method and is reachable through
  `PUT /api/areas/{id}` (toggling `Active`), so "soft delete via
  deactivation" is possible — true deletion is not.
- **What's needed**: either accept deactivation as the shipped behavior
  for "Excluir área" (rename the affordance client-side to avoid promising
  something that doesn't happen), or add a real `DELETE /api/areas/{id}`
  endpoint on the backend.
- **Workaround shipped**: none yet — screen not implemented.
- **Severity**: Feature gap.
- **Decision (2026-09-07)**: same conservative choice already made for
  course delete (`course-crud.md` pendency 5) — `Area`'s FK relationships
  (`CourseAreas`, `UserAreaAccess`, `RoleAreaAccess`) are all
  `DeleteBehavior.Restrict`, identical to `Course`'s. No real `DELETE`
  endpoint added; `Deactivate()` (already reachable via
  `PUT /api/areas/{id}`) is the shipped behavior behind "Excluir área."

## 2. No per-area course count or course list — CLOSED

- **Mockup expects**: the areas list (`1l`) shows a "Cursos" count column
  per area (7, 4, 3, 5, 2, 2 in the mockup), and the area edit screen
  (`1m`) shows a "Cursos nesta área" panel listing each course's title and
  status (Publicado/Rascunho).
- **Backend today**: `AreaOutput`
  (`Modules/Access/Application/DTOs/AreaOutput.cs`) — the DTO backing both
  `GET /api/areas` and `GET /api/areas/{id}` — only carries `Id`, `Name`,
  `Slug`, `Description`, `Active`, `DisplayOrder`, `CreatedAt`, `UpdatedAt`.
  No course count, no course list. `Course.AreaIds` exists on the course
  side (`Modules/Courses/Domain/Entities/Course.cs`) so the relationship is
  modeled, but nothing projects "courses by area" back out through an
  areas-facing endpoint. This is the same underlying gap already flagged
  for the public areas grid in
  [`Docs/backend-pendencies/landing/landing-page.md`](../landing/landing-page.md)
  (pendency 1) — here it recurs for the authenticated admin surface, and
  additionally needs each course's title + publish status, not just a
  count.
- **What's needed**: extend `AreaOutput` (or add a dedicated admin
  response) with a course count and, for the single-area view, a list of
  `{ title, published }` per attached course.
- **Workaround shipped**: none yet — screen not implemented.
- **Severity**: Feature gap — the rest of the CRUD (name, slug, description,
  active toggle, display order) is real and works; only this aggregation is
  missing.
- **Resolved (2026-09-07)**: `GET /api/areas` and `GET /api/areas/{id}` now
  return `CourseCount` (both endpoints), and `GET /api/areas/{id}`
  additionally returns `Courses: [{ id, title, slug, published }]` for
  every course linked to that area. Computed by filtering
  `ICourseRepository.ListAsync()` (all courses, published + draft — an
  admin view, unlike the public landing-page summary which is
  published-only) by `course.AreaIds.Contains(areaId)`, no new repository
  method needed.

## 3. No "accent color" field on Area — CLOSED

- **Mockup expects**: a "Cor de destaque" picker on the area edit screen
  (`1m`) — four preset swatches, one selected — presumably used to tint
  that area's cards/badges elsewhere in the UI.
- **Backend today**: `Area`
  (`Modules/Access/Domain/Entities/Area.cs`) has no color-related field at
  all (`Name`, `Slug`, `Description`, `Active`, `DisplayOrder` only).
- **What's needed**: an `AccentColor` (or similar) field on `Area`, exposed
  through create/update requests and `AreaOutput`.
- **Workaround shipped**: none yet — screen not implemented.
- **Severity**: Cosmetic — the rest of the screen works without it; the
  picker would just have nothing to persist to.
- **Resolved (2026-09-07)**: added `AreaAccentColor` enum
  (`Modules/Access/Domain/Enums/AreaAccentColor.cs`) with 4 preset values
  (`Blue`, `Green`, `Purple`, `Orange` — the actual 4 swatch colors/names
  weren't available in this backend repo's copy of the mockup; adjust the
  enum member names if the frontend's real presets differ, it's a
  same-shape rename). Exposed on create/update requests and `AreaOutput`,
  defaults to `Blue` for areas that don't set it explicitly.

## What's already real

- Full area CRUD minus delete: `POST /api/areas`, `PUT /api/areas/{id}`,
  `GET /api/areas/{id}`, `GET /api/areas` — all behind `ManageAreas`,
  covering name, slug, description, active/inactive, and display order.
  These back the bulk of both `1l` and `1m` already.

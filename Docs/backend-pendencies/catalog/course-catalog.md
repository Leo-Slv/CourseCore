# Backend Pendencies — Course Catalog Page

Spec: [`Docs/specs/catalog/course-catalog.md`](../../specs/catalog/course-catalog.md)

`GET /api/courses/available` → `CourseCatalogResponse` is the only data
source for this screen. Its full shape (`CourseCatalogItemResponse.cs`,
`AreaSummaryResponse.cs`):

```
Areas:   { id, name, slug, displayOrder }[]
Courses: { id, title, slug, description, thumbnailUrl, displayOrder,
           pricingModel /* "Free" | "Paid" */, areaIds[], hasAccess }[]
```

Everything below is mockup content with nothing in that shape behind it.

## 1. No module/lesson counts or duration per course

- **Mockup expects**: "24 aulas · 7h", "8 módulos" on each card.
- **Backend today**: only reachable via `GET /api/courses/{id}`
  (`CourseDetailsResponse.Modules`), one course at a time — fetching that
  for every catalog card would be N extra requests for a single list
  screen.
- **What's needed**: lesson/module counts (and ideally a summed duration)
  added directly to `CourseCatalogItemResponse`, computed server-side.
- **Workaround shipped**: none — card shows title/area/description only.
- **Severity**: Feature gap.

## 2. No per-course progress

- **Mockup expects**: "66% concluído · retomar" with a progress bar on
  courses already in progress.
- **Backend today**: `GET /api/progress/courses/{courseId}` exists but is
  per-course, not bulk — nothing returns progress for every course in one
  call.
- **What's needed**: either a bulk progress-by-course-ids endpoint, or
  progress folded directly into the catalog response.
- **Workaround shipped**: none — this belongs to a "Meus cursos" dashboard
  (mockup `1j`, not yet specced), not the catalog grid.
- **Severity**: Feature gap.

## 3. No real price amounts

- **Mockup expects**: "R$ 149", "R$ 99" on paid courses.
- **Backend today**: `PricingModel` is a string enum, `"Free"` or `"Paid"`
  only — there is no amount field anywhere in the domain.
- **What's needed**: a price field (amount + currency, presumably) on the
  course domain/DTO.
- **Workaround shipped**: badge only ever says "Gratuito" or "Pago", no
  number.
- **Severity**: Feature gap.

## 4. No "por concessão" (grant-only) category

- **Mockup expects**: a third pricing category, distinct from Free/Paid,
  for courses only reachable via an admin-granted area.
- **Backend today**: only two `PricingModel` values exist; this isn't a
  real category, it's an access-reason.
- **What's needed**: not necessarily a backend change — this may just be a
  frontend concept once richer access-reason data exists (see course-detail
  pendencies).
- **Workaround shipped**: replaced with a two-state model (unlocked /
  locked) — see the spec's "Access states" section.
- **Severity**: Cosmetic — folded into a simpler, honest model instead.

## 5. No certificate concept

- **Mockup expects**: "Certificado emitido" / "Certificado ao concluir"
  badges, plus a "Certificados" nav item.
- **Backend today**: zero certificate-related code anywhere (`grep -r
  Certificate` across `Modules/` — no hits).
- **What's needed**: an entire certificate feature (issuance, storage,
  verification) — this is not a missing field, the concept doesn't exist.
- **Workaround shipped**: "Certificados" nav item renders inert.
- **Severity**: Feature gap — largest single missing feature in this
  screen's mockup.

## 6. No request-access / "Solicitar acesso" endpoint

- **Mockup expects**: a self-service "Solicitar acesso" action on locked
  courses.
- **Backend today**: access is granted only by an admin directly via
  `UserAreaAccess`/`RoleAreaAccess` — no request/approval workflow exists.
- **What's needed**: a request-access endpoint plus an admin approval
  surface.
- **Workaround shipped**: locked courses show a badge only, no action.
- **Severity**: Feature gap.

## 7. No server-side search or area filter

- **Mockup expects**: implied by having filter pills and a search box at
  all.
- **Backend today**: no search/text query param exists on
  `GET /api/courses/available` — the only filter is `?hasAccess=true|false`.
- **What's needed**: nothing, currently — the full list is small enough
  (no pagination either) that client-side filtering is a reasonable
  permanent choice, not just a stopgap.
- **Workaround shipped**: area pill + search text are both client-side
  filters over the one fetched list.
- **Severity**: Cosmetic — noted for completeness; not considered something
  to request from backend unless the catalog grows large enough that
  fetching everything stops being viable.

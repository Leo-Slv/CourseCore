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

## 1. No module/lesson counts or duration per course — CLOSED

- **Mockup expects**: "24 aulas · 7h", "8 módulos" on each card.
- **Backend today**: only reachable via `GET /api/courses/{id}`
  (`CourseDetailsResponse.Modules`), one course at a time — fetching that
  for every catalog card would be N extra requests for a single list
  screen.
- **What's needed**: lesson/module counts (and ideally a summed duration)
  added directly to `CourseCatalogItemResponse`, computed server-side.
- **Workaround shipped**: none — card shows title/area/description only.
- **Severity**: Feature gap.
- **Resolved, 2026-09-04**: `CourseCatalogItemResponse` now carries
  `ModuleCount`, `LessonCount`, and `DurationSeconds` (summed video
  duration across all of the course's lessons, regardless of video
  processing status). Computed server-side via a new bulk
  `ICourseRepository.ListContentSummariesAsync` projection (module/lesson
  counts, no per-course N+1) combined with a new bulk
  `IVideoRepository.ListDurationSecondsByLessonIdsAsync` lookup, composed
  in `ListAvailableCoursesUseCase`. No schema change — both existing
  tables already carried the data. `GET /api/courses/available` is the
  only endpoint affected.

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

## 3. No real price amounts — CLOSED

- **Mockup expects**: "R$ 149", "R$ 99" on paid courses.
- **Backend today**: `PricingModel` is a string enum, `"Free"` or `"Paid"`
  only — there is no amount field anywhere in the domain.
- **What's needed**: a price field (amount + currency, presumably) on the
  course domain/DTO.
- **Workaround shipped**: badge only ever says "Gratuito" or "Pago", no
  number.
- **Severity**: Feature gap.
- **Resolved, 2026-09-04**: `Course` gained an optional `PriceAmount`
  (`decimal?`), exposed on the catalog item, course, and course-details
  responses. Two deliberate scope decisions, made with the user: (1) no
  `Currency` field — every mockup only ever shows `R$`, so the amount is
  implicitly BRL; a currency field can be added later without renaming if a
  real need appears. (2) no "Paid requires a price" invariant — this field is
  retrofitted onto an existing table, so every already-existing Paid course
  has no price today; the only enforced rule is that a **Free** course may
  never carry one (`DomainException`, 400). A Paid course without a price
  still just shows "Pago" with no number, same as before this change — a real
  number appears once an admin sets one via `PUT /api/courses/{id}`. This
  extends beyond `Docs/specs/catalog/self-registration-and-free-courses.md`
  §10, which scoped `PricingModel` to classification-only — the user
  explicitly chose to extend it for **display value only**; no
  payment/checkout/billing concept was added, matching that section's actual
  boundary.

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

## 6. No request-access / "Solicitar acesso" endpoint — CLOSED

- **Mockup expects**: a self-service "Solicitar acesso" action on locked
  courses.
- **Backend today**: access is granted only by an admin directly via
  `UserAreaAccess`/`RoleAreaAccess` — no request/approval workflow exists.
- **What's needed**: a request-access endpoint plus an admin approval
  surface.
- **Workaround shipped**: locked courses show a badge only, no action.
- **Severity**: Feature gap.
- **Resolved, 2026-09-04**: new `AccessRequest` aggregate (Access module,
  Pending/Approved/Rejected) plus a full API surface —
  `POST /api/access/requests` (any authenticated user; targets a **course**,
  not an area, since holding access to any one of a course's linked areas
  already unlocks it — same semantics `CourseAccessService` already uses;
  rejects free courses, already-has-access, and duplicate-pending with 409),
  `GET /api/access/requests/mine` (own requests), and, behind the same
  `ManageUserAreaAccess` policy an admin already needs to grant access
  directly: `GET /api/access/requests` (optional `?status=`),
  `POST /api/access/requests/{id}/approve` (grants `UserAreaAccess` for every
  currently active area linked to the course — no area picker needed),
  `POST /api/access/requests/{id}/reject`. This repo has no admin frontend
  (the admin course panel was itself skipped elsewhere in this directory), so
  "the admin approval surface" here is this API, not a screen.

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

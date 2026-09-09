# Profile (`1z`) — backend pendencies

Mirrors [`Docs/specs/auth/profile.md`](../../specs/auth/profile.md).

## 1. Self-service profile update (name/email) — no endpoint — CLOSED (name/phone/avatar only)

- **Mockup expects**: an editable "Nome completo" and "E-mail" that save
  back with a "Salvar alterações" button.
- **Backend has today**: `PUT /api/users/{userId}`
  (`Modules/Users/Presentation/Controllers/UsersController.cs`) is the only
  write path onto a `User`, and the controller is decorated
  `[Authorize(Policy = AuthPolicyNames.ManageUsers)]` at the class level —
  every action on it, including this one, requires the admin `ManageUsers`
  claim. There is no "update my own account" route a regular authenticated
  user (without that claim) can call, and no branch in
  `UpdateUserUseCase`/`UsersController` that treats "acting on my own
  `userId`" as a special case.
- **What closing the gap would need**: a new endpoint (e.g.
  `PUT /api/auth/me` or `PATCH /api/users/me`) authorized by "any
  authenticated user, acting on their own id" rather than the `ManageUsers`
  policy, plus a use case that doesn't allow changing fields
  `ManageUsers`-gated flows currently control (e.g. `Active`).
- **Workaround shipped**: name and email render read-only, sourced from
  `GET /api/auth/me`. No form, no save button.
- **Severity**: Feature gap.
- **Resolved (2026-09-09)**: `PUT /api/auth/me` (new `UpdateOwnProfileUseCase`,
  `AuthController.UpdateProfileAsync`) — any authenticated user, no new
  policy, identical to `MeAsync`'s "plain auth + `GetCurrentUserId()`"
  shape. Accepts `Name`, `Phone`, `AvatarUrl` — **not** `Email`: self-service
  email change stays out per pendency 2 of `confirm-email.md` ("won't
  implement", admin-only via `PUT /api/users/{id}`), a decision this round
  reaffirmed rather than reopened. Only changed fields are persisted and
  audited (`UserProfileUpdated`); unlike admin `UpdateUserUseCase`, a
  name/phone/avatar change does **not** increment `TokenVersion` or revoke
  other sessions — forcing re-login for a cosmetic change would be a poor
  self-service UX, and there's no security reason to (that stays reserved
  for password/email changes). Response reuses `CurrentUserResponse`, so it
  matches `GET /api/auth/me`'s shape exactly.

## 2. In-session password change — no endpoint — CLOSED

- **Mockup expects**: a "Nova senha" field with placeholder "Deixe em
  branco para manter a atual", implying an optional-change field on the
  same save action as above.
- **Backend has today**: password changes only exist via the
  forgot-password flow — `POST /api/auth/forgot-password` issues an email
  with a reset token, `POST /api/auth/reset-password` consumes that token.
  Neither endpoint accepts a current-session bearer token in place of the
  reset token; there's no "confirm current password, set new one from an
  authenticated request" use case anywhere in `Modules/Auth`.
- **What closing the gap would need**: a new
  `POST /api/auth/change-password` (or similar) endpoint, authenticated by
  the normal bearer token, accepting `{ currentPassword, newPassword }`.
- **Workaround shipped**: none — the field doesn't render. (The existing
  `/forgot-password` flow, already built per `Docs/specs/auth/login.md`,
  remains the only password-change path and isn't linked from this screen
  since it's an unauthenticated-flow entry point, not a "change it now"
  action.)
- **Severity**: Feature gap.
- **Resolved (2026-09-09)**: `POST /api/auth/change-password` (new
  `ChangeOwnPasswordUseCase`), `{ CurrentPassword, NewPassword }`. Verifies
  the current password via the existing `IPasswordHasher.Verify` (401 if
  wrong — same `UnauthorizedAccessException`/message `LoginUseCase` uses),
  validates the new one via the existing `IPasswordPolicy`, then mirrors
  `ConfirmPasswordResetUseCase`'s post-change block exactly: hash + set,
  `IncrementTokenVersion`, `RevokeActiveByUserIdAsync` (all other sessions
  log out), and the same 3 audit-log entries (a new `PasswordChanged`
  action instead of `PasswordResetSucceeded`, to distinguish self-service
  change from the forgot-password flow, plus the existing
  `UserTokenVersionIncremented`/`UserSessionsRevoked`). Gated by a new
  `AuthChangePassword` rate-limit policy (5/60s, same default as
  `AuthResetPassword` — brute-forcing a known account's current password is
  the same risk class).

## 3. Profile photo upload — no field, no endpoint — CLOSED (via decision: plain URL, no upload)

- **Mockup expects**: an avatar image with an "Alterar foto" upload
  action, "JPG ou PNG, até 4MB".
- **Backend has today**: `Modules/Users/Domain/Entities/User.cs` has no
  avatar/photo/image field at all, and no controller in the codebase
  accepts a file upload for a user record. This is the same standing gap
  already documented for course cover images (`admin/course-crud.md`) and
  lesson video files (`admin/lesson-editor.md`) — CourseCore has no binary
  upload/storage pipeline for user-facing media anywhere yet.
- **What closing the gap would need**: an `AvatarUrl` (or similar) field
  on `User`, a file-storage integration, and an upload endpoint.
- **Workaround shipped**: an initials-circle avatar (the same treatment
  `AppNav` already uses), no upload control rendered at all.
- **Severity**: Feature gap.
- **Decision/Resolved (2026-09-09)**: same conservative choice already made
  for course cover images (`admin/course-crud.md` #4, plain URL field) and
  lesson videos (YouTube-link only, no raw upload) — CourseCore still has
  no binary upload/storage pipeline anywhere, so this doesn't introduce a
  new exception to that. Added `User.AvatarUrl` (`string?`, plain URL,
  settable via `PUT /api/auth/me`), exposed on `CurrentUserResponse`. No
  upload endpoint; the "Alterar foto" drop-zone affordance would need to
  ship as a URL field instead, same as the course-cover decision.

## 4. Phone number — no field — CLOSED

- **Mockup expects**: a "Telefone" field showing a formatted number.
- **Backend has today**: no `Phone` (or equivalent) field on `User`.
- **What closing the gap would need**: a new field on `User` plus
  read/write support through whatever endpoint ends up handling pendency 1.
- **Workaround shipped**: dropped entirely — not shown, not disabled.
- **Severity**: Feature gap.
- **Resolved (2026-09-09)**: added `User.Phone` (`string?`, plain string,
  no format/country validation), settable via `PUT /api/auth/me`, exposed
  on `CurrentUserResponse`. New migration `AddUserPhoneAndAvatarUrl` (two
  nullable columns, no backfill).

## Resolved

- **`GET /api/auth/me`** — the screen's one working data source.
  `AuthController.MeAsync` → `GetCurrentUserUseCase` →
  `CurrentUserResponse { UserId, Name, Email, Active, EmailVerifiedAt, Roles }`.
  This closes the `CLAUDE.md` "Known gap, decided 2026-09-02" note about
  there being no current-user endpoint — that note should be removed from
  `CLAUDE.md` as part of shipping this screen.
- **`POST /api/auth/logout`** — real, `[AllowAnonymous]`, reads the refresh
  token from the httpOnly cookie (or an optional body field), calls
  `LogoutUseCase.ExecuteAsync`, and clears the refresh-token cookie
  server-side. Used both by this screen's "Sair da conta" and upgraded
  into `AppNav`'s existing "Sair" (previously client-only
  `clearAccessToken()`).

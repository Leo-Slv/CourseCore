# Backend Pendencies — Login Page

Spec: [`Docs/specs/auth/login.md`](../../specs/auth/login.md)

## 1. No forgot-password / password-recovery endpoint

- **Mockup expects**: an "Esqueci minha senha" link leading to a real
  recovery flow (the reference project this repo is adapted from has one,
  against a different backend).
- **Backend today**: no password-recovery endpoint anywhere in CourseCore —
  confirmed absent from the backend's own
  `Docs/specs/catalog/self-registration-and-free-courses.md` "Fora de
  escopo" list.
- **What's needed**: a request-reset endpoint (email a reset token/link) and
  a confirm-reset endpoint (accept the token + new password), plus an email
  template for it.
- **Workaround shipped**: "Esqueci" is a stub link (`appRoutes.auth
  .forgotPassword`), no page behind it.
- **Severity**: Feature gap — no partial version of this is possible until
  the backend adds it; revisit only if it gets specced on the CourseCore
  side first.

## 2. "Remember me" has no backend concept

- **Mockup expects**: a "Continuar conectado neste aparelho" checkbox that
  extends how long the session lasts.
- **Backend today**: `LoginRequest` is just `email`/`password` — no flag for
  this. The refresh-token cookie's lifetime (`RefreshTokenCookieService`,
  `MaxAgeDays`) is a fixed server config value regardless of any client
  input.
- **What's needed**: a boolean on `LoginRequest` and variable cookie
  lifetime logic in `RefreshTokenCookieService`, if real behavior is wanted.
- **Workaround shipped**: checkbox renders, fully decorative — matches the
  reference project's own equivalent checkbox, which is likewise rendered
  but never wired to its form state.
- **Severity**: Cosmetic — low priority; decorative parity with the
  reference project was already an accepted outcome, not treated as a gap
  to fill.

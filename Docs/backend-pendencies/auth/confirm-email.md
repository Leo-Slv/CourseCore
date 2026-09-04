# Backend Pendencies — Confirm Email Page

Spec: [`Docs/specs/auth/confirm-email.md`](../../specs/auth/confirm-email.md)

## 1. Verification email has no clickable link

- **Mockup expects**: "Enviamos um link para {email}" with "Reenviar link" /
  "Trocar e-mail" as the only actions — assumes the email contains a
  clickable confirmation URL.
- **Backend today**: `RegisterUseCase`/`ResendEmailConfirmationUseCase`'s
  `BuildVerificationEmailHtml` renders the raw ~43-character base64url token
  as plain text (`<strong>{token}</strong>`), with no `<a href>` wrapping it.
  There's also no configured frontend base URL in the backend for it to
  build a link with even if it wanted to.
- **What's needed**: a frontend base URL in backend config, and a template
  change to wrap the token in a real `<a href="{baseUrl}/confirm-email?token=...">`
  link.
- **Workaround shipped**: the page shows a "Código de confirmação" field
  instead of relying on a link ("link" copy swapped for "código"
  throughout), and separately reads a `?token=` query param on load
  (pre-fills + auto-submits) — so a real emailed link would work
  transparently the moment the backend template is fixed, with **no
  frontend change required** at that point.
- **Severity**: Feature gap, non-blocking — confirmation fully works today
  via copy/paste, it's just a worse experience than a clickable link.

## 2. No self-service email-change endpoint

- **Mockup expects**: "Trocar e-mail" actually lets the user change their
  registered address.
- **Backend today**: `PUT /api/users/{userId}` requires the `ManageUsers`
  policy (admin-only). A self-registered user has no roles at all (backend
  spec rule 1), so they cannot call this for themselves under any
  circumstance.
- **What's needed**: a self-service "change my own email" endpoint —
  implies its own re-verification cycle (new address needs its own
  confirmation token before it's trusted).
- **Workaround shipped**: "Trocar e-mail" is a stub link
  (`appRoutes.auth.changeEmail`), no page behind it.
- **Severity**: Feature gap.

## 3. No "am I confirmed?" read endpoint

- **Mockup expects (implicitly)**: nothing in the mockup itself, but a
  correct implementation would want the status card to reflect real-time
  confirmation state rather than a static one.
- **Backend today**: same missing `/auth/me`-equivalent gap already tracked
  centrally in `CLAUDE.md`'s "Auth" section — no endpoint reports the
  current user's `EmailVerifiedAt` (or any other current-user state).
- **What's needed**: a `GET /api/auth/me` (or equivalent) endpoint.
- **Workaround shipped**: the status card ("Conta criada" / "E-mail
  confirmado" / "Acesso às aulas") is static. The page only learns
  confirmation happened indirectly — its own confirm call succeeding, or a
  `409` from resend meaning "already confirmed."
- **Severity**: Feature gap — same root cause referenced by every later
  screen's client-only auth gating; revisit centrally, not per-screen, if
  `/auth/me` ever gets added.

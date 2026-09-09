# Backend Pendencies — Register Page

Spec: [`Docs/specs/auth/register.md`](../../specs/auth/register.md)

## 1. Turnstile public site key not provided

- **Mockup expects**: a working Cloudflare Turnstile CAPTCHA widget on the
  registration form, required to submit.
- **Backend today**: `POST /api/auth/register` verifies `captchaToken`
  server-side via `TurnstileCaptchaVerificationService.VerifyAsync`, which
  already has a documented graceful bypass — verification is skipped
  entirely outside Production when `Turnstile:SecretKey` is unconfigured.
  Not a code gap; CourseCore's own spec (§11.1) already flags that neither
  the Turnstile secret key nor a Resend API key have been provided yet.
- **What's needed**: two credentials, neither of which is a code change —
  a Turnstile **public site key** (for this frontend to render the widget)
  and the backend's **secret key** (for `Turnstile:SecretKey` config, to
  make verification real in Production).
- **Workaround shipped**: `NEXT_PUBLIC_TURNSTILE_SITE_KEY` env var, empty by
  default in `.env.example`. When unset, the widget doesn't render and the
  form submits an empty `captchaToken`, which the backend's dev-mode bypass
  accepts. No code branching needed once a real key is supplied — the widget
  just starts rendering.
- **Severity**: Config — this is a credentials request to whoever owns the
  Cloudflare/Resend accounts, not backend engineering work.
- **Partially resolved (2026-09-09)**: a real **Resend API key** was
  supplied and configured locally in the developer's own gitignored `.env`
  (`Resend__ApiKey`/`Resend__FromName`; `Resend__FromAddress` still needs a
  real verified sender address) — no code change, `ResendEmailSender`
  already sends for real once `Resend:ApiKey` is non-blank. The secret was
  never written to any tracked file or committed. **Turnstile keys (public
  site key + secret key) are still outstanding** — that half of this
  pendency remains open.

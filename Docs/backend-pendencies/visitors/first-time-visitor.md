# Primeira vez — cadastro de visitante (`1a` / `1b`)

**Mockup:** `Docs/design/mockups/Primeira Vez.html` no repo do frontend (`plataforma-vdg`)
**Backend spec:** [Docs/specs/visitors/first-time-visitor-registration.md](../../specs/visitors/first-time-visitor-registration.md)
**Status:** Backend pronto (2026-10-01) — nenhuma pendência de backend. Falta só a tela no frontend.

Não é login nem cadastro de conta: o endpoint só grava os dados de contato
para a equipe de recepção. Não cria usuário, sessão, cookie nem envia e-mail.

## Contrato

### `POST /api/visitors` — público, rate-limited por IP

```json
{
  "name": "Ana Lima",
  "phone": "(11) 98765-4321",
  "email": "ana@example.com",
  "address": "Rua A, 10, Centro, São Paulo",
  "captchaToken": "<token Turnstile>"
}
```

- `address` é opcional (omitido, `null` ou vazio → gravado como `null`).
- `phone` pode ir com a máscara do design ou só dígitos; precisa ter 10 ou 11
  dígitos (sem `+55`). É gravado só com dígitos.
- `name`: mínimo 2 caracteres úteis, máximo 200.
- Envio repetido com os mesmos dados é aceito e gera outro registro.

`201 Created`:

```json
{ "id": "…", "submittedAt": "2026-10-01T17:40:00Z" }
```

A resposta **não devolve** nome/telefone — a tela "Obrigado, {primeiro nome}…
pelo telefone {telefone}" deve usar o que o próprio formulário tem em estado.

Erros: `400` (`ApiErrorResponse`) para qualquer campo inválido ou CAPTCHA
inválido; `429` para excesso de envios do mesmo IP. O design usa uma mensagem
única ("Preencha nome, telefone e um e-mail válido."), então o front não precisa
mapear erro por campo.

### `GET /api/visitors?page=&pageSize=&search=` — `visitors.read` ou Admin

`PagedResponse<VisitorResponse>` (`id`, `name`, `phone`, `email`, `address`,
`submittedAt`), mais recentes primeiro. `search` casa nome, e-mail ou dígitos
do telefone. Ainda não há tela no design para isso.

## Notas para o frontend

1. **CAPTCHA:** o design não mostra widget, mas o backend exige `captchaToken`
   em produção. Usar Turnstile em modo *managed*/*invisible* para não mudar o
   visual. Fora de produção, sem `Turnstile:SecretKey`, a verificação é pulada.
2. **Totem da recepção:** "Cadastrar outra pessoa" deve gerar um novo token
   Turnstile a cada envio. O limite padrão é 10 envios/min por IP
   (`RateLimiting:VisitorRegistration`), ajustável por configuração.
3. Desabilitar o botão "Enviar" durante o request — clique duplo gera dois
   registros (duplicidade é permitida por regra).

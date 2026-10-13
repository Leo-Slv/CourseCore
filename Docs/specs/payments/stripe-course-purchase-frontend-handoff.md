# Handoff para o frontend — acesso por curso e compra via Stripe

Mudanças de contrato desta entrega (ver `stripe-course-purchase.md`).

## Breaking changes

| Antes | Agora |
| --- | --- |
| `POST /api/access/user-area`, `POST /api/access/role-area`, `GET /api/access/user-area/{id}`, `GET /api/access/role-area/{id}`, `DELETE /api/access/user-area/{userId}/{areaId}` | **Removidos** (404). Para dar acesso use `POST /api/access/requests/grant` (já existia). |
| — | `GET /api/access/user-course/{userId}` → `[{ userId, courseId, source, grantedAt }]` (`source`: `Manual` \| `Purchase` \| `Migrated`) |
| — | `DELETE /api/access/user-course/{userId}/{courseId}` → 204 / 404 |
| `UserResponse.areaNames` | `UserResponse.courseTitles` (títulos dos cursos com acesso concedido) |
| `CourseAccessResponse.reason` = `"No area access found."` | `"No course access found."` |

Comportamento: conceder acesso a um curso libera **somente** aquele curso. Admin e quem tem a permissão `courses.manage` enxergam todos os cursos publicados (com Area ativa).

## Fluxo de compra

1. Curso `Paid` com `priceAmount` (reais) e `hasAccess=false` → botão "Comprar".
2. `POST /api/courses/{courseId}/checkout` (Bearer, sem body) → `{ orderId, checkoutUrl }`; `window.location = checkoutUrl`.
   Erros: 403 (e-mail não confirmado/usuário inativo), 404 (curso inexistente/não publicado), 409 (já tem acesso, curso gratuito, sem preço ou sem Area ativa), 502 (Stripe indisponível — tentar de novo).
3. Stripe redireciona para `{Frontend:BaseUrl}/checkout/success?orderId=…` ou `/checkout/cancel?orderId=…` (o frontend precisa dessas duas rotas).
4. Na página de sucesso, faça polling em `GET /api/orders/me` (a cada ~2 s, até ~30 s) até o pedido do `orderId` ficar `Paid`; o acesso é liberado pelo webhook, **não** pelo redirect. Depois recarregue o curso (`hasAccess=true`).
   `OrderResponse`: `{ id, courseId, amountInCents, currency, status, paidAt, createdAt }`, `status` ∈ `Pending | Paid | Expired | Failed`.

Cartão de teste: `4242 4242 4242 4242`, qualquer data futura e CVC.

## Cliente no ambiente de produção

Veja `Docs/deployment-cloudrun.md`: o frontend deve chamar `/api/...` na própria origem da Vercel (rewrite para a API) para o cookie de refresh funcionar.

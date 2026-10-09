# Spec — Compra de curso pago via Stripe (modo teste) e acesso por curso

Status: **APROVADA e IMPLEMENTADA** (aprovada pelo dono em 2026-10-09; decisões em §11).

## 1. Objetivo

1. Permitir que um usuário autenticado e com e-mail confirmado compre um curso `Paid` por pagamento único, via Stripe Checkout hospedado, e receba acesso automaticamente após a confirmação do pagamento. Nesta entrega só roda com chaves de **teste**.
2. Trocar o modelo de acesso **por Area** (`UserAreaAccess` / `RoleAreaAccess`) por acesso **por curso** (`UserCourseAccess`), para que comprar/receber um curso libere apenas aquele curso.

## 2. Contexto

### 2.1 Antes

- Acesso a curso `Paid` vinha de `UserAreaAccess` (grant por usuário × Area) ou `RoleAreaAccess` (role × Area). Conceder um curso liberava todos os cursos pagos das Areas ativas dele.
- `Course.PricingModel` (`Free`/`Paid`/`EnrollmentControlled`) e `Course.PriceAmount` (decimal, reais) já existiam; não havia pedido/pagamento/Stripe.

### 2.2 Depois

- `UserCourseAccess` (usuário × curso, com `Source`: `Manual` | `Purchase` | `Migrated`) é o único grant individual.
- `UserAreaAccess`, `RoleAreaAccess`, seus use cases, endpoints (`/api/access/user-area`, `/api/access/role-area`) e políticas (`ManageUserAreaAccess`, `ManageRoleAreaAccess`) foram **removidos**.
- Area continua existindo como categoria/vitrine: `IsPublic` (cursos `Free` em Area pública abrem sem grant) e `Active` (Area inativa continua bloqueando o acesso aos seus cursos).

## 3. Comportamento

### 3.1 Regra de acesso (`CourseAccessService`)

Ordem: usuário existe/ativo/e-mail confirmado → curso existe/publicado → curso tem Area vinculada e ao menos uma ativa → `Free` em Area pública libera → existe `UserCourseAccess` → usuário é **staff** libera → nega (`NoCourseAccessFound`).

**Staff** = role `Admin` ou permissão `courses.manage` (roles ativas). Substitui o antigo `RoleAreaAccess` e dá a admins/instrutores acesso a todos os cursos, inclusive de Areas criadas depois. Nenhuma outra role concede acesso a conteúdo.

### 3.2 Grants manuais (inalterados no contrato)

`POST /api/access/requests/grant`, aprovação de solicitação e rejeição seguem iguais, mas agora criam `UserCourseAccess(Source=Manual)` só do curso pedido.
Novos: `GET /api/access/user-course/{userId}` (lista) e `DELETE /api/access/user-course/{userId}/{courseId}` (revoga; 404 se não existir), ambos com a policy `ManageCourseAccess` (permissão `users.manage` ou Admin). Revogação registra audit log `UserCourseAccessRevoked`.

### 3.3 Usuários

`UserResponse.areaNames` foi substituído por `courseTitles` (títulos dos cursos com grant).

### 3.4 Compra

`POST /api/courses/{courseId}/checkout` (autenticado) cria um `Order` `Pending` (valor = `Course.PriceAmount × 100`, moeda `brl`) e uma Checkout Session (`mode=payment`, `client_reference_id`=orderId, `metadata` orderId/userId/courseId). Retorna `{ orderId, checkoutUrl }`. URLs de retorno: `{Frontend:BaseUrl}/checkout/success?orderId=…` e `/checkout/cancel?orderId=…`.

### 3.5 Webhook

`POST /api/payments/stripe/webhook` (anônimo; assinatura `Stripe-Signature` validada contra o corpo bruto com `Stripe:WebhookSecret`):

- `checkout.session.completed` / `checkout.session.async_payment_succeeded` com `payment_status=paid`: em uma transação marca o pedido `Paid`, cria `UserCourseAccess(Source=Purchase)` (idempotente) e grava audit `OrderPaid`.
- `checkout.session.expired`: pedido `Pending` → `Expired`.
- Demais eventos: ignorados com 200.

### 3.6 Consulta

`GET /api/orders/me` lista os pedidos do usuário (`id, courseId, amountInCents, currency, status, paidAt, createdAt`). O frontend consulta o pedido na página de sucesso (o webhook pode chegar depois do redirect).

## 4. Regras de negócio

1. Só cursos `Paid`, publicados e com `PriceAmount > 0` podem ser comprados.
2. Usuário ativo e com e-mail confirmado (senão 403).
3. Não inicia compra se já tem acesso (409) ou se o curso não tem Area ativa vinculada (409).
4. O valor cobrado vem sempre do banco, nunca do cliente.
5. Acesso só é concedido pelo webhook assinado; o redirect de sucesso não concede nada.
6. Evento reprocessado não duplica grant nem altera pedido já `Paid`.
7. Assinatura inválida/ausente → 400, sem efeito.
8. O evento precisa bater com o pedido (session id, `amount_total`, moeda); divergência é registrada em log e ignorada (sem grant).
9. Pagamento recebido para pedido `Expired`/`Failed` não concede acesso (log de erro para revisão manual).
10. Curso despublicado/alterado após o checkout aberto: pedido pago **concede** o acesso (decisão 4).
11. Falha do Stripe ao criar a sessão: pedido fica `Failed` e a API responde 502.
12. Reembolso/revogação é manual pelo admin nesta entrega.
13. Em `Production` a API recusa subir com `Stripe:SecretKey` que não seja `sk_test_`/`rk_test_` ou `Stripe:WebhookSecret` que não comece com `whsec_`.

## 5. Migrations

- `ReplaceAreaAccessWithUserCourseAccess`: cria `user_course_accesses` e **migra** cada `user_area_accesses` ainda válido (`CanView`/`CanManage`, dentro de `StartsAt`/`ExpiresAt`) para um `UserCourseAccess(Source=Migrated)` por curso **não `Free`** vinculado à Area; depois remove `user_area_accesses` e `role_area_accesses`. `role_area_accesses` não é migrada (substituída pela regra de staff). Down recria as tabelas vazias.
- `AddOrders`: cria `orders`.

## 6. Critérios de aceite (cobertos por testes)

- [x] Acesso por curso: grant libera só o curso concedido; grant de outro curso da mesma Area não libera; catálogo reflete isso.
- [x] Staff (Admin / `courses.manage`) acessa qualquer curso; role Admin inativa não.
- [x] Endpoints `user-area`/`role-area` não existem mais; `user-course` lista/revoga com autorização correta.
- [x] Checkout: valor do banco, URLs de retorno, 404/409/403 conforme §4, 502 + pedido `Failed` se o Stripe falhar.
- [x] Webhook: grant na confirmação, idempotência, não pago, divergência de valor/moeda/sessão, pedido desconhecido, expirado, assinatura inválida/ausente, fluxo ponta a ponta com assinatura HMAC real.
- [x] Validação de configuração de produção para Stripe.
- [x] `dotnet build` e `dotnet test` (877 testes) passam.

## 7. Fora de escopo

Assinaturas, cupons, parcelamento, Pix/boleto, impostos/nota fiscal, reembolso automático (`charge.refunded`), modo live, múltiplas moedas, carrinho, reaproveitar/cancelar sessões pendentes, UI do frontend.

## 8. Decisões

1. Compra única por curso (sem assinatura).
2. BRL; grant permanente; reembolso/revogação manual.
3. Stripe Checkout hospedado + webhook (sem coletar cartão na API).
4. Pedido pago concede acesso mesmo que o curso tenha sido despublicado/alterado depois.
5. Acesso por curso (`UserCourseAccess`) em vez de por Area; acesso por Area removido por completo (user **e** role), com migração dos grants de usuário existentes.
6. SDK oficial `Stripe.net` atrás de `IStripeGateway` / `IStripeWebhookVerifier`.
7. Staff = role `Admin` ou permissão `courses.manage`.
8. Preço reutiliza `Course.PriceAmount` (reais, `decimal(10,2)`); nenhum campo novo no curso.

## 9. Testes locais sem chaves reais

- **Stripe CLI (recomendado com chaves de teste):** `stripe listen --forward-to http://localhost:8080/api/payments/stripe/webhook` imprime o `whsec_...` a usar em `Stripe__WebhookSecret`; depois `stripe trigger checkout.session.completed` ou conclua o checkout com o cartão `4242 4242 4242 4242`.
- **Sem conta Stripe:** suba `stripe/stripe-mock` (`docker run -p 12111:12111 stripe/stripe-mock`) e configure `Stripe__ApiBase=http://localhost:12111` com qualquer `Stripe__SecretKey`/`Stripe__WebhookSecret`. O checkout é criado contra o mock; o webhook é enviado à mão assinando o corpo com HMAC-SHA256 (`t=<unix>,v1=hex(hmac(secret, "<t>.<corpo>"))`). `Stripe:ApiBase` é rejeitado em Production.
- **Atalho:** `node scripts/simulate-stripe-webhook.mjs [orderId] [--status=paid|unpaid]` assina e envia o webhook por você (usa o pedido pendente mais recente se não passar o id; lê o banco por um container `postgres` descartável e precisa de `ConnectionStrings__CourseCoreDatabase` e `Stripe__WebhookSecret` no ambiente).

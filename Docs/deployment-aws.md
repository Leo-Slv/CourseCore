# Deploy em producao - Vercel + AWS EC2 + Neon

Topologia de baixo custo:

```text
Browser -> Vercel (frontend) --rewrite /api/*--> https://api.<ip>.sslip.io (Caddy, EC2) -> API (.NET, Docker) -> Neon (PostgreSQL)
                                                                                         \-> S3 (midia) / Resend (e-mail) / Turnstile
```

Custo estimado da AWS: uma `t3.micro` + IPv4 publico (Elastic IP) + S3. Contas abertas depois de julho de 2025 nao tem mais a `t3.micro` gratis por 12 meses: ganham cerca de US$ 200 em creditos, validos por ate 6 meses, e a EC2 consome esse credito (confira no console da sua conta). Depois disso, ordem de grandeza de US$ 10-15/mes (verifique a calculadora da AWS). Crie um **alerta de orcamento** (Billing → Budgets) antes de lancar a instancia.

## 1. Neon (PostgreSQL)

1. Crie o projeto e o banco no Neon (regiao proxima da EC2, ex.: `us-east-1`).
2. Anote duas connection strings: **pooled** (host com `-pooler`) para a API e **direct** (sem `-pooler`) para migrations.
3. Formato Npgsql:

```text
Host=ep-xxx.us-east-1.aws.neon.tech;Port=5432;Database=neondb;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;Timeout=30;Command Timeout=60
```

O compute do Neon free hiberna por inatividade; a primeira requisicao apos hibernar pode demorar alguns segundos (por isso `Timeout=30`). Nao use `/health/ready` como probe frequente - ele acorda o banco.

## 2. Migrations (da sua maquina, contra o Neon)

A API nao aplica migrations no startup. Gere o script revisado e aplique com a connection **direct**:

```powershell
./scripts/generate-migration-script.ps1
# revise artifacts/migrations/*.sql e aplique:
psql "postgresql://USER:PASSWORD@ep-xxx.us-east-1.aws.neon.tech/neondb?sslmode=require" -f artifacts/migrations/<arquivo>.sql
```

Alternativa (target `migrations` do Dockerfile):

```bash
docker build --target migrations -t coursecore-migrations .
docker run --rm -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__CourseCoreDatabase=<direct connection string>" \
  -e Jwt__SecretKey=<qualquer valor com 32+ chars> -e Media__Playback__SigningSecret=<outro valor 32+ chars> \
  coursecore-migrations
```

Siga `Docs/deployment-migrations.md` (snapshot/branch do Neon antes de aplicar). Nao rode seed em producao; crie o primeiro admin por procedimento controlado.

## 3. Servicos externos

- **Resend**: verifique o dominio remetente (ou use o dominio de teste `onboarding@resend.dev`, que so envia para o e-mail da sua conta). Limite free: 100 e-mails/dia. Preencha `Resend__ApiKey` e `Resend__FromAddress`.
- **Cloudflare Turnstile** (obrigatorio pelo validador de producao): crie um widget e use o secret em `Turnstile__SecretKey`. Adicione o dominio `*.vercel.app` do frontend ao widget.
- **S3** (opcional, so se for usar upload/download de midia): bucket privado, CORS permitindo `PUT`/`GET` a partir da origem do frontend, e uma IAM Role na EC2 com `s3:GetObject`/`s3:PutObject` no bucket. Deixe `AccessKeyId`/`SecretAccessKey` vazios para usar a role. Depois adicione `Media__Playback__AllowedStorageProviders__1=S3`.
- **Stripe (modo teste)**: em dashboard.stripe.com (Test mode) copie a Secret key (`sk_test_...`) para `Stripe__SecretKey`. Cadastre um endpoint de webhook em `https://api.<IP-com-hifens>.sslip.io/api/payments/stripe/webhook` com os eventos `checkout.session.completed`, `checkout.session.async_payment_succeeded` e `checkout.session.expired`, e copie o Signing secret (`whsec_...`) para `Stripe__WebhookSecret`. Em producao a API recusa subir com chave `sk_live_`. Cartao de teste: `4242 4242 4242 4242`. Localmente use `stripe listen --forward-to localhost:8080/api/payments/stripe/webhook`. O webhook chega direto na API (nao passa pelo rewrite da Vercel). Detalhes em `Docs/specs/payments/stripe-course-purchase.md`.

## 4. EC2

1. Lance uma instancia `t3.micro` (Ubuntu 24.04 LTS), disco gp3 de 20 GB.
2. Security group: entrada 80 e 443 de `0.0.0.0/0`; 22 **somente do seu IP**. Nao abra 8080.
3. Aloque um Elastic IP e associe a instancia. O host da API sera `api.<IP-com-hifens>.sslip.io` (ex.: `api.3-91-10-20.sslip.io`), que resolve para o IP e permite certificado Let's Encrypt automatico via Caddy, sem dominio proprio.
4. (Opcional) IAM Role com acesso ao bucket S3 anexada a instancia.
5. Na instancia:

```bash
sudo apt-get update && sudo apt-get install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER && newgrp docker
# swap de 1 GB (a t3.micro tem 1 GB de RAM)
sudo fallocate -l 1G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

6. Copie `deploy/aws/` para `~/coursecore/` (scp ou git clone), crie o `.env`:

```bash
cd ~/coursecore && cp .env.example .env && chmod 600 .env && nano .env
```

## 5. Imagem da API

A imagem e publicada pelo workflow `.github/workflows/cd.yml` (veja `Docs/ci-cd.md`): a cada merge na master com CI verde (`sha-<7>` e `latest`) e em tags `v*`. Se o pacote ficar privado, na EC2 faca `docker login ghcr.io` com um PAT `read:packages`; ou torne o pacote publico (a imagem nao contem segredos).

```bash
git tag v0.1.0 && git push origin v0.1.0
```

## 6. Subir e validar

```bash
cd ~/coursecore
docker compose pull && docker compose up -d
docker compose logs -f coursecore-api   # falha de config de producao aparece aqui
curl https://api.<IP-com-hifens>.sslip.io/health/live
```

Atualizar versao: altere `COURSECORE_IMAGE` (ou use `latest`) e rode `docker compose pull && docker compose up -d`.

## 7. Vercel (frontend)

O frontend esta em `*.vercel.app` e a API em outro host, entao o cookie de refresh seria cross-site (e bloqueado por Safari/Chrome com cookies de terceiros). Solucao recomendada: **rewrite** no projeto do frontend, para o navegador falar sempre com a origem da Vercel:

```json
{
  "rewrites": [
    { "source": "/api/:path*", "destination": "https://api.<IP-com-hifens>.sslip.io/api/:path*" }
  ]
}
```

O frontend deve chamar `/api/...` (mesma origem) em producao: na Vercel defina `NEXT_PUBLIC_API_URL` = `/` (o front monta `/api/...` relativo). Mantenha `Auth__RefreshTokenCookie__SameSite=Lax`. Defina `Cors__AllowedOrigins__0` e `Frontend__BaseUrl` com a URL `https://<app>.vercel.app` (usada nos links dos e-mails).

Alternativa sem rewrite: chamar a API direto e usar `SameSite=None` (+ `Secure`), sujeito ao bloqueio de cookies de terceiros em alguns navegadores - nao recomendado.

**IP do cliente e rate limiting.** Ha dois proxies na frente da API (rewrite da Vercel e Caddy). Por padrao o Caddy descarta o `X-Forwarded-For` que a Vercel envia (testado: a API passa a ver so o IP da Vercel e **todos os usuarios dividem o mesmo limite de login**). Por isso o `Caddyfile` usa `trusted_proxies` (preserva o IP do cliente) e o `docker-compose.yml` define `ForwardedHeaders__ForwardLimit=2`. Contrapartida: quem chamar a EC2 direto consegue forjar o primeiro IP e escapar do limitador; o login continua protegido por Turnstile e senha.

## 8. Checklist final

- [ ] `/health/live` responde 200 por HTTPS; `/health/ready` retorna 404 externamente.
- [ ] Registro -> e-mail de confirmacao chega (Resend) -> link abre o frontend correto.
- [ ] Login mantem sessao apos refresh da pagina (cookie `coursecore_refresh_token`).
- [ ] `Seed__Admin__Enabled=false`; segredos so no `.env` da EC2.
- [ ] Snapshot/branch do Neon antes de cada migration.

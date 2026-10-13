# Deploy em producao - Vercel + Google Cloud Run + Neon

```text
Navegador ──► Vercel (front, plataforma-vdg.vercel.app)
                 │  /api/*  (rewrite: o navegador ve uma unica origem)
                 ▼
            Cloud Run (API)  ──►  Neon (PostgreSQL)
                 ▲
Stripe ──────────┘  webhook direto na URL do Cloud Run (nao passa pela Vercel)
```

Nada daqui foi executado numa conta Google real ainda: `setup.sh` passou so em `bash -n`, e o workflow em `actionlint`. A imagem foi construida e iniciada localmente em modo Production com as mesmas variaveis do `env.yaml` (responde `/health/live`, `/health/ready` e a API).

## 1. Neon (banco)

Veja o passo a passo de migrations e do primeiro Admin em `Docs/deployment-migrations.md` e `Docs/specs/access/role-permission-management.md`. Resumo: gerar o script idempotente, aplicar na connection **direct**, depois criar o Admin.

Guarde a connection **pooled** (host com `-pooler`) no formato Npgsql:

```text
Host=ep-xxx-pooler.REGIAO.aws.neon.tech;Port=5432;Database=neondb;Username=USUARIO;Password=SENHA;SSL Mode=Require;Trust Server Certificate=true
```

## 2. Google Cloud (uma vez)

1. Crie um projeto no console do Google Cloud e **ative o faturamento** (o Cloud Run exige uma conta de faturamento mesmo dentro da cota gratis). Crie um **alerta de orcamento** (ex.: US$ 1) em Faturamento → Orcamentos.
2. **Regiao:** escolha uma regiao da *Tier 1* (cota gratis maior), perto do Neon. Se o Neon esta em `us-east-1` (Virgínia), use `us-east1`. `southamerica-east1` (Sao Paulo) e Tier 2: menor cota gratis e custo maior.
3. No **Cloud Shell** (ou com `gcloud` logado), na raiz do repositorio:

```bash
PROJECT_ID=meu-projeto REGION=us-east1 GITHUB_REPO=Leo-Slv/CourseCore bash deploy/cloudrun/setup.sh
```

O script ativa as APIs, cria o repositorio de imagens (mantendo so as 5 mais recentes, para ficar na cota gratis), uma conta de servico para a API e outra para o GitHub, os *secrets* e a federacao de identidade (o GitHub entra no Google **sem chave em arquivo**). Gera sozinho `Jwt__SecretKey` e `Media__Playback__SigningSecret` (diferentes entre si). Pode rodar de novo sem problema.

4. Adicione os valores que o script nao conhece (ele imprime os comandos prontos):

| Secret (Secret Manager) | Variavel da API | Valor |
| --- | --- | --- |
| `coursecore-db` | `ConnectionStrings__CourseCoreDatabase` | connection **pooled** do Neon (formato acima) |
| `coursecore-turnstile` | `Turnstile__SecretKey` | secret key do Cloudflare Turnstile |
| `coursecore-resend` | `Resend__ApiKey` | API key do Resend |
| `coursecore-stripe-key` | `Stripe__SecretKey` | `sk_test_...` ou `rk_test_...` (modo teste; em producao a API recusa `sk_live_`) |
| `coursecore-stripe-webhook` | `Stripe__WebhookSecret` | `whsec_...` do endpoint (passo 5) |

5. Revise `deploy/cloudrun/env.yaml` (valores **nao secretos**): confirme a URL do front e troque `Resend__FromAddress` por um remetente verificado no Resend.

## 3. GitHub

Em *Settings → Secrets and variables → Actions → Variables*, crie as 4 variaveis que o `setup.sh` imprimiu: `GCP_PROJECT_ID`, `GCP_REGION`, `GCP_WORKLOAD_IDENTITY_PROVIDER`, `GCP_SERVICE_ACCOUNT`. **Nao ha nenhuma secret do Google no GitHub.**

O restante (environment `production` com aprovacao, `NEON_DIRECT_CONNECTION`, `APPLY_MIGRATIONS`, regra de branch) esta em `Docs/ci-cd.md`.

## 4. Primeiro deploy

*Actions → CourseCore CD → Run workflow.* Ele constroi a imagem, gera o SQL, pede aprovacao, cria o servico `coursecore-api` e testa `/health/ready`. No final imprime a **URL da API**, algo como `https://coursecore-api-<numero>.us-east1.run.app`.

Os proximos deploys criam uma revisao **sem trafego**, testam essa revisao por uma URL propria e so entao movem o trafego. Se o teste falhar, a revisao anterior continua no ar.

## 5. Ligar o front e o Stripe

1. **Vercel** (projeto plataforma-vdg → Settings → Environment Variables), para *Production* e *Preview*:
   - `NEXT_PUBLIC_API_URL` = `/` (o front chama `/api/...` na propria origem)
   - `NEXT_PUBLIC_TURNSTILE_SITE_KEY` = site key do Turnstile
   - (opcional) `NEXT_PUBLIC_SUPPORT_WHATSAPP` / `NEXT_PUBLIC_SUPPORT_EMAIL`
2. No repositorio do front, crie `vercel.json` com a URL da API do passo 4 e faca deploy:

```json
{
  "rewrites": [
    { "source": "/api/:path*", "destination": "https://SUA-URL-DO-CLOUD-RUN/api/:path*" }
  ]
}
```

3. **Stripe** (Developers → Webhooks → Add endpoint, modo teste): URL `https://SUA-URL-DO-CLOUD-RUN/api/payments/stripe/webhook`, eventos `checkout.session.completed`, `checkout.session.async_payment_succeeded` e `checkout.session.expired`. Copie o *Signing secret* (`whsec_...`) para o secret `coursecore-stripe-webhook` e **crie uma nova versao do servico** (rode o CD de novo ou `gcloud run services update coursecore-api --region REGIAO --update-secrets ...`), pois o valor `:latest` e lido quando a revisao inicia.
4. **Turnstile** (Cloudflare): adicione `plataforma-vdg.vercel.app` aos dominios permitidos do widget.

## 6. Validar

```bash
curl https://SUA-URL-DO-CLOUD-RUN/health/ready          # 200
```
No site: cadastro, e-mail de confirmacao, login, refresh (recarregar a pagina mantem a sessao), compra com cartao de teste `4242 4242 4242 4242`.

## Como o Cloud Run se comporta (e o que isso muda)

- **Escala a zero** (`--min-instances 0`): depois de um tempo parado, a primeira requisicao leva alguns segundos (cold start do .NET). `--cpu-boost` ameniza. Se isso incomodar, `--min-instances 1` elimina o atraso, mas passa a custar o ano todo (alguns dolares por mes).
- **Ate 2 instancias** (`--max-instances 2`): o limitador de tentativas de login e por instancia (memoria), entao o limite real pode ser ate 2x o configurado. Aceitavel neste porte.
- **IP real do cliente:** atras de Vercel + Cloud Run ha dois proxies. Com `ForwardedHeaders__ForwardLimit=2` o limitador enxerga o IP do cliente, nao o do proxy. Sem isso, **todos os usuarios dividiriam o mesmo limite de login** (comprovado em teste local: depois de ~20 tentativas somadas, tudo virava 429). Contrapartida: quem chamar a URL do Cloud Run direto pode forjar o primeiro `X-Forwarded-For` e escapar do limitador. O login continua protegido por Turnstile e por senha; se isso virar problema, restrinja o acesso direto ou ponha um dominio proprio na frente.
- **Segredos** sao lidos do Secret Manager quando a revisao inicia (`:latest`). Mudou um segredo? Faca novo deploy.
- **Custo esperado:** dentro da cota gratis do Cloud Run para o uso de uma igreja; o Secret Manager (7 segredos) e o Artifact Registry podem gerar centavos. Confira sempre o alerta de orcamento.

## Midia (upload de arquivos)

`env.yaml` libera so o provedor `YouTube` (aulas em embed). Upload de materiais/avatar usa S3 e hoje o codigo fixa uma regiao AWS; se for usar, e preciso um bucket (AWS S3 ou outro compativel, com pequeno ajuste de codigo) e as variaveis `Media__S3__*`. Fica fora do escopo deste deploy.

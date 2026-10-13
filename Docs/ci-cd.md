# CI/CD - CourseCore API e plataforma-vdg

Nada aqui foi executado no GitHub ainda: os workflows foram validados com `actionlint` e os comandos de CI rodados localmente. O primeiro push vai exercitar de verdade. A hospedagem da API e o Google Cloud estao em `Docs/deployment-cloudrun.md`.

## Visao geral

| | CI (todo push/PR) | CD (producao) |
| --- | --- | --- |
| **API** (`CourseCore`) | restore, build, testes, migrations faltando, pacotes vulneraveis, build do Docker, scan de secrets | imagem no Artifact Registry → migrations → Cloud Run (revisao sem trafego → smoke test → trafego), com aprovacao manual |
| **Front** (`plataforma-vdg`) | typecheck, lint, testes, build, audit (nao bloqueia), scan de secrets | Vercel (integracao Git nativa) |

## API

### CI - `.github/workflows/ci.yml`

- `build-and-test`: `dotnet build/test` em Release; **falha se um model mudou sem migration** (`dotnet ef migrations has-pending-model-changes`); falha se houver pacote NuGet vulneravel; guarda o `.trx` como artefato.
- `docker-build`: constroi o stage `final` sem publicar (pega Dockerfile quebrado no PR).
- `secret-scan`: gitleaks no historico completo.
- Pushes novos na mesma branch cancelam a execucao anterior.

### CD - `.github/workflows/cd.yml`

Dispara quando o **CI da master termina com sucesso** (`workflow_run`), em tag `v*` ou manualmente. Um CI vermelho nunca chega em producao; o commit publicado e exatamente o testado.

1. **image**: autentica no Google por Workload Identity Federation (sem chave) e publica `<regiao>-docker.pkg.dev/<projeto>/coursecore/api` com as tags `sha-<7>` e `latest`.
2. **migrations** (environment `production`, pede aprovacao): gera o script SQL **idempotente** e o anexa como artefato `migration-script`. So aplica no Neon se a variavel do repositorio `APPLY_MIGRATIONS` for `true`. Respeita `Docs/deployment-migrations.md`: nada de migration no startup.
3. **deploy** (environment `production`): `gcloud run deploy` cria uma **revisao sem trafego** (tag `candidate`) com a configuracao de `deploy/cloudrun/env.yaml` e os segredos do Secret Manager; testa `/health/ready` nessa revisao e **so entao move o trafego**. Se o teste falhar, o trafego nunca muda e a revisao anterior continua servindo (nao precisa de rollback). No primeiro deploy, que nao tem revisao anterior, testa a URL do servico.

Os jobs `migrations` e `deploy` compartilham a mesma aprovacao do environment: aprove uma vez, depois de tirar o **branch/snapshot do Neon**.

### Configuracao unica no GitHub

**Settings → Environments → New environment `production`**
- *Required reviewers*: voce.
- *Deployment branches*: `master`.
- Secret do environment (so se for aplicar migrations pelo workflow): `NEON_DIRECT_CONNECTION` = connection string **direct** (sem `-pooler`) do Neon.
- Variables (repositorio): `GCP_PROJECT_ID`, `GCP_REGION`, `GCP_WORKLOAD_IDENTITY_PROVIDER` e `GCP_SERVICE_ACCOUNT` (o `deploy/cloudrun/setup.sh` imprime os valores) e `APPLY_MIGRATIONS` (`true` para aplicar pelo workflow; ausente = so gera o script). **Nao existe nenhuma secret do Google no GitHub.**

**Settings → Branches → regra para `master`**: exigir pull request e os status checks `build-and-test`, `docker-build` e `secret-scan` (e, no front, `quality`).

**No Google Cloud** (uma vez): `deploy/cloudrun/setup.sh`, descrito em `Docs/deployment-cloudrun.md`.

### Como fazer uma release

- Fluxo normal: merge na `master` → CI → CD pede aprovacao → aprovar.
- Reimplantar/voltar para uma versao: *Actions → CourseCore CD → Run workflow* (usa o commit atual) ou publique uma tag `v1.2.3`.
- Rollback manual: no console do Cloud Run (Revisoes) mande 100% do trafego para a revisao anterior, ou `gcloud run services update-traffic coursecore-api --region <regiao> --to-revisions <revisao>=100`.

## Front

### CI - `.github/workflows/ci.yml`

`npm ci` → `typecheck` → `lint` → `test` → `build` (com `NEXT_PUBLIC_API_URL` ficticio; os valores reais ficam na Vercel). O `npm audit` roda a parte e **nao bloqueia**: hoje acusa vulnerabilidades altas em dependencias do CLI `shadcn` (que esta em `dependencies`, deveria estar em `devDependencies`). Torne bloqueante depois de limpar isso.

### CD - Vercel

O deploy do front e feito pela **integracao Git da Vercel**: PR gera preview, merge na `master` gera producao. Nao ha workflow de deploy no GitHub porque duplicaria isso e exigiria `VERCEL_TOKEN`. Para a producao so sair com CI verde, use a **regra de branch** acima (so da merge com os checks passando).

Variaveis na Vercel (Project → Settings → Environment Variables): `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_TURNSTILE_SITE_KEY`, `NEXT_PUBLIC_SUPPORT_EMAIL`/`NEXT_PUBLIC_SUPPORT_WHATSAPP` (ver `.env.example`).

## Dependabot

Os dois repositorios tem `.github/dependabot.yml` (NuGet/Docker/Actions no back, npm/Actions no front), com PRs semanais ou mensais que passam pelo mesmo CI.

## Limites conhecidos

- O gitleaks usa a acao oficial, gratuita em contas pessoais; em organizacao exige licenca.
- Nao ha ambiente de homologacao: o `production` e o unico destino.
- A aplicacao de migrations pelo workflow nao tira o snapshot do Neon sozinha; isso e manual, antes de aprovar.
- O Cloud Run escala a zero: a primeira requisicao depois de um periodo parado e mais lenta (cold start).

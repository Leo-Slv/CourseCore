# CI/CD - CourseCore API e plataforma-vdg

Nada aqui foi executado no GitHub ainda: os workflows foram validados com `actionlint` e os comandos de CI rodados localmente. O primeiro push vai exercitar de verdade.

## Visao geral

| | CI (todo push/PR) | CD (producao) |
| --- | --- | --- |
| **API** (`CourseCore`) | restore, build, testes, migrations faltando, pacotes vulneraveis, build do Docker, scan de secrets | imagem no GHCR → migrations → deploy na EC2 → smoke test, com aprovacao manual e rollback |
| **Front** (`plataforma-vdg`) | typecheck, lint, testes, build, audit (nao bloqueia), scan de secrets | Vercel (integracao Git nativa) |

## API

### CI - `.github/workflows/ci.yml`

- `build-and-test`: `dotnet build/test` em Release; **falha se um model mudou sem migration** (`dotnet ef migrations has-pending-model-changes`); falha se houver pacote NuGet vulneravel; guarda o `.trx` como artefato.
- `docker-build`: constroi o stage `final` sem publicar (pega Dockerfile quebrado no PR).
- `secret-scan`: gitleaks no historico completo.
- Pushes novos na mesma branch cancelam a execucao anterior.

### CD - `.github/workflows/cd.yml`

Dispara quando o **CI da master termina com sucesso** (`workflow_run`), em tag `v*` ou manualmente. Um CI vermelho nunca chega em producao; o commit publicado e exatamente o testado.

1. **image**: build e push para `ghcr.io/<dono>/coursecore` com as tags `sha-<7>`, `latest` (e a tag `v*`).
2. **migrations** (environment `production`, pede aprovacao): gera o script SQL **idempotente** e o anexa como artefato `migration-script`. So aplica no Neon se a variavel do repositorio `APPLY_MIGRATIONS` for `true`. Respeita `Docs/deployment-migrations.md`: nada de migration no startup.
3. **deploy** (environment `production`): por SSH roda `deploy/aws/deploy.sh` na EC2: `pull`, `up -d`, espera o healthcheck do container e, **se nao ficar saudavel, volta para a imagem anterior** e falha o job. Depois faz um smoke test em `https://<API_HOST>/health/live` (o Caddy esconde `/health/ready` e `/health`, que mostram o estado do banco).

Os jobs `migrations` e `deploy` compartilham a mesma aprovacao do environment: aprove uma vez, depois de tirar o **branch/snapshot do Neon**.

### Configuracao unica no GitHub

**Settings → Environments → New environment `production`**
- *Required reviewers*: voce.
- *Deployment branches*: `master`.
- Secrets do environment:

| Secret | Valor |
| --- | --- |
| `SSH_HOST` | IP ou DNS da EC2 |
| `SSH_USER` | usuario de deploy (ex.: `ubuntu`) |
| `SSH_PRIVATE_KEY` | chave privada **so de deploy** (gere uma nova: `ssh-keygen -t ed25519`, publique a `.pub` no `~/.ssh/authorized_keys` da EC2) |
| `SSH_KNOWN_HOSTS` | saida de `ssh-keyscan -t ed25519 <host>` (conferida por voce; evita man-in-the-middle) |
| `NEON_DIRECT_CONNECTION` | connection string **direct** (sem `-pooler`) do Neon; so se for aplicar migrations pelo workflow |

- Variables do environment/repositorio: `API_HOST` (ex.: `api.1-2-3-4.sslip.io`, usado no smoke test) e `APPLY_MIGRATIONS` (`true` para aplicar pelo workflow; ausente = so gera o script).

**Settings → Branches → regra para `master`**: exigir pull request e os status checks `build-and-test`, `docker-build` e `secret-scan` (e, no front, `quality`).

**Na EC2** (uma vez, alem do que ja esta em `Docs/deployment-aws.md`):
- a pasta `/opt/coursecore` com `docker-compose.yml`, `Caddyfile` e `.env` (de `deploy/aws/`);
- o usuario de deploy no grupo `docker`;
- pacote GHCR **publico**, ou `docker login ghcr.io` feito uma vez na EC2 com um PAT `read:packages`.

### Como fazer uma release

- Fluxo normal: merge na `master` → CI → CD pede aprovacao → aprovar.
- Reimplantar/voltar para uma versao: *Actions → CourseCore CD → Run workflow* (usa o commit atual) ou publique uma tag `v1.2.3`.
- Rollback manual: na EC2, `deploy/aws/deploy.sh ghcr.io/<dono>/coursecore:sha-<antigo>`.

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
- O deploy usa uma unica EC2, entao ha uma janela curta de reinicio a cada release.

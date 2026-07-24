# Estrategia de migrations e deploy - CourseCore API

## Objetivo

Este documento define como as migrations EF Core da CourseCore API devem ser criadas, revisadas e aplicadas em ambientes local, homologacao e producao.

A aplicacao nao aplica migrations automaticamente no startup. Essa decisao reduz risco operacional, evita alteracoes de schema sem revisao e mantem o deploy da API separado do deploy do banco.

## Principios

- Nao aplicar migrations automaticamente no startup.
- Nao aplicar migrations sem revisao.
- Nao versionar secrets, senhas, tokens, JWT secret ou connection string real.
- Nao rodar seed em producao.
- Gerar script SQL idempotente para homologacao e producao.
- Revisar o script SQL antes de aplicar.
- Fazer backup ou snapshot antes de aplicar em producao.
- Aplicar com usuario e permissao adequados para DDL.
- Validar health checks apos a aplicacao.
- Separar criacao da migration, geracao do script, aprovacao e execucao.

## Estado Atual

O projeto usa um unico `CourseCoreDbContext` com EF Core e PostgreSQL. As migrations atuais ficam em:

```text
Shared/Infrastructure/Persistence/Migrations/
```

Migrations existentes:

```text
20260710184030_InitialCreate
20260722193622_AddRefreshTokens
```

Nao ha `IDesignTimeDbContextFactory` no estado atual. O EF Core usa a configuracao padrao do projeto de startup para criar o `CourseCoreDbContext` em design time.

## Fluxo Local

Use o fluxo local para desenvolvimento e validacao em banco descartavel ou controlado.

1. Altere os `PersistenceModels` e configuracoes EF quando houver mudanca real de schema.
2. Crie a migration:

```bash
dotnet ef migrations add NomeDaMigration --context CourseCoreDbContext --output-dir Shared/Infrastructure/Persistence/Migrations
```

3. Revise os arquivos gerados em `Shared/Infrastructure/Persistence/Migrations/`.
4. Aplique localmente somente quando apropriado:

```bash
dotnet ef database update --context CourseCoreDbContext
```

5. Rode a aplicacao e valide:

```bash
dotnet restore
dotnet build
dotnet test
```

6. Se o seed admin for necessario localmente, habilite-o apenas por variaveis locais e depois de aplicar o schema.

## Geracao de Script SQL Idempotente

Para homologacao e producao, gere um script SQL idempotente e revise antes de aplicar.

PowerShell:

```powershell
./scripts/generate-migration-script.ps1
```

Bash:

```bash
./scripts/generate-migration-script.sh
```

Os scripts geram por padrao:

```text
artifacts/migrations/coursecore-migration.sql
```

`artifacts/` ja e ignorado pelo Git. Nao commite SQL gerado nesta etapa sem uma decisao explicita de revisao e versionamento.

## Fluxo de Homologacao

1. Gere o script SQL idempotente.
2. Revise o script procurando operacoes destrutivas, locks longos, alteracoes de tipo e drops.
3. Aplique no banco de homologacao por ferramenta controlada, usando credenciais fora do repositorio.
4. Valide readiness:

```text
/health/ready
```

5. Valide login e endpoints principais.
6. Analise logs e correlation ids para falhas.
7. Registre a migration aplicada e o horario de aplicacao.

## Fluxo de Producao

1. Gere o mesmo tipo de script SQL idempotente a partir do commit aprovado.
2. Revise o script com atencao antes da janela de deploy.
3. Faca backup ou snapshot do banco.
4. Planeje janela de manutencao se a migration puder bloquear tabelas ou alterar muitos dados.
5. Aplique o script manualmente ou por pipeline controlado com aprovacao manual.
6. Suba ou recicle a API somente depois que o schema esperado estiver disponivel.
7. Valide:

```text
/health/live
/health/ready
/health
```

8. Valide login, refresh token e endpoints administrativos principais.
9. Monitore logs, status HTTP 5xx e erros de banco.

## Rollback

EF Core migrations nem sempre possuem rollback seguro para dados.

Nao assuma que uma `Down migration` e segura em producao. Operacoes como drop de coluna, alteracao de tipo, renomeacao e transformacao de dados exigem plano manual.

Preferencias para rollback:

- Restaurar backup ou snapshot quando houver perda ou corrupcao de dados.
- Preparar script manual de compensacao quando rollback automatico nao for seguro.
- Usar deploy progressivo quando a mudanca exigir compatibilidade entre versoes da API e do banco.
- Separar migrations destrutivas em etapas: adicionar novo schema, migrar dados, validar, remover schema antigo em outro deploy.

## Docker

`docker-compose.yml` nao aplica migrations automaticamente. Ele sobe PostgreSQL e API, mas a API pode iniciar antes do banco ter o schema esperado.

Se o schema ainda nao foi aplicado:

- `/health/live` pode responder saudavel, pois valida o processo.
- `/health/ready` pode falhar, pois valida conectividade e preparo do banco.
- endpoints que usam banco podem falhar ate a aplicacao controlada das migrations.

Para ambiente local com Docker, aplique migrations manualmente a partir do host ou gere um script SQL idempotente e aplique no PostgreSQL do compose com uma ferramenta adequada.

## CI/CD Futuro

O CI atual executa restore, build, tests e vulnerability check. Ele nao aplica migrations, nao usa PostgreSQL real e nao executa seed real.

Um pipeline futuro pode:

- gerar script SQL idempotente como artefato;
- publicar o artefato para revisao;
- exigir aprovacao manual para homologacao/producao;
- aplicar o script com credenciais vindas do provedor de CI/CD ou secret manager;
- bloquear aplicacao automatica em producao sem aprovacao.

## Comandos Uteis

Listar migrations:

```bash
dotnet ef migrations list --context CourseCoreDbContext --no-connect
```

Criar migration:

```bash
dotnet ef migrations add NomeDaMigration --context CourseCoreDbContext --output-dir Shared/Infrastructure/Persistence/Migrations
```

Gerar script SQL idempotente:

```bash
dotnet ef migrations script --context CourseCoreDbContext --idempotent --output ./artifacts/migrations/coursecore-migration.sql
```

Aplicar migration diretamente, somente local ou homologacao controlada:

```bash
dotnet ef database update --context CourseCoreDbContext
```

## Checklist Antes de Producao

- Script SQL gerado a partir do commit correto.
- Script revisado.
- Backup ou snapshot confirmado.
- Credenciais obtidas fora do repositorio.
- Janela de aplicacao comunicada, se necessaria.
- Plano de rollback documentado.
- Seed desabilitado.
- Health checks definidos para validacao.
- Logs acompanhados durante e apos a mudanca.

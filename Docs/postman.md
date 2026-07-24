# Postman

Este guia explica como usar a colecao Postman da CourseCore API para testar os endpoints HTTP principais.

## Arquivos

Importe estes arquivos no Postman:

```text
Postman/CourseCore.postman_collection.json
Postman/CourseCore.local.postman_environment.json
```

Selecione o environment `CourseCore Local` antes de executar as requests.

## Base URL

O environment usa Docker por padrao:

```text
baseUrl=http://localhost:8080
```

Para rodar sem Docker, troque `baseUrl` para a porta usada pelo profile local da API. Em geral, ela aparece em `Properties/launchSettings.json` ou no console do `dotnet run`, por exemplo:

```text
http://localhost:5278
```

Scalar e OpenAPI ficam disponiveis apenas em `Development`.

## Seed admin

A collection usa o admin esperado do seed:

```text
adminEmail=admin@coursecore.local
adminPassword=CHANGE_ME_LOCAL_ONLY
```

Antes de usar a collection, configure `adminPassword` no environment com a senha local que voce definiu para o seed. Nao salve senha real em arquivo versionado.

O seed admin e opt-in, roda somente em `Development` e exige schema atualizado. Para habilitar localmente:

```powershell
$env:Seed__Admin__Enabled="true"
$env:Seed__Admin__Name="CourseCore Admin"
$env:Seed__Admin__Email="admin@coursecore.local"
$env:Seed__Admin__Password="CHANGE_ME_LOCAL_ONLY"
$env:Seed__Admin__ResetPassword="false"
dotnet run
```

Veja tambem `Docs/database-seeding.md`.

## Fluxo recomendado

1. Suba a API com PostgreSQL e migrations aplicadas.
2. Importe a collection e o environment.
3. Preencha `adminPassword`.
4. Execute `Auth / Login as Seed Admin`.
5. Use os endpoints protegidos.
6. Quando necessario, execute `Auth / Refresh Token` para renovar os tokens.

A request de login salva automaticamente:

```text
accessToken
refreshToken
```

A request de refresh token tambem atualiza os dois valores.

## Variaveis do environment

Variaveis preenchidas pela collection:

```text
accessToken
refreshToken
correlationId
createdUserId
courseId
moduleId
lessonId
videoId
```

Variaveis que normalmente precisam ser preenchidas manualmente ou obtidas por seed/banco:

```text
adminPassword
areaId
roleId
```

`areaId` e necessario para criar cursos e conceder acesso por area. `roleId` e necessario para conceder acesso por role. Se voce criar um curso pela collection, rode depois `Courses / Get Course Details` para tentar salvar `moduleId` e `lessonId` a partir da resposta.

## Authorization

Os folders protegidos usam:

```text
Bearer {{accessToken}}
```

Login e refresh token sao publicos e nao usam Bearer.

## Correlation id

A collection possui um pre-request script global que gera um novo GUID por request e salva em:

```text
correlationId
```

Todas as requests enviam:

```text
X-Correlation-ID: {{correlationId}}
```

As respostas tambem devem devolver esse header.

## Cuidados

- Nao versionar senha real.
- Nao versionar JWT real.
- Nao versionar refresh token real.
- Nao versionar connection string real.
- Nao copiar valores reais de `.env` para a collection.
- Docker Compose nao aplica migrations automaticamente.
- `/health/ready` pode falhar enquanto o schema do banco nao estiver aplicado.

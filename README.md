# CourseCore

CourseCore e uma API modular em ASP.NET Core para gerenciamento de cursos, aulas, videos, acessos, usuarios, progresso e autenticacao.

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Npgsql
- Scalar para documentacao da API
- JWT Bearer Authentication

## Requisitos

- .NET SDK 10 instalado
- PostgreSQL rodando localmente na porta `5432`
- Usuario PostgreSQL `postgres` com senha `root`

## Configuracao do banco

A aplicacao usa a connection string `CourseCoreDatabase`, configurada em `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "CourseCoreDatabase": "Host=127.0.0.1;Port=5432;Database=coursecore;Username=postgres;Password=root;Timeout=15;Command Timeout=60"
  }
}
```

O banco `coursecore` nao precisa existir antes. O Entity Framework Core cria o banco ao aplicar as migrations.

## Como iniciar o PostgreSQL

No Windows, se o PostgreSQL estiver instalado como servico:

```powershell
Get-Service | Where-Object { $_.Name -like '*postgres*' }
Start-Service postgresql-x64-18
```

Se o nome do servico for diferente, use o nome exibido pelo primeiro comando.

Para conferir se a porta esta acessivel:

```powershell
Test-NetConnection 127.0.0.1 -Port 5432
```

## Como criar ou atualizar o banco

Restaure a ferramenta local do EF Core:

```powershell
dotnet tool restore
```

Aplique as migrations:

```powershell
dotnet ef database update
```

Esse comando cria o banco `coursecore`, se ele ainda nao existir, e aplica a migration inicial.

Para listar as migrations aplicadas:

```powershell
dotnet ef migrations list
```

## Como rodar a aplicacao

Rode o projeto usando o profile HTTP:

```powershell
dotnet run --launch-profile http
```

A API ficara disponivel em:

```text
http://localhost:5278
```

A documentacao OpenAPI pode ser acessada em:

```text
http://localhost:5278/openapi/v1.json
```

Em ambiente de desenvolvimento, a interface do Scalar tambem fica disponivel em:

```text
http://localhost:5278/scalar
```

## Seed de dados

O projeto possui seed para criar dados administrativos, mas ele fica desabilitado por padrao. Para habilitar, configure a secao `Seed:Admin` no `appsettings.Development.json` ou via variaveis de ambiente.

Exemplo:

```json
{
  "Seed": {
    "Admin": {
      "Enabled": true,
      "Name": "CourseCore Admin",
      "Email": "admin@coursecore.local",
      "Password": "change-this-password",
      "ResetPassword": false
    }
  }
}
```

Quando a aplicacao inicia em `Development`, ela executa o seed automaticamente se `Seed:Admin:Enabled` estiver como `true`.

## Comandos uteis

Compilar o projeto:

```powershell
dotnet build
```

Rodar a aplicacao:

```powershell
dotnet run --launch-profile http
```

Atualizar o banco:

```powershell
dotnet ef database update
```

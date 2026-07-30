# Observability

## Objetivo

A CourseCore API usa um correlation id por request para facilitar a investigacao de erros entre cliente, API e logs da aplicacao. Esse identificador nao substitui o `traceId` do ASP.NET, mas cria um valor controlado e simples de compartilhar em atendimentos, homologacao e producao.

## Header

O header padrao e:

```text
X-Correlation-ID
```

Quando o cliente envia um `X-Correlation-ID` valido em formato `Guid`, a API preserva o mesmo valor e o devolve no response header.

Quando o cliente nao envia o header, envia um valor vazio, invalido ou maior que 128 caracteres, a API gera um novo `Guid` e o devolve no response header.

## Erros

As respostas geradas pelo middleware global de excecoes incluem:

```text
traceId
correlationId
```

Use o `correlationId` para procurar os logs da requisicao. Use o `traceId` como identificador tecnico adicional do ASP.NET para diagnostico interno.

## Logs Seguros

O correlation id e adicionado ao logging scope com a propriedade `CorrelationId`, permitindo que logs emitidos durante a request sejam relacionados sem precisar repetir o valor manualmente em cada mensagem.

Os logs de autenticacao registram eventos operacionais, como login bem-sucedido, credenciais invalidas e rotacao de refresh token, sem expor dados sensiveis.

Login, refresh token e logout possuem rate limiting. Requisicoes acima do limite retornam `429 Too Many Requests` antes do controller, usando resposta segura com `traceId` e `correlationId` quando disponivel. O login de usuario inexistente executa uma verificacao BCrypt ficticia para reduzir enumeracao por tempo, mantendo a mensagem publica generica.

Nao devem ser logados:

```text
access token
refresh token
refresh token hash
senha
JWT secret
connection string
segredos de ambiente
header Cookie
header Authorization
```

Rate limiting ajuda a conter volume e custo de BCrypt, mas nao substitui MFA, CAPTCHA, lockout progressivo ou monitoramento antifraude.

## Depuracao

Para depurar um erro:

```text
1. Copie o correlationId retornado no response header ou no body de erro.
2. Pesquise esse valor nos logs da aplicacao.
3. Use o traceId como apoio quando for necessario correlacionar com detalhes internos do ASP.NET.
```

## Pendencias Futuras

Esta etapa usa o logging padrao do ASP.NET Core. Para as proximas etapas, podem ser avaliados:

```text
Serilog
OpenTelemetry
metricas externas
tracing distribuido
exportacao de logs para ferramenta centralizada
```

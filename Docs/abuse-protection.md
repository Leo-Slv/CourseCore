# Protecao contra abuso (login e rate limit)

## Bloqueio de login por conta

Alem do limite por IP (`RateLimiting__Login`, 5/min), cada **e-mail** tem um contador de falhas. Isso protege mesmo quando o atacante troca de IP ou forja o `X-Forwarded-For` chamando a EC2 direto.

- 5 senhas erradas em 15 minutos bloqueiam aquele e-mail por 15 minutos.
- Durante o bloqueio a API responde `429 Too Many Requests` com o cabecalho `Retry-After` (segundos), **sem nem conferir a senha** (a senha correta tambem e recusada).
- Um login com sucesso zera o contador. Falhas durante o bloqueio nao o prolongam.
- E-mails que nao existem sao contados do mesmo jeito, entao o bloqueio nao revela quais contas existem.
- Os contadores ficam **em memoria**: reiniciar a API libera todos (aceitavel com uma unica instancia).

```text
Auth__LoginLockout__MaxFailedAttempts=5
Auth__LoginLockout__FailureWindowMinutes=15
Auth__LoginLockout__LockoutMinutes=15
```

**Contrapartida:** alguem que saiba o e-mail de uma pessoa pode bloqueá-la por 15 minutos errando a senha de proposito. Para um usuario comum e um incomodo temporario; o Admin pode esperar o prazo. Se isso virar problema, aumente `MaxFailedAttempts` ou reduza `LockoutMinutes`.

## Limites por usuario e globais

As rotas autenticadas que custam dinheiro ou aceitam conteudo sao limitadas **por usuario** (nao por IP, para usuarios atras do mesmo proxy nao dividirem a cota):

| Politica | Rotas | Padrao |
| --- | --- | --- |
| `Checkout` | `POST /api/courses/{id}/checkout` | 5/min |
| `Upload` | todos os `*-upload-url` (video, material, capa, imagem de area/modulo, avatar) | 10/min |
| `UserContent` | perguntas, depoimentos (`/mine`) e pedidos de acesso | 10/min |
| `Global` | **toda** requisicao, por usuario (ou por IP se anonima); `/health` fica de fora | 300/min |

Cada uma aceita `PermitLimit`, `WindowSeconds` e `QueueLimit`:

```text
RateLimiting__Checkout__PermitLimit=5
RateLimiting__Upload__PermitLimit=10
RateLimiting__UserContent__PermitLimit=10
RateLimiting__Global__PermitLimit=300
```

O rate limiter agora roda **depois** da autenticacao (`Program.cs`), para enxergar o usuario.

O envio de progresso da aula (a cada 5 s) e as demais telas ficam bem abaixo de 300/min; se algum dia o painel admin precisar de mais, suba o `Global`.

## O que continua limitado so por IP
Rotas publicas de autenticacao (login, cadastro, esqueci/redefinir senha, reenvio de confirmacao, refresh, logout) e o formulario de visitantes. O IP visto pela API depende de `ForwardedHeaders__ForwardLimit=2` (veja `Docs/deployment-aws.md`).

## Testes
`AbuseProtectionIntegrationTests`, `LoginUseCaseTests` e `InMemoryLoginAttemptTrackerTests` cobrem o bloqueio (inclusive com IP forjado), o isolamento entre contas, os limites por usuario e o limite global.

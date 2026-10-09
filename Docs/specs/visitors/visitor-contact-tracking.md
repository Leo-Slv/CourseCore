# Spec — Controle de contato dos visitantes de primeira vez

Status: implementado (2026-10-09). Complementa `first-time-visitor-registration.md`.

## 1. Objetivo

A equipe da igreja precisa saber, na lista de visitantes que preencheram o formulário "Primeira vez", **quem já foi contatado** e **quem ainda falta**, e poder marcar/desmarcar isso.

## 2. Comportamento

- Todo visitante nasce **não contatado**.
- `PUT /api/visitors/{id}/contacted` com `{ "contacted": true|false }` marca ou desmarca. Resposta: o `VisitorResponse` atualizado.
- `GET /api/visitors` passa a devolver `contacted` (bool) e `contactedAt` (data/hora UTC, nula quando não contatado) e aceita o filtro opcional `?contacted=true|false` (omitido = todos). Os demais parâmetros (`page`, `pageSize`, `search`) não mudam.
- O backend guarda quem marcou (`ContactedByUserId`) e quando (`ContactedAt`); esses dados não são expostos na API, só `contactedAt`.

## 3. Regras de negócio

1. Marcar é **idempotente**: marcar um visitante já contatado mantém a data e o responsável originais e não gera novo audit log.
2. Desmarcar limpa data e responsável; desmarcar quem nunca foi contatado não faz nada.
3. Só muda estado (e só grava audit log) quando houve mudança real: `VisitorMarkedContacted` / `VisitorMarkedNotContacted`, com o usuário que agiu e o nome do visitante em `displayName`.
4. Visitante inexistente → 404.
5. **Quem pode:** listar exige `ReadVisitors` (`visitors.read` ou Admin, inalterado); marcar/desmarcar exige a nova policy `ManageVisitors` (`visitors.manage` ou Admin). O formulário público de cadastro continua anônimo e não é afetado.

## 3.1 Dados e migration

`AddVisitorContacted`: colunas nulas `ContactedAt` e `ContactedByUserId` em `visitors` (+ índice em `ContactedAt`). Visitantes existentes ficam como não contatados. `ContactedByUserId` não tem FK de propósito (o histórico de quem contatou não deve bloquear a exclusão de um usuário).

A permissão `visitors.manage` é criada pelo seed de desenvolvimento (junto com `visitors.read`). Em produção, o role Admin passa pela policy mesmo sem a permissão; para dar o acesso a outra pessoa, atribua a permissão ao role dela.

## 4. Critérios de aceite (cobertos por testes)

- [x] Visitante novo não é contatado; marcar registra data e responsável; desmarcar limpa.
- [x] Marcar duas vezes mantém data/responsável e não duplica audit log.
- [x] Listagem expõe `contacted`/`contactedAt` e filtra por `contacted`.
- [x] Admin consegue marcar/desmarcar; anônimo → 401; só `visitors.read` → 403; `visitors.manage` com id inexistente → 404.
- [x] Audit log `VisitorMarkedContacted` gravado ao marcar.

## 5. Fora de escopo

Anotações livres por visitante, histórico completo de contatos, atribuir visitante a uma pessoa da equipe, exportação.

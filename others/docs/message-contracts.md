# Contrato de Mensagens (topics / events)

Documento simples descrevendo os eventos principais trocados via mensageria.

## Visão geral

O serviço publica/consome eventos para atualizar a biblioteca do usuário e notificar outros serviços. Abaixo há exemplos de tópicos e payloads JSON sugeridos.

---

## Tópicos / Eventos

1) `purchases.completed` (emitido quando uma compra é concluída)

Payload (exemplo):

```json
{
  "eventId": "b6a1e2f0-...",
  "occurredAt": "2025-11-26T12:00:00Z",
  "userId": "guid-do-usuario",
  "purchaseId": "guid-da-compra",
  "items": [
    { "gameId": "guid-do-jogo", "title": "Space Adventure", "price": 29.9, "currency": "BRL" }
  ],
  "total": 29.9
}
```

Consumidores esperados: `FiapCloudGames.Users` (atualiza biblioteca), serviço de faturamento, notificações por e-mail.

2) `games.created` (emitido quando um novo jogo é disponibilizado na plataforma)

Payload (exemplo):

```json
{
  "eventId": "3f4d2a1b-...",
  "occurredAt": "2025-11-26T09:00:00Z",
  "gameId": "guid-do-jogo",
  "title": "Nova Aventura",
  "metadata": { "genre": "RPG", "releaseDate": "2025-12-01" }
}
```

3) `users.updated` (opcional — quando perfil do usuário muda)

Payload (exemplo):

```json
{
  "eventId": "...",
  "occurredAt": "2025-11-26T10:00:00Z",
  "userId": "guid-do-usuario",
  "changes": { "name": "Novo Nome" }
}
```

---

## Convenções e recomendações

- Use `eventId` e `occurredAt` em todos os eventos para rastreabilidade.
- JSON deve ser compatível com UTF-8 e conter campos explícitos de versão quando necessário (ex.: `schemaVersion`).
- Para filas/receivers idempotentes, inclua `eventId` para evitar processamento duplicado.
- Documente TTL e política de reentrega no broker (Azure Service Bus, Kafka, etc.).

---

Se quiser, posso gerar um arquivo `contracts/openapi-events.yml` em formato AsyncAPI ou um diagrama de sequência para um fluxo de compra. Diga qual prefere.

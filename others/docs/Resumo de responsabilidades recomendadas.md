Resumo de responsabilidades recomendadas

Games Service: propriedade das entidades Game e Promotion. Responsável por catálogo, preços e promoções.
Endpoints principais: GET /games, GET /games/{id}, GET /games/{id}/promotions, POST /games (admin).
DB: games_db.
Publica eventos: PromotionCreated/Updated/Deleted.
Users Service: propriedade das entidades User e Library (coleção de jogos do usuário).
Endpoints principais: GET /users/{id}, POST /users, GET /users/{id}/library, POST /users/{id}/library (adicionar jogo via evento).
DB: users_db.
Consome: PurchaseCompleted para atualizar Library.
Payments Service: propriedade do fluxo de pagamento / transações (não mantém Library).
Endpoints principais: POST /payments/purchase (body: userId, gameId, paymentMethod).
DB: payments_db (transactions/audit).
No sucesso: publica evento PurchaseCompleted { purchaseId, userId, gameId, amount, timestamp }.
No erro: publica PurchaseFailed.
Motivações e escolhas

Colocar Promotion em Games mantém regras de preço próximas ao catálogo e evita replicação de lógica. Payments pode consultar Games sincronamente para preço atual ou manter um read-model atualizado assinando eventos de promoção.
Library pertence ao Users pois é o agregado de propriedade do usuário (evita que Payments altere o estado do usuário diretamente).
Comunicação: REST para consultas síncronas (ex.: conferir preço se necessário); mensagens assíncronas para atualização de estado/consistência (RabbitMQ / Azure Service Bus).
Consistência: eventual consistency via eventos. Para compras, implementar um saga simples no Payments ou confirmar sequências de eventos (PurchaseRequested -> processa -> PurchaseCompleted).
Fluxo de compra (exemplo)

Cliente chama POST /payments/purchase com userId e gameId.
Payments verifica preço:
Opção A (sincrona): consulta GET /games/{gameId} para obter preço/active promotion.
Opção B (offline): usa cache/read-model mantido assinando eventos de PromotionUpdated.
Payments processa a cobrança.
Se sucesso:
Persiste transação em payments_db.
Publica PurchaseCompleted.
Users consome PurchaseCompleted e adiciona gameId ao Library do userId.
Games pode consumir para atualizar estatísticas de vendas.
Se falha:
Publica PurchaseFailed.
Exemplos de contratos (DTOs/eventos)

Purchase request (Payments API):
{ userId: Guid, gameId: Guid, paymentMethod: { type, token }, currency: string }
Event PurchaseCompleted:
{ purchaseId: Guid, userId: Guid, gameId: Guid, amount: decimal, currency: string, timestamp: ISO8601 }
Event PromotionUpdated:
{ promotionId: Guid, gameId: Guid, discountPercent: int, validFrom: ISO8601, validTo: ISO8601 }
Estrutura proposta no repositório

src/FiapCloudGames.Games/ (API, domain, persistence, migrations)
src/FiapCloudGames.Users/ (API, domain, persistence, migrations)
src/FiapCloudGames.Payments/ (API, domain, persistence, migrations)
Manter projeto FiapCloudGames.Shared/ para DTOs compartilhados, contratos de eventos e libs utilitárias (cuidado com dependências para evitar acoplamento forte).
Passos práticos imediatos (o que posso fazer agora)

Opcional A: Gerar scaffold dos 3 projetos (APIs mínimas, pasta Domain/DTOs, arquivos csproj) e atualizar a solução.
Opcional B: Mapear onde, no código atual (FiapCloudGames.Api, Application, Domain), cada classe/Controller deve ser movida para cada serviço e listar os arquivos a mover.
Opcional C: Gerar exemplos de eventos e pequenos handlers em C# para integrar Payments -> Users.
# Arquitetura de Busca e Analytics com Elasticsearch

## Contexto

Microsserviços existentes:

- **user**
  - Cria e edita usuários.
  - Autentica.
  - Grava a biblioteca de jogos do usuário após compra.
- **game**
  - Cria, edita e deleta jogos.
- **payments**
  - Recebe solicitações de pagamento e processa.
  - Grava histórico no Elasticsearch (eventos financeiros / compra).

Requisitos de Elasticsearch:

- Armazenar e indexar dados dos jogos para **busca eficiente**.
- Criar **consultas avançadas** para sugerir jogos baseados no histórico do usuário.
- Implementar **agregações** para métricas (jogos mais populares, etc.).

Decisão de projeto (estudo):

- Não criar um microserviço separado de busca.
- Cada contexto terá **workers próprios** (processos em `user` e `game`) para lidar com indexação e leitura no Elasticsearch.

---

## Visão geral da solução

### Papel do Elasticsearch

- Elasticsearch é tratado como **read model otimizado para busca/analytics**, **não** como fonte de verdade.
- Fonte de verdade permanece nos bancos transacionais de cada serviço (`user`, `game`, `payments`).
- ES recebe **eventos** dos serviços para manter índices atualizados de forma **assíncrona**.

### Serviços e responsabilidades

- **game**
  - Dono do modelo de domínio `Game` (catálogo de jogos).
  - Responsável pela consistência do dado de jogo no banco transacional.
  - Emite eventos de domínio:
    - `GameCreated`
    - `GameUpdated`
    - `GameDeleted`
  - Possui um **worker de indexação** que consome esses eventos e atualiza o índice `games` no Elasticsearch.

- **user**
  - Dono de usuário e biblioteca/histórico de jogos do usuário.
  - Emite eventos, por exemplo:
    - `UserGameAddedToLibrary`
    - `UserGameRemovedFromLibrary`
    - (Pode consumir eventos de `payments`, como `PaymentSucceeded`, para refletir compras na biblioteca.)
  - Possui um **worker de indexação** que atualiza o índice `user_games` no Elasticsearch.

- **payments**
  - Dono do fluxo de pagamento.
  - Emite eventos de domínio:
    - `PaymentSucceeded`
    - `PaymentFailed`
  - Pode alimentar um índice de histórico/analytics, como `payments` ou `game_popularity`, via worker próprio ou via integração com `user` e `game`.

---

## Fluxo de escrita: DB primeiro, Elasticsearch depois

Objetivo: garantir que o **core de negócio** continue funcionando mesmo se o Elasticsearch estiver indisponível.

### Passos gerais (padrão outbox/eventos)

1. **Recebimento da requisição** no serviço de domínio (ex.: `POST /games`, `PUT /games/{id}`, `DELETE /games/{id}`, `POST /users/{id}/library`).
2. **Persistência transacional** no banco principal do serviço (SQL/NoSQL de domínio).
3. Registro de um **evento de domínio** associado à transação, por exemplo via **tabela de outbox**:
   - Ex.: `GameCreated`, `GameUpdated`, `GameDeleted`, `UserGameAddedToLibrary`, etc.
4. Um **worker** (hosted service ou processo separado dentro do mesmo bounded context) lê periodicamente a tabela de outbox / fila de eventos e:
   - Constrói o documento a ser indexado no Elasticsearch.
   - Chama as operações:
     - `index` / `update` / `delete` no **índice correspondente** (`games`, `user_games`, `payments`, etc.).
5. Em caso de **falha no Elasticsearch**:
   - O worker aplica **retry com backoff exponencial**.
   - Após exceder um número máximo de tentativas, envia o evento para uma **DLQ (Dead Letter Queue)** ou marca para intervenção manual.

Com isso, se o cluster Elasticsearch estiver fora do ar ou lento, o sistema de negócio continua aceitando criação/edição de jogos e atualização de biblioteca de usuário. Apenas a **busca e analytics ficam eventualmente inconsistentes**.

---

## Fluxo de leitura: busca, recomendações e métricas

### Busca de jogos

- Operações de busca, como `GET /games?search=...`, utilizam **exclusivamente** o índice `games` do Elasticsearch.
- O índice `games` contém documentos com campos relevantes para busca, por exemplo:
  - `id`, `title`, `description`, `genres`, `tags`, `platforms`, `releaseDate`, etc.
- Consultas podem usar:
  - `multi_match` em título/descrição.
  - Filtros por gênero/plataforma.
  - Ordenação por data de lançamento, popularidade, etc.

### Recomendações baseadas no histórico do usuário

- O índice `user_games` (ou similar) armazena a **biblioteca/histórico de jogos por usuário**:
  - `userId`, `gameId`, `acquiredAt`, `playedTime`, `lastPlayedAt`, etc.
- Com base nesse índice e no índice `games`, os workers/APIs podem fazer consultas para:
  - Obter jogos similares a outros já jogados/comprados (`more_like_this`, filtros por gênero/tags).
  - Combinar informações de popularidade do índice de `payments` com preferências do usuário.

### Métricas e agregações (jogos mais populares)

- O serviço `payments` emite eventos de compra (`PaymentSucceeded`) contendo `userId`, `gameId`, valor, timestamp, etc.
- Um índice de eventos de pagamento (`payments` ou `game_popularity`) permite agregações como:
  - `terms` aggregation por `gameId` para **jogos mais vendidos**.
  - `date_histogram` para evolução temporal de vendas.
  - Filtros por período (últimos 7 dias, 30 dias, etc.).
- Essas agregações podem ser expostas via endpoints específicos (por exemplo, em `payments` ou em uma API de relatório dentro do mesmo contexto).

---

## Resiliência e tolerância a falhas

### Princípios

- Elasticsearch é uma **dependência não-crítica** para o core de negócio:
  - Se o ES falhar, pagamentos, criação/edição de usuários e jogos continuam funcionando.
  - O impacto é na **qualidade da busca/recomendações/analytics**, não na capacidade de registrar transações de domínio.

### Estratégias de resiliência

1. **Integração assíncrona (outbox + worker)**
   - Evita que o commit da transação de negócio dependa do sucesso da indexação.
   - Permite reprocessar eventos quando o ES voltar.

2. **Retry com backoff**
   - Em caso de erros temporários (timeout, nó indisponível), o worker tenta novamente com intervalos crescentes.

3. **Circuit breaker**
   - Implementado nas chamadas ao ES (tanto nos workers quanto nas APIs de leitura):
     - Se um limiar de falhas for atingido, o circuito "abre" e novas chamadas são bloqueadas por um período.
     - Enquanto o circuito está aberto:
       - Os workers podem pausar a indexação.
       - As APIs de busca podem devolver uma resposta degradada (mensagem indicando indisponibilidade da busca) ou um fallback simples.

4. **DLQ / reprocessamento manual**
   - Eventos que falham repetidamente na indexação são enviados a uma fila de erros.
   - Permite inspeção e reprocessamento manual, sem perder informações.

5. **Fallback de leitura (opcional)**
   - Em algumas consultas críticas, pode-se ter um fallback simples em banco transacional (por exemplo, listar os jogos mais recentes) caso o ES esteja indisponível.
   - Em geral, para funcionalidades avançadas de busca, é aceitável exibir um erro amigável e não tentar fallback complexo.

---

## Organização do Elasticsearch para múltiplos microsserviços

### Cluster único compartilhado x clusters por serviço

Para o projeto de estudo, a opção adotada é um **cluster único compartilhado** entre `user`, `game` e `payments`, com **índices separados por contexto**.

Índices sugeridos:

- `games` – catálogo de jogos, mantido pelo worker do serviço `game`.
- `user_games` – biblioteca/histórico do usuário, mantido pelo worker do serviço `user`.
- `payments` ou `game_popularity` – eventos de compra e métricas de popularidade, mantidos por `payments`.

Com isso:

- Simplifica-se a operação (monitoramento, backup, tuning, upgrades de ES).
- Mantém-se **isolamento lógico** por índice.
- Permite consultas e analytics combinando dados de diferentes contextos dentro do mesmo cluster.

#### Clusters separados por serviço (cenário alternativo)

Tecnicamente, é possível ter:

- Cluster `es-games` usado apenas pelo serviço/worker de `game`.
- Cluster `es-user` usado apenas pelo serviço/worker de `user`.
- Cluster `es-payments` usado apenas por `payments`.

Prós:

- Falha no cluster de `games` não afeta índices de `user` ou `payments`.
- Isolamento de recursos: consultas pesadas de um domínio não impactam outro.

Contras:

- **Custo operacional maior**: mais clusters para monitorar, fazer backup, atualizar, escalar.
- Maior complexidade para fazer analytics cruzando domínios (ex.: recomendações que combinem dados de `user`, `game` e `payments`).

Dada a natureza de **projeto de estudos**, e até mesmo em muitos cenários reais de pequeno/médio porte, um **único cluster compartilhado com índices bem definidos por contexto é a opção preferencial**.

### Configuração de resiliência no cluster

Mesmo com um único cluster, é possível obter boa resiliência:

- Múltiplos nós Elasticsearch (pelo menos 2–3 em ambiente mais sério).
- Configuração de **shards e réplicas** em cada índice:
  - `number_of_shards` ajustado ao volume esperado.
  - `number_of_replicas >= 1` para tolerância a falha de nó.
- Snapshots periódicos (S3/Blob/FS) para recuperação de desastre.

---

## Resumo

- `game`, `user` e `payments` continuam donos dos seus dados e regras de negócio; o Elasticsearch é apenas um **read model para busca e analytics**.
- Indexação é feita de forma **assíncrona**, via **workers** nos serviços (`user` e `game`, e opcionalmente `payments`), usando padrão **outbox + eventos**.
- Erros no Elasticsearch **não interrompem o fluxo de negócio**: apenas atrasam ou degradam busca, recomendações e métricas.
- Um **cluster Elasticsearch único compartilhado** com índices separados por contexto oferece o melhor equilíbrio entre simplicidade e resiliência para este projeto de estudos.
- Caso a necessidade de isolamento forte cresça, é possível evoluir para clusters separados por serviço ou para um microserviço dedicado de busca/recomendações sem quebrar o modelo atual de eventos + índices.

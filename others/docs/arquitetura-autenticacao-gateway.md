# Arquitetura de Autenticação, Autorização e API Gateway

## Contexto dos Microsserviços

- **user**
  - Cria e edita usuários.
  - Autentica usuários (login).
  - Grava a biblioteca de jogos do usuário após compra.
- **game**
  - Cria, edita e deleta games.
- **payments**
  - Recebe solicitações de pagamento e processa.
  - Grava histórico no ElasticSearch.

Requisitos:
- Todos os serviços devem ter **autenticação e autorização**.
- Uso obrigatório de **API Gateway** como única porta pública.
- **Não** usar Entra ID (Azure AD).
- Projeto de estudo: priorizar **soluções simples**, mas corretas conceitualmente.

---

## Visão Geral da Arquitetura Alvo

### Componentes

1. **API Gateway** (porta de entrada pública)
   - Expõe uma URL pública HTTP/HTTPS (por exemplo, `https://api.seudominio.com`).
   - Roteia chamadas para os microsserviços internos: `user`, `game`, `payments`.
   - Pode:
     - Validar JWT.
     - Fazer rate limiting, logging, tracing, etc.

2. **Serviço de Identidade (IdP simples)**
   - Implementado pelo próprio serviço **user** (mais simples) ou, em uma evolução, por um serviço `auth` dedicado.
   - Responsável por autenticação (login) e emissão de **tokens JWT**.

3. **Microsserviços de Negócio**
   - `user`: gestão de usuário e biblioteca.
   - `game`: catálogo e operações administrativas de jogos.
   - `payments`: processamento de pagamentos e registro no ElasticSearch.

### Fluxo alto nível

1. Cliente chama `POST /auth/login` no serviço **user** (via Gateway):
   - Envia credenciais.
   - Se válidas, o serviço `user` gera um **JWT** assinado.
2. Cliente usa o **JWT** no header `Authorization: Bearer <token>` para chamar:
   - Endpoints de `user`.
   - Endpoints de `game`.
   - Endpoints de `payments`.
3. O Gateway e/ou os próprios serviços validam o JWT e aplicam **autorização** baseada em claims (roles, scopes, etc.).

---

## Modelo de Token JWT

O serviço `user` é o emissor oficial do JWT.

### Claims recomendados

- `sub`: identificador único do usuário (UserId).
- `email` ou `username`: identificação amigável.
- `role` ou `roles`: lista de papéis, por exemplo:
  - `user`
  - `admin`
- `scopes` (opcional, mas útil para granularidade):
  - `games.read`, `games.write`
  - `payments.create`, `payments.read`
- `iat`, `exp`: data de emissão e expiração.
- `iss`: emissor (ex.: `https://api.seudominio.com/user`).
- `aud`: audiência (ex.: `fiap-cloud-games-api`).

### Exemplo de payload JWT

```json
{
  "sub": "c1f4e8a2-1234-5678-90ab-ffeeddccbbaa",
  "email": "user@example.com",
  "roles": ["user"],
  "scopes": ["games.read", "payments.create"],
  "iss": "https://api.seudominio.com/user",
  "aud": "fiap-cloud-games-api",
  "iat": 1735960000,
  "exp": 1735963600
}
```

### Algoritmo e chave

- Para estudo, usar **HMAC-SHA256 (HS256)** com um **segredo simétrico** compartilhado entre `user`, `game`, `payments` e o Gateway.
- O segredo deve ser configurado via variável de ambiente ou configuração segura.
- Em cenários mais avançados, é possível usar **RSA** (chave privada para assinar e chave pública para validar).

---

## Abordagens de Autenticação e Autorização

### Abordagem 1 – Autenticação no Gateway, Headers Internos para Serviços (Mais Simples)

**Ideia principal:**
- O **API Gateway valida o JWT** recebido do cliente.
- Se for válido, o Gateway extrai as claims (UserId, roles, scopes) e injeta em **headers internos** nas chamadas para os microsserviços, por exemplo:
  - `X-User-Id`
  - `X-User-Roles`
  - `X-User-Scopes`
- `user`, `game` e `payments` **não precisam conhecer JWT** diretamente, apenas confiam nesses headers vindos do Gateway.

**Requisitos de segurança:**
- `user`, `game` e `payments` **não podem ser acessados diretamente de fora** (somente o Gateway é público).
- Infraestrutura deve garantir que **apenas o Gateway** consiga chamar os serviços internos.

**Vantagens:**
- Lógica de autenticação fica **centralizada** no Gateway.
- Menos código e configuração de segurança dentro de cada microsserviço.
- Ideal para um **projeto de estudo** que quer priorizar simplicidade.

**Desvantagens:**
- Se futuramente houver outro entrypoint público (por exemplo, outro Gateway ou gRPC externo), será necessário replicar a lógica.
- Os serviços ficam bastante dependentes da infra estar bem configurada para não serem expostos diretamente.

### Abordagem 2 – Cada Microsserviço Valida o JWT (Mais Robusta)

**Ideia principal:**
- O serviço `user` emite o JWT.
- O Gateway pode:
  - Apenas rotear o JWT para trás, ou
  - Fazer uma `pré-validação`, mas **não é o único ponto de validação**.
- Cada serviço (`user`, `game`, `payments`) implementa um middleware de **autenticação JWT** que:
  - Valida a assinatura usando o mesmo segredo.
  - Verifica `iss`, `aud`, `exp`, etc.

**Autorização nos serviços:**
- Cada serviço define **políticas** com base nas claims do token:
  - `game`:
    - `GET /games` → qualquer usuário autenticado.
    - `POST /games`, `PUT /games/{id}`, `DELETE /games/{id}` → somente `role=admin`.
  - `payments`:
    - `POST /payments` → usuário autenticado com `payments.create` ou `role=user`.
    - Consultas de histórico avançadas → possivelmente restritas a `admin`.

**Vantagens:**
- Mesmo se alguém conseguir chegar direto a `game` ou `payments` (sem Gateway), ainda precisa de JWT válido.
- Arquitetura mais **próxima de um cenário real de produção**.

**Desvantagens:**
- Mais configuração de segurança em cada serviço.
- Para estudo, é um pouco mais trabalhosa, mas educacionalmente melhor.

---

## Garantindo que Somente o Gateway é Público

A frase "garantir que só o Gateway é público" é, principalmente, uma **decisão de infraestrutura/rede**.

### Conceito

- Apenas o **Gateway** possui IP/porta acessível pela Internet (por exemplo, `80`/`443`).
- `user`, `game` e `payments` possuem apenas **IP/porta internos**, acessíveis somente:
  - Pela própria máquina (localhost), ou
  - Por uma rede interna (Docker network, rede interna do cluster Kubernetes, VNet, etc.).

### Cenário 1 – Tudo em uma VM/Servidor Único

- Gateway:
  - Ouve em `http://0.0.0.0:80` ou `https://0.0.0.0:443`.
- `user`, `game`, `payments`:
  - Ouvem somente em `http://127.0.0.1:5001`, `http://127.0.0.1:5002`, etc.
  - **Não** em `0.0.0.0`.
- Configuração de firewall / regras de rede:
  - Abrir apenas as portas 80/443 para o mundo externo.
  - Não expor as portas internas 5001/5002/5003.
- O Gateway acessa os serviços usando `http://localhost:5001`, etc.

### Cenário 2 – Docker / Docker Compose (Recomendado para Estudo)

- Todos os serviços em uma **mesma Docker network** (padrão do `docker-compose`).
- Apenas o container do **Gateway** expõe portas para o host.
- Exemplo simplificado de `docker-compose.yml`:

```yaml
services:
  gateway:
    build: ./gateway
    ports:
      - "8080:80"       # único ponto público
    depends_on:
      - user
      - game
      - payments

  user:
    build: ./user
    expose:
      - "80"            # visível só para outros containers
    # não usar "ports" aqui

  game:
    build: ./game
    expose:
      - "80"

  payments:
    build: ./payments
    expose:
      - "80"
```

- O cliente externo acessa `http://host:8080/...` → Gateway.
- O Gateway acessa `http://user/`, `http://game/`, `http://payments/` pela rede interna Docker.
- Como `game` e `payments` não têm `ports` mapeados, não são acessíveis diretamente do host/Internet.

### Cenário 3 – Kubernetes (Cluster)

- `gateway`:
  - `Service` do tipo `LoadBalancer` ou `NodePort` com `Ingress` apontando para ele.
- `user`, `game`, `payments`:
  - `Service` do tipo `ClusterIP` (padrão): somente acessíveis dentro do cluster.
- Opcionalmente, usar **NetworkPolicies** para restringir o acesso aos pods dos serviços internos apenas a partir do pod do Gateway.

### Checklist "Somente o Gateway é Público"

- [ ] Apenas o Gateway tem `ports` expostos / `Service` público / IP externo.
- [ ] `user`, `game`, `payments` **não** possuem portas expostas para o mundo externo.
- [ ] Gateway e serviços estão na **mesma rede interna** (localhost, Docker network, cluster).
- [ ] Serviços internos não confiam em headers de autenticação se também estiverem expostos diretamente.
- [ ] Logs e monitoramento verificam que chamadas para serviços internos vêm somente de origens esperadas (por exemplo, IP do Gateway).

---

## Aplicando Autorização em Cada Serviço

### Serviço `user`

Responsabilidades:
- Gerenciar dados do usuário.
- Autenticar e emitir JWT.
- Gerenciar a biblioteca de jogos do usuário (após compras aprovadas).

Sugestão de endpoints:
- `POST /auth/login` → gera JWT.
- `POST /auth/refresh` (opcional) → renova token.
- `GET /users/{id}` → detalhes do usuário (autenticado e autorizado).
- `PUT /users/{id}` → editar dados (usuário dono ou admin).

Autorização:
- Usuário comum: pode acessar/editar apenas seus próprios dados.
- Admin: possui permissões adicionais (ex.: gerenciar outros usuários).

### Serviço `game`

Responsabilidades:
- CRUD de jogos.

Sugestão de regras de acesso:
- `GET /games` → qualquer usuário autenticado.
- `GET /games/{id}` → qualquer usuário autenticado.
- `POST /games`, `PUT /games/{id}`, `DELETE /games/{id}` → somente `role=admin`.

Como aplicar:
- Abordagem 1: ler `X-User-Roles` e decidir.
- Abordagem 2: validar JWT e usar roles/policies (por exemplo, `[Authorize(Roles = "admin")]`).

### Serviço `payments`

Responsabilidades:
- Receber solicitações de pagamento.
- Processar pagamento.
- Gravar histórico no ElasticSearch.
- Notificar `user` para atualizar a biblioteca de jogos após compra bem-sucedida.

Sugestão de regras de acesso:
- `POST /payments` → qualquer usuário autenticado com `payments.create` ou `role=user`.
- `GET /payments/{id}` → usuário autenticado dono daquele pagamento ou `admin`.
- Consultas avançadas/relatórios → `admin`.

Comunicação service-to-service:
- Após pagamento aprovado, `payments` chama `user` para atualizar biblioteca do usuário.
- Essa chamada pode usar:
  - Um **token de serviço** (JWT com claim `client_id=payments`), ou
  - Uma chave interna simples em um primeiro momento de estudo.

---

## Caminho Prático para Implementação

Sugestão de ordem de implementação, pensando em simplicidade e aprendizado:

1. **Escolher e configurar o API Gateway**
   - Se estiver em .NET, opções comuns: Ocelot ou YARP.
   - Configurar rotas:
     - `/user/**` → serviço `user`.
     - `/game/**` → serviço `game`.
     - `/payments/**` → serviço `payments`.

2. **Transformar o serviço `user` em emissor de JWT**
   - Implementar `POST /auth/login`.
   - Usar hashing de senha (por exemplo, `PasswordHasher` do ASP.NET Identity) para não armazenar senha em texto plano.
   - Assinar tokens com segredo configurado.

3. **Escolher a abordagem de autenticação**
   - Se prioridade é **máxima simplicidade**:
     - Abordagem 1: validar JWT apenas no Gateway e repassar contexto por headers internos.
   - Se prioridade é **ficar mais próximo de produção real**:
     - Abordagem 2: todos os serviços validam o JWT.

4. **Definir roles e scopes mínimos**
   - Roles:
     - `user`: usuário comum.
     - `admin`: administrador (pode gerenciar jogos, consultar relatórios, etc.).
   - Scopes (opcional, adiciona granularidade):
     - `games.read`, `games.write`.
     - `payments.create`, `payments.read`.

5. **Configurar infraestrutura para garantir que só o Gateway é público**
   - VM: serviços internos ouvindo apenas em `localhost` e firewall bloqueando portas internas.
   - Docker: somente Gateway com `ports`; serviços internos com `expose` (ou nem isso).
   - Kubernetes: Gateway com `Ingress`/`LoadBalancer`; serviços internos `ClusterIP`.

6. **Service-to-service seguro (opcional, para aprofundar)**
   - Criar um "service identity" ou token técnico para `payments` chamar `user`.
   - Validar esse token no serviço `user` por meio de uma policy específica.

---

## Resumo

- O **API Gateway** é o único ponto público e responsável por rotear (e, opcionalmente, autenticar) as requisições.
- O serviço **user** atua como emissor de **JWT**, contendo claims (`sub`, `roles`, `scopes`, etc.).
- Existem duas abordagens principais para autenticação/autorização:
  - **Abordagem 1:** Gateway valida JWT e injeta contexto em headers para os serviços internos (mais simples).
  - **Abordagem 2:** Cada serviço valida o JWT e aplica políticas de autorização (mais robusta e próxima de produção).
- Garantir que só o Gateway é público é uma questão de **rede/infraestrutura**: apenas o Gateway expõe portas externas; serviços internos existem apenas em redes privadas.

Esta arquitetura é adequada para um **projeto de estudo**, mantendo a complexidade sob controle, mas já introduzindo conceitos importantes usados em ambientes reais de microsserviços.

# Plano de Implementacao MVP - Conexao Solidaria

## Objetivo

Implementar uma nova solucao com dois microsservicos, reaproveitando a logica ja comprovada nos repositorios existentes:

- `fiap-solidarity-connection` (Users/Campaigns)
- `fiap-solidarity-connection-donations` (Donations)

Diretrizes fixas desta entrega:

- Broker: Azure Service Bus.
- Observabilidade: New Relic.
- Sem `inbox_events` e sem `outbox_events`.
- Cada servico escreve apenas no proprio banco.
- Integracao entre servicos somente por eventos.

## Escopo funcional do MVP

1. Autenticacao JWT com RBAC (`NgoManager` e `Donor`).
2. Cadastro de doador.
3. Gestao de campanhas (criar/editar/listar publica).
4. Intencao de doacao por doador autenticado.
5. Processamento assincrono da doacao.
6. Atualizacao do total arrecadado por evento assinado.

## Macroarquitetura de implementacao

- Servico A (Core API): Users + Campaigns + API publica + emissao/consumo de eventos.
- Servico B (Donations): intake de doacoes + processamento assincrono + emissao de evento processado.
- Azure Service Bus como barramento entre A e B.
- SQL Server separado por servico.
- New Relic para metrics/traces/logs.

## Contratos de integracao

### Eventos

1. `DonationRequestedEvent`
- Emitido por: Servico A.
- Consumido por: Servico B.
- Campos:
  - `donationId` (Guid)
  - `campaignId` (Guid)
  - `donorId` (Guid)
  - `donationAmount` (decimal)
  - `requestedAt` (DateTimeOffset)

2. `DonationProcessedEvent`
- Emitido por: Servico B.
- Consumido por: Servico A.
- Campos:
  - `donationId` (Guid)
  - `campaignId` (Guid)
  - `donationAmount` (decimal)
  - `status` (`Processed` | `Failed`)
  - `processedAt` (DateTimeOffset)
  - `errorMessage` (string opcional)

### Topicos/FIlas (Service Bus)

- `donation-requested`
- `donation-processed`

## Modelo de dados minimo

### Servico A (Core)

Tabela `users`:

- `id` (PK)
- `full_name`
- `email` (UNIQUE)
- `cpf` (UNIQUE)
- `password_hash`
- `role` (`NgoManager` | `Donor`)
- `created_at`
- `updated_at`

Tabela `campaigns`:

- `id` (PK)
- `title`
- `description`
- `start_date`
- `end_date`
- `financial_goal`
- `status` (`Active` | `Completed` | `Canceled`)
- `total_raised_amount`
- `created_at`
- `updated_at`

### Servico B (Donations)

Tabela `donations`:

- `id` (PK, `donationId`)
- `campaign_id`
- `donor_id`
- `donation_amount`
- `status` (`Received` | `Processing` | `Processed` | `Failed`)
- `attempts`
- `error_message`
- `received_at`
- `processed_at`
- `last_retry_at`

## Fases de implementacao

## Fase 0 - Fundacao tecnica (D1)

Objetivo: preparar estrutura e padroes para os dois servicos.

Checklist:

- [x] Definir convencoes de nomes (eventos, topicos, DTOs, status).
- [x] Centralizar configuracao de Service Bus por `appsettings` + secrets.
- [x] Centralizar configuracao de New Relic (API e Donations).
- [x] Garantir endpoint `/health` em ambos os servicos.
- [x] Documentar variaveis de ambiente obrigatorias.

Criterio de pronto:

- Ambos os servicos sobem localmente com health check e configuracoes externas.

Variaveis obrigatorias (minimo):

- Servico Core:
  - `ConnectionStrings__DefaultConnection`
  - `ServiceBus__ConnectionString`
  - `DONATION_PROCESSED_TOPIC`
  - `DONATION_PROCESSED_SUBSCRIPTION`
  - `NewRelic__OtlpEndpoint`
  - `NewRelic__Protocol`
  - `NewRelic__LicenseKey` (ou `NEW_RELIC_LICENSE_KEY`)
  - `JwtSettings__SecretKey`
  - `JwtSettings__Issuer`
  - `JwtSettings__Audience`
- Servico Donations:
  - `ConnectionStrings__DefaultConnection`
  - `ServiceBus__ConnectionString`
  - `DONATION_TOPIC`
  - `DONATION_SUBSCRIPTION`
  - `NewRelic__OtlpEndpoint`
  - `NewRelic__Protocol`
  - `NewRelic__LicenseKey` (ou `NEW_RELIC_LICENSE_KEY`)
  - `DonationProcessing__MaxAttempts`
  - `Features__DonationGateway__Enabled`
  - `DonationGateway__BaseUrl`
  - `DonationGateway__FunctionRoute`

## Fase 1 - Servico A (Core API) (D2-D3)

Objetivo: entregar identidade, RBAC e dominio de campanhas.

Checklist:

- [x] JWT + autorizacao por role (`NgoManager`, `Donor`).
- [x] Endpoint de registro de doador.
- [x] Endpoint de login.
- [x] CRUD de campanhas com regras:
  - `end_date` nao pode ser no passado.
  - `financial_goal` maior que zero.
- [x] Endpoint publico de campanhas ativas com:
  - `title`
  - `financial_goal`
  - `total_raised_amount`
- [x] Endpoint de intencao de doacao:
  - valida doador autenticado
  - valida campanha ativa
  - valida valor maior que zero
  - publica `DonationRequestedEvent`

Criterio de pronto:

- Fluxos HTTP do Core funcionando e evento de doacao publicado no SB.

## Fase 2 - Servico B (Donations) (D4-D5)

Objetivo: processar doacoes de forma assincrona com persistencia minima.

Checklist:

- [x] Criar tabela `donations` e repositorio.
- [x] Consumer de `DonationRequestedEvent`.
- [x] Pipeline de processamento:
  - `Received` -> `Processing` -> `Processed` ou `Failed`
- [x] Controle de retries via `attempts` + `last_retry_at`.
- [x] Publicar `DonationProcessedEvent` ao final do processamento.
- [x] Registrar `error_message` em falhas definitivas.

Criterio de pronto:

- Donacao recebida no SB e processada com status persistido.

## Fase 3 - Integracao fim a fim (D6)

Objetivo: fechar ciclo assinado entre os dois servicos.

Checklist:

- [x] Consumer de `DonationProcessedEvent` no Servico A.
- [x] Atualizar `campaigns.total_raised_amount` somente quando `status=Processed`.
- [x] Garantir idempotencia por `donationId` no update de arrecadacao.
- [x] Ajustar endpoint publico para refletir valor atualizado.

Criterio de pronto:

- Fluxo completo validado: requisicao de doacao -> processamento -> campanha atualizada.

## Fase 4 - Plataforma e entrega (D7-D8)

Objetivo: empacotar, observar e entregar no formato da avaliacao.

Checklist:

- [x] Docker image dos dois servicos.
- [x] Manifests Kubernetes dos dois servicos:
  - Deployment
  - Service
  - ConfigMap
- [x] Segredos para SB, SQL e New Relic.
- [ ] Dashboard New Relic com metricas reais:
  - taxa de requisicao
  - erros
  - latencia
- [x] Pipeline CI por push na branch principal:
  - build
  - testes
  - build da imagem Docker

Execucao do dashboard New Relic (passo a passo objetivo):

1. Deploy dos dois servicos no AKS com secrets e configmaps aplicados.
2. Gerar trafego real:
   - `POST /api/auth/login`
   - `POST /api/campaigns`
   - `POST /api/donations`
   - `GET /api/campaigns/public`
3. Confirmar ingestao no New Relic APM (2 services):
   - `solidarity-connection-donors-identity-api`
   - `solidarity-connection-donations-api`
4. Criar dashboard com 3 widgets obrigatorios:
   - Throughput (requisicoes por minuto)
   - Error rate (porcentagem de erro)
   - Latencia p95

Consultas NRQL sugeridas:

- Throughput:
  - `FROM Transaction SELECT rate(count(*), 1 minute) WHERE appName = 'solidarity-connection-donors-identity-api' TIMESERIES`
- Error rate:
  - `FROM Transaction SELECT percentage(count(*), WHERE error IS true) WHERE appName = 'solidarity-connection-donors-identity-api' TIMESERIES`
- Latencia p95:
  - `FROM Transaction SELECT percentile(duration, 95) WHERE appName = 'solidarity-connection-donors-identity-api' TIMESERIES`
- Donations throughput:
  - `FROM Transaction SELECT rate(count(*), 1 minute) WHERE appName = 'solidarity-connection-donations-api' TIMESERIES`

Criterio de pronto:

- Deploy funcional em Kubernetes + telemetria visivel + pipeline verde.

Guia pratico para execucao local no Kubernetes (Docker Desktop):

- `others/docs/validacao-kubernetes-docker-desktop.md`

## Plano de testes (enxuto)

1. Testes de unidade (dominio/aplicacao):
- validacoes de campanha e doacao.
- transicoes de status da doacao.

2. Teste E2E manual de demo:
- login -> criar campanha -> enviar doacao -> confirmar atualizacao no endpoint publico.

## Riscos e mitigacoes

1. Perda de mensagem sem outbox:
- Mitigacao: retries e monitoramento de falhas no consumer.

2. Duplicidade de evento:
- Mitigacao: idempotencia por `donationId` no Servico B e no update do Servico A.

3. Drift de contrato de evento:
- Mitigacao: versionar DTOs e validar schema no consumer.

## Definicao de pronto final

- [x] Requisitos funcionais obrigatorios implementados.
- [ ] Requisitos tecnicos obrigatorios implementados (pendente apenas comprovacao operacional do dashboard New Relic em ambiente).
- [x] Sem acesso a banco entre microsservicos.
- [x] Comunicacao assincrona operacional em Azure Service Bus.
- [ ] Observabilidade operacional no New Relic.
- [x] CI executando build/teste/imagem Docker.
- [x] README com passo a passo de execucao e validacao.

## Sequencia sugerida de execucao diaria

- Dia 1: Fase 0
- Dia 2-3: Fase 1
- Dia 4-5: Fase 2
- Dia 6: Fase 3
- Dia 7-8: Fase 4

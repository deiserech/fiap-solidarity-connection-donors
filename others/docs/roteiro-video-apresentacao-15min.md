# Roteiro de Video - Apresentacao de 15 Minutos

## Objetivo do video

Comprovar que a solucao atende o enunciado com simplicidade:

1. Dois microsservicos.
2. Comunicacao assincrona via Azure Service Bus.
3. Processamento de doacao fora da API principal.
4. Kubernetes, pipeline e observabilidade.

## Preparacao antes de gravar

1. Deixar abas prontas:
   - Diagrama de arquitetura.
   - Azure Pipeline (ultima execucao verde).
   - Terminal com `kubectl get pods -w`.
   - New Relic com dashboard aberto.
   - Postman ou Swagger para chamadas.
2. Garantir dados minimos:
   - 1 campanha ativa.
   - 1 doador para login.
3. Testar uma rodada completa antes de gravar.

## Cronograma sugerido (15:00)

## 00:00 - 01:00 | Abertura

Falar:

1. Nome do projeto e objetivo.
2. Problema que a ONG tinha.
3. Escopo do MVP entregue.

Tela:

1. Slide simples com titulo do projeto.

## 01:00 - 03:00 | Arquitetura

Falar:

1. Servico A: Users/Campaigns/Core API.
2. Servico B: Donations (worker orientado a eventos).
3. Azure Service Bus entre os servicos.
4. Cada servico com banco proprio.

Tela:

1. Diagrama de arquitetura em `others/docs/arquitetura-microsservicos-doacoes.md`.

Mensagem-chave:

1. Sem acesso ao banco do outro microsservico.

## 03:00 - 05:00 | Requisitos obrigatorios mapeados

Falar (objetivo e curto):

1. JWT + RBAC (`NgoManager`, `Donor`).
2. CRUD de campanha com regras de negocio.
3. API publica de campanhas ativas com total arrecadado.
4. Doacao assincrona por evento.

Tela:

1. Lista de endpoints no Swagger/Postman.

## 05:00 - 07:00 | Pipeline CI

Falar:

1. Pipeline dispara em push na branch principal.
2. Etapas: build, test, build/push de imagem Docker.

Tela:

1. Execucao verde do pipeline Core.
2. Execucao verde do pipeline Donations.

Comprovacao minima:

1. Mostrar logs de build.
2. Mostrar etapa de Docker image concluida.

## 07:00 - 09:00 | Kubernetes

Falar:

1. Deploy dos dois servicos no cluster.
2. ConfigMap + Secrets usados para configuracao.

Tela:

1. `kubectl get pods`
2. `kubectl get deploy`
3. `kubectl get svc`

Comando sugerido:

```bash
kubectl get pods -n <namespace>
kubectl get deploy -n <namespace>
kubectl get svc -n <namespace>
```

## 09:00 - 13:00 | Demo funcional fim a fim

Fluxo (sequencial):

1. Login (obter JWT).
2. Criar campanha (ou usar uma ativa ja criada).
3. Enviar doacao em `POST /api/donations` no Core.
4. Mostrar passagem da mensagem no Service Bus (portal ou ferramenta de monitoramento).
5. Consultar `GET /api/campaigns/public` e mostrar `total_raised_amount` atualizado.

Mensagem-chave durante demo:

1. A API nao atualiza total direto.
2. O worker processa e publica evento.
3. O Core aplica o resultado e atualiza o proprio banco.

## 13:00 - 14:00 | Observabilidade (New Relic)

Falar:

1. Telemetria dos dois servicos via OpenTelemetry + OTLP.
2. Dashboard com throughput, error rate e latencia p95.

Tela:

1. Dashboard New Relic com dados reais da demo.

## 14:00 - 15:00 | Encerramento

Falar:

1. Resumo dos requisitos atendidos.
2. Trade-offs de simplicidade do MVP.
3. Proximos passos (sem implementar agora).

## Checklist de evidencias obrigatorias na gravacao

1. Diagrama de arquitetura explicado.
2. Pipeline executando e gerando imagem.
3. Pods no Kubernetes rodando.
4. Dashboard New Relic com dados reais.
5. JWT obtido no login.
6. Doacao enviada.
7. Mensagem no Service Bus.
8. Total arrecadado atualizado na API publica.

## Plano de contingencia (se algo falhar durante gravacao)

1. Ter uma execucao de pipeline verde aberta em aba separada.
2. Ter uma campanha ativa previamente criada.
3. Ter token valido de backup.
4. Se Service Bus oscilar, mostrar logs dos consumers e repetir apenas a etapa de doacao.
5. Se New Relic atrasar ingestao, mostrar traces/transactions recentes e recarregar dashboard.

## Divisao sugerida em pair

1. Pessoa A (narracao tecnica): arquitetura, requisitos, pipeline.
2. Pessoa B (demonstracao ao vivo): kubernetes, chamadas de API, observabilidade.
3. Troca no minuto 7 para equilibrar participacao.

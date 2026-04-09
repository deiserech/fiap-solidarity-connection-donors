# Observabilidade e Rastreamento Distribuído com New Relic

## Objetivo

Implementar rastreamento distribuído (traces) de ponta a ponta entre os serviços da plataforma Fiap Cloud Games, usando:

- OpenTelemetry para coleta e propagação de contexto.
- New Relic como backend de observabilidade (via OTLP).

A ideia é que cada requisição de negócio (por exemplo, "criar usuário" ou "realizar pagamento") possua um `traceId` único, que acompanhe o fluxo por APIs, workers, Functions, filas e banco de dados.

---

## Decisões arquiteturais

- **Padrão de propagação**: usar o padrão W3C Trace Context (`traceparent` / `tracestate`).
- **Stack de observabilidade**:
  - Coleta: OpenTelemetry (tracing).
  - Backend: New Relic, recebendo dados via OTLP (gRPC).
- **Identidade dos serviços**:
  - Usar `service.name` para identificar cada componente:
    - `fiap-users-api`
    - `fiap-games-api`
    - `fiap-payments-api`
    - `fiap-payments-functions`
    - `fiap-payments-worker`
    - (outros conforme necessário)
  - Usar `deployment.environment` para distinguir ambientes: `dev`, `staging`, `prod`.
- **Configuração por projeto** (decisão para projeto de estudos):
  - Repetir a configuração de OpenTelemetry + New Relic em cada projeto (APIs, workers, Functions), ajustando apenas o `service.name` e detalhes pontuais.
  - Evitar criar uma biblioteca compartilhada neste momento para não aumentar a complexidade inicial.
  - Se a duplicação começar a incomodar, evoluir depois para um projeto compartilhado de observabilidade.

---

## Configuração base OpenTelemetry + New Relic

### Pacotes NuGet principais (APIs / Workers / Functions .NET)

Adicionar, conforme o tipo de aplicação:

- `OpenTelemetry`
- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore` (para APIs)
- `OpenTelemetry.Instrumentation.Http` (para chamadas HTTP entre serviços)
- `OpenTelemetry.Instrumentation.SqlClient` (se o serviço falar com banco via SQL Client)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` (OTLP exporter para New Relic)

Em alguns casos específicos (por exemplo Functions com bindings próprios), podem ser necessários pacotes adicionais, mas estes são a base.

### Configuração de exportação para New Relic (OTLP)

- Endpoint OTLP (gRPC): `https://otlp.nr-data.net:4317`
- Autenticação via header `api-key` com a licença do New Relic.
- Não armazenar a licença em código-fonte: usar variável de ambiente ou secret.

Exemplo de variáveis de ambiente:

```bash
NEW_RELIC_LICENSE_KEY=xxxxxx
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net:4317
OTEL_EXPORTER_OTLP_HEADERS="api-key=xxxxxx"
```

No código, podemos ler `NEW_RELIC_LICENSE_KEY` a partir da configuração/variáveis de ambiente.

---

## Como aplicar em APIs ASP.NET Core (.NET 7/8)

### 1. Instalar pacotes NuGet

Em cada projeto de API (por exemplo `FiapCloudGames.Api`, `FiapCloudGames.Games.Api`, `FiapCloudGames.Payments.Api`):

- Adicionar as referências:
  - `OpenTelemetry`
  - `OpenTelemetry.Extensions.Hosting`
  - `OpenTelemetry.Instrumentation.AspNetCore`
  - `OpenTelemetry.Instrumentation.Http`
  - `OpenTelemetry.Instrumentation.SqlClient` (se usar SQL Client)
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol`

### 2. Configurar OpenTelemetry no Program.cs

Em `Program.cs` (exemplo genérico de API):

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "fiap-games-api"; // ajustar por API
var environment = builder.Environment.EnvironmentName; // dev/staging/prod

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", environment),
                    }))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("https://otlp.nr-data.net:4317");
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                otlp.Headers = $"api-key={builder.Configuration["NEW_RELIC_LICENSE_KEY"]}";
            });
    });

// configuração restante da API
var app = builder.Build();

// middlewares, endpoints, etc.

app.Run();
```

Pontos importantes:

- `serviceName` deve ser único por serviço.
- A licença (`NEW_RELIC_LICENSE_KEY`) deve vir de variável de ambiente / secret e não ficar em código.
- A instrumentação de `AspNetCore` e `HttpClient` cuida da criação de spans para requests HTTP e chamadas via `HttpClient` automaticamente.

### 3. Uso de HttpClient

Para garantir propagação automática de contexto entre APIs:

- Usar `IHttpClientFactory` e `AddHttpClient` em vez de criar `new HttpClient()` manualmente em todo lugar.
- A instrumentação de `HttpClient` adicionará o header `traceparent` nas requisições de saída.

---

## Como aplicar em Workers / Background Services

Workers que rodam como `IHostedService` ou `BackgroundService` (por exemplo em `FiapCloudGames.Payments.Worker`) podem usar a mesma configuração base de OpenTelemetry.

### 1. Instalar pacotes

- Mesmos pacotes principais utilizados para as APIs, exceto `AspNetCore` se não houver pipeline HTTP.

### 2. Configurar no host

Exemplo em um `Program.cs` de worker:

```csharp
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

var serviceName = "fiap-payments-worker";
var environment = builder.Environment.EnvironmentName;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", environment),
                    }))
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("https://otlp.nr-data.net:4317");
                otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                otlp.Headers = $"api-key={builder.Configuration["NEW_RELIC_LICENSE_KEY"]}";
            });
    });

// registrar o BackgroundService / HostedService
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

### 3. Spans manuais em lógica de domínio

Para partes críticas do processamento (por exemplo, processamento de pagamento), podemos criar spans manuais:

```csharp
using System.Diagnostics;

public class PaymentWorker : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("FiapCloudGames.Payments");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = ActivitySource.StartActivity("ProcessarPagamento");

            // lógica de processamento

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

Esses spans aparecerão dentro do trace atual (se houver) ou criarão um novo trace (se não houver contexto pai).

---

## Como aplicar em Azure Functions

A configuração exata depende se as Functions estão em modelo in-process ou isolated. A recomendação atual é usar Functions .NET isolado com o mesmo padrão de host builder.

### 1. Instalar pacotes

- Mesmos pacotes de OpenTelemetry base, adicionando o que for específico para Functions se necessário.

### 2. Configurar no host das Functions (isolated)

Exemplo genérico de `Program.cs` para Functions isolado:

```csharp
using Microsoft.Azure.Functions.Worker.Extensions.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        // Configurações específicas de Functions, se necessário
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var environment = context.HostingEnvironment.EnvironmentName;
        var serviceName = "fiap-payments-functions";

        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
                            .AddAttributes(new[]
                            {
                                new KeyValuePair<string, object>("deployment.environment", environment),
                            }))
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri("https://otlp.nr-data.net:4317");
                        otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        otlp.Headers = $"api-key={configuration["NEW_RELIC_LICENSE_KEY"]}";
                    });
            });
    })
    .Build();

host.Run();
```

As Functions que fazem chamadas HTTP para outros serviços herdarão o contexto via `HttpClient` instrumentado. Para gatilhos de fila/Service Bus, podemos precisar manipular trace context manualmente nas mensagens (ver seção de propagação abaixo).

---

## Propagação de contexto entre serviços

### HTTP (APIs → APIs / APIs → Functions)

Com `AspNetCore` + `HttpClient` instrumentation:

- A API que recebe uma requisição HTTP cria uma `Activity` raiz com o `traceId` vindo do header `traceparent`, se existir.
- Ao fazer chamadas HTTP com `HttpClient`, o OpenTelemetry adiciona automaticamente os headers `traceparent` e `tracestate`.

Boas práticas:

- Sempre usar `IHttpClientFactory` (`services.AddHttpClient(...)`).
- Evitar `new HttpClient()` manual desacoplado do container.

### Mensageria (filas, Service Bus, etc.)

Se o SDK usado para filas/eventos não suportar propagação automática, podemos fazer manualmente.

#### No produtor (enviando mensagem)

```csharp
var activity = Activity.Current;
if (activity is not null)
{
    var traceparent = activity.Id; // W3C traceparent
    message.Properties["traceparent"] = traceparent;
}
```

#### No consumidor (processando mensagem)

```csharp
if (message.Properties.TryGetValue("traceparent", out var obj) &&
    obj is string parentId)
{
    var activity = new Activity("ProcessarMensagem");
    activity.SetParentId(parentId);
    activity.Start();
    try
    {
        // lógica de processamento
    }
    finally
    {
        activity.Stop();
    }
}
```

Assim, o processamento no worker/Function aparecerá como parte do mesmo trace no New Relic.

---

## Passo a passo de desenvolvimento (roadmap)

### Fase 0 – Preparação

1. Criar (ou revisar) conta e chave de licença do New Relic.
2. Definir nomes de serviço (`service.name`) para cada projeto.
3. Definir convenção de ambientes (`dev`, `staging`, `prod`) e como isso será configurado (EnvironmentName, variáveis, etc.).
4. Configurar variáveis de ambiente em desenvolvimento:
   - `NEW_RELIC_LICENSE_KEY`
   - `OTEL_EXPORTER_OTLP_ENDPOINT` (opcional, pode ficar no código por enquanto)
   - `OTEL_EXPORTER_OTLP_HEADERS` (opcional, se preferir padronizar via env vars).

### Fase 1 – Habilitar tracing em UMA API (ex.: Users)

1. Escolher uma API (por exemplo, Users) como piloto.
2. Instalar os pacotes NuGet de OpenTelemetry + OTLP exporter.
3. Configurar o tracing no `Program.cs` da API (conforme exemplo acima), com `service.name` apropriado.
4. Garantir que as chamadas HTTP dessa API para outros serviços usem `HttpClient` via `IHttpClientFactory`.
5. Rodar a API localmente e enviar algumas requisições.
6. Verificar no New Relic se os traces estão chegando e se o serviço aparece com o `service.name` esperado.

### Fase 2 – Expandir para outras APIs (Games e Payments)

1. Repetir a instalação dos pacotes e a configuração do OpenTelemetry nas APIs de Games e Payments.
2. Ajustar apenas:
   - `service.name`.
   - Quais instrumentações são necessárias (por exemplo, `SqlClient` só se usar SQL diretamente).
3. Validar um fluxo de negócio que passe por múltiplas APIs (ex.: login, compra de jogo) e garantir que o mesmo `traceId` conecte todas as chamadas no New Relic.

### Fase 3 – Integrar Workers e Functions

1. No projeto de Worker (por exemplo, `FiapCloudGames.Payments.Worker`):
   - Instalar pacotes de OpenTelemetry.
   - Configurar tracing no host (Program.cs), com `service.name` do worker.
   - Adicionar spans manuais em pontos críticos (ex.: processamento de mensagens, integração com gateways externos).
2. No projeto de Functions (por exemplo, `FiapCloudGames.Payments.Functions`):
   - Instalar pacotes de OpenTelemetry.
   - Configurar tracing no host das Functions (isolated), com `service.name` das Functions.
   - Se houver triggers por fila/mensageria, implementar propagação manual do `traceparent` nas mensagens quando necessário.
3. Validar um fluxo que envolva fila/worker/Function e garantir que todos participem do mesmo trace.

### Fase 4 – Refinamento e observabilidade de negócio

1. Identificar fluxos de alto valor (ex.: criação de usuário, compra de jogo, pagamento aprovado/negado).
2. Adicionar spans manuais com nomes de operação de negócio (ex.: `Users.CreateUser`, `Payments.AuthorizePayment`).
3. Adicionar atributos (tags) de negócio nos spans (sem dados sensíveis), por exemplo:
   - `user.type`, `payment.method`, `game.category`.
4. No New Relic, criar:
   - Dashboards baseados nesses spans.
   - Alertas/SLOs de tempo de resposta ou taxa de erro por cenário de negócio.

### Fase 5 – Evolução para biblioteca compartilhada (opcional)

1. Se a duplicação de configuração de OpenTelemetry entre os projetos começar a gerar manutenção pesada:
   - Criar um projeto compartilhado (por exemplo, `FiapCloudGames.Observability`).
   - Extrair nele métodos de extensão como `AddObservabilityTracing(this IServiceCollection services, IConfiguration configuration, string serviceName)`.
   - Referenciar esse projeto em todas as APIs, workers e Functions.
2. Garantir que as convenções de `service.name` e `deployment.environment` permaneçam consistentes.

---

## Checklist rápido

- [ ] Definidos nomes de serviço (`service.name`).
- [ ] Definidas convenções de ambiente (`deployment.environment`).
- [ ] Variáveis de ambiente de New Relic configuradas (dev/staging/prod).
- [ ] Pelo menos uma API com OpenTelemetry + OTLP configurados e reportando para o New Relic.
- [ ] Todas as APIs principais instrumentadas (Users, Games, Payments).
- [ ] Workers e Functions integrados ao tracing.
- [ ] Propagação de contexto em HTTP funcionando (headers `traceparent`/`tracestate`).
- [ ] Propagação de contexto em filas/mensageria implementada (quando aplicável).
- [ ] Spans de negócio principais criados.
- [ ] Dashboards/alertas básicos no New Relic configurados.

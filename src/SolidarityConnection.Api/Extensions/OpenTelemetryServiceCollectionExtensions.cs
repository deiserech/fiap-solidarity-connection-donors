using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SolidarityConnection.Api.Extensions
{
    public static class OpenTelemetryServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenTel(this IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

            OpenTelemetryServicesExtensions.AddOpenTelemetry(services)
                .ConfigureResource(resource => resource
                    .AddService(serviceName: "solidarity-connection-donors-identity-api")
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", environmentName)
                    }))
                .WithTracing(builder =>
                {
                    builder
                        .AddSource("SolidarityConnection.Application")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSqlClientInstrumentation()
                        .AddOtlpExporter(ConfigureOtlpExporter);
                })
                .WithLogging(builder =>
                {
                    builder.AddOtlpExporter(ConfigureOtlpExporter);
                })
                .WithMetrics(builder =>
                {
                    builder
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddOtlpExporter(ConfigureOtlpExporter);
                });

            return services;
        }

        private static void ConfigureOtlpExporter(OtlpExporterOptions options)
        {
            options.Endpoint = new Uri("https://otlp.nr-data.net:4317");
            options.Protocol = OtlpExportProtocol.Grpc;

            var newRelicKey = Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY");
            if (!string.IsNullOrWhiteSpace(newRelicKey))
            {
                options.Headers = $"api-key={newRelicKey}";
            }
        }
    }
}


using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SolidarityConnection.Donors.Identity.Api.Extensions
{
    public static class OpenTelemetryServiceCollectionExtensions
    {
        public static IServiceCollection AddFiapCloudGamesOpenTelemetry(this IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: "solidarity-connection-donors-identity-api")
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", environmentName)
                    }))
                .WithTracing(builder =>
                {
                    builder
                        .AddSource("SolidarityConnection.Donors.Identity.Application")
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


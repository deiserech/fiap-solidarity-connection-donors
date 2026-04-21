using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SolidarityConnection.Api.Extensions
{
    public static class OpenTelemetryServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenTel(this IServiceCollection services, IConfiguration configuration)
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
                        .AddOtlpExporter(options => ConfigureOtlpExporter(options, configuration));
                })
                .WithLogging(builder =>
                {
                    builder.AddOtlpExporter(options => ConfigureOtlpExporter(options, configuration));
                })
                .WithMetrics(builder =>
                {
                    builder
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddOtlpExporter(options => ConfigureOtlpExporter(options, configuration));
                });

            return services;
        }

        private static void ConfigureOtlpExporter(OtlpExporterOptions options, IConfiguration configuration)
        {
            var endpoint = configuration["NewRelic:OtlpEndpoint"] ?? "https://otlp.nr-data.net:4317";
            var protocol = configuration["NewRelic:Protocol"];

            options.Endpoint = new Uri(endpoint);
            options.Protocol = string.Equals(protocol, "http/protobuf", StringComparison.OrdinalIgnoreCase)
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;

            var newRelicKey = configuration["NewRelic:LicenseKey"]
                ?? Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY");
            if (!string.IsNullOrWhiteSpace(newRelicKey))
            {
                options.Headers = $"api-key={newRelicKey}";
            }
        }
    }
}


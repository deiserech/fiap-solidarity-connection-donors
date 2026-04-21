using System.Text.Json;
using Azure.Messaging.ServiceBus;
using SolidarityConnection.Application.Interfaces.Services;
using SolidarityConnection.Domain.Events;
using SolidarityConnection.Infrastructure.ServiceBus;
using SolidarityConnection.Shared.Tracing;

namespace SolidarityConnection.Api.BackgroundServices
{
    public class DonationProcessedConsumer : BackgroundService
    {
        private readonly IServiceBusClientWrapper _serviceBus;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DonationProcessedConsumer> _logger;
        private IServiceBusProcessor? _processor;

        public DonationProcessedConsumer(
            IServiceBusClientWrapper serviceBus,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<DonationProcessedConsumer> logger)
        {
            _serviceBus = serviceBus;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topic = _configuration["DONATION_PROCESSED_TOPIC"] ?? "donation-processed";
            var subscription = _configuration["DONATION_PROCESSED_SUBSCRIPTION"] ?? "solidarity-connection-core-api";

            try
            {
                _processor = _serviceBus.CreateProcessorWrapper(topic, subscription);

                _processor.ProcessMessageAsync += async args =>
                {
                    var message = args.Message;

                    using var activity = ServiceBusTracingHelper.StartConsumerActivity(
                        message,
                        "Core.DonationProcessedConsumer.Process",
                        topic,
                        subscription);

                    var body = message.Body.ToString();
                    var evt = JsonSerializer.Deserialize<DonationProcessedEvent>(
                        body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evt is null)
                    {
                        _logger.LogWarning("Received invalid DonationProcessedEvent payload.");
                        await args.CompleteMessageAsync(message);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

                    await campaignService.ApplyProcessedDonationAsync(
                        evt.DonationId,
                        evt.CampaignId,
                        evt.DonationAmount,
                        evt.Status);

                    await args.CompleteMessageAsync(message);
                };

                _processor.ProcessErrorAsync += ErrorHandler;
                await _processor.StartProcessingAsync(stoppingToken);
            }
            catch (ServiceBusException ex)
            {
                _logger.LogError(
                    ex,
                    "ServiceBus error creating/starting processor for topic {Topic}, subscription {Subscription}",
                    topic,
                    subscription);
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "DonationProcessedConsumer error");
            return Task.CompletedTask;
        }
    }
}
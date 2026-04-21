using SolidarityConnection.Application.Interfaces.Publishers;
using SolidarityConnection.Domain.Events;
using SolidarityConnection.Infrastructure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SolidarityConnection.Application.Publishers
{
    public class DonationRequestedEventPublisher : IDonationRequestedEventPublisher
    {
        private const string DonationRequestedTopic = "donation-requested";
        private readonly IServiceBusPublisher _busPublisher;
        private readonly ILogger<DonationRequestedEventPublisher> _logger;

        public DonationRequestedEventPublisher(
            IServiceBusPublisher busPublisher,
            ILogger<DonationRequestedEventPublisher> logger)
        {
            _busPublisher = busPublisher;
            _logger = logger;
        }

        public async Task PublishAsync(DonationRequestedEvent @event)
        {
            try
            {
                await _busPublisher.PublishAsync(@event, DonationRequestedTopic);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing DonationRequestedEvent. DonationId={DonationId}, CampaignId={CampaignId}",
                    @event.DonationId,
                    @event.CampaignId);
                throw;
            }
        }
    }
}
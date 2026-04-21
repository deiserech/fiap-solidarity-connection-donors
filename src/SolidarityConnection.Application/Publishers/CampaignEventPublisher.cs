using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Events;
using Microsoft.Extensions.Logging;
using SolidarityConnection.Application.Interfaces.Publishers;
using SolidarityConnection.Infrastructure.ServiceBus;

namespace SolidarityConnection.Application.Publishers
{
    public class CampaignEventPublisher : ICampaignEventPublisher
    {
        private readonly IServiceBusPublisher _busPublisher;
        private readonly ILogger<CampaignEventPublisher> _logger;
        private const string CampaignTopic = "campaigns-upserted";

        public CampaignEventPublisher(IServiceBusPublisher busPublisher, ILogger<CampaignEventPublisher> logger)
        {
            _busPublisher = busPublisher;
            _logger = logger;
        }

        public async Task PublishCampaignEventAsync(Campaign campaign, bool isRemoved = false)
        {
            DateTimeOffset? removedAt = isRemoved ? DateTimeOffset.UtcNow : null;
            var evt = new CampaignEvent(
                campaign.Id,
                campaign.Title,
                campaign.Status,
                campaign.GoalAmount,
                campaign.TotalAmountRaised,
                DateTimeOffset.UtcNow,
                removedAt);
            try
            {
                await _busPublisher.PublishAsync(evt, CampaignTopic);
            }
            catch (Exception e)
            {
                _logger.LogError("Error publishing event {Event}: {CampaignId}. Message: {Message}", nameof(CampaignEvent), campaign.Id, e.Message);
            }
        }
    }
}

using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Application.Interfaces.Publishers
{
    public interface ICampaignEventPublisher
    {
        Task PublishCampaignEventAsync(Campaign campaign, bool isRemoved = false);
    }
}
using SolidarityConnection.Domain.Enums;

namespace SolidarityConnection.Domain.Events
{
    public record CampaignEvent(
        Guid CampaignId,
        string Title,
        CampaignStatus Status,
        decimal GoalAmount,
        decimal TotalAmountRaised,
        DateTimeOffset? UpdatedAt,
        DateTimeOffset? RemovedAt
    );
}

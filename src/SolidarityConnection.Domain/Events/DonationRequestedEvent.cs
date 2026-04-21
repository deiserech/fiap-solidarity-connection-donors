namespace SolidarityConnection.Domain.Events
{
    public record DonationRequestedEvent(
        Guid DonationId,
        Guid CampaignId,
        Guid DonorId,
        decimal DonationAmount,
        DateTimeOffset RequestedAt);
}
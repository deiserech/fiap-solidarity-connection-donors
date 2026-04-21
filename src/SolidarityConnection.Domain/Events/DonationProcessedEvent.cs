namespace SolidarityConnection.Domain.Events
{
    public record DonationProcessedEvent(
        Guid DonationId,
        Guid CampaignId,
        decimal DonationAmount,
        string Status,
        DateTimeOffset ProcessedAt,
        string? ErrorMessage);
}
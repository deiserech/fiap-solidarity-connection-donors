namespace SolidarityConnection.Domain.Entities
{
    public class ProcessedDonation
    {
        public Guid DonationId { get; set; }
        public Guid CampaignId { get; set; }
        public decimal DonationAmount { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}
namespace SolidarityConnection.Domain.Entities
{
    public class ProcessedDonation
    {
        public Guid DonationId { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}
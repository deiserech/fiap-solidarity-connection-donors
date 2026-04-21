using SolidarityConnection.Domain.Enums;

namespace SolidarityConnection.Domain.Entities
{
    public class Campaign
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal GoalAmount { get; set; }
        public decimal TotalAmountRaised { get; set; }
        public CampaignStatus Status { get; set; } = CampaignStatus.Active;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

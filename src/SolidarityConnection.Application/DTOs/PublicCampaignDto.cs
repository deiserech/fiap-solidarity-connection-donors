namespace SolidarityConnection.Application.DTOs
{
    public class PublicCampaignDto
    {
        public string Title { get; set; } = string.Empty;

        public decimal GoalAmount { get; set; }

        public decimal TotalAmountRaised { get; set; }
    }
}
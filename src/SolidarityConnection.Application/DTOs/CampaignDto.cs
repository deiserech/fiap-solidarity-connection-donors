using System.ComponentModel.DataAnnotations;
using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Enums;

namespace SolidarityConnection.Application.DTOs
{
    public class CampaignDto
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal GoalAmount { get; set; }

        public CampaignStatus Status { get; set; } = CampaignStatus.Active;

        public static Campaign ToEntity(CampaignDto dto)
        {
            return new Campaign
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                GoalAmount = dto.GoalAmount,
                Status = dto.Status
            };
        }

        public static CampaignDto FromEntity(Campaign campaign)
        {
            return new CampaignDto
            {
                Title = campaign.Title,
                Description = campaign.Description,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                GoalAmount = campaign.GoalAmount,
                Status = campaign.Status
            };
        }
    }
}

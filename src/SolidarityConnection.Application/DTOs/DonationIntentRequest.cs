using System.ComponentModel.DataAnnotations;

namespace SolidarityConnection.Application.DTOs
{
    public class DonationIntentRequest
    {
        [Required]
        public Guid CampaignId { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal DonationAmount { get; set; }
    }
}
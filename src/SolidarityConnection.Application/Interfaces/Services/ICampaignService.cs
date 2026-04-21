using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Application.Interfaces.Services
{
    public interface ICampaignService
    {
        Task<Campaign?> GetCampaignByIdAsync(Guid id);
        Task<IEnumerable<Campaign>> GetActiveCampaignsAsync();
        Task<IEnumerable<PublicCampaignDto>> GetPublicCampaignsAsync();
        Task<Campaign> CreateCampaignAsync(CampaignDto campaign);
        Task<Campaign> UpdateCampaignAsync(Guid id, CampaignDto campaign);
        Task DeleteCampaignAsync(Guid id);
    }
}

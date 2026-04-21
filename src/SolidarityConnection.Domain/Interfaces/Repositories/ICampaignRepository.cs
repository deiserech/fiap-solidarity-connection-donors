using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Domain.Interfaces.Repositories
{
    public interface ICampaignRepository
    {
        Task<Campaign?> GetByIdAsync(Guid id);
        Task<IEnumerable<Campaign>> GetActiveAsync();
        Task<Campaign> CreateAsync(Campaign campaign);
        Task<Campaign> UpdateAsync(Campaign campaign);
        Task DeleteAsync(Campaign campaign);
        Task<bool> ApplyProcessedDonationAsync(Guid donationId, Guid campaignId, decimal donationAmount, DateTimeOffset processedAt);
    }
}

using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Enums;
using SolidarityConnection.Domain.Interfaces.Repositories;
using SolidarityConnection.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace SolidarityConnection.Infrastructure.Repositories
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CampaignRepository> _logger;

        public CampaignRepository(AppDbContext context, ILogger<CampaignRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<Campaign?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Looking up campaign by id {Id}", id);
            return await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Campaign>> GetActiveAsync()
        {
            _logger.LogDebug("Looking up active campaigns");
            return await _context.Campaigns
                .Where(c => c.Status == CampaignStatus.Active)
                .ToListAsync();
        }

        public async Task<Campaign> CreateAsync(Campaign campaign)
        {
            _logger.LogDebug("Creating campaign {Id}", campaign.Id);
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(campaign.Id) ?? campaign;
        }

        public async Task<Campaign> UpdateAsync(Campaign campaign)
        {
            _context.Entry(campaign).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(campaign.Id) ?? campaign;
        }

        public async Task DeleteAsync(Campaign campaign)
        {
            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ApplyProcessedDonationAsync(Guid donationId, Guid campaignId, decimal donationAmount, DateTimeOffset processedAt)
        {
            var alreadyProcessed = await _context.ProcessedDonations
                .AsNoTracking()
                .AnyAsync(p => p.DonationId == donationId);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Donation event already processed. DonationId={DonationId}", donationId);
                return false;
            }

            var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
            if (campaign is null)
            {
                _logger.LogWarning("Campaign not found while applying donation. CampaignId={CampaignId}", campaignId);
                return false;
            }

            campaign.TotalAmountRaised += donationAmount;
            campaign.UpdatedAt = DateTimeOffset.UtcNow;

            _context.ProcessedDonations.Add(new ProcessedDonation
            {
                DonationId = donationId,
                ProcessedAt = processedAt
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

using Microsoft.Extensions.Logging;
using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Application.Interfaces.Publishers;
using SolidarityConnection.Application.Interfaces.Services;
using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Enums;
using SolidarityConnection.Domain.Interfaces.Repositories;
using SolidarityConnection.Shared.Tracing;

namespace SolidarityConnection.Application.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ILogger<CampaignService> _logger;

        public CampaignService(ICampaignRepository campaignRepository, ILogger<CampaignService> logger)
        {
            _campaignRepository = campaignRepository;
            _logger = logger;
        }

        public async Task<Campaign?> GetCampaignByIdAsync(Guid id)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(CampaignService)}.GetCampaignByIdAsync");
            _logger.LogInformation("Looking up campaign by id {Id}", id);
            return await _campaignRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Campaign>> GetActiveCampaignsAsync()
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(CampaignService)}.GetActiveCampaignsAsync");
            _logger.LogInformation("Looking up active campaigns");
            return await _campaignRepository.GetActiveAsync();
        }

        public async Task<IEnumerable<PublicCampaignDto>> GetPublicCampaignsAsync()
        {
            var campaigns = await GetActiveCampaignsAsync();
            return campaigns.Select(campaign => new PublicCampaignDto
            {
                Title = campaign.Title,
                GoalAmount = campaign.GoalAmount,
                TotalAmountRaised = campaign.TotalAmountRaised
            });
        }

        public async Task<Campaign> CreateCampaignAsync(CampaignDto dto)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(CampaignService)}.CreateCampaignAsync");
            _logger.LogInformation("Creating campaign {Title}", dto.Title);

            ValidateCampaign(dto);

            var campaign = CampaignDto.ToEntity(dto);
            campaign.Status = campaign.Status == 0 ? CampaignStatus.Active : campaign.Status;
            campaign.CreatedAt = DateTimeOffset.UtcNow;
            campaign.UpdatedAt = DateTimeOffset.UtcNow;

            var created = await _campaignRepository.CreateAsync(campaign);
            _logger.LogInformation("Campaign created successfully: {Id}", created.Id);

            return created;
        }

        public async Task<Campaign> UpdateCampaignAsync(Guid id, CampaignDto dto)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(CampaignService)}.UpdateCampaignAsync");
            var campaign = await _campaignRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("Campaign not found.");

            ValidateCampaign(dto);

            campaign.Title = dto.Title;
            campaign.Description = dto.Description;
            campaign.StartDate = dto.StartDate;
            campaign.EndDate = dto.EndDate;
            campaign.GoalAmount = dto.GoalAmount;
            campaign.Status = dto.Status;
            campaign.UpdatedAt = DateTimeOffset.UtcNow;
            var updated = await _campaignRepository.UpdateAsync(campaign);
            _logger.LogInformation("Campaign updated successfully: {Id}", updated.Id);

            return updated;
        }

        public async Task DeleteCampaignAsync(Guid id)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(CampaignService)}.DeleteCampaignAsync");
            var campaign = await _campaignRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("Campaign not found.");

            await _campaignRepository.DeleteAsync(campaign);
        }

        public async Task ApplyProcessedDonationAsync(Guid donationId, Guid campaignId, decimal donationAmount, string status)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(CampaignService)}.ApplyProcessedDonationAsync");

            if (!string.Equals(status, "Processed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping donation {DonationId} with status {Status}", donationId, status);
                return;
            }

            if (donationAmount <= 0)
            {
                _logger.LogWarning("Skipping donation {DonationId} due to non-positive amount {Amount}", donationId, donationAmount);
                return;
            }

            var applied = await _campaignRepository.ApplyProcessedDonationAsync(
                donationId,
                campaignId,
                donationAmount,
                DateTimeOffset.UtcNow);

            if (!applied)
            {
                _logger.LogInformation(
                    "Donation event not applied (already processed or campaign missing). DonationId={DonationId}, CampaignId={CampaignId}",
                    donationId,
                    campaignId);
                return;
            }

            _logger.LogInformation(
                "Campaign total updated from donation event. DonationId={DonationId}, CampaignId={CampaignId}, Amount={Amount}",
                donationId,
                campaignId,
                donationAmount);
        }

        private static void ValidateCampaign(CampaignDto campaign)
        {
            if (campaign.GoalAmount <= 0)
                throw new ArgumentException("GoalAmount must be greater than zero.");

            if (campaign.EndDate < DateTime.UtcNow)
                throw new ArgumentException("EndDate cannot be in the past.");

            if (campaign.StartDate > campaign.EndDate)
                throw new ArgumentException("StartDate must be earlier than or equal to EndDate.");
        }
    }
}

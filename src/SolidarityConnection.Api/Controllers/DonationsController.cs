using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Application.Interfaces.Publishers;
using SolidarityConnection.Application.Interfaces.Services;
using SolidarityConnection.Domain.Enums;
using SolidarityConnection.Domain.Events;

namespace SolidarityConnection.Api.Controllers
{
    [ApiController]
    [Route("api/donations")]
    [Authorize(Roles = "Donor")]
    [Produces("application/json")]
    public class DonationsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly IDonationRequestedEventPublisher _donationRequestedPublisher;

        public DonationsController(
            ICampaignService campaignService,
            IDonationRequestedEventPublisher donationRequestedPublisher)
        {
            _campaignService = campaignService;
            _donationRequestedPublisher = donationRequestedPublisher;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateDonationIntent([FromBody] DonationIntentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(requesterId, out var donorId))
            {
                return Unauthorized();
            }

            var campaign = await _campaignService.GetCampaignByIdAsync(request.CampaignId);
            if (campaign is null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            if (campaign.Status != CampaignStatus.Active)
            {
                return BadRequest(new { message = "Donations are allowed only for active campaigns" });
            }

            var donationRequestedEvent = new DonationRequestedEvent(
                Guid.NewGuid(),
                request.CampaignId,
                donorId,
                request.DonationAmount,
                DateTimeOffset.UtcNow);

            await _donationRequestedPublisher.PublishAsync(donationRequestedEvent);

            return Accepted(new
            {
                donationRequestedEvent.DonationId,
                donationRequestedEvent.CampaignId,
                donationRequestedEvent.DonorId,
                donationRequestedEvent.DonationAmount,
                donationRequestedEvent.RequestedAt,
                status = "Received"
            });
        }
    }
}
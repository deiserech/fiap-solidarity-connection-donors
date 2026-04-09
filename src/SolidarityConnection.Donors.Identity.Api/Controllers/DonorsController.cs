using System.Security.Claims;
using SolidarityConnection.Donors.Identity.Application.DTOs;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SolidarityConnection.Donors.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/donors")]
    [Authorize]
    [Produces("application/json")]
    public class DonorsController : ControllerBase
    {
        private readonly IUserService _service;

        public DonorsController(IUserService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "NgoManager")]
        [ProducesResponseType(typeof(IEnumerable<DonorDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDonors()
        {
            var donors = await _service.GetAllAsync();
            return Ok(donors.Select(DonorDto.FromEntity));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "NgoManager,Donor")]
        [ProducesResponseType(typeof(DonorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDonor(Guid id)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("NgoManager") && requesterId != id.ToString())
            {
                return Forbid();
            }

            var donor = await _service.GetByIdAsync(id);
            if (donor is null)
            {
                return NotFound();
            }

            return Ok(DonorDto.FromEntity(donor));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "NgoManager,Donor")]
        [ProducesResponseType(typeof(DonorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDonor(Guid id, [FromBody] UpdateDonorRequest request)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("NgoManager") && requesterId != id.ToString())
            {
                return Forbid();
            }

            try
            {
                var donor = await _service.UpdateAsync(id, request);
                return Ok(DonorDto.FromEntity(donor));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "NgoManager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateDonor(Guid id)
        {
            var deactivated = await _service.DeactivateAsync(id);
            if (!deactivated)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize(Roles = "NgoManager,Donor")]
        [ProducesResponseType(typeof(DonorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Me()
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(requesterId, out var donorId))
            {
                return Unauthorized();
            }

            var donor = await _service.GetByIdAsync(donorId);
            if (donor is null)
            {
                return NotFound();
            }

            return Ok(DonorDto.FromEntity(donor));
        }
    }
}


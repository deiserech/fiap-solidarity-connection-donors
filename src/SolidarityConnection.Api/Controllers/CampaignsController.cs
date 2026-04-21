using Microsoft.AspNetCore.Mvc;
using SolidarityConnection.Api.Extensions;
using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
namespace SolidarityConnection.Api.Controllers
{
    /// <summary>
    /// Controller responsible for campaign management
    /// </summary>
    [ApiController]
    [Route("api/campaigns")]
    [Authorize(Roles = "NgoManager")]
    [Produces("application/json")]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;

        public CampaignsController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        /// <summary>
        /// Obtém todas as campanhas ativas para o endpoint público
        /// </summary>
        /// <returns>Lista de campanhas ativas</returns>
        /// <response code="200">Retorna a lista de campanhas ativas</response>
        /// <response code="400">Erro na solicitação</response>
        [HttpGet("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PublicCampaignDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<PublicCampaignDto>>> GetPublicCampaignsAsync()
        {
            var campaigns = await _campaignService.GetPublicCampaignsAsync();
            return Ok(campaigns);
        }

        /// <summary>
        /// Obtém uma campanha específica pelo identificador
        /// </summary>
        /// <param name="id">Identificador da campanha</param>
        /// <returns>Dados da campanha</returns>
        /// <response code="200">Retorna a campanha encontrada</response>
        /// <response code="404">Campanha não encontrada</response>
        /// <response code="400">Erro na solicitação</response>
        [HttpGet("{id:guid}", Name = "GetCampaignAsync")]
        [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCampaignAsync(Guid id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign is null)
                return this.NotFoundProblem("Campaign not found", $"Campaign with id {id} was not found.");

            return Ok(CampaignDto.FromEntity(campaign));
        }

        /// <summary>
        /// Cria uma nova campanha
        /// </summary>
        /// <param name="campaign">Dados da nova campanha</param>
        /// <returns>Campanha criada</returns>
        /// <response code="201">Campanha criada com sucesso</response>
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<CampaignDto>> CreateCampaignAsync([FromBody] CampaignDto campaign)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdCampaign = await _campaignService.CreateCampaignAsync(campaign);
            return CreatedAtRoute("GetCampaignAsync", new { id = createdCampaign.Id }, CampaignDto.FromEntity(createdCampaign));
        }

        /// <summary>
        /// Atualiza uma campanha existente
        /// </summary>
        /// <param name="id">Identificador da campanha</param>
        /// <param name="campaign">Dados atualizados da campanha</param>
        /// <returns>Campanha atualizada</returns>
        /// <response code="200">Campanha atualizada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Campanha não encontrada</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCampaignAsync(Guid id, [FromBody] CampaignDto campaign)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingCampaign = await _campaignService.GetCampaignByIdAsync(id);
            if (existingCampaign is null)
                return this.NotFoundProblem("Campaign not found", $"Campaign with id {id} was not found.");

            var updatedCampaign = await _campaignService.UpdateCampaignAsync(id, campaign);
            return Ok(CampaignDto.FromEntity(updatedCampaign));
        }


        /// <summary>
        /// Remove uma campanha
        /// </summary>
        /// <param name="id">Identificador da campanha a ser removida</param>
        /// <returns>Resposta vazia em caso de sucesso</returns>
        /// <response code="204">Campanha removida com sucesso</response>
        /// <response code="404">Campanha não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCampaignAsync(Guid id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign is null)
                return this.NotFoundProblem("Campaign not found", $"Campaign with id {id} was not found.");

            await _campaignService.DeleteCampaignAsync(id);
            return NoContent();
        }
    }
}

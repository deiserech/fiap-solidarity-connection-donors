using SolidarityConnection.Donors.Identity.Application.DTOs;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Services;
using SolidarityConnection.Donors.Identity.Application.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SolidarityConnection.Donors.Identity.Api.Controllers
{
    /// <summary>
    /// Controller responsÃ¡vel pela autenticaÃ§Ã£o e registro de usuÃ¡rios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// Realiza o login de um usuÃ¡rio no sistema
        /// </summary>
        /// <param name="loginDto">Dados de login (email e senha)</param>
        /// <returns>Token JWT e informaÃ§Ãµes do usuÃ¡rio autenticado</returns>
        /// <response code="200">Login realizado com sucesso</response>
        /// <response code="400">Dados de entrada invÃ¡lidos</response>
        /// <response code="401">Email ou senha invÃ¡lidos</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
       public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.Login(loginDto);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Email ou senha invÃ¡lidos" });
            }

            return Ok(result);
        }

        /// <summary>
        /// Registra um novo doador no sistema
        /// </summary>
        /// <param name="registerDto">Dados para registro do doador</param>
        /// <returns>Token JWT e informaÃ§Ãµes do doador registrado</returns>
        /// <response code="200">Doador registrado com sucesso</response>
        /// <response code="400">Dados invÃ¡lidos, senha nÃ£o atende aos critÃ©rios ou email jÃ¡ estÃ¡ em uso</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDonorRequest registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
 
            var errors = ValidationHelper.ValidateRegisterEntries(registerDto.Password, registerDto.Email, registerDto.Cpf);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("RegisterDonorRequest", error);
                }
                return BadRequest(ModelState);
            }

            var result = await _authService.Register(registerDto);
            
            if (result == null)
            {
                return BadRequest(new { message = "Email or CPF already in use" });
            }

            return Ok(result);
        }

        [HttpPost("managers")]
        [Authorize(Roles = "NgoManager")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateManager([FromBody] CreateManagerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var errors = ValidationHelper.ValidateRegisterEntries(request.Password, request.Email);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("CreateManagerRequest", error);
                }

                return BadRequest(ModelState);
            }

            var result = await _authService.CreateManager(request);
            if (result is null)
            {
                return BadRequest(new { message = "Email already in use" });
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}


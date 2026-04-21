using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Application.Interfaces.Services;
using SolidarityConnection.Application.Utils;

namespace SolidarityConnection.Api.Controllers
{
    /// <summary>
    /// Controller responsável pela autenticaÃ§Ã£o e registro de usuários
    /// </summary>
    [ApiController]
    [Authorize]
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
        /// Realiza o login de um usuário no sistema
        /// </summary>
        /// <param name="loginDto">Dados de login (email e senha)</param>
        /// <returns>Token JWT e informaÃ§Ãµes do usuário autenticado</returns>
        /// <response code="200">Login realizado com sucesso</response>
        /// <response code="400">Dados de entrada inválidos</response>
        /// <response code="401">Email ou senha inválidos</response>
        [HttpPost("login")]
        [AllowAnonymous]
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
                return Unauthorized(new { message = "Email ou senha inválidos" });
            }

            return Ok(result);
        }

        /// <summary>
        /// Registra um novo doador no sistema
        /// </summary>
        /// <param name="registerDto">Dados para registro do doador</param>
        /// <returns>Token JWT e informaÃ§Ãµes do doador registrado</returns>
        /// <response code="200">Doador registrado com sucesso</response>
        /// <response code="400">Dados inválidos, senha nÃ£o atende aos critÃ©rios ou email já está em uso</response>
        [HttpPost("register")]
        [AllowAnonymous]
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


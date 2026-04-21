using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Application.Interfaces.Publishers;
using SolidarityConnection.Application.Interfaces.Services;
using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Enums;
using SolidarityConnection.Domain.Interfaces.Repositories;
using SolidarityConnection.Shared.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace SolidarityConnection.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IUserEventPublisher _userEventPublisher;


        public AuthService(IUserRepository userRepository, IConfiguration configuration, ILogger<AuthService> logger, IUserEventPublisher userEventPublisher)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
            _userEventPublisher = userEventPublisher;
        }

        public async Task<AuthResponseDto?> Login(LoginDto loginDto)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(AuthService)}.Login");
            _logger.LogInformation("Tentativa de login para o email: {Email}", loginDto.Email);
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null || !user.VerifyPassword(loginDto.Password))
            {
                _logger.LogWarning("Falha no login para o email: {Email}", loginDto.Email);
                return null;
            }

            var token = GenerateJwtToken(user);

            _logger.LogInformation("Login realizado com sucesso para o email: {Email}", loginDto.Email);
            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString()
            };
        }

        public async Task<AuthResponseDto?> Register(RegisterDonorRequest registerDto)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(AuthService)}.Register");
            _logger.LogInformation("Tentativa de registro para o email: {Email}", registerDto.Email);
            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                _logger.LogWarning("Registro falhou: email já existe: {Email}", registerDto.Email);
                return null;
            }

            if (await _userRepository.CpfExistsAsync(registerDto.Cpf))
            {
                _logger.LogWarning("Registro falhou: CPF já existe: {Cpf}", registerDto.Cpf);
                return null;
            }

            var user = new User
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                Cpf = registerDto.Cpf,
                Role = UserRole.Donor,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            user.SetPassword(registerDto.Password);

            var created = await _userRepository.CreateAsync(user);
            await _userEventPublisher.PublishUserEventAsync(created);

            var token = GenerateJwtToken(user);

            _logger.LogInformation("Usuário registrado com sucesso: {Email}", registerDto.Email);
            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString()
            };
        }

        public async Task<AuthResponseDto?> CreateManager(CreateManagerRequest request)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(AuthService)}.CreateManager");

            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                _logger.LogWarning("CriaÃ§Ã£o de manager falhou: email já existe: {Email}", request.Email);
                return null;
            }

            var manager = new User
            {
                Name = request.Name,
                Email = request.Email,
                Cpf = string.Empty,
                Role = UserRole.NgoManager,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            manager.SetPassword(request.Password);

            await _userRepository.CreateAsync(manager);
            await _userEventPublisher.PublishUserEventAsync(manager);

            var token = GenerateJwtToken(manager);

            return new AuthResponseDto
            {
                Token = token,
                Email = manager.Email,
                Name = manager.Name,
                Role = manager.Role.ToString()
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");

            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey is missing or too short. It must be at least 32 characters long.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


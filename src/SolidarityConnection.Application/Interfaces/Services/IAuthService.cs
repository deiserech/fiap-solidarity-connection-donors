using SolidarityConnection.Application.DTOs;

namespace SolidarityConnection.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> Login(LoginDto loginDto);
        Task<AuthResponseDto?> Register(RegisterDonorRequest registerDto);
        Task<AuthResponseDto?> CreateManager(CreateManagerRequest request);
    }
}


using SolidarityConnection.Donors.Identity.Application.DTOs;

namespace SolidarityConnection.Donors.Identity.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> Login(LoginDto loginDto);
        Task<AuthResponseDto?> Register(RegisterDonorRequest registerDto);
        Task<AuthResponseDto?> CreateManager(CreateManagerRequest request);
    }
}


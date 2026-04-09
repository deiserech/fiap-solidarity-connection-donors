using SolidarityConnection.Donors.Identity.Application.DTOs;
using SolidarityConnection.Donors.Identity.Domain.Entities;

namespace SolidarityConnection.Donors.Identity.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByCpfAsync(string cpf);
        Task<User> UpdateAsync(Guid id, UpdateDonorRequest request);
        Task<bool> DeactivateAsync(Guid id);
        Task<IReadOnlyCollection<User>> GetAllAsync();
    }
}


using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Application.Interfaces.Services
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


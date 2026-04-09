using SolidarityConnection.Donors.Identity.Domain.Entities;

namespace SolidarityConnection.Donors.Identity.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByCpfAsync(string cpf);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CpfExistsAsync(string cpf);
        Task<IReadOnlyCollection<User>> GetAllAsync();
    }
}


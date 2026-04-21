using Microsoft.Extensions.Logging;
using SolidarityConnection.Shared.Tracing;
using SolidarityConnection.Domain.Interfaces.Repositories;
using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Application.Interfaces.Services;
using SolidarityConnection.Application.DTOs;
using SolidarityConnection.Application.Interfaces.Publishers;

namespace SolidarityConnection.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IUserEventPublisher _userEventPublisher;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository repo, ILogger<UserService> logger, IUserEventPublisher userEventPublisher)
        {
            _repo = repo;
            _logger = logger;
            _userEventPublisher = userEventPublisher;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(UserService)}.GetByIdAsync");
            _logger.LogInformation("Buscando usuário por ID: {Id}", id);
            return await _repo.GetByIdAsync(id);
        }

        public async Task<User?> GetByCpfAsync(string cpf)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(UserService)}.GetByCpfAsync");
            _logger.LogInformation("Buscando usuário por cpf");
            return await _repo.GetByCpfAsync(cpf);
        }

        public async Task<User> UpdateAsync(Guid id, UpdateDonorRequest request)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(UserService)}.UpdateAsync");

            var user = await _repo.GetByIdAsync(id)
                ?? throw new InvalidOperationException("Donor not found.");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailInUse = await _repo.EmailExistsAsync(request.Email);
                if (emailInUse)
                {
                    throw new InvalidOperationException("E-mail já está em uso.");
                }

                user.Email = request.Email;
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            return await _repo.UpdateAsync(user);
        }

        public async Task<bool> DeactivateAsync(Guid id)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(UserService)}.DeactivateAsync");

            var user = await _repo.GetByIdAsync(id);
            if (user is null)
            {
                return false;
            }

            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _repo.UpdateAsync(user);
            await _userEventPublisher.PublishUserEventAsync(user, true);

            return true;
        }

        public async Task<IReadOnlyCollection<User>> GetAllAsync()
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(UserService)}.GetAllAsync");
            _logger.LogInformation("Buscando lista de usuários");
            return await _repo.GetAllAsync();
        }

    }
}

using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Interfaces.Repositories;
using SolidarityConnection.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SolidarityConnection.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Buscando usuário por ID: {Id}", id);
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            _logger.LogDebug("Buscando usuário por email: {Email}", email);
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByCpfAsync(string cpf)
        {
            _logger.LogDebug("Buscando usuário por CPF");
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Cpf == cpf);
        }

        public async Task<User> CreateAsync(User user)
        {
            _logger.LogDebug("Criando usuário: {Email}", user.Email);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(user.Id) ?? user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> CpfExistsAsync(string cpf)
        {
            return await _context.Users.AnyAsync(u => u.Cpf == cpf);
        }

        public async Task<IReadOnlyCollection<User>> GetAllAsync()
        {
            _logger.LogDebug("Buscando lista de usuários");
            var users = await _context.Users.AsNoTracking().ToListAsync();
            return users;
        }

    }
}


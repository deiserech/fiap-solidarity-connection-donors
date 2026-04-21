using SolidarityConnection.Domain.Enums;

namespace SolidarityConnection.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string Cpf { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public required UserRole Role { get; set; } = UserRole.Donor;
        public string PasswordHash { get; set; } = string.Empty;

        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(PasswordHash))
            {
                return false;
            }
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }

        public void SetPassword(string password)
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}



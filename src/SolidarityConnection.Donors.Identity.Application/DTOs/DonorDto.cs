using SolidarityConnection.Donors.Identity.Domain.Entities;
using SolidarityConnection.Donors.Identity.Domain.Enums;

namespace SolidarityConnection.Donors.Identity.Application.DTOs
{
    public class DonorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public IdentityRole Role { get; set; }

        public static DonorDto FromEntity(User user)
        {
            return new DonorDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Cpf = user.Cpf,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Role = user.Role
            };
        }
    }
}


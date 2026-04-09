using SolidarityConnection.Donors.Identity.Application.DTOs;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Publishers;
using SolidarityConnection.Donors.Identity.Application.Services;
using SolidarityConnection.Donors.Identity.Domain.Entities;
using SolidarityConnection.Donors.Identity.Domain.Enums;
using SolidarityConnection.Donors.Identity.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace SolidarityConnection.Donors.Identity.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<IUserEventPublisher> _publisher = new();
    private readonly Mock<ILogger<UserService>> _logger = new();

    private UserService CreateService() => new(_repo.Object, _logger.Object, _publisher.Object);

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Name = "U", Email = "u@x.com", Role = IdentityRole.Donor };
        _repo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        var svc = CreateService();

        // Act
        var result = await svc.GetByIdAsync(user.Id);

        // Assert
        result.ShouldBe(user);
        _repo.Verify(r => r.GetByIdAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task GetByCpfAsync_DelegatesToRepository()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Name = "U2", Email = "u2@x.com", Cpf = "12345678901", Role = IdentityRole.Donor };
        _repo.Setup(r => r.GetByCpfAsync(user.Cpf)).ReturnsAsync(user);
        var svc = CreateService();

        // Act
        var result = await svc.GetByCpfAsync(user.Cpf);

        // Assert
        result.ShouldBe(user);
        _repo.Verify(r => r.GetByCpfAsync(user.Cpf), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenDonorNotFound()
    {
        var svc = CreateService();

        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await svc.UpdateAsync(Guid.NewGuid(), new UpdateDonorRequest { Name = "X" }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAndPersists_WhenValid()
    {
        var existing = new User { Id = Guid.NewGuid(), Name = "Old", Email = "old@x.com", Cpf = "12345678901", Role = IdentityRole.Donor };

        _repo.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _repo.Setup(r => r.EmailExistsAsync("new@x.com")).ReturnsAsync(false);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var svc = CreateService();

        var request = new UpdateDonorRequest { Name = "New", Email = "new@x.com" };

        // Act
        var result = await svc.UpdateAsync(existing.Id, request);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe(request.Email);
        result.Name.ShouldBe(request.Name);
        _repo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Email == request.Email && u.Name == request.Name)), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsFalse_WhenNotFound()
    {
        var svc = CreateService();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await svc.DeactivateAsync(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_UpdatesAndPublishes_WhenFound()
    {
        var donor = new User { Id = Guid.NewGuid(), Name = "D", Email = "d@x.com", Cpf = "12345678901", Role = IdentityRole.Donor, IsActive = true };
        var svc = CreateService();

        _repo.Setup(r => r.GetByIdAsync(donor.Id)).ReturnsAsync(donor);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await svc.DeactivateAsync(donor.Id);

        result.ShouldBeTrue();
        donor.IsActive.ShouldBeFalse();
        _publisher.Verify(p => p.PublishUserEventAsync(It.IsAny<User>(), true), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), Name = "U1", Email = "u1@x.com", Role = IdentityRole.Donor },
            new User { Id = Guid.NewGuid(), Name = "U2", Email = "u2@x.com", Role = IdentityRole.Donor }
        };

        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        var svc = CreateService();

        // Act
        var result = await svc.GetAllAsync();

        // Assert
        result.ShouldBe(users);
        _repo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}


using System.Collections.Generic;
using SolidarityConnection.Donors.Identity.Application.DTOs;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Publishers;
using SolidarityConnection.Donors.Identity.Application.Services;
using SolidarityConnection.Donors.Identity.Domain.Entities;
using SolidarityConnection.Donors.Identity.Domain.Enums;
using SolidarityConnection.Donors.Identity.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace SolidarityConnection.Donors.Identity.Tests.Services;

public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepo = new();
    private Mock<ILogger<AuthService>> _logger = new();
    private Mock<IUserEventPublisher> _userEventPublisher = new();


    private IConfiguration BuildConfiguration(string secretKey)
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = secretKey,
            ["JwtSettings:Issuer"] = "unit-tests",
            ["JwtSettings:Audience"] = "unit-tests-aud",
            ["JwtSettings:ExpiryInMinutes"] = "60",
        };

        return new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenUserNotFound()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        var svc = new AuthService(_userRepo.Object, BuildConfiguration(new string('a', 32)), _logger.Object, _userEventPublisher.Object);

        var result = await svc.Login(new LoginDto { Email = "no@u.com", Password = "x" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenPasswordInvalid()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@x.com", Role = IdentityRole.Donor };
        // PasswordHash is empty -> VerifyPassword returns false
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var svc = new AuthService(_userRepo.Object, BuildConfiguration(new string('a', 32)), _logger.Object, _userEventPublisher.Object);

        var result = await svc.Login(new LoginDto { Email = user.Email, Password = "wrong" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Login_ReturnsAuthResponse_WhenSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@x.com", Role = IdentityRole.NgoManager };
        user.SetPassword("P@ssword1");

        _userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var svc = new AuthService(_userRepo.Object, BuildConfiguration(new string('b', 64)), _logger.Object, _userEventPublisher.Object);

        var result = await svc.Login(new LoginDto { Email = user.Email, Password = "P@ssword1" });

        result.ShouldNotBeNull();
        result!.Email.ShouldBe(user.Email);
        result.Token.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Throws_WhenSecretTooShort()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@x.com", Role = IdentityRole.NgoManager };
        user.SetPassword("P@ssword1");
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var svc = new AuthService(_userRepo.Object, BuildConfiguration("short"), _logger.Object, _userEventPublisher.Object);

        await Should.ThrowAsync<InvalidOperationException>(async () => await svc.Login(new LoginDto { Email = user.Email, Password = "P@ssword1" }));
    }

    [Fact]
    public async Task Register_ReturnsNull_WhenEmailExists()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var svc = new AuthService(_userRepo.Object, BuildConfiguration(new string('c', 32)), _logger.Object, _userEventPublisher.Object);

        var result = await svc.Register(new RegisterDonorRequest { Name = "X", Email = "e@x.com", Cpf = "12345678901", Password = "P@ssword1" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Register_ReturnsAuthResponse_WhenSuccess()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CpfExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var svc = new AuthService(_userRepo.Object, BuildConfiguration(new string('d', 64)), _logger.Object, _userEventPublisher.Object);

        var dto = new RegisterDonorRequest { Name = "New", Email = "new@x.com", Cpf = "12345678901", Password = "P@ssword1" };

        var result = await svc.Register(dto);

        result.ShouldNotBeNull();
        result!.Email.ShouldBe(dto.Email);
        result.Token.ShouldNotBeNullOrEmpty();
        _userRepo.Verify(r => r.CreateAsync(It.Is<User>(u => u.Email == dto.Email && u.Name == dto.Name)), Times.Once);
    }
}


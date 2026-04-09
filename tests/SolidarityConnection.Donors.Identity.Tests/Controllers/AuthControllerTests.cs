using SolidarityConnection.Donors.Identity.Api.Controllers;
using SolidarityConnection.Donors.Identity.Application.DTOs;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Services;
using Shouldly;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace SolidarityConnection.Donors.Identity.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();

    private static AuthController CreateController(Mock<IAuthService> authService)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

        var controller = new AuthController(authService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return controller;
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var controller = CreateController(_authService);
        controller.ModelState.AddModelError("Email", "Required");

        var dto = new LoginDto { Email = "a@a.com", Password = "P@ssword1" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var dto = new LoginDto { Email = "no@user.com", Password = "wrong" };
        _authService.Setup(s => s.Login(dto)).ReturnsAsync((AuthResponseDto?)null);

        var controller = CreateController(_authService);

        // Act
        var result = await controller.Login(dto);

        // Assert
        var unauth = result as UnauthorizedObjectResult;
        unauth.ShouldNotBeNull();
        var unauthMsg = unauth!.Value?.ToString();
        unauthMsg.ShouldNotBeNull();
        unauthMsg.ShouldContain("Email ou senha invÃ¡lidos");
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var dto = new LoginDto { Email = "user@x.com", Password = "P@ssword1" };
        var resp = new AuthResponseDto { Token = "t", Email = dto.Email, Name = "User" };
        _authService.Setup(s => s.Login(dto)).ReturnsAsync(resp);

        var controller = CreateController(_authService);

        // Act
        var result = await controller.Login(dto);

        // Assert
        var ok = result as OkObjectResult;
        ok.ShouldNotBeNull();
        var okVal = ok!.Value as AuthResponseDto;
        okVal.ShouldNotBeNull();
        okVal!.Email.ShouldBe(dto.Email);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var controller = CreateController(_authService);
        controller.ModelState.AddModelError("Name", "Required");

        var dto = new RegisterDonorRequest { Name = "x", Email = "x@x.com", Cpf = "12345678901", Password = "P@ssword1" };

        // Act
        var result = await controller.Register(dto);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange: choose password/email that fail ValidationHelper
        var dto = new RegisterDonorRequest { Name = "Bob", Email = "invalid-email", Cpf = "123", Password = "short" };
        var controller = CreateController(_authService);

        // Act
        var result = await controller.Register(dto);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailInUse()
    {
        // Arrange
        var dto = new RegisterDonorRequest { Name = "Jane", Email = "jane@x.com", Cpf = "12345678901", Password = "P@ssword1" };
        _authService.Setup(s => s.Register(dto)).ReturnsAsync((AuthResponseDto?)null);

        var controller = CreateController(_authService);

        // Act
        var result = await controller.Register(dto);

        // Assert
        var bad = result as BadRequestObjectResult;
        bad.ShouldNotBeNull();
        var badMsg = bad!.Value?.ToString();
        badMsg.ShouldNotBeNull();
        badMsg.ShouldContain("Email or CPF already in use");
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var dto = new RegisterDonorRequest { Name = "Sam", Email = "sam@x.com", Cpf = "12345678901", Password = "P@ssword1" };
        var resp = new AuthResponseDto { Token = "t", Email = dto.Email, Name = dto.Name };
        _authService.Setup(s => s.Register(dto)).ReturnsAsync(resp);

        var controller = CreateController(_authService);

        // Act
        var result = await controller.Register(dto);

        // Assert
        var ok = result as OkObjectResult;
        ok.ShouldNotBeNull();
        var okVal = ok!.Value as AuthResponseDto;
        okVal.ShouldNotBeNull();
        okVal!.Email.ShouldBe(dto.Email);
    }
}


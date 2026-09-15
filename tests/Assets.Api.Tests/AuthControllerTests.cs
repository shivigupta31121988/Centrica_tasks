using Assets.Api.Controllers;
using Assets.Api.Dtos;
using Assets.Api.Services;
using Assets.Domain.Auth;
using Assets.Infrastructure.Persistence;
using Assets.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Assets.Api.Tests;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            Mock.Of<ILogger<AuthController>>());
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRole()
    {
        var user = new User { Username = "admin1", PasswordHash = "hashed", Role = UserRole.Admin };
        _userRepository.Setup(r => r.GetByUsernameAsync("admin1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed")).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(user)).Returns("fake-jwt-token");

        var result = await _controller.Login(new LoginRequestDto { Username = "admin1", Password = "correct-password" }, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var body = ok!.Value as LoginResponseDto;
        body!.Token.Should().Be("fake-jwt-token");
        body.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized_AndDoesNotRevealWhy()
    {
        _userRepository.Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _controller.Login(new LoginRequestDto { Username = "ghost", Password = "whatever" }, CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var user = new User { Username = "trader1", PasswordHash = "hashed", Role = UserRole.Trader };
        _userRepository.Setup(r => r.GetByUsernameAsync("trader1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        var result = await _controller.Login(new LoginRequestDto { Username = "trader1", Password = "wrong" }, CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _jwtTokenService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}

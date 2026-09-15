using Assets.Domain.Auth;
using Assets.Infrastructure.Persistence;
using Assets.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Assets.Api.Tests;

public class ConfigUserRepositoryTests
{
    [Fact]
    public async Task GetByUsernameAsync_ReturnsConfiguredUser_WhenUsernameExists()
    {
        var settings = Options.Create(new AuthenticationSettings
        {
            Users =
            [
                new ConfiguredUser { Username = "admin1", Password = "@Admin1234", Role = nameof(UserRole.Admin) },
                new ConfiguredUser { Username = "trader1", Password = "@Trader1234", Role = nameof(UserRole.Trader) },
            ],
        });

        var repository = new ConfigUserRepository(settings, new BCryptPasswordHasher());

        var user = await repository.GetByUsernameAsync("admin1");

        user.Should().NotBeNull();
        user!.Username.Should().Be("admin1");
        user.Role.Should().Be(UserRole.Admin);
        user.PasswordHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsNull_WhenUsernameDoesNotExist()
    {
        var settings = Options.Create(new AuthenticationSettings
        {
            Users = [new ConfiguredUser { Username = "admin1", Password = "@Admin1234", Role = nameof(UserRole.Admin) }],
        });

        var repository = new ConfigUserRepository(settings, new BCryptPasswordHasher());

        var user = await repository.GetByUsernameAsync("ghost");

        user.Should().BeNull();
    }
}

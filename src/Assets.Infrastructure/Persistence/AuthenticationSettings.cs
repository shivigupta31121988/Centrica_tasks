using Assets.Domain.Auth;
using Assets.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Assets.Infrastructure.Persistence;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public List<ConfiguredUser> Users { get; set; } = [];
}

public sealed class ConfiguredUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = nameof(UserRole.Trader);
}

public sealed class ConfigUserRepository : IUserRepository
{
    private readonly AuthenticationSettings _settings;
    private readonly IPasswordHasher _passwordHasher;

    public ConfigUserRepository(IOptions<AuthenticationSettings> settings, IPasswordHasher passwordHasher)
    {
        _settings = settings.Value;
        _passwordHasher = passwordHasher;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var configuredUser = _settings.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        if (configuredUser is null)
        {
            return Task.FromResult<User?>(null);
        }

        var passwordHash = !string.IsNullOrWhiteSpace(configuredUser.PasswordHash)
            ? configuredUser.PasswordHash
            : _passwordHasher.Hash(configuredUser.Password);

        var role = Enum.TryParse<UserRole>(configuredUser.Role, true, out var parsedRole)
            ? parsedRole
            : UserRole.Trader;

        var user = new User
        {
            Username = configuredUser.Username,
            PasswordHash = passwordHash,
            Role = role,
        };

        return Task.FromResult<User?>(user);
    }
}

using Assets.Domain.Auth;

namespace Assets.Infrastructure.Persistence;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}

using Assets.Domain.Auth;
using MongoDB.Driver;

namespace Assets.Infrastructure.Persistence;

public sealed class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public MongoUserRepository(MongoContext context)
    {
        _collection = context.Users;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }
}

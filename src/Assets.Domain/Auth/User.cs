using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Assets.Domain.Auth;

/// <summary>
/// A provisioned system user. There is deliberately no self-service
/// registration flow - users (and their role) are created directly in
/// the database, e.g. via scripts/seed-users.js, so access is granted
/// or revoked purely through database changes.
/// </summary>
public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt hash - never the plaintext password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; } = UserRole.Trader;
}

namespace Assets.Domain.Auth;

/// <summary>
/// Admin: can view and create assets.
/// Trader: view only.
/// </summary>
public enum UserRole
{
    Trader = 0,
    Admin = 1,
}

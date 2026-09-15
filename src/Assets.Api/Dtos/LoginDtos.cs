using System.ComponentModel.DataAnnotations;

namespace Assets.Api.Dtos;

public sealed class LoginRequestDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

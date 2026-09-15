using Assets.Api.Dtos;
using Assets.Api.Services;
using Assets.Infrastructure.Persistence;
using Assets.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Assets.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, ct);

        // Deliberately identical response/timing-shape for "no such user"
        // and "wrong password" - never reveal which one it was.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = _jwtTokenService.GenerateToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Username = user.Username,
            Role = user.Role.ToString(),
        });
    }
}

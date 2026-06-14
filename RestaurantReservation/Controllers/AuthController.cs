using Microsoft.AspNetCore.Mvc;
using RestaurantReservation.Models.DTOs.Auth;
using RestaurantReservation.Services.Interfaces;

namespace RestaurantReservation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Registers a new customer account and returns a JWT.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto, CancellationToken ct)
        => Ok(await _auth.RegisterAsync(dto, ct));

    /// <summary>Authenticates a user and returns a JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct)
        => Ok(await _auth.LoginAsync(dto, ct));
}

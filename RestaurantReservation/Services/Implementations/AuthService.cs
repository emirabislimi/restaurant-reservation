using AutoMapper;
using Microsoft.AspNetCore.Identity;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Auth;
using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Interfaces;

namespace RestaurantReservation.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository users, ITokenService tokenService, IPasswordHasher<User> passwordHasher)
    {
        _users = users;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _users.ExistsAsync(u => u.Email == email, ct))
            throw new BusinessRuleException("An account with this email already exists.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = UserRole.Customer // self-registration always creates a Customer
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);

        // Same generic error for "no user" and "wrong password" to avoid user enumeration.
        if (user is null)
            throw new BusinessRuleException("Invalid email or password.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new BusinessRuleException("Invalid email or password.");

        return BuildResponse(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var (token, expires) = _tokenService.CreateToken(user);
        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = token,
            ExpiresAtUtc = expires
        };
    }
}

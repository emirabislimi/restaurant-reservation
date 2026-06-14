using Microsoft.AspNetCore.Identity;
using NSubstitute;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Auth;
using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Implementations;
using RestaurantReservation.Services.Interfaces;
using Xunit;

namespace RestaurantReservation.Tests.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, _tokens, _hasher);
        _tokens.CreateToken(Arg.Any<User>()).Returns(("fake.jwt.token", DateTime.UtcNow.AddHours(2)));
    }

    [Fact]
    public async Task Register_Throws_WhenEmailAlreadyExists()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(true);

        var dto = new RegisterDto { FullName = "A", Email = "a@x.com", Password = "secret1" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task Register_HashesPassword_AndReturnsToken()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            Arg.Any<CancellationToken>()).Returns(false);

        var dto = new RegisterDto { FullName = "Jane", Email = "Jane@X.com", Password = "secret1" };

        var result = await _sut.RegisterAsync(dto);

        Assert.Equal("fake.jwt.token", result.Token);
        Assert.Equal("jane@x.com", result.Email); // normalised to lower-case
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash != "secret1" && !string.IsNullOrEmpty(u.PasswordHash)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_Throws_WhenUserNotFound()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var dto = new LoginDto { Email = "missing@x.com", Password = "whatever" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.LoginAsync(dto));
    }

    [Fact]
    public async Task Login_Succeeds_WithCorrectPassword()
    {
        var user = new User { FullName = "Jane", Email = "jane@x.com" };
        user.PasswordHash = _hasher.HashPassword(user, "secret1");
        _users.GetByEmailAsync("jane@x.com", Arg.Any<CancellationToken>()).Returns(user);

        var dto = new LoginDto { Email = "jane@x.com", Password = "secret1" };

        var result = await _sut.LoginAsync(dto);

        Assert.Equal("fake.jwt.token", result.Token);
    }

    [Fact]
    public async Task Login_Throws_WithWrongPassword()
    {
        var user = new User { FullName = "Jane", Email = "jane@x.com" };
        user.PasswordHash = _hasher.HashPassword(user, "secret1");
        _users.GetByEmailAsync("jane@x.com", Arg.Any<CancellationToken>()).Returns(user);

        var dto = new LoginDto { Email = "jane@x.com", Password = "WRONG" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.LoginAsync(dto));
    }
}

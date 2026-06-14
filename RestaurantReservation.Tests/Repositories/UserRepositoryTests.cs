using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Implementations;
using Xunit;

namespace RestaurantReservation.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetByEmail_ReturnsMatchingUser()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        ctx.Users.Add(new User { FullName = "Jane", Email = "jane@x.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var repo = new UserRepository(ctx);

        var user = await repo.GetByEmailAsync("jane@x.com");

        Assert.NotNull(user);
        Assert.Equal("Jane", user!.FullName);
    }

    [Fact]
    public async Task GetByEmail_ReturnsNull_WhenMissing()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        var repo = new UserRepository(ctx);

        var user = await repo.GetByEmailAsync("missing@x.com");

        Assert.Null(user);
    }
}

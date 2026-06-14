using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Implementations;
using Xunit;

namespace RestaurantReservation.Tests.Repositories;

public class TableRepositoryTests
{
    [Fact]
    public async Task GetCandidates_ReturnsOnlyActiveTablesThatFit_SmallestFirst()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        ctx.Tables.AddRange(
            new RestaurantTable { TableNumber = 1, Capacity = 2, Location = "Indoor", IsActive = true },
            new RestaurantTable { TableNumber = 2, Capacity = 6, Location = "Indoor", IsActive = true },
            new RestaurantTable { TableNumber = 3, Capacity = 4, Location = "Indoor", IsActive = true },
            new RestaurantTable { TableNumber = 4, Capacity = 8, Location = "Indoor", IsActive = false }
        );
        await ctx.SaveChangesAsync();
        var repo = new TableRepository(ctx);

        var candidates = await repo.GetCandidateTablesAsync(partySize: 4, location: null);

        // Capacity 2 too small, table 4 inactive -> only 4 and 6, ordered by capacity.
        Assert.Equal(2, candidates.Count);
        Assert.Equal(4, candidates[0].Capacity);
        Assert.Equal(6, candidates[1].Capacity);
    }

    [Fact]
    public async Task GetCandidates_FiltersByLocation()
    {
        await using var ctx = TestHelpers.NewInMemoryContext();
        ctx.Tables.AddRange(
            new RestaurantTable { TableNumber = 1, Capacity = 4, Location = "Indoor", IsActive = true },
            new RestaurantTable { TableNumber = 2, Capacity = 4, Location = "Patio", IsActive = true }
        );
        await ctx.SaveChangesAsync();
        var repo = new TableRepository(ctx);

        var candidates = await repo.GetCandidateTablesAsync(partySize: 2, location: "Patio");

        Assert.Single(candidates);
        Assert.Equal("Patio", candidates[0].Location);
    }
}
